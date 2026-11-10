using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using XinSoft.Models;

namespace XinSoft.Services;

/// <summary>
/// 站点配置持久化：sites.json 保存在程序目录下。
/// 使用 Windows DPAPI 加密整份配置（账号、密码等均不落明文）；
/// 兼容旧版明文 JSON，读取后下次保存自动转为密文。
/// </summary>
public static class SiteManager
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "sites.json");

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static List<SiteProfile> Load()
    {
        if (!File.Exists(FilePath)) return new List<SiteProfile>();
        try
        {
            var raw = File.ReadAllText(FilePath).Trim();
            if (raw.Length == 0) return new List<SiteProfile>();

            byte[] plainBytes;
            try
            {
                var blob = Convert.FromBase64String(raw);
                plainBytes = Dpapi.Unprotect(blob);
            }
            catch
            {
                // 旧版明文 JSON，直接反序列化；下次 Save 会写回密文。
                return JsonSerializer.Deserialize<List<SiteProfile>>(raw)
                       ?? new List<SiteProfile>();
            }

            var json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<List<SiteProfile>>(json)
                   ?? new List<SiteProfile>();
        }
        catch
        {
            return new List<SiteProfile>();
        }
    }

    public static void Save(List<SiteProfile> sites)
    {
        var json = JsonSerializer.Serialize(sites, Opts);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var blob = Dpapi.Protect(plainBytes);
        File.WriteAllText(FilePath, Convert.ToBase64String(blob));
    }

    /// <summary>Windows DPAPI（crypt32）加解密，绑定当前用户 + 本机，无需额外密钥。</summary>
    private static class Dpapi
    {
        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        [StructLayout(LayoutKind.Sequential)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptProtectData(
            ref DATA_BLOB pDataIn, string szDataDescr, IntPtr pOptionalEntropy,
            IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
            IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll")]
        private static extern void LocalFree(IntPtr hMem);

        public static byte[] Protect(byte[] plain)
        {
            var inBlob = ToBlob(plain);
            var outBlob = new DATA_BLOB();
            try
            {
                if (!CryptProtectData(ref inBlob, "ShellFtp sites", IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    throw new InvalidOperationException("DPAPI 加密失败。");
                return CopyFromBlob(outBlob);
            }
            finally
            {
                ReleaseBlob(inBlob);
                ReleaseBlob(outBlob);
            }
        }

        public static byte[] Unprotect(byte[] blob)
        {
            var inBlob = ToBlob(blob);
            var outBlob = new DATA_BLOB();
            try
            {
                if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    throw new InvalidOperationException("DPAPI 解密失败。");
                return CopyFromBlob(outBlob);
            }
            finally
            {
                ReleaseBlob(inBlob);
                ReleaseBlob(outBlob);
            }
        }

        private static DATA_BLOB ToBlob(byte[] data)
        {
            var ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);
            return new DATA_BLOB { cbData = data.Length, pbData = ptr };
        }

        private static byte[] CopyFromBlob(DATA_BLOB blob)
        {
            var result = new byte[blob.cbData];
            Marshal.Copy(blob.pbData, result, 0, blob.cbData);
            return result;
        }

        private static void ReleaseBlob(DATA_BLOB blob)
        {
            if (blob.pbData != IntPtr.Zero)
            {
                LocalFree(blob.pbData);
            }
        }
    }
}

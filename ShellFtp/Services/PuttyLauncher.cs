using System.Diagnostics;
using System.IO;
using System.Text;
using XinSoft.Models;

namespace XinSoft.Services;

/// <summary>
/// 调用外部 PuTTY 客户端打开独立 SSH 窗口。
/// 路径优先取程序同目录，其次 Downloads，最后在 PATH 中查找。
/// </summary>
public static class PuttyLauncher
{
    private const string ExeName = "putty.exe";

    /// <summary>解析 putty.exe 完整路径；找不到时返回 null。</summary>
    public static string? ResolvePuttyPath()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, ExeName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", ExeName)
        };

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                candidates.Add(Path.Combine(dir.Trim(), ExeName));
            }
        }

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>用站点配置启动外部 PuTTY 窗口；remotePath 非空时进入该远程目录。</summary>
    public static void Launch(SiteProfile profile, string? remotePath = null)
    {
        var exe = ResolvePuttyPath();
        if (exe == null)
            throw new FileNotFoundException("未找到 putty.exe：请把 PuTTY 放到程序同目录，或放入 Downloads / PATH。");

        string? scriptPath = null;
        var args = BuildArguments(profile, remotePath, ref scriptPath);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = string.Join(" ", args),
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory
        };

        var proc = Process.Start(psi);
        if (proc != null && scriptPath != null)
        {
            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) => CleanupScript(scriptPath);
        }
    }

    private static List<string> BuildArguments(SiteProfile profile, string? remotePath, ref string? scriptPath)
    {
        var args = new List<string>
        {
            "-ssh",
            "-t",
            "-P", profile.Port.ToString(),
            "-l", Quote(profile.Username)
        };

        if (!string.IsNullOrEmpty(profile.Password))
        {
            args.Add("-pw");
            args.Add(Quote(profile.Password));
        }

        if (!string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
        {
            args.Add("-i");
            args.Add(Quote(profile.PrivateKeyPath));
        }

        if (TryCreateStartupScript(remotePath, out scriptPath))
        {
            // 用 -m 脚本文件传远程命令，避免 &、引号被 Windows 命令行解析破坏。
            args.Add("-m");
            args.Add(Quote(scriptPath));
        }

        args.Add(Quote(profile.Host));
        return args;
    }

    private static bool TryCreateStartupScript(string? remotePath, out string? scriptPath)
    {
        scriptPath = null;
        if (string.IsNullOrWhiteSpace(remotePath) || remotePath == "/")
            return false;

        var path = remotePath.Trim();
        var script = "cd " + ShellSingleQuote(path) + "\nexec ${SHELL:-/bin/sh}\n";
        scriptPath = Path.Combine(Path.GetTempPath(), $"shellftp-putty-{Guid.NewGuid():N}.sh");
        File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
        return true;
    }

    private static void CleanupScript(string scriptPath)
    {
        try
        {
            if (File.Exists(scriptPath)) File.Delete(scriptPath);
        }
        catch
        {
            // 临时脚本清理失败不影响主流程，留待系统临时目录回收。
        }
    }

    private static string Quote(string? value) =>
        "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    private static string ShellSingleQuote(string value) =>
        "'" + (value ?? string.Empty).Replace("'", "'\\''") + "'";
}

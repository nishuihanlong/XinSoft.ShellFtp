using Renci.SshNet;
using XinSoft.Models;

namespace XinSoft.Services;

public class SftpService : IDisposable
{
    private SftpClient? _client;

    public bool IsConnected => _client?.IsConnected ?? false;
    public string CurrentPath { get; private set; } = "/";

    public void Connect(SiteProfile site)
    {
        _client = new SftpClient(SshAuth.CreateConnectionInfo(site));
        _client.Connect();
        CurrentPath = string.IsNullOrWhiteSpace(site.RemotePath) ? "/" : site.RemotePath;
    }

    public List<SftpItem> List(string path)
    {
        EnsureConnected();
        CurrentPath = path;

        var items = new List<SftpItem>();
        foreach (var f in _client!.ListDirectory(path))
        {
            if (f.Name is "." or "..") continue;
            items.Add(new SftpItem
            {
                Name = f.Name,
                IsDirectory = f.IsDirectory,
                Size = (ulong)f.Length,
                LastModified = f.LastWriteTime,
                FullPath = f.FullName
            });
        }

        return items
            .OrderBy(i => !i.IsDirectory)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void UploadFile(string localFile, string remotePath, Action<ulong, ulong>? progress)
    {
        EnsureConnected();
        using var fs = File.OpenRead(localFile);
        ulong total = (ulong)fs.Length;
        _client!.UploadFile(fs, remotePath, true, sent => progress?.Invoke(sent, total));
    }

    public void DownloadFile(string remotePath, string localFile, Action<ulong, ulong>? progress)
    {
        EnsureConnected();
        ulong total = 0;
        try { total = (ulong)_client!.GetAttributes(remotePath).Size; } catch { /* 忽略取大小失败 */ }

        using var fs = File.Create(localFile);
        _client!.DownloadFile(remotePath, fs, received => progress?.Invoke(received, total));
    }

    public void MakeDirectory(string path) => _client?.CreateDirectory(path);

    public void Delete(string path, bool isDirectory)
    {
        EnsureConnected();
        if (isDirectory) _client!.DeleteDirectory(path);
        else _client!.DeleteFile(path);
    }

    public void Rename(string oldPath, string newPath) => _client?.RenameFile(oldPath, newPath);

    public bool Exists(string path) => _client?.Exists(path) ?? false;

    public static string Combine(string parent, string name)
    {
        if (string.IsNullOrEmpty(parent) || parent == "/") return "/" + name;
        return parent.TrimEnd('/') + "/" + name;
    }

    private void EnsureConnected()
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("尚未连接到服务器");
    }

    public void Dispose()
    {
        if (_client?.IsConnected == true) _client.Disconnect();
        _client?.Dispose();
        _client = null;
    }
}

public class SftpItem
{
    public string Name { get; set; } = "";
    public bool IsDirectory { get; set; }
    public ulong Size { get; set; }
    public DateTime LastModified { get; set; }
    public string FullPath { get; set; } = "";
}

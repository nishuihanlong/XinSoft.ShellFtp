using System.Text;
using Renci.SshNet;
using XinSoft.Models;

namespace XinSoft.Services;

/// <summary>
/// SSH 交互式终端：基于 SshClient + ShellStream，支持写入命令、读取输出。
/// </summary>
public class ShellService : IDisposable
{
    private SshClient? _client;
    private ShellStream? _stream;
    private readonly byte[] _buffer = new byte[1024];

    public bool IsConnected => _client?.IsConnected ?? false;

    public void Connect(SiteProfile site, uint columns = 120, uint rows = 40)
    {
        _client = new SshClient(SshAuth.CreateConnectionInfo(site));
        _client.Connect();
        // terminalName=xterm，宽高字符数，像素宽高，缓冲大小
        _stream = _client.CreateShellStream("xterm", columns, rows, 800, 600, 1024);
    }

    /// <summary>读取当前可用的输出；无数据时返回空字符串。</summary>
    public string ReadAvailable()
    {
        if (_stream == null || !_stream.DataAvailable) return string.Empty;

        var sb = new StringBuilder();
        while (_stream.DataAvailable)
        {
            int read = _stream.Read(_buffer, 0, _buffer.Length);
            if (read <= 0) break;
            sb.Append(Encoding.UTF8.GetString(_buffer, 0, read));
        }
        return sb.ToString();
    }

    /// <summary>向终端写入命令或字符（调用方负责补换行）。</summary>
    public void Write(string text)
    {
        if (_stream == null) return;
        var bytes = Encoding.UTF8.GetBytes(text);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.Flush();
    }

    /// <summary>一次性执行单条命令并返回输出（非交互式）。</summary>
    public SshCommand RunCommand(string commandText)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("尚未连接到服务器");
        return _client.RunCommand(commandText);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        if (_client?.IsConnected == true) _client.Disconnect();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }
}

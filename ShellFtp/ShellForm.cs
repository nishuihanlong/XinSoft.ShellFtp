using System.Runtime.InteropServices;
using System.Text;
using XinSoft.Models;
using XinSoft.Services;

namespace XinSoft;

/// <summary>
/// SSH 交互式终端窗体。界面见 ShellForm.Designer.cs，
/// 本文件保留连接、直接键入转发、ANSI 渲染、批量刷新与复制粘贴逻辑。
/// </summary>
public partial class ShellForm : Form
{
    private readonly SiteProfile _profile;
    private ShellService? _shell;
    private TerminalEmulator? _emu;
    private CancellationTokenSource? _cts;
    private Task? _reader;

    private readonly StringBuilder _pending = new();
    private readonly object _pendingLock = new();
    private System.Windows.Forms.Timer? _flushTimer;

    private const int WM_SETREDRAW = 0x0B;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public ShellForm(SiteProfile profile)
    {
        _profile = profile;
        InitializeComponent();

        _emu = new TerminalEmulator(120, 40);

        _flushTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _flushTimer.Tick += OnFlush;
        _flushTimer.Start();

        Load += OnFormLoad;
        FormClosing += OnFormClosing;
    }

    private async void OnFormLoad(object? sender, EventArgs e) => await InitShellAsync();

    private async Task InitShellAsync()
    {
        try
        {
            _shell = new ShellService();
            await Task.Run(() => _shell.Connect(_profile));
            UpdateEmulatorSize();
            FeedText($"已连接到 {_profile.Host}:{_profile.Port}\r\n");

            _cts = new CancellationTokenSource();
            _reader = Task.Run(() => ReadLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            FeedText("连接失败：" + ex.Message + "\r\n");
        }
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _shell != null && _shell.IsConnected)
        {
            try
            {
                var output = _shell.ReadAvailable();
                if (!string.IsNullOrEmpty(output))
                {
                    lock (_pendingLock) _pending.Append(output);
                }
                else
                {
                    await Task.Delay(120, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(200, ct);
            }
        }
    }

    #region 渲染与批量刷新

    private void OnFlush(object? sender, EventArgs e)
    {
        string batch;
        lock (_pendingLock)
        {
            if (_pending.Length == 0) return;
            batch = _pending.ToString();
            _pending.Clear();
        }
        FeedText(batch);
    }

    private void FeedText(string text)
    {
        if (_emu == null) return;
        _emu.Feed(text);
        RenderTerminal();
    }

    private void RenderTerminal()
    {
        if (_emu == null || _txtTerminal.IsDisposed) return;

        var box = _txtTerminal;
        var baseFont = box.Font;
        var boldFont = new Font(baseFont, FontStyle.Bold);

        SendMessage(box.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
        try
        {
            box.Clear();
            var rows = _emu.Grid;

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                int c = 0;
                while (c < row.Count)
                {
                    var cell = row[c];
                    var sb = new StringBuilder();
                    while (c < row.Count && SameStyle(row[c], cell))
                    {
                        sb.Append(row[c].Char);
                        c++;
                    }

                    string text = sb.ToString();
                    int selStart = box.TextLength;
                    box.AppendText(text);
                    box.SelectionStart = selStart;
                    box.SelectionLength = text.Length;
                    box.SelectionColor = cell.Fore;
                    box.SelectionBackColor = cell.Back;
                    box.SelectionFont = cell.Bold ? boldFont : baseFont;
                }

                if (r < rows.Count - 1) box.AppendText("\r\n");
            }

            box.SelectionStart = box.TextLength;
        }
        finally
        {
            SendMessage(box.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            box.Invalidate();
            boldFont.Dispose();
        }

        box.ScrollToCaret();
    }

    private static bool SameStyle(TerminalCell a, TerminalCell b) =>
        a.Fore == b.Fore && a.Back == b.Back && a.Bold == b.Bold;

    private void UpdateEmulatorSize()
    {
        if (_emu == null || _txtTerminal.IsDisposed) return;

        SizeF sz = TextRenderer.MeasureText("M", _txtTerminal.Font);
        int cols = Math.Max(1, _txtTerminal.ClientSize.Width / Math.Max(1, (int)sz.Width));
        int rows = Math.Max(1, _txtTerminal.ClientSize.Height / Math.Max(1, (int)sz.Height));

        _emu.Resize(cols, rows);
        RenderTerminal();
    }

    private void OnFormResize(object? sender, EventArgs e) => UpdateEmulatorSize();

    #endregion

    #region 直接键入转发

    private void WriteRemote(string data)
    {
        if (_shell == null || !_shell.IsConnected) return;
        _shell.Write(data);
    }

    private void OnTerminalKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            if (!string.IsNullOrEmpty(_txtTerminal.SelectedText))
                OnCopy(sender, e);
            else
                WriteRemote("\u0003");
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.V)
        {
            OnPaste(sender, e);
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.A)
        {
            _txtTerminal.SelectAll();
            e.SuppressKeyPress = true;
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Enter: WriteRemote("\r"); break;
            case Keys.Back: WriteRemote("\b"); break;
            case Keys.Tab: WriteRemote("\t"); break;
            case Keys.Up: WriteRemote("\u001B[A"); break;
            case Keys.Down: WriteRemote("\u001B[B"); break;
            case Keys.Right: WriteRemote("\u001B[C"); break;
            case Keys.Left: WriteRemote("\u001B[D"); break;
            case Keys.Delete: WriteRemote("\u001B[3~"); break;
            case Keys.Home: WriteRemote("\u001B[H"); break;
            case Keys.End: WriteRemote("\u001B[F"); break;
            case Keys.Escape: WriteRemote("\u001B"); break;
            default: return;
        }
        e.SuppressKeyPress = true;
    }

    private void OnTerminalKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
        {
            e.Handled = true;
            return;
        }
        WriteRemote(e.KeyChar.ToString());
        e.Handled = true;
    }

    #endregion

    #region 复制 / 粘贴 / 全选 / 清空

    private void OnCopy(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_txtTerminal.SelectedText))
            Clipboard.SetText(_txtTerminal.SelectedText);
    }

    private void OnPaste(object? sender, EventArgs e)
    {
        var text = Clipboard.GetText();
        if (!string.IsNullOrEmpty(text)) WriteRemote(text);
    }

    private void OnSelectAll(object? sender, EventArgs e) => _txtTerminal.SelectAll();

    private void OnClear(object? sender, EventArgs e)
    {
        _emu?.Clear();
        RenderTerminal();
    }

    #endregion

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _flushTimer?.Stop();
        _cts?.Cancel();
        try { _reader?.Wait(500); } catch { /* 忽略等待超时 */ }
        _shell?.Dispose();
        _cts?.Dispose();
    }
}

using System.Runtime.InteropServices;
using XinSoft.Models;
using XinSoft.Services;

namespace XinSoft;

/// <summary>
/// 主窗体。界面控件、布局与事件挂接见 MainForm.Designer.cs，
/// 本文件只保留连接、导航、传输队列与业务操作逻辑。
/// </summary>
public partial class MainForm : Form
{
    private SftpService? _sftp;
    private SiteProfile? _currentProfile;
    private string _localDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private string _remoteDir = "/";

    private readonly Queue<TransferItem> _queue = new();
    private bool _processing;
    private bool _activeIsLocal = true;

    private const int IconFolder = 0;
    private const int IconFile = 1;

    public MainForm()
    {
        InitializeComponent();
        ConfigureToolbar();
        ConfigureImageList();
        Load += OnFormLoad;
        RefreshLocal();
        FormClosing += OnFormClosing;
    }

    private void OnFormLoad(object? sender, EventArgs e) =>
        _split.SplitterDistance = _split.Width / 2;

    private void OnFormClosing(object? sender, FormClosingEventArgs e) =>
        _sftp?.Dispose();

    /// <summary>
    /// 工具栏按钮的文字样式与间距配置（界面静态属性在 Designer，遍历配置在逻辑层）。
    /// </summary>
    private void ConfigureToolbar()
    {
        foreach (ToolStripItem item in _toolStrip.Items)
        {
            if (item is ToolStripButton btn)
            {
                btn.DisplayStyle = ToolStripItemDisplayStyle.Text;
                btn.Padding = new Padding(10, 4, 10, 4);
                btn.Margin = new Padding(2, 0, 2, 0);
                btn.ForeColor = SystemColors.ControlText;
            }
        }
    }

    /// <summary>从系统外壳提取文件夹 / 通用文件图标，加入 ImageList。</summary>
    private void ConfigureImageList()
    {
        _imageList.Images.Add(ExtractShellIcon(true));
        _imageList.Images.Add(ExtractShellIcon(false));
    }

    private Bitmap ExtractShellIcon(bool directory)
    {
        var info = new SHFILEINFO();
        string path;
        uint attr;
        uint flags;

        if (directory)
        {
            // 用真实存在的系统目录取标准黄色文件夹图标。
            path = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            attr = FILE_ATTRIBUTE_DIRECTORY;
            flags = SHGFI_ICON | SHGFI_SMALLICON;
        }
        else
        {
            // 不依赖真实文件，按文件属性取通用文档图标。
            path = "document.txt";
            attr = 0;
            flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
        }

        SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);

        if (info.hIcon != IntPtr.Zero)
        {
            try
            {
                using var icon = Icon.FromHandle(info.hIcon);
                return icon.ToBitmap();
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        }
        return new Bitmap(16, 16);
    }

    #region 列表刷新

    private void RefreshLocal()
    {
        _listLocal.Items.Clear();
        _txtLocalPath.Text = _localDir;

        var dir = new DirectoryInfo(_localDir);
        foreach (var d in dir.EnumerateDirectories())
            AddLocal(d.Name, true, d.FullName, 0, d.LastWriteTime);
        foreach (var f in dir.EnumerateFiles())
            AddLocal(f.Name, false, f.FullName, (ulong)f.Length, f.LastWriteTime);
    }

    private void RefreshRemote()
    {
        _listRemote.Items.Clear();
        _txtRemotePath.Text = _remoteDir;
        if (_sftp == null || !_sftp.IsConnected) return;

        foreach (var item in _sftp.List(_remoteDir))
        {
            var lvi = new ListViewItem(item.Name)
            {
                ImageIndex = item.IsDirectory ? IconFolder : IconFile
            };
            lvi.SubItems.Add(FormatSize(item.Size));
            lvi.SubItems.Add(item.LastModified.ToString("yyyy-MM-dd HH:mm"));
            lvi.SubItems.Add(item.IsDirectory ? "文件夹" : "文件");
            lvi.Tag = item;
            _listRemote.Items.Add(lvi);
        }
    }

    private void AddLocal(string name, bool isDir, string fullPath, ulong size, DateTime time)
    {
        var lvi = new ListViewItem(name)
        {
            ImageIndex = isDir ? IconFolder : IconFile
        };
        lvi.SubItems.Add(isDir ? "" : FormatSize(size));
        lvi.SubItems.Add(time.ToString("yyyy-MM-dd HH:mm"));
        lvi.SubItems.Add(isDir ? "文件夹" : "文件");
        lvi.Tag = new LocalNode { IsDirectory = isDir, Path = fullPath };
        _listLocal.Items.Add(lvi);
    }

    private static string FormatSize(ulong bytes)
    {
        if (bytes == 0) return "";
        string[] units = { "B", "KB", "MB", "GB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return v.ToString("0.##") + " " + units[i];
    }

    #endregion

    #region 连接 / 断开

    private void OnConnect(object? sender, EventArgs e)
    {
        using var dlg = new ConnectDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Profile == null) return;

        try
        {
            _sftp?.Dispose();
            _sftp = new SftpService();
            _sftp.Connect(dlg.Profile);
            _currentProfile = dlg.Profile;
            _remoteDir = string.IsNullOrWhiteSpace(dlg.Profile.RemotePath) ? "/" : dlg.Profile.RemotePath;
            RefreshRemote();
            SetStatus($"已连接：{dlg.Profile.Host}:{dlg.Profile.Port}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("连接失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("连接失败");
        }
    }

    private void OnDisconnect(object? sender, EventArgs e)
    {
        _sftp?.Dispose();
        _sftp = null;
        _currentProfile = null;
        _listRemote.Items.Clear();
        _txtRemotePath.Text = "";
        SetStatus("已断开");
    }

    #endregion

    #region 导航

    private void OnRefresh(object? sender, EventArgs e)
    {
        RefreshLocal();
        RefreshRemote();
    }

    private void OnLocalEnter(object? sender, EventArgs e) => _activeIsLocal = true;

    private void OnRemoteEnter(object? sender, EventArgs e) => _activeIsLocal = false;

    private void OnGoUp(object? sender, EventArgs e)
    {
        if (_activeIsLocal)
        {
            var parent = Directory.GetParent(_localDir);
            if (parent != null) _localDir = parent.FullName;
            RefreshLocal();
            return;
        }

        if (_sftp == null || !_sftp.IsConnected)
        {
            SetStatus("远程未连接");
            return;
        }
        _remoteDir = RemoteParent(_remoteDir);
        RefreshRemote();
    }

    private void EnterLocalItem(object? sender, EventArgs e)
    {
        if (_listLocal.SelectedItems.Count == 0) return;
        var node = (LocalNode)_listLocal.SelectedItems[0].Tag!;
        if (!node.IsDirectory) return;
        _activeIsLocal = true;
        _localDir = node.Path;
        RefreshLocal();
    }

    private void EnterRemoteItem(object? sender, EventArgs e)
    {
        if (_listRemote.SelectedItems.Count == 0) return;
        var item = (SftpItem)_listRemote.SelectedItems[0].Tag!;
        if (!item.IsDirectory) return;
        _activeIsLocal = false;
        _remoteDir = item.FullPath;
        RefreshRemote();
    }

    private static string RemoteParent(string path)
    {
        if (path == "/" || string.IsNullOrEmpty(path)) return "/";
        var trimmed = path.TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        return idx <= 0 ? "/" : trimmed[..idx];
    }

    #endregion

    #region 上传 / 下载（队列）

    private void OnUpload(object? sender, EventArgs e)
    {
        if (!EnsureRemoteConnected()) return;

        var items = new List<TransferItem>();
        foreach (ListViewItem sel in _listLocal.SelectedItems)
        {
            var node = (LocalNode)sel.Tag!;
            string remote = SftpService.Combine(_remoteDir, Path.GetFileName(node.Path));
            items.Add(new TransferItem
            {
                IsUpload = true,
                IsDirectory = node.IsDirectory,
                LocalPath = node.Path,
                RemotePath = remote
            });
        }
        Enqueue(items);
    }

    private void OnDownload(object? sender, EventArgs e)
    {
        if (!EnsureRemoteConnected()) return;

        var items = new List<TransferItem>();
        foreach (ListViewItem sel in _listRemote.SelectedItems)
        {
            var item = (SftpItem)sel.Tag!;
            string local = Path.Combine(_localDir, item.Name);
            items.Add(new TransferItem
            {
                IsUpload = false,
                IsDirectory = item.IsDirectory,
                LocalPath = local,
                RemotePath = item.FullPath
            });
        }
        Enqueue(items);
    }

    private void Enqueue(List<TransferItem> items)
    {
        foreach (var t in items) _queue.Enqueue(t);
        _ = ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (_processing) return;
        _processing = true;
        try
        {
            while (_queue.Count > 0)
            {
                var t = _queue.Dequeue();
                try
                {
                    await Task.Run(() => ExecuteTransfer(t));
                }
                catch (Exception ex)
                {
                    Invoke(() => MessageBox.Show("传输失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error));
                }
            }
            Invoke(() => { RefreshLocal(); RefreshRemote(); });
        }
        finally
        {
            _processing = false;
            Invoke(() => { _progress.Value = 0; SetStatus("传输完成"); });
        }
    }

    private void ExecuteTransfer(TransferItem t)
    {
        if (t.IsUpload)
        {
            if (t.IsDirectory) UploadDirectory(t.LocalPath, t.RemotePath);
            else _sftp!.UploadFile(t.LocalPath, t.RemotePath, ReportProgress);
        }
        else
        {
            if (t.IsDirectory) DownloadDirectory(t.RemotePath, t.LocalPath);
            else _sftp!.DownloadFile(t.RemotePath, t.LocalPath, ReportProgress);
        }
    }

    private void UploadDirectory(string localDir, string remoteDir)
    {
        if (!_sftp!.Exists(remoteDir)) _sftp.MakeDirectory(remoteDir);

        foreach (var f in Directory.EnumerateFiles(localDir))
        {
            var remote = SftpService.Combine(remoteDir, Path.GetFileName(f));
            _sftp.UploadFile(f, remote, ReportProgress);
        }
        foreach (var d in Directory.EnumerateDirectories(localDir))
        {
            UploadDirectory(d, SftpService.Combine(remoteDir, Path.GetFileName(d)));
        }
    }

    private void DownloadDirectory(string remoteDir, string localDir)
    {
        Directory.CreateDirectory(localDir);
        foreach (var item in _sftp!.List(remoteDir))
        {
            var local = Path.Combine(localDir, item.Name);
            if (item.IsDirectory) DownloadDirectory(item.FullPath, local);
            else _sftp.DownloadFile(item.FullPath, local, ReportProgress);
        }
    }

    private void ReportProgress(ulong sent, ulong total)
    {
        int percent = total > 0 ? (int)Math.Min(100, sent * 100 / total) : 0;
        if (IsDisposed) return;
        try
        {
            Invoke(() =>
            {
                _progress.Value = percent;
                SetStatus($"进度 {percent}%（{FormatSize(sent)} / {FormatSize(total)}）");
            });
        }
        catch { /* 窗体关闭时忽略 */ }
    }

    #endregion

    #region 拖拽

    private void OnLocalItemDrag(object? sender, ItemDragEventArgs e)
    {
        var paths = _listLocal.SelectedItems
            .Cast<ListViewItem>()
            .Select(i => ((LocalNode)i.Tag!).Path)
            .ToArray();
        _listLocal.DoDragDrop(paths, DragDropEffects.Copy);
    }

    private void OnRemoteItemDrag(object? sender, ItemDragEventArgs e)
    {
        var payload = _listRemote.SelectedItems
            .Cast<ListViewItem>()
            .Select(i =>
            {
                var item = (SftpItem)i.Tag!;
                return item.FullPath + "|" + item.IsDirectory;
            })
            .ToArray();
        _listRemote.DoDragDrop(payload, DragDropEffects.Copy);
    }

    private void OnLocalDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = DragDropEffects.Copy;

    private void OnRemoteDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = DragDropEffects.Copy;

    private void OnRemoteDragDrop(object? sender, DragEventArgs e)
    {
        if (!EnsureRemoteConnected()) return;

        if (e.Data?.GetData(DataFormats.FileDrop) is string[] localPaths)
        {
            var items = localPaths.Select(p => new TransferItem
            {
                IsUpload = true,
                IsDirectory = Directory.Exists(p),
                LocalPath = p,
                RemotePath = SftpService.Combine(_remoteDir, Path.GetFileName(p))
            }).ToList();
            Enqueue(items);
        }
    }

    private void OnLocalDragDrop(object? sender, DragEventArgs e)
    {
        if (!EnsureRemoteConnected()) return;

        if (e.Data?.GetData(typeof(string[])) is string[] payload)
        {
            var items = payload.Select(p =>
            {
                var parts = p.Split('|');
                var isDir = parts.Length > 1 && bool.Parse(parts[1]);
                return new TransferItem
                {
                    IsUpload = false,
                    IsDirectory = isDir,
                    RemotePath = parts[0],
                    LocalPath = Path.Combine(_localDir, Path.GetFileName(parts[0]))
                };
            }).ToList();
            Enqueue(items);
        }
    }

    #endregion

    #region 远程操作：新建目录 / 删除 / 重命名

    private void OnMkdir(object? sender, EventArgs e)
    {
        if (!EnsureRemoteConnected()) return;
        var name = Prompt(this, "新建远程目录", "目录名称：");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            _sftp!.MakeDirectory(SftpService.Combine(_remoteDir, name.Trim()));
            RefreshRemote();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_listRemote.SelectedItems.Count > 0)
        {
            if (!EnsureRemoteConnected()) return;
            foreach (ListViewItem sel in _listRemote.SelectedItems)
            {
                var item = (SftpItem)sel.Tag!;
                if (MessageBox.Show($"确认删除 {item.Name}？", "删除确认", MessageBoxButtons.YesNo) != DialogResult.Yes) continue;
                try { _sftp!.Delete(item.FullPath, item.IsDirectory); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            RefreshRemote();
        }
        else if (_listLocal.SelectedItems.Count > 0)
        {
            foreach (ListViewItem sel in _listLocal.SelectedItems)
            {
                var node = (LocalNode)sel.Tag!;
                if (MessageBox.Show($"确认删除 {Path.GetFileName(node.Path)}？", "删除确认", MessageBoxButtons.YesNo) != DialogResult.Yes) continue;
                try
                {
                    if (node.IsDirectory) Directory.Delete(node.Path, true);
                    else File.Delete(node.Path);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            RefreshLocal();
        }
    }

    private void OnRename(object? sender, EventArgs e)
    {
        if (_listRemote.SelectedItems.Count == 0 && _listLocal.SelectedItems.Count == 0) return;

        if (_listRemote.SelectedItems.Count > 0)
        {
            if (!EnsureRemoteConnected()) return;
            var item = (SftpItem)_listRemote.SelectedItems[0].Tag!;
            var newName = Prompt(this, "重命名", "新名称：", item.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;
            try
            {
                _sftp!.Rename(item.FullPath, SftpService.Combine(RemoteParent(item.FullPath), newName.Trim()));
                RefreshRemote();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        else
        {
            var node = (LocalNode)_listLocal.SelectedItems[0].Tag!;
            var newName = Prompt(this, "重命名", "新名称：", Path.GetFileName(node.Path));
            if (string.IsNullOrWhiteSpace(newName) || newName == Path.GetFileName(node.Path)) return;
            try
            {
                var newPath = Path.Combine(Path.GetDirectoryName(node.Path)!, newName.Trim());
                if (node.IsDirectory) Directory.Move(node.Path, newPath);
                else File.Move(node.Path, newPath);
                RefreshLocal();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    #endregion

    #region 终端

    private void OnOpenShell(object? sender, EventArgs e)
    {
        if (_currentProfile == null)
        {
            MessageBox.Show("请先连接服务器。");
            return;
        }

        try
        {
            PuttyLauncher.Launch(_currentProfile, _remoteDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show("启动 PuTTY 失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region 辅助

    private bool EnsureRemoteConnected()
    {
        if (_sftp == null || !_sftp.IsConnected)
        {
            MessageBox.Show("请先连接服务器。");
            return false;
        }
        return true;
    }

    private void SetStatus(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) Invoke(() => _statusLabel.Text = text);
        else _statusLabel.Text = text;
    }

    private static string? Prompt(Form owner, string title, string label, string defaultValue = "")
    {
        using var f = new Form
        {
            Text = title,
            Width = 320,
            Height = 150,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lbl = new Label { Text = label, Left = 12, Top = 12, Width = 280 };
        var txt = new TextBox { Left = 12, Top = 36, Width = 280, Text = defaultValue };
        var ok = new Button { Text = "确定", Left = 150, Top = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", Left = 230, Top = 75, DialogResult = DialogResult.Cancel };
        f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
    }

    #region 外壳图标

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    #endregion

    private class LocalNode
    {
        public bool IsDirectory { get; set; }
        public string Path { get; set; } = "";
    }

    private class TransferItem
    {
        public bool IsUpload { get; set; }
        public bool IsDirectory { get; set; }
        public string LocalPath { get; set; } = "";
        public string RemotePath { get; set; } = "";
    }

    #endregion
}

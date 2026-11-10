namespace XinSoft;

partial class MainForm
{
    private ToolStrip _toolStrip;
    private ToolStripButton _btnConnect;
    private ToolStripButton _btnDisconnect;
    private ToolStripButton _btnRefresh;
    private ToolStripButton _btnUp;
    private ToolStripButton _btnUpload;
    private ToolStripButton _btnDownload;
    private ToolStripButton _btnMkdir;
    private ToolStripButton _btnDelete;
    private ToolStripButton _btnRename;
    private ToolStripButton _btnShell;

    private SplitContainer _split;
    private Panel _leftPanel;
    private Panel _rightPanel;
    private ListView _listLocal;
    private ListView _listRemote;
    private ImageList _imageList;
    private TextBox _txtLocalPath;
    private TextBox _txtRemotePath;

    private StatusStrip _statusStrip;
    private ToolStripProgressBar _progress;
    private ToolStripStatusLabel _statusLabel;

    private void InitializeComponent()
    {
        _toolStrip = new ToolStrip();
        _btnConnect = new ToolStripButton();
        _btnDisconnect = new ToolStripButton();
        _btnRefresh = new ToolStripButton();
        _btnUp = new ToolStripButton();
        _btnUpload = new ToolStripButton();
        _btnDownload = new ToolStripButton();
        _btnMkdir = new ToolStripButton();
        _btnDelete = new ToolStripButton();
        _btnRename = new ToolStripButton();
        _btnShell = new ToolStripButton();

        _split = new SplitContainer();
        _leftPanel = new Panel();
        _rightPanel = new Panel();
        _listLocal = new ListView();
        _listRemote = new ListView();
        _imageList = new ImageList();
        _txtLocalPath = new TextBox();
        _txtRemotePath = new TextBox();

        _statusStrip = new StatusStrip();
        _progress = new ToolStripProgressBar();
        _statusLabel = new ToolStripStatusLabel();

        ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
        _split.SuspendLayout();
        _leftPanel.SuspendLayout();
        _rightPanel.SuspendLayout();
        SuspendLayout();

        // 工具栏
        _btnConnect.Text = "连接";
        _btnConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnConnect.Click += OnConnect;

        _btnDisconnect.Text = "断开";
        _btnDisconnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnDisconnect.Click += OnDisconnect;

        _btnRefresh.Text = "刷新";
        _btnRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnRefresh.Click += OnRefresh;

        _btnUp.Text = "上级目录";
        _btnUp.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnUp.Click += OnGoUp;

        _btnUpload.Text = "上传 →";
        _btnUpload.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnUpload.Click += OnUpload;

        _btnDownload.Text = "← 下载";
        _btnDownload.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnDownload.Click += OnDownload;

        _btnMkdir.Text = "新建远程目录";
        _btnMkdir.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnMkdir.Click += OnMkdir;

        _btnDelete.Text = "删除";
        _btnDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnDelete.Click += OnDelete;

        _btnRename.Text = "重命名";
        _btnRename.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnRename.Click += OnRename;

        _btnShell.Text = "终端";
        _btnShell.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnShell.Click += OnOpenShell;

        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _btnConnect, _btnDisconnect,
            new ToolStripSeparator(),
            _btnRefresh, _btnUp,
            new ToolStripSeparator(),
            _btnUpload, _btnDownload,
            new ToolStripSeparator(),
            _btnMkdir, _btnDelete, _btnRename,
            new ToolStripSeparator(),
            _btnShell
        });

        _toolStrip.Dock = DockStyle.Top;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.AutoSize = false;
        _toolStrip.Height = 32;
        _toolStrip.Font = new Font("Segoe UI", 9F);
        _toolStrip.Padding = new Padding(6, 4, 6, 4);
        _toolStrip.BackColor = SystemColors.Control;
        _toolStrip.ForeColor = SystemColors.ControlText;

        // 左栏：本地
        _txtLocalPath.Dock = DockStyle.Top;
        _txtLocalPath.ReadOnly = true;
        ConfigureListView(_listLocal);
        _listLocal.Dock = DockStyle.Fill;
        _listLocal.Enter += OnLocalEnter;
        _listLocal.DoubleClick += EnterLocalItem;
        _listLocal.ItemDrag += OnLocalItemDrag;
        _listLocal.DragEnter += OnLocalDragEnter;
        _listLocal.DragDrop += OnLocalDragDrop;
        _leftPanel.Dock = DockStyle.Fill;
        _leftPanel.Controls.Add(_listLocal);
        _leftPanel.Controls.Add(_txtLocalPath);

        // 右栏：远程
        _txtRemotePath.Dock = DockStyle.Top;
        _txtRemotePath.ReadOnly = true;
        ConfigureListView(_listRemote);
        _listRemote.Dock = DockStyle.Fill;
        _listRemote.Enter += OnRemoteEnter;
        _listRemote.DoubleClick += EnterRemoteItem;
        _listRemote.ItemDrag += OnRemoteItemDrag;
        _listRemote.DragEnter += OnRemoteDragEnter;
        _listRemote.DragDrop += OnRemoteDragDrop;
        _rightPanel.Dock = DockStyle.Fill;
        _rightPanel.Controls.Add(_listRemote);
        _rightPanel.Controls.Add(_txtRemotePath);

        _imageList.ImageSize = new Size(16, 16);
        _imageList.ColorDepth = ColorDepth.Depth32Bit;
        _listLocal.SmallImageList = _imageList;
        _listRemote.SmallImageList = _imageList;

        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Vertical;
        _split.Panel1MinSize = 120;
        _split.Panel2MinSize = 120;
        _split.SplitterDistance = _split.Width / 2;
        _split.Panel1.Controls.Add(_leftPanel);
        _split.Panel2.Controls.Add(_rightPanel);

        // 状态栏
        _progress.Width = 220;
        _statusLabel.Text = "就绪";
        _statusStrip.Items.Add(_progress);
        _statusStrip.Items.Add(_statusLabel);

        Text = "ShellFtp - SFTP 文件传输 / SSH 终端";
        Font = new Font("Segoe UI", 9F);
        Width = 1100;
        Height = 680;
        MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterScreen;

        // 停靠顺序：先 Fill 内容区，再 Bottom 状态栏，最后 Top 工具栏（z 序由 Add 顺序决定，勿再 BringToFront）
        Controls.Add(_split);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);

        ((System.ComponentModel.ISupportInitialize)_split).EndInit();
        _split.ResumeLayout(false);
        _leftPanel.ResumeLayout(false);
        _leftPanel.PerformLayout();
        _rightPanel.ResumeLayout(false);
        _rightPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private static void ConfigureListView(ListView lv)
    {
        lv.View = View.Details;
        lv.FullRowSelect = true;
        lv.AllowDrop = true;
        lv.MultiSelect = true;
        lv.Sorting = SortOrder.None;
        lv.Columns.Add("名称", 200);
        lv.Columns.Add("大小", 100);
        lv.Columns.Add("修改时间", 150);
        lv.Columns.Add("类型", 80);
    }
}

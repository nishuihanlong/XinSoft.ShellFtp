namespace XinSoft;

partial class ConnectDialog
{
    private TableLayoutPanel _table;
    private ComboBox _cboName;
    private TextBox _txtHost;
    private NumericUpDown _numPort;
    private TextBox _txtUser;
    private TextBox _txtPwd;
    private Panel _keyPanel;
    private TextBox _txtKey;
    private Button _btnBrowseKey;
    private TextBox _txtRemote;
    private CheckBox _chkSave;
    private Panel _buttonPanel;
    private Button _btnOk;
    private Button _btnCancel;
    private Button _btnDelete;

    private void InitializeComponent()
    {
        _table = new TableLayoutPanel();
        _cboName = new ComboBox();
        _txtHost = new TextBox();
        _numPort = new NumericUpDown();
        _txtUser = new TextBox();
        _txtPwd = new TextBox();
        _keyPanel = new Panel();
        _txtKey = new TextBox();
        _btnBrowseKey = new Button();
        _txtRemote = new TextBox();
        _chkSave = new CheckBox();
        _buttonPanel = new Panel();
        _btnOk = new Button();
        _btnCancel = new Button();
        _btnDelete = new Button();

        ((System.ComponentModel.ISupportInitialize)_numPort).BeginInit();
        SuspendLayout();

        // 输入表（顶部停靠）
        _table.Dock = DockStyle.Top;
        _table.Height = 224;
        _table.ColumnCount = 2;
        _table.RowCount = 7;
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _cboName.Dock = DockStyle.Fill;
        _cboName.DropDownStyle = ComboBoxStyle.DropDown;
        _txtHost.Dock = DockStyle.Fill;
        _numPort.Dock = DockStyle.Fill;
        _numPort.Minimum = 1;
        _numPort.Maximum = 65535;
        _numPort.Value = 22;
        _txtUser.Dock = DockStyle.Fill;
        _txtPwd.Dock = DockStyle.Fill;
        _txtPwd.PasswordChar = '*';
        _txtRemote.Dock = DockStyle.Fill;
        _txtRemote.Text = "/";

        AddRow(0, "站点名称", _cboName);
        AddRow(1, "主机", _txtHost);
        AddRow(2, "端口", _numPort);
        AddRow(3, "用户名", _txtUser);
        AddRow(4, "密码", _txtPwd);
        AddRow(6, "远程目录", _txtRemote);

        // 私钥行：文本框填充 + 浏览按钮靠右
        _txtKey.Dock = DockStyle.Fill;
        _btnBrowseKey.Text = "浏览…";
        _btnBrowseKey.Dock = DockStyle.Right;
        _btnBrowseKey.Width = 76;
        _btnBrowseKey.Click += BrowseKey;
        _keyPanel.Controls.Add(_txtKey);
        _keyPanel.Controls.Add(_btnBrowseKey);
        _table.Controls.Add(
            new Label { Text = "私钥路径", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0, 5);
        _table.Controls.Add(_keyPanel, 1, 5);

        // 保存复选框（顶部停靠，独立一行）
        _chkSave.Text = "保存为站点";
        _chkSave.Dock = DockStyle.Top;
        _chkSave.Height = 28;
        _chkSave.Checked = true;

        // 底部按钮面板（底部停靠）
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.Height = 44;
        _buttonPanel.Padding = new Padding(12, 8, 12, 8);

        _btnDelete.Text = "删除站点";
        _btnDelete.Dock = DockStyle.Left;
        _btnDelete.Width = 90;
        _btnDelete.Margin = new Padding(0, 0, 12, 0);
        _btnDelete.Click += DeleteSelected;

        _btnOk.Text = "连接";
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Dock = DockStyle.Right;
        _btnOk.Width = 80;
        _btnOk.Margin = new Padding(8, 0, 0, 0);
        _btnOk.Click += OnConfirm;

        _btnCancel.Text = "取消";
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Width = 80;

        _buttonPanel.Controls.Add(_btnDelete);
        _buttonPanel.Controls.Add(_btnOk);
        _buttonPanel.Controls.Add(_btnCancel);

        Text = "连接 / 站点管理";
        Font = new Font("Segoe UI", 9F);
        Width = 470;
        Height = 380;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.Add(_chkSave);
        Controls.Add(_table);
        Controls.Add(_buttonPanel);

        ((System.ComponentModel.ISupportInitialize)_numPort).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private void AddRow(int row, string label, Control control)
    {
        _table.Controls.Add(
            new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0, row);
        _table.Controls.Add(control, 1, row);
    }
}

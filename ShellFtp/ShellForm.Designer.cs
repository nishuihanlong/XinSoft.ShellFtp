namespace XinSoft;

partial class ShellForm
{
    private RichTextBox _txtTerminal;
    private ContextMenuStrip _terminalMenu;
    private ToolStripMenuItem _miCopy;
    private ToolStripMenuItem _miPaste;
    private ToolStripMenuItem _miSelectAll;
    private ToolStripMenuItem _miClear;

    private void InitializeComponent()
    {
        _txtTerminal = new RichTextBox();
        _terminalMenu = new ContextMenuStrip();
        _miCopy = new ToolStripMenuItem();
        _miPaste = new ToolStripMenuItem();
        _miSelectAll = new ToolStripMenuItem();
        _miClear = new ToolStripMenuItem();

        _terminalMenu.SuspendLayout();
        SuspendLayout();

        // 终端输出 / 输入区（直接在该区域敲键，按键由逻辑层转发到远端）
        _txtTerminal.Dock = DockStyle.Fill;
        _txtTerminal.ReadOnly = true;
        _txtTerminal.Multiline = true;
        _txtTerminal.ScrollBars = RichTextBoxScrollBars.Vertical;
        _txtTerminal.WordWrap = false;
        _txtTerminal.BackColor = Color.Black;
        _txtTerminal.ForeColor = Color.FromArgb(190, 230, 160);
        _txtTerminal.Font = new Font("Consolas", 10F);
        _txtTerminal.ContextMenuStrip = _terminalMenu;
        _txtTerminal.KeyDown += OnTerminalKeyDown;
        _txtTerminal.KeyPress += OnTerminalKeyPress;

        // 右键菜单
        _miCopy.Text = "复制";
        _miCopy.Click += OnCopy;
        _miPaste.Text = "粘贴";
        _miPaste.Click += OnPaste;
        _miSelectAll.Text = "全选";
        _miSelectAll.Click += OnSelectAll;
        _miClear.Text = "清空终端";
        _miClear.Click += OnClear;
        _terminalMenu.Items.AddRange(new ToolStripItem[]
        {
            _miCopy, _miPaste, _miSelectAll, new ToolStripSeparator(), _miClear
        });

        Text = "SSH 终端";
        Width = 800;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        Resize += OnFormResize;

        Controls.Add(_txtTerminal);

        _terminalMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}

using XinSoft.Models;
using XinSoft.Services;

namespace XinSoft;

/// <summary>
/// 连接 / 站点管理对话框。
/// 界面控件与布局见 ConnectDialog.Designer.cs，本文件只保留业务逻辑。
/// </summary>
public partial class ConnectDialog : Form
{
    private readonly List<SiteProfile> _sites;

    public SiteProfile? Profile { get; private set; }

    public ConnectDialog()
    {
        _sites = SiteManager.Load();
        InitializeComponent();

        foreach (var s in _sites) _cboName.Items.Add(s.Name);
        _cboName.SelectedIndexChanged += FillFromSelected;
    }

    private void FillFromSelected(object? sender, EventArgs e)
    {
        var site = _sites.FirstOrDefault(s => s.Name == _cboName.Text);
        if (site == null) return;

        _txtHost.Text = site.Host;
        _numPort.Value = site.Port;
        _txtUser.Text = site.Username;
        _txtPwd.Text = site.Password;
        _txtKey.Text = site.PrivateKeyPath ?? "";
        _txtRemote.Text = site.RemotePath;
    }

    private void DeleteSelected(object? sender, EventArgs e)
    {
        var site = _sites.FirstOrDefault(s => s.Name == _cboName.Text);
        if (site == null) return;

        _sites.Remove(site);
        SiteManager.Save(_sites);
        _cboName.Items.Remove(site.Name);
        MessageBox.Show("已删除站点：" + site.Name);
    }

    private void BrowseKey(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "密钥文件|*.ppk;*.pem;*.key|所有文件|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK) _txtKey.Text = dlg.FileName;
    }

    private void OnConfirm(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtHost.Text) || string.IsNullOrWhiteSpace(_txtUser.Text))
        {
            MessageBox.Show("主机和用户名不能为空。");
            DialogResult = DialogResult.None;
            return;
        }

        Profile = new SiteProfile
        {
            Name = string.IsNullOrWhiteSpace(_cboName.Text) ? _txtHost.Text : _cboName.Text,
            Host = _txtHost.Text.Trim(),
            Port = (int)_numPort.Value,
            Username = _txtUser.Text.Trim(),
            Password = _txtPwd.Text,
            PrivateKeyPath = string.IsNullOrWhiteSpace(_txtKey.Text) ? null : _txtKey.Text,
            RemotePath = string.IsNullOrWhiteSpace(_txtRemote.Text) ? "/" : _txtRemote.Text.Trim()
        };

        if (_chkSave.Checked)
        {
            var existing = _sites.FirstOrDefault(s => s.Name == Profile.Name);
            if (existing != null)
            {
                existing.Host = Profile.Host;
                existing.Port = Profile.Port;
                existing.Username = Profile.Username;
                existing.Password = Profile.Password;
                existing.PrivateKeyPath = Profile.PrivateKeyPath;
                existing.RemotePath = Profile.RemotePath;
            }
            else
            {
                _sites.Add(Profile);
            }
            SiteManager.Save(_sites);
        }
    }
}

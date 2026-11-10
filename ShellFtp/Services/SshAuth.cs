using Renci.SshNet;
using XinSoft.Models;

namespace XinSoft.Services;

/// <summary>
/// 统一构造 SSH 认证方式（密码 / 私钥），供 SFTP 与 Shell 两个客户端复用。
/// </summary>
internal static class SshAuth
{
    public static AuthenticationMethod GetAuth(SiteProfile site)
    {
        if (!string.IsNullOrWhiteSpace(site.PrivateKeyPath) && File.Exists(site.PrivateKeyPath))
        {
            var key = new PrivateKeyFile(site.PrivateKeyPath);
            return new PrivateKeyAuthenticationMethod(site.Username, key);
        }

        return new PasswordAuthenticationMethod(site.Username, site.Password ?? "");
    }

    public static ConnectionInfo CreateConnectionInfo(SiteProfile site)
    {
        return new ConnectionInfo(site.Host, site.Port, site.Username, GetAuth(site));
    }
}

namespace XinSoft.Models;

public class SiteProfile
{
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? PrivateKeyPath { get; set; }
    public string RemotePath { get; set; } = "/";
}

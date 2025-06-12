namespace SimpleAuthNet.Models.Config;

public class SsoProviderSettings
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? TenantId { get; set; }
    public string? RedirectUri { get; set; }
}
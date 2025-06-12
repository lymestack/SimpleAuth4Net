namespace SimpleAuthNet.Models.Config;

public class AppConfig
{
    public string Version { get; set; }

    public Environment Environment { get; set; }

    public Guid SessionId { get; set; }

    public SimpleAuthSettings SimpleAuth { get; set; }
}

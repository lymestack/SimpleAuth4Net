namespace SimpleAuthNet.Models.Config;

public class AppConfig
{
    public Environment Environment { get; set; }

    public Guid SessionId { get; set; }

    public SimpleAuthSettings SimpleAuth { get; set; }
}


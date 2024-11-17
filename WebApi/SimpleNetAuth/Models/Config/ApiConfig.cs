namespace SimpleNetAuth.Models.Config;

public class ApiConfig
{
    public string Environment { get; set; }

    public string TokenSecret { get; set; }

    public int RefreshTokenLengthBytes { get; set; }
}


public class AppConfig
{
    public Environment Environment { get; set; }

    public AuthSettings AuthSettings { get; set; }

    public CaptchaSettings CaptchaSettings { get; set; }

    public Guid SessionId { get; set; }
}

public class AuthSettings
{
    public string GoogleClientId { get; set; }

    public int AccessTokenExpirationMinutes { get; set; }

    public int RefreshTokenExpirationDays { get; set; }
}

public class CaptchaSettings
{
    public bool Enabled { get; set; }

    public string SecretKey { get; set; }
}

public class Environment
{
    public string Name { get; set; }

    public string Url { get; set; }

    public string Api { get; set; }

    public string Description { get; set; }

    public string BootstrapLabelCss { get; set; }
}
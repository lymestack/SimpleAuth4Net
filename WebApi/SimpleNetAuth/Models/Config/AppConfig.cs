namespace SimpleNetAuth.Models.Config;

public class ApiConfig
{
    public string Environment { get; set; }
    public string TokenSecret { get; set; }
    public int RefreshTokenLengthBytes { get; set; }
}

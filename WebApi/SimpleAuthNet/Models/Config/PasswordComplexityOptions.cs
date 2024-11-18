namespace SimpleAuthNet.Models.Config;

public class PasswordComplexityOptions
{
    public int RequiredLength { get; set; } = 8;
    public int RequiredUniqueChars { get; set; } = 4;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
}
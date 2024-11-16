using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SimpleNetAuth;

public interface IRecaptchaService
{
    Task<bool> ValidateCaptchaAsync(string token);
}

public class RecaptchaService(IConfiguration configuration, HttpClient httpClient) : IRecaptchaService
{
    private readonly string _secretKey = configuration.GetValue<string>("SimpleNetAuthSettings:Captcha:SecretKey");

    public async Task<bool> ValidateCaptchaAsync(string token)
    {
        var response = await httpClient.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
            null
        );

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RecaptchaResponse>(content);

        return result?.Success ?? false;
    }
}

public class RecaptchaResponse
{
    public bool Success { get; set; }
    public string[]? ErrorCodes { get; set; }
}
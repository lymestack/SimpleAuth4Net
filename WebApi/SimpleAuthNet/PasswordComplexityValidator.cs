using SimpleAuthNet.Models.Config;

namespace SimpleAuthNet;

public class PasswordComplexityValidator(PasswordComplexityOptions options)
{
    private readonly PasswordComplexityOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public PasswordValidationResult Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return PasswordValidationResult.Failure("Password cannot be null or whitespace.");
        }

        var errors = new List<string>();

        // Check length
        if (password.Length < _options.RequiredLength)
        {
            errors.Add($"Password must be at least {_options.RequiredLength} characters long.");
        }

        // Check unique characters
        if (password.Distinct().Count() < _options.RequiredUniqueChars)
        {
            errors.Add($"Password must contain at least {_options.RequiredUniqueChars} unique characters.");
        }

        // Check for digit
        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one numeric digit.");
        }

        // Check for lowercase
        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        // Check for uppercase
        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        // Check for non-alphanumeric characters
        if (_options.RequireNonAlphanumeric && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain at least one non-alphanumeric character.");
        }

        return errors.Any()
            ? PasswordValidationResult.Failure(errors)
            : PasswordValidationResult.Success();
    }
}

public class PasswordValidationResult
{
    public bool Succeeded { get; }
    public IEnumerable<string> Errors { get; }

    private PasswordValidationResult(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static PasswordValidationResult Success()
    {
        return new PasswordValidationResult(true, Enumerable.Empty<string>());
    }

    public static PasswordValidationResult Failure(IEnumerable<string> errors)
    {
        return new PasswordValidationResult(false, errors);
    }

    public static PasswordValidationResult Failure(string error)
    {
        return new PasswordValidationResult(false, new[] { error });
    }
}

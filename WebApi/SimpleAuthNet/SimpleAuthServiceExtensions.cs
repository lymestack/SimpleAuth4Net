using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthNet.Data;
using SimpleAuthNet.Logging;
using SimpleAuthNet.Models.Config;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace SimpleAuthNet;

public static class SimpleAuthServiceExtensions
{
    public static IServiceCollection AddSimpleAuthHttpClient(this IServiceCollection services)
    {
        // Registers HttpClient for dependency injection
        services.AddHttpClient();
        return services;
    }

    /// <summary>
    /// Registers the default <see cref="ISimpleAuthEmailSender"/> that reads the top-level
    /// EmailSettings config section and sends via SMTP or pickup directory.
    /// Uses TryAddScoped so downstream layers (e.g. LymeStackCore) can override.
    /// </summary>
    public static IServiceCollection AddSimpleAuthEmailSender(this IServiceCollection services)
    {
        services.TryAddScoped<ISimpleAuthEmailSender, DefaultSimpleAuthEmailSender>();
        return services;
    }

    public static IServiceCollection AddSimpleAuthDbContext(this IServiceCollection services)
    {
        services.AddDbContext<SimpleAuthContext>();
        return services;
    }

    public static IServiceCollection AddSimpleAuthControllers(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(x =>
            x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
        return services;
    }

    public static IServiceCollection AddSimpleAuthCors(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;
        services.AddCors(options =>
        {
            options.AddPolicy("default", builder =>
            {
                builder
                    .WithOrigins(settings.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Allow cookies w/ cross-origin
            });
        });

        return services;
    }

    public static IServiceCollection AddSimpleAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitOptions = configuration.GetSection("AuthSettings:RateLimit").Get<RateLimitOptions>()!;
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("fixed", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitOptions.QueueLimit
                    }));

            options.RejectionStatusCode = 429;

            if (rateLimitOptions.EnableRateLimitRejectionLogging)
            {
                options.OnRejected = async (context, token) =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<object>>();
                    logger.LogWarning("Rate limit exceeded: {IP} on {Path}",
                        context.HttpContext.Connection.RemoteIpAddress,
                        context.HttpContext.Request.Path);
                    await Task.CompletedTask;
                };
            }
        });

        return services;
    }

    public static IServiceCollection AddSimpleAuthLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var auditLogging = configuration.GetSection("AuthSettings:AuditLogging").Get<AuditLoggingOptions>();
        if (auditLogging?.Enabled == true)
        {
            if (!string.IsNullOrWhiteSpace(auditLogging.LogFolder) && Directory.Exists(auditLogging.LogFolder))
                services.AddScoped<IAuthLogger, FileAuthLogger>();
            else
                services.AddScoped<IAuthLogger, DefaultAuthLogger>();
        }
        return services;
    }

    public static IServiceCollection AddSimpleAuthJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["AuthSettings:TokenSecret"];
        Debug.Assert(secret != null, "AuthSettings:TokenSecret must be defined.");

        var authSettings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha512 }
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Resolve settings at request time (not registration time) so config overrides
                    // from WebApplicationFactory and environment variables are respected
                    var settings = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                        .GetSection("AuthSettings").Get<AuthSettings>()!;

                    if (settings.Mode != SimpleAuthMode.RelyingApp)
                        return Task.CompletedTask;

                    // Don't redirect API calls — they should still get 401
                    var acceptHeader = context.Request.Headers["Accept"].ToString();
                    if (acceptHeader.Contains("application/json") || context.Request.Path.StartsWithSegments("/api"))
                        return Task.CompletedTask;

                    // Build the redirect URL with return URL parameter
                    var returnUrl = Uri.EscapeDataString($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                    var redirectUrl = $"{settings.IdentityProviderUrl.TrimEnd('/')}/login?{settings.ReturnUrlParameter}={returnUrl}";

                    context.Response.Redirect(redirectUrl);
                    context.HandleResponse(); // Suppress the default 401 response

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddSimpleAuthDefaultAuthorization(this IServiceCollection services)
    {
        services.AddMvc(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        });

        return services;
    }

    public static IServiceCollection AddSimpleAuthStartupValidation(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("AuthSettings").Get<AuthSettings>()!;

        if (settings.Mode == SimpleAuthMode.IdentityProvider && string.IsNullOrEmpty(settings.CookieDomain))
        {
            var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("SimpleAuth");
            logger.LogWarning("SimpleAuth is running in IdentityProvider mode but CookieDomain is not configured. Cross-subdomain SSO will not work without a CookieDomain (e.g., '.lymestack.com').");
        }

        return services;
    }

    public static IServiceCollection AddSimpleAuthLocalRoles<TContext>(this IServiceCollection services)
        where TContext : DbContext, IRoleDbContext
    {
        services.AddScoped<IRoleDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IClaimsTransformation, LocalRoleClaimsTransformer>();
        return services;
    }
}

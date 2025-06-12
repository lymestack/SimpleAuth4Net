using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
}

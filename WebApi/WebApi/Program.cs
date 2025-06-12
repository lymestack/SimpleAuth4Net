using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthNet.Data;
using SimpleAuthNet.Logging;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI - Included in WebApi project by default. More info: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region SimpleAuth

// Database Context:
builder.Services.AddDbContext<SimpleAuthContext>();

// Enable Controllers:
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient();

var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>()!;

// Define CORS Policy:
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", builder =>
    {
        // FUTURE: Put the Origins in the appsettings.json file.
        builder
            .WithOrigins(authSettings.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required to allow cookies with cross-origin requests
    });
});

// Rate Limiting:
var rateLimitOptions = builder.Configuration.GetSection("AuthSettings:RateLimit").Get<RateLimitOptions>()!;
builder.Services.AddRateLimiter(options =>
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
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Rate limit hit for {IP} on {Path}",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Path);
            await Task.CompletedTask;
        };
    }
});

// Auth Audit Logging:
var auditLogging = authSettings.AuditLogging!;
if (auditLogging.Enabled == true)
{
    if (!string.IsNullOrEmpty(auditLogging.LogFolder) && Directory.Exists(auditLogging.LogFolder))
        builder.Services.AddScoped<IAuthLogger, FileAuthLogger>();
    else
        builder.Services.AddScoped<IAuthLogger, DefaultAuthLogger>();

}

var secret = builder.Configuration["AuthSettings:TokenSecret"];

// Define Authentication
builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;

            Debug.Assert(secret != null, nameof(secret) + " != null");
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true, // Ensure token hasn't expired
                ClockSkew = TimeSpan.Zero, // Optional: minimize time discrepancy
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha512 }
            };
            x.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                    return Task.CompletedTask;
                }
            };
        }
    );

// Enable [Authorize] attribute by default on all controllers:
builder.Services.AddMvc(o =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    o.Filters.Add(new AuthorizeFilter(policy));
});

#endregion

var app = builder.Build();

// If using Sqlite:
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<SqliteDbContext>();
//    DbInitializer.Initialize(context); // Seed the database
//}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
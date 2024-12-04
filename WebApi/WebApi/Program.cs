using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthNet.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Swagger/OpenAPI - Included in WebApi project by default. More info: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context:
builder.Services.AddDbContext<SimpleAuthNetDataContext>();
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();

// Enable Controllers:
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Define CORS Policy:
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", builder =>
    {
        builder
            .WithOrigins(
                "http://localhost:4200",  // Angular App
                "https://your-production-url.com",
                "http://localhost:8080",
                "http://localhost:3000") // React App
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required to allow cookies with cross-origin requests
    });
});

var secret = configuration["AuthSettings:TokenSecret"];

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
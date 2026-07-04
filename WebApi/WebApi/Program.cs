using SimpleAuthNet;

var builder = WebApplication.CreateBuilder(args);

// Local dev-only secrets (DB credentials, etc.). Gitignored — never committed.
// Loaded last so it overrides appsettings.json / appsettings.Development.json.
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddSimpleAuthDbContext()
    .AddSimpleAuthForwardedHeaders()
    .AddSimpleAuthControllers()
    .AddSimpleAuthCors(builder.Configuration)
    .AddSimpleAuthRateLimiting(builder.Configuration)
    .AddSimpleAuthLogging(builder.Configuration)
    .AddSimpleAuthJwt(builder.Configuration)
    .AddSimpleAuthDefaultAuthorization()
    .AddSimpleAuthHttpClient()
    .AddSimpleAuthEmailSender();

var app = builder.Build();

if (app.Environment.IsDevelopment() && bool.Parse(builder.Configuration["Swagger:Enabled"]!))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSimpleAuthForwardedHeaders();
app.UseCors("default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
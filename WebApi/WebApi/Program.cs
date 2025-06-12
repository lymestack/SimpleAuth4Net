using SimpleAuthNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddSimpleAuthDbContext()
    .AddSimpleAuthControllers()
    .AddSimpleAuthCors(builder.Configuration)
    .AddSimpleAuthRateLimiting(builder.Configuration)
    .AddSimpleAuthLogging(builder.Configuration)
    .AddSimpleAuthJwt(builder.Configuration)
    .AddSimpleAuthDefaultAuthorization()
    .AddSimpleAuthHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment() && bool.Parse(builder.Configuration["Swagger:Enabled"]!))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
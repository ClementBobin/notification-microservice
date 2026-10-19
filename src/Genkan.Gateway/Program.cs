using Genkan.Gateway;
using Genkan.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHttpClient("auth", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:AuthUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseCorrelationId();
app.UseMiddleware<TokenValidationMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" }));
app.MapReverseProxy();

app.Run();

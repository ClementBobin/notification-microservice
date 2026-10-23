using Genkan.Gateway;
using Genkan.Gateway.RateLimiting;
using Genkan.Gateway.Swagger;
using Genkan.Shared.Hosting;
using Genkan.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddGenkanLogging();

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHttpClient("auth", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:AuthUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewaySwagger(builder.Configuration);

var app = builder.Build();

app.UseCorrelationId();
app.UseGatewaySwagger();   // /swagger (combined UI of every service), only when Swagger:Enabled=true; answered before the token check
app.UseMiddleware<TokenValidationMiddleware>();
app.UseGatewayRateLimit("user");   // after the token check, so the bucket is the user's (X-User-Id)

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" }));
app.MapReverseProxy();

app.Run();

using Genkan.Auth.Api.Extensions;
using Genkan.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddGenkanLogging();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddGenkanApi();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseGenkanApi();

app.MapOpenApi();
app.MapControllers();
app.MapAuthHealth();

app.Run();

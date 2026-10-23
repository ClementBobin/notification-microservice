using Genkan.Lab.Api.Extensions;
using Genkan.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddGenkanLogging();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRabbitServices(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddGenkanApi();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseGenkanApi();

app.MapOpenApi();
app.MapControllers();
app.MapLabHealth();

app.Run();

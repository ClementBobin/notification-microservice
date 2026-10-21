using Genkan.Notification.Api.Extensions;
using Genkan.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddGenkanLogging();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddPushSender(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddRabbitServices(builder.Configuration);
builder.Services.AddGenkanApi();
builder.Services.AddOpenApi();

var app = builder.Build();

app.ValidatePushConfiguration();
await app.InitializeDatabaseAsync();

app.UseGenkanApi();

app.MapOpenApi();
app.MapControllers();
app.MapNotificationHealth();

app.Run();

using Genkan.Notification.Api.Extensions;
using Genkan.Notification.Application.Extensions;
using Genkan.Notification.Rabbit.Extensions;
using Genkan.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddGenkanLogging();

// Api and Rabbit both call Application; Application calls Infrastructure and Firebase.
builder.Services.AddNotificationApplication(builder.Configuration);
builder.Services.AddNotificationRabbit(builder.Configuration);
builder.Services.AddGenkanApi();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseGenkanApi();

app.MapOpenApi();
app.MapControllers();
app.MapNotificationHealth();

app.Run();

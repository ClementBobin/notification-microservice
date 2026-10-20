using FirebaseAdmin.Messaging;
using Genkan.Notification.Api.Extensions;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;
using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Messaging;
using Genkan.Notification.Infrastructure.Repositories;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddPushSender(builder.Configuration);
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
builder.Services.AddScoped<IEventHandler, NotificationProcessor>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<EventConsumer>();
builder.Services.AddGatewayHeaderAuth();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Fail fast when Firebase is enabled but the credentials are missing or invalid,
// instead of dead-lettering every RabbitMQ event later.
if (app.Configuration.GetValue<bool>("Firebase:Enabled"))
    app.Services.GetRequiredService<FirebaseMessaging>();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>()
        .EnsureSchemaAsync(NotificationDbContext.Schema, "notifications", app.Logger);

app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();

async Task<IResult> Health(NotificationDbContext db, RabbitMqConnection rabbit, CancellationToken ct)
{
    try
    {
        var dbOk = await db.Database.CanConnectAsync(ct);
        var mq = await rabbit.GetAsync(ct);
        return dbOk && mq.IsOpen
            ? Results.Ok(new { status = "ok", service = "notification" })
            : Results.Json(new { status = "degraded", db = dbOk, rabbitmq = mq.IsOpen }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", error = ex.Message }, statusCode: 503);
    }
}
app.MapGet("/health", Health);
app.MapGet("/notifications/health", Health);

app.Run();

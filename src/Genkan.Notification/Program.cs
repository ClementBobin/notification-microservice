using Genkan.Notification.Api;
using Genkan.Notification.Business;
using Genkan.Notification.Data;
using Genkan.Notification.Messaging;
using Genkan.Notification.Repository;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddSingleton<IPushSender, LoggingPushSender>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationProcessor>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<EventConsumer>();
builder.Services.AddGatewayHeaderAuth();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>()
        .EnsureSchemaAsync(NotificationDbContext.Schema, "notifications", app.Logger);

app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapNotificationEndpoints();

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

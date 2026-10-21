using Genkan.Migrator;
using Genkan.Shared;
using Genkan.Shared.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true));
var log = loggerFactory.CreateLogger("migrator");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    log.LogError("ConnectionStrings__Postgres is not set");
    return 1;
}

var options = new DbContextOptionsBuilder<GenkanDbContext>();
GenkanDbContext.Configure(options, connectionString);
await using var db = new GenkanDbContext(options.Options);

// No migration generated yet: create the tables straight from the model (development only).
if (!db.Database.GetMigrations().Any())
{
    log.LogWarning("No EF Core migration found: creating tables directly. Run 'dotnet ef migrations add InitialCreate -p src/Genkan.Migrator -o Migrations'.");
    await db.EnsureSchemaAsync(GenkanSchema.Name, "users", log);
    return 0;
}

for (var attempt = 1; ; attempt++)
{
    try
    {
        await db.Database.MigrateAsync();
        log.LogInformation("Schema {Schema} is up to date", GenkanSchema.Name);
        return 0;
    }
    catch (Exception ex) when (attempt < 30 && ex is not InvalidOperationException)
    {
        log.LogWarning("Database not ready ({Message}), retry {Attempt}/30", ex.Message, attempt);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Migration failed");
        return 1;
    }
}

using Genkan.Migrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true));
var log = loggerFactory.CreateLogger("migrator");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    log.LogError("ConnectionStrings__Postgres is not set");
    return 1;
}

// No schema name is imposed: tables go where the connection string points ("Search Path=app" -> schema "app", default "public").
// PostgreSQL does not create a schema by itself, so create the first schema of the Search Path when one is given.
var firstSchema = new NpgsqlConnectionStringBuilder(connectionString).SearchPath?.Split(',')[0].Trim().Trim('"');
if (string.IsNullOrEmpty(firstSchema) || firstSchema.StartsWith('$') || firstSchema.Equals("public", StringComparison.OrdinalIgnoreCase))
    firstSchema = null;

var options = new DbContextOptionsBuilder<GenkanDbContext>();
GenkanDbContext.Configure(options, connectionString);
await using var db = new GenkanDbContext(options.Options);

var hasMigrations = db.Database.GetMigrations().Any();
if (!hasMigrations)
    log.LogWarning("No EF Core migration found: creating tables directly (development only). Run 'dotnet ef migrations add InitialCreate -p src/Genkan.Migrator -o Migrations'.");

for (var attempt = 1; ; attempt++)
{
    try
    {
        if (firstSchema is not null)
            await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{firstSchema.Replace("\"", "\"\"")}\"");

        if (hasMigrations)
            await db.Database.MigrateAsync();
        else if (!await db.Database.SqlQueryRaw<bool>("SELECT to_regclass('users') IS NOT NULL AS \"Value\"").SingleAsync())
            await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

        log.LogInformation("Database is up to date");
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

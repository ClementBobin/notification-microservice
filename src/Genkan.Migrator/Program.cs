using Genkan.Migrator;
using Genkan.Migrator.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

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

var seed = new SeedOptions();
new ConfigurationBuilder().AddEnvironmentVariables().Build().GetSection("Seed").Bind(seed);

var hasMigrations = db.Database.GetMigrations().Any();
if (!hasMigrations)
    log.LogWarning("No EF Core migration found: creating tables directly (development only). Run 'dotnet ef migrations add InitialCreate -p src/Genkan.Migrator -o Migrations'.");

for (var attempt = 1; ; attempt++)
{
    try
    {
        if (hasMigrations)
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await new Seeder(db, Options.Create(seed), loggerFactory.CreateLogger<Seeder>()).SeedAsync();

        log.LogInformation("Database is up to date");
        return 0;
    }
    catch (Exception ex) when (attempt < 30 && IsTransient(ex))
    {
        log.LogWarning("Database not ready ({Message}), retry {Attempt}/30", ex.Message, attempt);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Migration failed");
        return 1;
    }

    static bool IsTransient(Exception? ex) =>
        ex is not null && (ex is NpgsqlException or TimeoutException || IsTransient(ex.InnerException));
}
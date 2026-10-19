using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Genkan.Shared;

public static class DbBootstrap
{
    /// <summary>
    /// One shared database, one schema per service. Creates the schema and this context's tables if the probe
    /// table is missing. Retries while PostgreSQL is starting. Replace by EF migrations when the model stabilises.
    /// </summary>
    public static async Task EnsureSchemaAsync(this DbContext db, string schema, string probeTable, ILogger log, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\"", ct);
                var exists = await db.Database
                    .SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = '{schema}' AND table_name = '{probeTable}'")
                    .SingleAsync(ct);
                if (exists == 0 && db.Database.GetService<IDatabaseCreator>() is IRelationalDatabaseCreator creator)
                {
                    await creator.CreateTablesAsync(ct);
                    log.LogInformation("Created tables in schema {Schema}", schema);
                }
                return;
            }
            catch (Exception ex) when (attempt < 15 && !ct.IsCancellationRequested)
            {
                log.LogWarning("Database not ready ({Message}), retry {Attempt}/15", ex.Message, attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}

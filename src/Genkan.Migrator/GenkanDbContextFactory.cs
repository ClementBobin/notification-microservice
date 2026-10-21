using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Genkan.Migrator;

/// <summary>Used by 'dotnet ef' (migrations add / script); the connection string comes from ConnectionStrings__Postgres.</summary>
public sealed class GenkanDbContextFactory : IDesignTimeDbContextFactory<GenkanDbContext>
{
    public GenkanDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                               ?? "Host=localhost;Database=genkan;Username=genkan;Password=genkan";
        var options = new DbContextOptionsBuilder<GenkanDbContext>();
        GenkanDbContext.Configure(options, connectionString);
        return new GenkanDbContext(options.Options);
    }
}

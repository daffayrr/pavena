using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Pavena.Infrastructure.Persistence;

public class PavenaDbContextFactory : IDesignTimeDbContextFactory<PavenaDbContext>
{
    public PavenaDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("DatabaseProvider") ?? "MySQL";
        var optionsBuilder = new DbContextOptionsBuilder<PavenaDbContext>();

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=pavena_db;Username=postgres;Password=postgres;";

            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PavenaDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });
        }
        else
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=localhost;Port=3306;Database=pavena_db;User=root;Password=abc_123;CharSet=utf8mb4;";

            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            optionsBuilder.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.MigrationsAssembly(typeof(PavenaDbContext).Assembly.FullName);
                mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });
        }

        optionsBuilder.ReplaceService<IMigrationsAssembly, ProviderAwareMigrationsAssembly>();

        return new PavenaDbContext(optionsBuilder.Options);
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Pavena.Infrastructure.Persistence;

#pragma warning disable EF1001 // Internal EF Core API usage
public class ProviderAwareMigrationsAssembly : MigrationsAssembly
{
    private readonly DbContext _context;

    public ProviderAwareMigrationsAssembly(
        ICurrentDbContext currentContext,
        IDbContextOptions options,
        IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
        : base(currentContext, options, idGenerator, logger)
    {
        _context = currentContext.Context;
    }

    public override IReadOnlyDictionary<string, TypeInfo> Migrations
    {
        get
        {
            var isPostgreSql = _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
            var targetNamespace = isPostgreSql ? ".PostgreSql" : ".MySql";

            var all = base.Migrations;
            return all
                .Where(m => m.Value.Namespace?.Contains(targetNamespace, StringComparison.OrdinalIgnoreCase) == true)
                .ToDictionary(m => m.Key, m => m.Value);
        }
    }

    public override ModelSnapshot? ModelSnapshot
    {
        get
        {
            var isPostgreSql = _context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
            var targetNamespace = isPostgreSql ? ".PostgreSql" : ".MySql";

            var snapshots = _context.GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(ModelSnapshot).IsAssignableFrom(t) && t.Namespace?.Contains(targetNamespace, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (snapshots.Count == 0) return base.ModelSnapshot;

            return (ModelSnapshot?)Activator.CreateInstance(snapshots[0]);
        }
    }
}
#pragma warning restore EF1001

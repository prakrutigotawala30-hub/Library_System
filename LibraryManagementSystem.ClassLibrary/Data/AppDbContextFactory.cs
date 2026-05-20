using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Design-time factory used by `dotnet ef migrations add …` and similar
    /// tooling when run directly against this class library. Without this,
    /// EF can't construct an AppDbContext outside an executable project that
    /// registers it in DI — which is what produced the "Unable to resolve
    /// service for type DbContextOptions" error.
    ///
    /// Migrations target SQL Server (see AppDbContextModelSnapshot — uses
    /// SqlServer identity columns). At runtime the apps detect the provider
    /// and use EnsureCreated() on Sqlite (Mac/Linux dev) or MigrateAsync()
    /// on SqlServer (Windows). So this factory should ALWAYS pick SqlServer
    /// for design-time work, regardless of OS — generate migrations on
    /// Windows, ship the files, every machine consumes them the same way.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            const string ConnectionString =
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=LibraryManagementDB;" +
                "Trusted_Connection=True;" +
                "MultipleActiveResultSets=true;" +
                "TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}

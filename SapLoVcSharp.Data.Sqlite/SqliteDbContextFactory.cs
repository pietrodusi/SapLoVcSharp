using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SapLoVcSharp.Data.Sqlite
{
    /// <summary>
    /// Design-time factory for creating DbContext during migrations.
    /// </summary>
    public class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();

            // Use a temporary database for design-time operations
            optionsBuilder.UseSqlite("Data Source=saplo-vc-design.db");

            return new SqliteDbContext(optionsBuilder.Options);
        }
    }
}
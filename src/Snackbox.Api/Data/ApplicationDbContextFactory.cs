using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Snackbox.Api.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// This is used by EF Core tools (migrations, etc.) to create the DbContext at design time.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a temporary connection string for design-time operations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=snackbox_designtime;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore.Design;

namespace SpecterOps.OktaHound.Database;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        string outputDirectory = Directory.GetCurrentDirectory();
        return new AppDbContext(outputDirectory);
    }
}

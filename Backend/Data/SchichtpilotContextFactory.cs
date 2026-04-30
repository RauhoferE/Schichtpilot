using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data;

/// <summary>
/// This calsss creates the db context.
/// This is only used by the CLI tools (dotnet ef) to scaffold migrations.
/// </summary>
public class SchichtpilotContextFactory : IDesignTimeDbContextFactory<SchichtpilotDbContext>
{
    public SchichtpilotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SchichtpilotDbContext>();

        // Use your Docker connection string here.
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=Schichtpilot.Test;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");

        return new SchichtpilotDbContext(optionsBuilder.Options);
    }
}
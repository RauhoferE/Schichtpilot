using Microsoft.EntityFrameworkCore;

namespace Data;

public class SchichtpilotDbContext : DbContext
{
    public SchichtpilotDbContext(DbContextOptions<SchichtpilotDbContext> options) : base(options)
    {
        
    }
    
    // Here should be the DBSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add data that should be in the database
        // Add attributes to model properties like pk, fk, etc..
        base.OnModelCreating(modelBuilder);
    }
}
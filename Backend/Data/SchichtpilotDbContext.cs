using Core;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class SchichtpilotDbContext : IdentityDbContext<
    User, 
    IdentityRole<long>,
long,
IdentityUserClaim<long>,
    IdentityUserRole<long>,
    IdentityUserLogin<long>,
    IdentityRoleClaim<long>,
    IdentityUserToken<long>
    >
{
    public SchichtpilotDbContext(DbContextOptions<SchichtpilotDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<User> Users { get; set; }
    
    public DbSet<UserJobRoles> UserJobRoles { get; set; }
    
    public DbSet<JobRole> JobRoles { get; set; }
    
    public DbSet<JobRoleDependency> JobRoleDependencies { get; set; }
    
    public DbSet<IdentityRole<long>> AccountRoles { get; set; }
    
    public DbSet<IdentityUserRole<long>> UserAccountRoles { get; set; }
    
    
    
    // Here should be the DBSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        // Add data that should be in the database
        // Add attributes to model properties like pk, fk, etc..
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole<long>>(entity =>
        {
            entity.HasData(
                new IdentityRole<long>()
                {
                    Id = 1,
                    Name = UserRolesClass.Admin,
                },
                new IdentityRole<long>()
                {
                    Id = 2,
                    Name = UserRolesClass.User,
                }
            );
        });

        modelBuilder.Entity<UserJobRoles>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.JobRoleId });
        });

        modelBuilder.Entity<JobRoleDependency>(entity =>
        {
            entity.HasKey(d => new { d.JobRoleId, d.DependencyJobRoleId });
            entity.HasOne(d => d.JobRole)
                .WithMany(x => x.Dependencies)
                .HasForeignKey(d => d.DependencyJobRoleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(d => d.Dependency)
                .WithMany(x => x.Prerequisites)
                .HasForeignKey(d => d.JobRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
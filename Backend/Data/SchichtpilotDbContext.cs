using Core;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data;

/// <summary>
/// Manages all tables and database entities.
/// </summary>
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

    public DbSet<ShiftRequirement> ShiftRequirements { get; set; }

    public DbSet<Shift> Shifts { get; set; }

    public DbSet<Timeslot> Timeslots { get; set; }

    public DbSet<Break> Breaks { get; set; }

    public DbSet<Absence> Absences { get; set; }

    public DbSet<WorkPolicy> WorkPolicies { get; set; }

    public DbSet<Holiday> Holidays { get; set; }

    public DbSet<WorkSchedule> WorkSchedules { get; set; }

    public DbSet<ShiftAssignment> ShiftAssignments { get; set; }

    public DbSet<WorkScheduleShifts> WorkScheduleShifts { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <summary>
    /// Configures the entites and tables.
    /// </summary>
    /// <param name="modelBuilder">Defines the shape of your entities, the relationships between them, and how they map to the database.</param>
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
                    NormalizedName = UserRolesClass.Admin.ToUpper()
                },
                new IdentityRole<long>()
                {
                    Id = 2,
                    Name = UserRolesClass.User,
                    NormalizedName = UserRolesClass.User.ToUpper()
                }
            );
        });

        modelBuilder.Entity<UserJobRoles>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.JobRoleId });
            entity.HasOne(x => x.User)
                .WithMany(x => x.JobRoles);
            entity.HasOne(x => x.JobRole)
                .WithMany(x => x.UsersWithRole);
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

        modelBuilder.Entity<JobRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasMany(x => x.UsersWithRole).WithOne(x => x.JobRole);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.HasMany(x => x.Timeslots)
                .WithOne(x => x.Shift)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.JobRequirements)
                .WithOne(x => x.Shift)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ShiftAssignments).WithOne(e => e.Shift);
        });

        modelBuilder.Entity<Break>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Timeslot).WithMany(t => t.Breaks).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(a => a.AbsenceType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Status).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<WorkPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasMany(e => e.Shifts).WithOne(e => e.WorkSchedule);
            entity.HasMany(e => e.ShiftAssignments).WithOne(e => e.WorkSchedule);
        });

        modelBuilder.Entity<WorkScheduleShifts>(entity =>
        {
            entity.HasKey(e => new { e.WorkScheduleId, e.ShiftId });
        });

        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(e => new { e.TimeslotId, e.WorkScheduleId, e.UserId, e.JobRoleId });
            entity.HasOne(e => e.WorkSchedule)
                .WithMany(s => s.ShiftAssignments)
                .HasForeignKey(e => e.WorkScheduleId);

            entity.HasOne(e => e.Timeslot)
                .WithMany(s => s.ShiftAssignments)
                .HasForeignKey(e => e.TimeslotId);

            entity.HasOne(e => e.UserJobRole)
                .WithMany(s => s.ShiftAssignments)
                .HasForeignKey(e => new { e.UserId, e.JobRoleId });

        });
    }
}
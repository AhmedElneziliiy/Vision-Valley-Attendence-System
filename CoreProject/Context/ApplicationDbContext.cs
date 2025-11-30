using CoreProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreProject.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        # region DbSets — All entities
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Timetable> Timetables => Set<Timetable>();
        public DbSet<Configuration> Configurations => Set<Configuration>();
        public DbSet<TimetableConfiguration> TimetableConfigurations => Set<TimetableConfiguration>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Lamp> Lamps => Set<Lamp>();
        public DbSet<AttendanceActionType> AttendanceActionTypes => Set<AttendanceActionType>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<UserImage> UserImages => Set<UserImage>();
        public DbSet<LampAccessRequest> LampAccessRequests => Set<LampAccessRequest>();
        #endregion
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region RenamingIdentityTables 
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users", schema: "security");
            });

            builder.Entity<IdentityRole<int>>(entity =>
            {
                entity.ToTable(name: "Roles", schema: "security");
            });

            builder.Entity<IdentityUserRole<int>>(entity =>
            {
                entity.ToTable("UserRoles", "security");
            });

            builder.Entity<IdentityUserClaim<int>>(entity =>
            {
                entity.ToTable("UserClaims", "security");
            });

            builder.Entity<IdentityUserLogin<int>>(entity =>
            {
                entity.ToTable("UserLogins", "security");
            });

            builder.Entity<IdentityRoleClaim<int>>(entity =>
            {
                entity.ToTable("RoleClaims", "security");
            });

            builder.Entity<IdentityUserToken<int>>(entity =>
            {
                entity.ToTable("UserTokens", "security");
            });
            #endregion

            // SOFT DELETE FILTERS - Only filter by IsActive, branch access control handled in application layer
            builder.Entity<ApplicationUser>().HasQueryFilter(u => u.IsActive);
            builder.Entity<Branch>().HasQueryFilter(b => b.IsActive);
            builder.Entity<Department>().HasQueryFilter(d => d.IsActive);
            builder.Entity<Timetable>().HasQueryFilter(t => t.IsActive);
            builder.Entity<Device>().HasQueryFilter(d => d.IsActive);
            builder.Entity<Lamp>().HasQueryFilter(l => l.IsActive);

            // CHILD ENTITY FILTERS - Filter based on parent entity's IsActive status
            builder.Entity<Attendance>().HasQueryFilter(a => a.User.IsActive);
            builder.Entity<AttendanceRecord>().HasQueryFilter(r => r.Attendance.User.IsActive);
            builder.Entity<UserImage>().HasQueryFilter(i => i.User.IsActive);
            builder.Entity<TimetableConfiguration>().HasQueryFilter(tc => tc.Timetable.IsActive);
            // ------------------------------------------------------------------

            // 3. RELATIONSHIPS — FIXED CASCADE PATHS
            builder.Entity<Branch>()
                .HasOne(b => b.Organization)
                .WithMany(o => o.Branches)
                .HasForeignKey(b => b.OrganizationID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(u => u.ManagerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Branch)
                .WithMany(b => b.Users)
                .HasForeignKey(u => u.BranchID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Timetable)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TimetableID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(r => r.Reason)
                .WithMany(a => a.Records)
                .HasForeignKey(r => r.ReasonID)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Attendance>()
                .HasOne(a => a.HRUser)
                .WithMany()
                .HasForeignKey(a => a.HRUserID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserImage>()
                .HasKey(i => i.UserID);

            builder.Entity<UserImage>()
                .HasOne(i => i.User)
                .WithOne(u => u.Image)
                .HasForeignKey<UserImage>(i => i.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Lamp>()
                .HasOne(l => l.Branch)
                .WithMany()
                .HasForeignKey(l => l.BranchID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Lamp>()
                .HasOne(l => l.Timetable)
                .WithMany()
                .HasForeignKey(l => l.TimetableID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<LampAccessRequest>()
                .HasOne(r => r.Lamp)
                .WithMany()
                .HasForeignKey(r => r.LampID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<LampAccessRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<LampAccessRequest>()
                .HasOne(r => r.RespondedByUser)
                .WithMany()
                .HasForeignKey(r => r.RespondedByUserID)
                .OnDelete(DeleteBehavior.NoAction);

            // ------------------------------------------------------------------
            // 5. Indexes
            // ------------------------------------------------------------------
            builder.Entity<Attendance>()
                .HasIndex(a => new { a.UserID, a.Date })
                .IsUnique();

            builder.Entity<AttendanceRecord>()
                .HasIndex(r => new { r.AttendanceID, r.Time });

            builder.Entity<Lamp>()
                .HasIndex(l => l.DeviceID)
                .IsUnique();

            builder.Entity<LampAccessRequest>()
                .HasIndex(r => r.Status);

            builder.Entity<LampAccessRequest>()
                .HasIndex(r => new { r.UserID, r.RequestedAt });

            builder.Entity<LampAccessRequest>()
                .HasIndex(r => new { r.LampID, r.RequestedAt });

            builder.Entity<LampAccessRequest>()
                .HasIndex(r => r.TimeoutAt)
                .HasFilter("[Status] = 'Pending'");

            builder.Entity<LampAccessRequest>()
                .HasIndex(r => r.ApprovedUntil)
                .HasFilter("[Status] = 'Approved' AND [IsAutoClosed] = 0");

            // ------------------------------------------------------------------
            // 6. Default Values
            // ------------------------------------------------------------------
            builder.Entity<Attendance>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }

}

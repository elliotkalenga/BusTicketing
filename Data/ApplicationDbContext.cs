using BusTicketing.Models;
using BusTicketing.Models.Auth;
using BusTicketing.Models.Company;
using BusTicketing.Models.Fleet;
using BusTicketing.Models.Location;
using BusTicketing.Models.Network;
using Microsoft.EntityFrameworkCore;

namespace BusTicketing.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================
        //       DB SETS
        // =====================

        // Company
        public DbSet<Company> Companies { get; set; }
        public DbSet<Agency> Agencies { get; set; }
        // Auth
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        // Fleet
        public DbSet<Bus> Buses { get; set; }
        public DbSet<SeatLayout> SeatLayouts { get; set; }
        public DbSet<SeatDefinition> SeatDefinitions { get; set; }
        public DbSet<BusAmenity> BusAmenities { get; set; }
        public DbSet<BusAmenityMap> BusAmenityMap { get; set; }

        // Network
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Town> Towns { get; set; }
        public DbSet<Area> Areas { get; set; }

        public DbSet<BusRoute> BusRoutes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripStopTime> TripStopTimes { get; set; }
        public DbSet<InventorySnapshot> InventorySnapshots { get; set; }
        public DbSet<FareRule> FareRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================
            //     COMPANY MODELS
            // =====================
            modelBuilder.Entity<Company>(b =>
            {
                b.ToTable("Companies");
                b.Property(x => x.RowVersion).IsRowVersion();

                b.HasMany(x => x.Agencies)
                 .WithOne(x => x.Company)
                 .HasForeignKey(x => x.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Agency>(b => b.ToTable("Agencies"));


            // =====================
            //        USERS
            // =====================
            modelBuilder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasIndex(x => x.Username).IsUnique();
            });

            modelBuilder.Entity<Role>(b =>
            {
                b.ToTable("Roles");
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<Permission>(b =>
            {
                b.ToTable("Permissions");
                b.HasIndex(x => x.Code).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(b =>
            {
                b.ToTable("RolePermissions");
                b.Property(x => x.RowVersion).IsRowVersion();

                b.HasOne(x => x.Role)
                 .WithMany(r => r.RolePermissions)
                 .HasForeignKey(x => x.RoleId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Permission)
                 .WithMany(p => p.RolePermissions)
                 .HasForeignKey(x => x.PermissionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================
            //        BUS MODEL
            // =====================
            modelBuilder.Entity<Bus>(b =>
            {
                b.ToTable("Buses");
                b.HasIndex(x => x.PlateNumber).IsUnique();
            });

            // =====================
            //        FLEET
            // =====================
            modelBuilder.Entity<SeatLayout>(b =>
            {
                b.ToTable("SeatLayouts");

                b.HasMany(x => x.Seats)
                 .WithOne(x => x.SeatLayout)
                 .HasForeignKey(x => x.SeatLayoutId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SeatDefinition>(b =>
            {
                b.ToTable("SeatDefinitions");
                b.HasIndex(x => new { x.SeatLayoutId, x.SeatNumber }).IsUnique();
            });

            modelBuilder.Entity<BusAmenity>(b =>
            {
                b.ToTable("BusAmenities");
                b.HasIndex(x => x.Name);
            });

            modelBuilder.Entity<BusAmenityMap>(b =>
            {
                b.HasKey(x => new { x.BusId, x.BusAmenityId });

                b.HasOne(x => x.Bus)
                 .WithMany(b => b.BusAmenities)
                 .HasForeignKey(x => x.BusId);

                b.HasOne(x => x.BusAmenity)
                 .WithMany(a => a.Buses)
                 .HasForeignKey(x => x.BusAmenityId);
            });

            // =====================
            //       NETWORK MODELS
            // =====================

            // Province → Town → Area
            modelBuilder.Entity<Town>()
                .HasOne(t => t.Province)
                .WithMany(p => p.Towns)
                .HasForeignKey(t => t.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Area>()
                .HasOne(a => a.Town)
                .WithMany(t => t.Areas)
                .HasForeignKey(a => a.TownId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Province>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Town>()
                .HasIndex(t => new { t.Name, t.ProvinceId })
                .IsUnique();

            modelBuilder.Entity<Area>()
                .HasIndex(a => new { a.Name, a.TownId })
                .IsUnique();

            // BusRoute → RouteStop
            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.BusRoute)
                .WithMany(r => r.Stops)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Area)
                .WithMany()
                .HasForeignKey(rs => rs.TerminalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip → BusRoute, Bus
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.BusRoute)
                .WithMany()
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany()
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // TripStopTime → Trip, RouteStop
            modelBuilder.Entity<TripStopTime>()
                .HasOne(ts => ts.Trip)
                .WithMany(t => t.StopTimes)
                .HasForeignKey(ts => ts.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TripStopTime>()
                .HasOne(ts => ts.RouteStop)
                .WithMany()
                .HasForeignKey(ts => ts.RouteStopId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventorySnapshot → Trip
            modelBuilder.Entity<InventorySnapshot>()
                .HasOne(i => i.Trip)
                .WithMany(t => t.InventorySnapshots)
                .HasForeignKey(i => i.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // FareRule → BusRoute
            modelBuilder.Entity<FareRule>()
                .HasOne(f => f.Route)
                .WithMany()
                .HasForeignKey(f => f.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Add indexes for FareRule
            modelBuilder.Entity<FareRule>()
                .HasIndex(f => new { f.RouteId, f.FromTerminalId, f.ToTerminalId, f.Class })
                .IsUnique();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Models;

namespace PredictiveMaintenance.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Threshold> Thresholds => Set<Threshold>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.UserId).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(a => new { a.UserId, a.Id });
            e.Property(a => a.Id).ValueGeneratedNever();

            e.HasOne(a => a.User)
                .WithMany(u => u.Assets)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sensor>(e =>
        {
            e.HasKey(s => new { s.UserId, s.Id });
            e.Property(s => s.Id).ValueGeneratedNever();

            e.HasOne(s => s.Asset)
                .WithMany(a => a.Sensors)
                .HasForeignKey(s => new { s.UserId, s.Id })
                .HasPrincipalKey(a => new { a.UserId, a.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Threshold>(e =>
        {
            e.HasKey(t => new { t.UserId, t.AssetId });

            e.HasOne(t => t.Asset)
                .WithOne(a => a.Threshold)
                .HasForeignKey<Threshold>(t => new { t.UserId, t.AssetId })
                .HasPrincipalKey<Asset>(a => new { a.UserId, a.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(t => t.Status).HasDefaultValue("Open");

            e.HasOne(t => t.Asset)
                .WithMany(a => a.Tickets)
                .HasForeignKey(t => new { t.UserId, t.AssetId })
                .HasPrincipalKey(a => new { a.UserId, a.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reading>(e =>
        {
            e.HasOne(r => r.Sensor)
                .WithMany(s => s.Readings)
                .HasForeignKey(r => new { r.UserId, r.SensorId })
                .HasPrincipalKey(s => new { s.UserId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

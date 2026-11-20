using Microsoft.EntityFrameworkCore;
using WorkOrderHub.Api.Domain;

namespace WorkOrderHub.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Technician> Technicians => Set<Technician>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.ToTable("work_orders");
            e.Property(w => w.Title).HasMaxLength(120).IsRequired();
            e.Property(w => w.AssetTag).HasMaxLength(40).IsRequired();
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.Priority).HasConversion<int>();
            e.Property(w => w.Status).HasConversion<int>();
            e.HasIndex(w => new { w.Status, w.Priority });
            e.HasIndex(w => w.DueAtUtc);
            e.HasOne(w => w.Technician)
                .WithMany(t => t.WorkOrders)
                .HasForeignKey(w => w.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Technician>(e =>
        {
            e.ToTable("technicians");
            e.Property(t => t.Name).HasMaxLength(80).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("ck_technicians_max_active", "MaxActiveOrders BETWEEN 1 AND 10"));
        });
    }
}

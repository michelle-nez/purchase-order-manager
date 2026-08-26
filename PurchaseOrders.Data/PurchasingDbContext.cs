using Microsoft.EntityFrameworkCore;
using PurchaseOrders.Data.Models;

namespace PurchaseOrders.Data;

public class PurchasingDbContext : DbContext
{
    public PurchasingDbContext(DbContextOptions<PurchasingDbContext> options)
        : base(options)
    {
    }

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Money needs an exact SQL type, or SQL Server picks a float-like default.
        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(l => l.UnitCost)
            .HasColumnType("decimal(18,2)");

        // The database refuses duplicate PO numbers.
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(p => p.PoNumber)
            .IsUnique();

        // Master-detail. Cascade is correct here and only here: a line has no
        // meaning without its purchase order, so deleting the parent takes the
        // children with it.
        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(l => l.PurchaseOrder)
            .WithMany(p => p.Lines)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // A supplier with orders against it cannot be deleted.
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Total, TotalOrdered, TotalReceived and Receipt are computed in C# from
        // the lines. Tell EF Core not to look for columns behind them.
        modelBuilder.Entity<PurchaseOrder>().Ignore(p => p.Total);
        modelBuilder.Entity<PurchaseOrder>().Ignore(p => p.TotalOrdered);
        modelBuilder.Entity<PurchaseOrder>().Ignore(p => p.TotalReceived);
        modelBuilder.Entity<PurchaseOrder>().Ignore(p => p.Receipt);

        modelBuilder.Entity<PurchaseOrderLine>().Ignore(l => l.LineTotal);
        modelBuilder.Entity<PurchaseOrderLine>().Ignore(l => l.Outstanding);
        modelBuilder.Entity<PurchaseOrderLine>().Ignore(l => l.IsFullyReceived);

        // Starter suppliers, so the dropdown is never empty on first run.
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { Id = 1, Name = "Ultra Spec Cables", Email = "sales@ultraspec.example" },
            new Supplier { Id = 2, Name = "FiberCables.com",   Email = "orders@fibercables.example" },
            new Supplier { Id = 3, Name = "Wallplate City",    Email = "supply@wallplatecity.example" },
            new Supplier { Id = 4, Name = "Nerdom Micro",      Email = "purchasing@nerdommicro.example" });

        base.OnModelCreating(modelBuilder);
    }
}

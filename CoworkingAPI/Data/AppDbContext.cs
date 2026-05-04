using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Building>  Buildings { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Restroom> Restrooms { get; set; }
    public DbSet<RestroomAsset> RestroomAssets { get; set; }
    public DbSet<IssueReport>  IssueReports { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Building>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Floor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Restroom>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RestroomAsset>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<IssueReport>().HasQueryFilter(e => !e.IsDeleted);
        
        modelBuilder.Entity<Restroom>()
            .Property(r => r.GridWidth).HasColumnType("smallint");
        modelBuilder.Entity<Restroom>()
            .Property(r => r.GridHeight).HasColumnType("smallint");
        modelBuilder.Entity<RestroomAsset>()
            .Property(a => a.PositionX).HasColumnType("smallint");
        modelBuilder.Entity<RestroomAsset>()
            .Property(a => a.PositionY).HasColumnType("smallint");
        
        modelBuilder.Entity<IssueReport>()
            .HasIndex(i => new { i.RestroomAssetId, i.Status })
            .HasFilter("[Status] < 2 AND [IsDeleted] = 0")
            .IsUnique();
        
        modelBuilder.Entity<RestroomAsset>()
            .Property(a => a.Type)
            .HasConversion<byte>();

        modelBuilder.Entity<IssueReport>()
            .Property(ir => ir.Status)
            .HasConversion<byte>();
        
        modelBuilder.Entity<Building>()
            .HasMany(b => b.Floors)
            .WithOne() // No navigation property defined inside Floor pointing back to Building
            .HasForeignKey(f => f.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Floor>()
            .HasMany(f => f.Restrooms)
            .WithOne()
            .HasForeignKey(r => r.FloorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Restroom>()
            .HasMany(r => r.Assets)
            .WithOne()
            .HasForeignKey(a => a.RestroomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RestroomAsset>()
            .HasMany<IssueReport>() 
            .WithOne()
            .HasForeignKey(i => i.RestroomAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RestroomAsset>()
            .HasDiscriminator(a => a.Type)
            .HasValue<Toilet>(AssetType.Toilet)
            .HasValue<Sink>(AssetType.Sink)
            .HasValue<HandDryer>(AssetType.HandDryer)
            .HasValue<PaperDispenser>(AssetType.PaperDispenser);
        
        modelBuilder.Entity<Building>().Property(b => b.Name).HasMaxLength(100);
        modelBuilder.Entity<Restroom>().Property(r => r.Identifier).HasMaxLength(100);
        modelBuilder.Entity<IssueReport>().Property(ir => ir.IssueDescription).HasMaxLength(500);
    
        modelBuilder.Entity<IssueReport>().Property(ir => ir.ReportingUserId).HasMaxLength(450); 
        modelBuilder.Entity<IssueReport>().Property(ir => ir.AssignedOperatorId).HasMaxLength(450);
    }
}
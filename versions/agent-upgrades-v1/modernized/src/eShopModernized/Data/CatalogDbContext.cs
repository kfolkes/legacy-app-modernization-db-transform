using eShopModernized.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShopModernized.Data;

/// <summary>
/// EF Core DbContext — replaces legacy CatalogDBContext (EF6).
/// 
/// MIGRATION CHANGES:
///   - DbModelBuilder            → ModelBuilder
///   - EntityTypeConfiguration   → IEntityTypeConfiguration (separate classes)
///   - HasRequired<T>()          → HasOne().WithMany().HasForeignKey()
///   - DatabaseGeneratedOption   → UseHiLo() built-in sequence support
///   - SqlQuery<T>()             → ELIMINATED (UseHiLo handles sequences)
///   - Ignore()                  → .Ignore() (same API, different namespace)
///   
/// SECURITY FIX:
///   - Connection string no longer hardcoded — injected via DI (DbContextOptions)
///   - No raw SQL execution — all queries through LINQ
/// </summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // ---- HiLo Sequences (replaces manual CatalogItemHiLoGenerator) ----
        // MIGRATION: The legacy app used a custom lock-based HiLo generator
        // with raw SQL: db.Database.SqlQuery<Int64>("SELECT NEXT VALUE FOR catalog_hilo;")
        // EF Core has built-in HiLo support — no raw SQL needed.
        builder.HasSequence<long>("catalog_hilo")
            .StartsAt(1)
            .IncrementsBy(10);

        builder.HasSequence<long>("catalog_brand_hilo")
            .StartsAt(1)
            .IncrementsBy(10);

        builder.HasSequence<long>("catalog_type_hilo")
            .StartsAt(1)
            .IncrementsBy(10);

        // Apply entity configurations
        builder.ApplyConfiguration(new CatalogItemConfiguration());
        builder.ApplyConfiguration(new CatalogBrandConfiguration());
        builder.ApplyConfiguration(new CatalogTypeConfiguration());

        // Seed data (replaces CatalogDBInitializer + PreconfiguredData)
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<CatalogBrand>().HasData(
            new CatalogBrand { Id = 1, Brand = "Azure" },
            new CatalogBrand { Id = 2, Brand = ".NET" },
            new CatalogBrand { Id = 3, Brand = "Visual Studio" },
            new CatalogBrand { Id = 4, Brand = "SQL Server" },
            new CatalogBrand { Id = 5, Brand = "Other" }
        );

        builder.Entity<CatalogType>().HasData(
            new CatalogType { Id = 1, Type = "Mug" },
            new CatalogType { Id = 2, Type = "T-Shirt" },
            new CatalogType { Id = 3, Type = "Sheet" },
            new CatalogType { Id = 4, Type = "USB Memory Stick" }
        );

        builder.Entity<CatalogItem>().HasData(
            new CatalogItem { Id = 1, Name = ".NET Bot Black Hoodie", Description = ".NET Bot Black Hoodie", Price = 19.5m, PictureFileName = "1.png", CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 100, RestockThreshold = 10, MaxStockThreshold = 200, OnReorder = false },
            new CatalogItem { Id = 2, Name = ".NET Black & White Mug", Description = ".NET Black & White Mug", Price = 8.50m, PictureFileName = "2.png", CatalogTypeId = 1, CatalogBrandId = 2, AvailableStock = 89, RestockThreshold = 10, MaxStockThreshold = 200, OnReorder = false },
            new CatalogItem { Id = 3, Name = "Prism White T-Shirt", Description = "Prism White T-Shirt", Price = 12.00m, PictureFileName = "3.png", CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 56, RestockThreshold = 15, MaxStockThreshold = 150, OnReorder = false },
            new CatalogItem { Id = 4, Name = ".NET Foundation T-Shirt", Description = ".NET Foundation T-Shirt", Price = 12.00m, PictureFileName = "4.png", CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 120, RestockThreshold = 25, MaxStockThreshold = 300, OnReorder = false },
            new CatalogItem { Id = 5, Name = "Roslyn Red Sheet", Description = "Roslyn Red Sheet", Price = 8.50m, PictureFileName = "5.png", CatalogTypeId = 3, CatalogBrandId = 5, AvailableStock = 8, RestockThreshold = 10, MaxStockThreshold = 100, OnReorder = true },
            new CatalogItem { Id = 6, Name = ".NET Blue Hoodie", Description = ".NET Blue Hoodie", Price = 12.00m, PictureFileName = "6.png", CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 17, RestockThreshold = 20, MaxStockThreshold = 200, OnReorder = true },
            new CatalogItem { Id = 7, Name = "Roslyn Red T-Shirt", Description = "Roslyn Red T-Shirt", Price = 12.00m, PictureFileName = "7.png", CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 5, RestockThreshold = 10, MaxStockThreshold = 100, OnReorder = true },
            new CatalogItem { Id = 8, Name = "Kudu Purple Hoodie", Description = "Kudu Purple Hoodie", Price = 8.50m, PictureFileName = "8.png", CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 34, RestockThreshold = 10, MaxStockThreshold = 150, OnReorder = false },
            new CatalogItem { Id = 9, Name = "Cup<T> White Mug", Description = "Cup<T> White Mug", Price = 12.00m, PictureFileName = "9.png", CatalogTypeId = 1, CatalogBrandId = 5, AvailableStock = 76, RestockThreshold = 10, MaxStockThreshold = 200, OnReorder = false },
            new CatalogItem { Id = 10, Name = ".NET Foundation Sheet", Description = ".NET Foundation Sheet", Price = 12.00m, PictureFileName = "10.png", CatalogTypeId = 3, CatalogBrandId = 2, AvailableStock = 11, RestockThreshold = 15, MaxStockThreshold = 100, OnReorder = true },
            new CatalogItem { Id = 11, Name = "Cup<T> Pin", Description = "Cup<T> Pin", Price = 8.50m, PictureFileName = "11.png", CatalogTypeId = 4, CatalogBrandId = 5, AvailableStock = 0, RestockThreshold = 5, MaxStockThreshold = 50, OnReorder = true },
            new CatalogItem { Id = 12, Name = "Prism White USB Stick", Description = "Prism White USB Stick", Price = 12.00m, PictureFileName = "12.png", CatalogTypeId = 4, CatalogBrandId = 5, AvailableStock = 49, RestockThreshold = 10, MaxStockThreshold = 100, OnReorder = false }
        );
    }
}

// ---- Entity Configurations (replaces inline Fluent API in OnModelCreating) ----

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");
        builder.HasKey(ci => ci.Id);

        // HiLo sequence for ID generation (replaces CatalogItemHiLoGenerator)
        builder.Property(ci => ci.Id)
            .UseHiLo("catalog_hilo");

        builder.Property(ci => ci.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(ci => ci.PictureFileName)
            .IsRequired();

        // Navigation properties (replaces HasRequired<T>().WithMany().HasForeignKey())
        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogBrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ci => ci.CatalogType)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CatalogBrandConfiguration : IEntityTypeConfiguration<CatalogBrand>
{
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        builder.ToTable("CatalogBrand");
        builder.HasKey(cb => cb.Id);

        builder.Property(cb => cb.Id)
            .UseHiLo("catalog_brand_hilo");

        builder.Property(cb => cb.Brand)
            .IsRequired()
            .HasMaxLength(100);
    }
}

public class CatalogTypeConfiguration : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        builder.ToTable("CatalogType");
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Id)
            .UseHiLo("catalog_type_hilo");

        builder.Property(ct => ct.Type)
            .IsRequired()
            .HasMaxLength(100);
    }
}

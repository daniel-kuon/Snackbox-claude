using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Barcode> Barcodes => Set<Barcode>();
    public DbSet<BarcodeScan> BarcodeScans => Set<BarcodeScan>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<ShelvingAction> ShelvingActions => Set<ShelvingAction>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<CashRegister> CashRegister => Set<CashRegister>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply snake_case naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }
        }

        // Only configure what differs from convention
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired(false);
        });

        modelBuilder.Entity<Barcode>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<BarcodeScan>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ProductBarcode>(entity =>
        {
            entity.HasIndex(e => e.Barcode).IsUnique();
            entity.Property(e => e.Barcode).HasMaxLength(50);
            entity.HasOne(pb => pb.Product)
                .WithMany(p => p.Barcodes)
                .HasForeignKey(pb => pb.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.HasOne(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.Property(e => e.ManualAmount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Withdrawal>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Supplier).HasMaxLength(200);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.AdditionalCosts).HasPrecision(10, 2);
            entity.Property(e => e.PriceReduction).HasPrecision(10, 2);
            
            // Map CreatedByUserId to created_by_id column (which has the FK constraint)
            entity.Property(e => e.CreatedByUserId)
                .HasColumnName("created_by_id");
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PaidBy)
                .WithMany()
                .HasForeignKey(e => e.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Payment)
                .WithOne(p => p.Invoice)
                .HasForeignKey<Invoice>(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
            entity.HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShelvingAction>(entity =>
        {
            entity.HasOne(sa => sa.InvoiceItem)
                .WithMany(ii => ii.ShelvingActions)
                .HasForeignKey(sa => sa.InvoiceItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Deposit>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.HasOne(e => e.LinkedPayment)
                .WithOne(p => p.LinkedDeposit)
                .HasForeignKey<Payment>(p => p.LinkedDepositId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashRegister>(entity =>
        {
            entity.Property(e => e.CurrentBalance).HasPrecision(10, 2);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed users with properly hashed passwords
        // Password for admin and john.doe is "password123"
        // These are pre-generated BCrypt hashes to avoid dynamic values in HasData

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@snackbox.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminPassword", "$2a$11$7EW8wLqhqKQZH8J6rX5kQ."),                IsAdmin = true,
                CreatedAt = seedDate
            },
            new User
            {
                Id = 2,
                Username = "john.doe",
                Email = "john.doe@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("johnPassword", "$2a$11$7EW8wLqhqKQZH8J6rX5kQ.VzB4L5rZ5lYJ3VN2vY8K8eH5F0oJ8.G"),                IsAdmin = false,
                CreatedAt = seedDate
            },
            new User
            {
                Id = 3,
                Username = "jane.smith",
                Email = "jane.smith@company.com",
                PasswordHash = null, // Barcode-only login - no password set
                IsAdmin = false,
                CreatedAt = seedDate
            }
        );

        // Seed products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Chips - Salt",
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 2,
                Name = "Chocolate Bar",
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 3,
                Name = "Energy Drink",
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 4,
                Name = "Cookies",
                CreatedAt = seedDate
            }
        );

        // Seed product barcodes
        modelBuilder.Entity<ProductBarcode>().HasData(
            new ProductBarcode
            {
                Id = 1,
                ProductId = 1,
                Barcode = "1234567890123",
                Quantity = 1,
                CreatedAt = seedDate
            },
            new ProductBarcode
            {
                Id = 2,
                ProductId = 1,
                Barcode = "1234567890123-BOX",
                Quantity = 12,
                CreatedAt = seedDate
            },
            new ProductBarcode
            {
                Id = 3,
                ProductId = 2,
                Barcode = "1234567890124",
                Quantity = 1,
                CreatedAt = seedDate
            },
            new ProductBarcode
            {
                Id = 4,
                ProductId = 2,
                Barcode = "1234567890124-PACK",
                Quantity = 5,
                CreatedAt = seedDate
            },
            new ProductBarcode
            {
                Id = 5,
                ProductId = 3,
                Barcode = "1234567890125",
                Quantity = 1,
                CreatedAt = seedDate
            },
            new ProductBarcode
            {
                Id = 6,
                ProductId = 4,
                Barcode = "1234567890126",
                Quantity = 1,
                CreatedAt = seedDate
            }
        );

        // Seed product batches
        modelBuilder.Entity<ProductBatch>().HasData(
            new ProductBatch
            {
                Id = 1,
                ProductId = 1,
                BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = seedDate
            },
            new ProductBatch
            {
                Id = 2,
                ProductId = 2,
                BestBeforeDate = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = seedDate
            },
            new ProductBatch
            {
                Id = 3,
                ProductId = 3,
                BestBeforeDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = seedDate
            },
            new ProductBatch
            {
                Id = 4,
                ProductId = 4,
                BestBeforeDate = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = seedDate
            }
        );

        // Seed shelving actions
        modelBuilder.Entity<ShelvingAction>().HasData(
            new ShelvingAction { Id = 1, ProductBatchId = 1, Quantity = 50, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 2, ProductBatchId = 1, Quantity = 20, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) },
            new ShelvingAction { Id = 3, ProductBatchId = 2, Quantity = 30, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 4, ProductBatchId = 2, Quantity = 15, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) },
            new ShelvingAction { Id = 5, ProductBatchId = 3, Quantity = 40, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 6, ProductBatchId = 3, Quantity = 25, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(2) },
            new ShelvingAction { Id = 7, ProductBatchId = 4, Quantity = 35, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 8, ProductBatchId = 4, Quantity = 18, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) }
        );

        // Seed barcodes for users
        modelBuilder.Entity<Barcode>().HasData(
            // Keep existing IDs to avoid foreign key conflicts with existing data
            new Barcode
            {
                Id = 1,
                UserId = 2,
                Code = "4061461764012",
                Amount = 5.00m,
                IsActive = true,
                IsLoginOnly = false,
                CreatedAt = seedDate
            },
            new Barcode
            {
                Id = 2,
                UserId = 2,
                Code = "USER2-10EUR",
                Amount = 10.00m,
                IsActive = true,
                IsLoginOnly = false,
                CreatedAt = seedDate
            },
            new Barcode
            {
                Id = 3,
                UserId = 3,
                Code = "USER3-5EUR",
                Amount = 5.00m,
                IsActive = true,
                IsLoginOnly = false,
                CreatedAt = seedDate
            },
            new Barcode
            {
                Id = 4,
                UserId = 3,
                Code = "USER3-10EUR",
                Amount = 10.00m,
                IsActive = true,
                IsLoginOnly = false,
                CreatedAt = seedDate
            },
            // New login-only barcodes
            new Barcode
            {
                Id = 5,
                UserId = 1,
                Code = "4260473313809",
                Amount = 0m,
                IsActive = true,
                IsLoginOnly = true,
                CreatedAt = seedDate
            },
            new Barcode
            {
                Id = 6,
                UserId = 2,
                Code = "USER2-LOGIN",
                Amount = 0m,
                IsActive = true,
                IsLoginOnly = true,
                CreatedAt = seedDate
            },
            new Barcode
            {
                Id = 7,
                UserId = 3,
                Code = "USER3-LOGIN",
                Amount = 0m,
                IsActive = true,
                IsLoginOnly = true,
                CreatedAt = seedDate
            }
        );

        // Seed some purchases (grouping of barcode scans)
        modelBuilder.Entity<Purchase>().HasData(
            new Purchase
            {
                Id = 1,
                UserId = 2,
                CreatedAt = seedDate.AddDays(5),
                CompletedAt = seedDate.AddDays(5).AddMinutes(5)
            },
            new Purchase
            {
                Id = 2,
                UserId = 3,
                CreatedAt = seedDate.AddDays(10),
                CompletedAt = seedDate.AddDays(10).AddMinutes(3)
            }
        );

        // Seed barcode scans
        modelBuilder.Entity<BarcodeScan>().HasData(
            // Purchase 1 - john.doe scanned twice (using original barcode IDs 1,2)
            new BarcodeScan
            {
                Id = 1,
                PurchaseId = 1,
                BarcodeId = 1,
                Amount = 5.00m,
                ScannedAt = seedDate.AddDays(5)
            },
            new BarcodeScan
            {
                Id = 2,
                PurchaseId = 1,
                BarcodeId = 2,
                Amount = 10.00m,
                ScannedAt = seedDate.AddDays(5).AddMinutes(2)
            },
            // Purchase 2 - jane.smith scanned once (using original barcode ID 3)
            new BarcodeScan
            {
                Id = 3,
                PurchaseId = 2,
                BarcodeId = 3,
                Amount = 5.00m,
                ScannedAt = seedDate.AddDays(10)
            }
        );

        // Seed some payments
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = 1,
                UserId = 2,
                Amount = 20.00m,
                PaidAt = seedDate,
                Notes = "Initial payment"
            },
            new Payment
            {
                Id = 2,
                UserId = 3,
                Amount = 15.00m,
                PaidAt = seedDate.AddDays(2),
                Notes = "Cash payment"
            }
        );
    }
}

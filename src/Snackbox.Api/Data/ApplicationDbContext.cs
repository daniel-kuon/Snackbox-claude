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
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
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

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.AchievementId }).IsUnique();
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

        // Seed achievements
        modelBuilder.Entity<Achievement>().HasData(
            // Single purchase amount achievements
            new Achievement { Id = 1, Code = "BIG_SPENDER_5", Name = "Snack Attack!", Description = "Spent €5 or more in a single purchase", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 2, Code = "BIG_SPENDER_10", Name = "Hunger Games Champion", Description = "Spent €10 or more in a single purchase", Category = AchievementCategory.SinglePurchase },
            new Achievement { Id = 3, Code = "BIG_SPENDER_15", Name = "Snack Hoarder", Description = "Spent €15 or more in a single purchase", Category = AchievementCategory.SinglePurchase },

            // Daily purchase count achievements
            new Achievement { Id = 4, Code = "DAILY_BUYER_5", Name = "Frequent Flyer", Description = "Made 5 or more purchases in a single day", Category = AchievementCategory.DailyActivity },
            new Achievement { Id = 5, Code = "DAILY_BUYER_10", Name = "Snack Marathon", Description = "Made 10 or more purchases in a single day", Category = AchievementCategory.DailyActivity },

            // Streak achievements
            new Achievement { Id = 6, Code = "STREAK_DAILY_3", Name = "Three-peat", Description = "Made a purchase 3 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 7, Code = "STREAK_DAILY_7", Name = "Week Warrior", Description = "Made a purchase 7 days in a row", Category = AchievementCategory.Streak },
            new Achievement { Id = 8, Code = "STREAK_WEEKLY_4", Name = "Monthly Muncher", Description = "Made at least one purchase per week for 4 weeks", Category = AchievementCategory.Streak },

            // Comeback achievements
            new Achievement { Id = 9, Code = "COMEBACK_30", Name = "Long Time No See", Description = "First purchase after 1 month away", Category = AchievementCategory.Comeback },
            new Achievement { Id = 10, Code = "COMEBACK_60", Name = "The Return", Description = "First purchase after 2 months away", Category = AchievementCategory.Comeback },
            new Achievement { Id = 11, Code = "COMEBACK_90", Name = "Lazarus Rising", Description = "First purchase after 3 months away", Category = AchievementCategory.Comeback },

            // High debt achievements
            new Achievement { Id = 12, Code = "IN_DEBT_50", Name = "Credit Card Lifestyle", Description = "Unpaid balance of €50 or more", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 13, Code = "IN_DEBT_100", Name = "Financial Freedom? Never Heard of It", Description = "Unpaid balance of €100 or more", Category = AchievementCategory.HighDebt },
            new Achievement { Id = 14, Code = "IN_DEBT_150", Name = "Living on the Edge", Description = "Unpaid balance of €150 or more", Category = AchievementCategory.HighDebt },

            // Total spent achievements
            new Achievement { Id = 15, Code = "TOTAL_SPENT_100", Name = "Century Club", Description = "Spent €100 or more in total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 16, Code = "TOTAL_SPENT_150", Name = "Snack Connoisseur", Description = "Spent €150 or more in total", Category = AchievementCategory.TotalSpent },
            new Achievement { Id = 17, Code = "TOTAL_SPENT_200", Name = "Snackbox Legend", Description = "Spent €200 or more in total", Category = AchievementCategory.TotalSpent }
        );
    }
}

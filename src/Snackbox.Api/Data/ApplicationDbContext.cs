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
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<CashRegister> CashRegister => Set<CashRegister>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<PurchaseDiscount> PurchaseDiscounts => Set<PurchaseDiscount>();

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

        // Configure DateTime properties to handle UTC conversion
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        )
                    );
                }
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
        });

        // Configure TPH inheritance for Barcode
        modelBuilder.Entity<Barcode>();
        modelBuilder.Entity<LoginBarcode>();
        modelBuilder.Entity<PurchaseBarcode>();

        modelBuilder.Entity<BarcodeScan>();

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
            entity.HasOne(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Purchase>();

        modelBuilder.Entity<Withdrawal>();

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Supplier).HasMaxLength(200);

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
            entity.HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ii => ii.Product)
                .WithMany()
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShelvingAction>(entity =>
        {
            entity.HasOne(sa => sa.InvoiceItem)
                .WithMany(ii => ii.ShelvingActions)
                .HasForeignKey(sa => sa.InvoiceItemId)
                .OnDelete(DeleteBehavior.SetNull);
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
            // Remove unique constraint to allow multiple instances of same achievement
            entity.HasIndex(e => new { e.UserId, e.AchievementId, e.EarnedAt });
        });

        modelBuilder.Entity<Deposit>(entity =>
        {
            entity.HasOne(e => e.LinkedPayment)
                .WithOne(p => p.LinkedDeposit)
                .HasForeignKey<Payment>(p => p.LinkedDepositId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashRegister>();

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.MinimumPurchaseAmount).HasPrecision(10, 2);
            entity.Property(e => e.Value).HasPrecision(10, 2);
        });

        modelBuilder.Entity<PurchaseDiscount>(entity =>
        {
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.HasOne(pd => pd.Purchase)
                .WithMany(p => p.AppliedDiscounts)
                .HasForeignKey(pd => pd.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pd => pd.Discount)
                .WithMany()
                .HasForeignKey(pd => pd.DiscountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Only seed achievements - all other sample data is seeded via DatabaseSeeder service
        // Seed achievements
        modelBuilder.Entity<Achievement>().HasData(
            // Single purchase amount achievements (2, 3, 4, 5, 6€)
            new Achievement { Id = 1, Code = "BIG_SPENDER_2", Name = "Snack Nibbler", Description = "Spent €2 or more in a single purchase", Category = AchievementCategory.SinglePurchase, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE5B4\" stroke=\"#FF8C00\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF8C00\">🍪</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF8C00\">€2</text></svg>" },
            new Achievement { Id = 2, Code = "BIG_SPENDER_3", Name = "Snack Attack!", Description = "Spent €3 or more in a single purchase", Category = AchievementCategory.SinglePurchase, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FF6347\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF0000\">⚡</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF0000\">€3</text></svg>" },
            new Achievement { Id = 3, Code = "BIG_SPENDER_4", Name = "Hungry Hippo", Description = "Spent €4 or more in a single purchase", Category = AchievementCategory.SinglePurchase, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#DC143C\">🦛</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#DC143C\">€4</text></svg>" },
            new Achievement { Id = 18, Code = "BIG_SPENDER_5", Name = "Snack Hoarder", Description = "Spent €5 or more in a single purchase", Category = AchievementCategory.SinglePurchase, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E6E6FA\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">📦</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">€5</text></svg>" },
            new Achievement { Id = 19, Code = "BIG_SPENDER_6", Name = "The Whale", Description = "Spent €6 or more in a single purchase", Category = AchievementCategory.SinglePurchase, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#B0E0E6\" stroke=\"#1E90FF\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">🐋</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">€6</text></svg>" },

            // Daily purchase count achievements
            new Achievement { Id = 4, Code = "DAILY_BUYER_5", Name = "Frequent Flyer", Description = "Made 5 or more purchases in a single day", Category = AchievementCategory.DailyActivity, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4169E1\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">✈️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">x5</text></svg>" },
            new Achievement { Id = 5, Code = "DAILY_BUYER_10", Name = "Snack Marathon", Description = "Made 10 or more purchases in a single day", Category = AchievementCategory.DailyActivity, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFDAB9\" stroke=\"#D2691E\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🏃</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">x10</text></svg>" },
            new Achievement { Id = 20, Code = "DAILY_BUYER_3", Name = "Hat Trick", Description = "Made 3 purchases in a single day", Category = AchievementCategory.DailyActivity, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#98FB98\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">🎩</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">x3</text></svg>" },

            // Streak achievements
            new Achievement { Id = 6, Code = "STREAK_DAILY_3", Name = "Three-peat", Description = "Made a purchase 3 days in a row", Category = AchievementCategory.Streak, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFA07A\" stroke=\"#FF4500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">🔥</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">3d</text></svg>" },
            new Achievement { Id = 7, Code = "STREAK_DAILY_7", Name = "Week Warrior", Description = "Made a purchase 7 days in a row", Category = AchievementCategory.Streak, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FF8C00\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">⚔️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">7d</text></svg>" },
            new Achievement { Id = 8, Code = "STREAK_WEEKLY_4", Name = "Monthly Muncher", Description = "Made at least one purchase per week for 4 weeks", Category = AchievementCategory.Streak, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#90EE90\" stroke=\"#32CD32\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">📆</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">4w</text></svg>" },
            new Achievement { Id = 21, Code = "STREAK_DAILY_14", Name = "Fortnight Fanatic", Description = "Made a purchase 14 days in a row", Category = AchievementCategory.Streak, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#9932CC\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">🎯</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">14d</text></svg>" },
            new Achievement { Id = 22, Code = "STREAK_DAILY_30", Name = "Snack Addict", Description = "Made a purchase 30 days in a row", Category = AchievementCategory.Streak, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#C71585\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">💉</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">30d</text></svg>" },

            // Comeback achievements
            new Achievement { Id = 9, Code = "COMEBACK_30", Name = "Long Time No See", Description = "First purchase after 1 month away", Category = AchievementCategory.Comeback, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#F0E68C\" stroke=\"#BDB76B\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">👋</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">30d</text></svg>" },
            new Achievement { Id = 10, Code = "COMEBACK_60", Name = "The Return", Description = "First purchase after 2 months away", Category = AchievementCategory.Comeback, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#D8BFD8\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">↩️</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">60d</text></svg>" },
            new Achievement { Id = 11, Code = "COMEBACK_90", Name = "Lazarus Rising", Description = "First purchase after 3 months away", Category = AchievementCategory.Comeback, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4E1\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🧟</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">90d</text></svg>" },

            // High debt achievements (15, 20, 25, 30, 35€)
            new Achievement { Id = 12, Code = "IN_DEBT_15", Name = "Tab Starter", Description = "Unpaid balance of €15 or more", Category = AchievementCategory.HighDebt, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4B5\" stroke=\"#DEB887\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">📝</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">€15</text></svg>" },
            new Achievement { Id = 13, Code = "IN_DEBT_20", Name = "Credit Curious", Description = "Unpaid balance of €20 or more", Category = AchievementCategory.HighDebt, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFDAB9\" stroke=\"#CD853F\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">💳</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">€20</text></svg>" },
            new Achievement { Id = 14, Code = "IN_DEBT_25", Name = "Living on Credit", Description = "Unpaid balance of €25 or more", Category = AchievementCategory.HighDebt, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">💸</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">€25</text></svg>" },
            new Achievement { Id = 23, Code = "IN_DEBT_30", Name = "Debt Collector's Friend", Description = "Unpaid balance of €30 or more", Category = AchievementCategory.HighDebt, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFA07A\" stroke=\"#FF6347\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">📞</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€30</text></svg>" },
            new Achievement { Id = 24, Code = "IN_DEBT_35", Name = "Financial Freedom? Never Heard of It", Description = "Unpaid balance of €35 or more", Category = AchievementCategory.HighDebt, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF6B6B\" stroke=\"#DC143C\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"32\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">🚫💰</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€35</text></svg>" },

            // Total spent achievements
            new Achievement { Id = 15, Code = "TOTAL_SPENT_100", Name = "Century Club", Description = "Spent €100 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">💯</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">€100</text></svg>" },
            new Achievement { Id = 16, Code = "TOTAL_SPENT_150", Name = "Snack Connoisseur", Description = "Spent €150 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E6E6FA\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">🍷</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">€150</text></svg>" },
            new Achievement { Id = 17, Code = "TOTAL_SPENT_200", Name = "Snackbox Legend", Description = "Spent €200 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4169E1\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">🏆</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">€200</text></svg>" },
            new Achievement { Id = 25, Code = "TOTAL_SPENT_50", Name = "First Fifty", Description = "Spent €50 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#C0C0C0\" stroke=\"#A9A9A9\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">🥈</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">€50</text></svg>" },
            new Achievement { Id = 26, Code = "TOTAL_SPENT_300", Name = "Snack Royalty", Description = "Spent €300 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">👑</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">€300</text></svg>" },
            new Achievement { Id = 27, Code = "TOTAL_SPENT_500", Name = "Snack God", Description = "Spent €500 or more in total", Category = AchievementCategory.TotalSpent, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#FF1493\" stroke-width=\"3\"/><path d=\"M 60 20 L 70 45 L 95 45 L 75 60 L 85 85 L 60 70 L 35 85 L 45 60 L 25 45 L 50 45 Z\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"2\"/><text x=\"60\" y=\"65\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€500</text></svg>" },

            // Time-based achievements
            new Achievement { Id = 28, Code = "EARLY_BIRD", Name = "Early Bird", Description = "Made a purchase before 8 AM", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4682B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#191970\">🐦</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#191970\">&lt;8AM</text></svg>" },
            new Achievement { Id = 29, Code = "NIGHT_OWL", Name = "Night Owl", Description = "Made a purchase after 8 PM", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#191970\" stroke=\"#000080\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">🦉</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">&gt;8PM</text></svg>" },
            new Achievement { Id = 30, Code = "LUNCH_RUSH", Name = "Lunch Rush Survivor", Description = "Made a purchase between 12-1 PM", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4B5\" stroke=\"#D2691E\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🍱</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">12-1PM</text></svg>" },
            new Achievement { Id = 31, Code = "WEEKEND_WARRIOR", Name = "Weekend Warrior", Description = "Made a purchase on a Saturday or Sunday", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#98FB98\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">🎉</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">Sat/Sun</text></svg>" },
            new Achievement { Id = 32, Code = "MONDAY_BLUES", Name = "Monday Blues Cure", Description = "Made a purchase on a Monday", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#ADD8E6\" stroke=\"#4682B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#000080\">😔</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#000080\">Monday</text></svg>" },
            new Achievement { Id = 33, Code = "FRIDAY_TREAT", Name = "Friday Treat Yourself", Description = "Made a purchase on a Friday", Category = AchievementCategory.TimeBased, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🎊</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">Friday</text></svg>" },

            // Milestone achievements
            new Achievement { Id = 34, Code = "FIRST_PURCHASE", Name = "Welcome to the Club!", Description = "Made your first purchase", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4E1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🎈</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">1st</text></svg>" },
            new Achievement { Id = 35, Code = "PURCHASE_10", Name = "Regular Customer", Description = "Made 10 purchases total", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#F0E68C\" stroke=\"#DAA520\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🎫</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">10</text></svg>" },
            new Achievement { Id = 36, Code = "PURCHASE_50", Name = "Snack Veteran", Description = "Made 50 purchases total", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#C0C0C0\" stroke=\"#A9A9A9\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">🎖️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">50</text></svg>" },
            new Achievement { Id = 37, Code = "PURCHASE_100", Name = "Snack Centurion", Description = "Made 100 purchases total", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">🎖️</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">100</text></svg>" },
            new Achievement { Id = 38, Code = "PURCHASE_250", Name = "Snack Master", Description = "Made 250 purchases total", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">🥋</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">250</text></svg>" },
            new Achievement { Id = 39, Code = "PURCHASE_500", Name = "Snack Overlord", Description = "Made 500 purchases total", Category = AchievementCategory.Milestone, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF4500\" stroke=\"#8B0000\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">👑</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">500</text></svg>" },

            // Fun/quirky achievements
            new Achievement { Id = 40, Code = "SPEED_DEMON", Name = "Speed Demon", Description = "Made 2 scans within 3 seconds", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF6347\" stroke=\"#DC143C\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">⚡</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">&lt;1min</text></svg>" },
            new Achievement { Id = 41, Code = "DOUBLE_TROUBLE", Name = "Double Trouble", Description = "Made 2 or more scans in a session", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">2️⃣</text></svg>" },
            new Achievement { Id = 42, Code = "TRIPLE_THREAT", Name = "Triple Threat", Description = "Made 3 or more scans in a session", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">3️⃣</text></svg>" },
            new Achievement { Id = 43, Code = "LUCKY_SEVEN", Name = "Lucky Seven", Description = "Made 7 or more scans in a session", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#32CD32\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">7️⃣</text></svg>" },
            new Achievement { Id = 44, Code = "ROUND_NUMBER", Name = "OCD Approved", Description = "Made a purchase totaling exactly €5 or €10", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E0FFFF\" stroke=\"#00CED1\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#008B8B\">✔️</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#008B8B\">€5/€10</text></svg>" },
            new Achievement { Id = 45, Code = "SAME_AGAIN", Name = "Same Again, Please", Description = "Made 3 identical purchases in a row", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FAFAD2\" stroke=\"#BDB76B\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">🔁</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">x3</text></svg>" },
            new Achievement { Id = 46, Code = "PAID_UP", Name = "Debt Free!", Description = "Paid off your entire balance", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#90EE90\" stroke=\"#32CD32\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">✅</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">€0</text></svg>" },
            new Achievement { Id = 47, Code = "GENEROUS_SOUL", Name = "Generous Soul", Description = "Have a positive balance (credit) of €10 or more", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">💝</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">+€10</text></svg>" },
            new Achievement { Id = 48, Code = "SNACK_BIRTHDAY", Name = "Happy Snack-iversary!", Description = "Made a purchase exactly 1 year after your first", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🎂</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">1 year</text></svg>" },
            new Achievement { Id = 49, Code = "THIRTEENTH", Name = "Unlucky 13", Description = "Made a purchase totaling exactly €13", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#2F4F4F\" stroke=\"#000000\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">🖤</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">€13</text></svg>" },
            new Achievement { Id = 50, Code = "NICE", Name = "Nice.", Description = "Made a purchase totaling exactly €6.90", Category = AchievementCategory.Special, ImageUrl = "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">😏</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">€6.90</text></svg>" }
        );
    }
}

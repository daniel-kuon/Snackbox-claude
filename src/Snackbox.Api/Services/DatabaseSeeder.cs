using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedSampleDataAsync()
    {
        _logger.LogInformation("Seeding sample data");

        // Check if data already exists
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Sample data already exists, skipping");
            return;
        }

        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed users
        var users = new[]
        {
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@snackbox.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminPassword", "$2a$11$7EW8wLqhqKQZH8J6rX5kQ."),
                IsAdmin = true,
                CreatedAt = seedDate
            },
            new User
            {
                Id = 2,
                Username = "john.doe",
                Email = "john.doe@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("johnPassword", "$2a$11$7EW8wLqhqKQZH8J6rX5kQ."),
                IsAdmin = false,
                CreatedAt = seedDate
            },
            new User
            {
                Id = 3,
                Username = "jane.smith",
                Email = "jane.smith@company.com",
                PasswordHash = null,
                IsAdmin = false,
                CreatedAt = seedDate
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users", users.Length);

        // Seed products
        var products = new[]
        {
            new Product { Id = 1, Name = "Chips - Salt", CreatedAt = seedDate },
            new Product { Id = 2, Name = "Chocolate Bar", CreatedAt = seedDate },
            new Product { Id = 3, Name = "Energy Drink", CreatedAt = seedDate },
            new Product { Id = 4, Name = "Cookies", CreatedAt = seedDate }
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} products", products.Length);

        // Seed product barcodes
        var productBarcodes = new[]
        {
            new ProductBarcode { Id = 1, ProductId = 1, Barcode = "1234567890123", Quantity = 1, CreatedAt = seedDate },
            new ProductBarcode { Id = 2, ProductId = 1, Barcode = "1234567890123-BOX", Quantity = 12, CreatedAt = seedDate },
            new ProductBarcode { Id = 3, ProductId = 2, Barcode = "1234567890124", Quantity = 1, CreatedAt = seedDate },
            new ProductBarcode { Id = 4, ProductId = 2, Barcode = "1234567890124-PACK", Quantity = 5, CreatedAt = seedDate },
            new ProductBarcode { Id = 5, ProductId = 3, Barcode = "1234567890125", Quantity = 1, CreatedAt = seedDate },
            new ProductBarcode { Id = 6, ProductId = 4, Barcode = "1234567890126", Quantity = 1, CreatedAt = seedDate }
        };

        _context.ProductBarcodes.AddRange(productBarcodes);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} product barcodes", productBarcodes.Length);

        // Seed product batches
        var batches = new[]
        {
            new ProductBatch { Id = 1, ProductId = 1, BestBeforeDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), CreatedAt = seedDate },
            new ProductBatch { Id = 2, ProductId = 2, BestBeforeDate = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc), CreatedAt = seedDate },
            new ProductBatch { Id = 3, ProductId = 3, BestBeforeDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), CreatedAt = seedDate },
            new ProductBatch { Id = 4, ProductId = 4, BestBeforeDate = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc), CreatedAt = seedDate }
        };

        _context.ProductBatches.AddRange(batches);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} product batches", batches.Length);

        // Seed shelving actions
        var shelvingActions = new[]
        {
            new ShelvingAction { Id = 1, ProductBatchId = 1, Quantity = 50, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 2, ProductBatchId = 1, Quantity = 20, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) },
            new ShelvingAction { Id = 3, ProductBatchId = 2, Quantity = 30, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 4, ProductBatchId = 2, Quantity = 15, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) },
            new ShelvingAction { Id = 5, ProductBatchId = 3, Quantity = 40, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 6, ProductBatchId = 3, Quantity = 25, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(2) },
            new ShelvingAction { Id = 7, ProductBatchId = 4, Quantity = 35, Type = ShelvingActionType.AddedToStorage, ActionAt = seedDate },
            new ShelvingAction { Id = 8, ProductBatchId = 4, Quantity = 18, Type = ShelvingActionType.MovedToShelf, ActionAt = seedDate.AddDays(1) }
        };

        _context.ShelvingActions.AddRange(shelvingActions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} shelving actions", shelvingActions.Length);

        // Seed user barcodes
        var barcodes = new Barcode[]
        {
            new PurchaseBarcode { Id = 1, UserId = 2, Code = "4061461764012", Amount = 5.00m, CreatedAt = seedDate },
            new PurchaseBarcode { Id = 2, UserId = 2, Code = "USER2-10EUR", Amount = 10.00m, CreatedAt = seedDate },
            new PurchaseBarcode { Id = 3, UserId = 3, Code = "USER3-5EUR", Amount = 5.00m, CreatedAt = seedDate },
            new PurchaseBarcode { Id = 4, UserId = 3, Code = "USER3-10EUR", Amount = 10.00m, CreatedAt = seedDate },
            new LoginBarcode { Id = 5, UserId = 1, Code = "4260473313809", Amount = 0m, CreatedAt = seedDate },
            new LoginBarcode { Id = 6, UserId = 2, Code = "USER2-LOGIN", Amount = 0m, CreatedAt = seedDate },
            new LoginBarcode { Id = 7, UserId = 3, Code = "USER3-LOGIN", Amount = 0m, CreatedAt = seedDate }
        };

        _context.Barcodes.AddRange(barcodes);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} user barcodes", barcodes.Length);

        // Seed sample purchases
        var purchases = new[]
        {
            new Purchase
            {
                Id = 1,
                UserId = 2,
                CreatedAt = seedDate.AddDays(5),
                UpdatedAt = seedDate.AddDays(5).AddMinutes(5)
            },
            new Purchase
            {
                Id = 2,
                UserId = 3,
                CreatedAt = seedDate.AddDays(10),
                UpdatedAt = seedDate.AddDays(10).AddMinutes(3)
            }
        };

        _context.Purchases.AddRange(purchases);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample purchases", purchases.Length);

        // Seed barcode scans for purchases
        var scans = new[]
        {
            new BarcodeScan { Id = 1, PurchaseId = 1, BarcodeId = 1, Amount = 5.00m, ScannedAt = seedDate.AddDays(5) },
            new BarcodeScan { Id = 2, PurchaseId = 1, BarcodeId = 2, Amount = 10.00m, ScannedAt = seedDate.AddDays(5).AddMinutes(2) },
            new BarcodeScan { Id = 3, PurchaseId = 2, BarcodeId = 3, Amount = 5.00m, ScannedAt = seedDate.AddDays(10) }
        };

        _context.BarcodeScans.AddRange(scans);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} barcode scans", scans.Length);

        // Seed sample payments
        var payments = new[]
        {
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
        };

        _context.Payments.AddRange(payments);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample payments", payments.Length);

        _logger.LogInformation("Sample data seeding completed");
    }
}

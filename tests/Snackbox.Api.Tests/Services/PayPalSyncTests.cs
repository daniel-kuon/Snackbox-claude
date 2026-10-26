using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Data;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class PayPalSyncTests
{
    private readonly ApplicationDbContext _context;
    private readonly FakePayPal _paypal = new();
    private readonly PayPalSync _sync;

    public PayPalSyncTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayPal:ClientId"] = "id",
                ["PayPal:ClientSecret"] = "secret",
                ["PayPal:AdminUserId"] = "9"
            })
            .Build();
        _sync = new PayPalSync(_context, configuration, new PayPalApi(new HttpClient(_paypal)), new PayPalSync.Status(),
                               NullLogger<PayPalSync>.Instance);

        var now = DateTime.UtcNow;
        _context.Users.AddRange(
            new User { Id = 1, Username = "Tina", CreatedAt = now },
            new User { Id = 9, Username = "Admin", IsAdmin = true, CreatedAt = now });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Sync_KeepsOnlyCompletedIncomingSnackboxPayments()
    {
        _paypal.Transactions =
        [
            Transaction("T1", "5.00", "S", "PAYER-A", "Snackbox Oktober"),
            Transaction("T2", "3.00", "S", "PAYER-B", "Birthday present"),       // not for us
            Transaction("T3", "-4.00", "S", "PAYER-A", "snackbox refund"),        // money we sent
            Transaction("T4", "2.00", "P", "PAYER-A", "Snackbox")                 // still pending
        ];

        var status = await _sync.SyncAsync();

        Assert.Null(status.LastError);
        var stored = await _context.PayPalTransactions.SingleAsync();
        Assert.Equal("T1", stored.TransactionId);
        Assert.Equal(5.00m, stored.Amount);
        Assert.Equal("Tina Payer", stored.PayerName);
        Assert.Null(stored.PaymentId); // payer unknown: waits for the admin
    }

    [Fact]
    public async Task Assign_BooksThePayment_AndLaterOnesFromThatPayerBookThemselves()
    {
        _paypal.Transactions = [Transaction("T1", "5.00", "S", "PAYER-A", "Snackbox")];
        await _sync.SyncAsync();
        var first = await _context.PayPalTransactions.SingleAsync();

        await _sync.AssignAsync(first.Id, userId: 1);

        var payment = await _context.Payments.SingleAsync();
        Assert.Equal(1, payment.UserId);
        Assert.Equal(5.00m, payment.Amount);
        Assert.Equal(PaymentType.PayPal, payment.Type);
        Assert.Equal(9, payment.AdminUserId);
        Assert.Equal(5.00m, (await _context.Withdrawals.SingleAsync()).Amount); // the admin holds the money
        Assert.Equal(payment.Id, (await _context.PayPalTransactions.SingleAsync()).PaymentId);

        // Next fetch: the same one again (overlap) and a new one from the same payer
        _paypal.Transactions = [Transaction("T1", "5.00", "S", "PAYER-A", "Snackbox"), Transaction("T5", "2.50", "S", "PAYER-A", "Snackbox")];
        var status = await _sync.SyncAsync();

        Assert.Equal(1, status.LastAdded);
        Assert.Equal(1, status.LastBooked);
        Assert.Equal(new[] { 2.50m, 5.00m }, await _context.Payments.Select(p => p.Amount).OrderBy(a => a).ToArrayAsync());
    }

    private static string Transaction(string id, string amount, string status, string payer, string note) => $$"""
        {
          "transaction_info": {
            "transaction_id": "{{id}}",
            "transaction_event_code": "T0011",
            "transaction_initiation_date": "2026-10-20T10:00:00+0000",
            "transaction_amount": { "currency_code": "EUR", "value": "{{amount}}" },
            "transaction_status": "{{status}}",
            "transaction_note": "{{note}}"
          },
          "payer_info": {
            "account_id": "{{payer}}",
            "email_address": "tina@example.com",
            "payer_name": { "given_name": "Tina", "surname": "Payer" }
          }
        }
        """;

    /// <summary>PayPal's token and transaction search endpoints.</summary>
    private sealed class FakePayPal : HttpMessageHandler
    {
        public List<string> Transactions { get; set; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.RequestUri!.AbsolutePath == "/v1/oauth2/token"
                ? """{ "access_token": "token" }"""
                : $$"""{ "transaction_details": [ {{string.Join(",", Transactions)}} ], "total_pages": 1 }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}

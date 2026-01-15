using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Snackbox.Api.Dtos;
using Snackbox.Components.Models;
using Timer = System.Timers.Timer;

namespace Snackbox.Components.Services;

public class ScannerService : IScannerService
{
    private readonly HttpClient _httpClient;
    private Timer? _timeoutTimer;

    public event Action<PurchaseSession>? OnPurchaseStarted;
    public event Action<PurchaseSession>? OnPurchaseUpdated;
    public event Action? OnPurchaseCompleted;
    public event Action? OnPurchaseTimeout;

    public PurchaseSession? CurrentSession { get; private set; }
    public bool IsSessionActive => CurrentSession != null;
    public int TimeoutSeconds { get; }

    public ScannerService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        TimeoutSeconds = configuration.GetValue("Scanner:TimeoutSeconds", 60);
    }

    public async Task<ScanResult> ProcessBarcodeAsync(string barcodeCode)
    {
        if (string.IsNullOrWhiteSpace(barcodeCode))
            return new ScanResult { IsSuccess = false, ErrorMessage = "Empty barcode" };

        try
        {
            // Call the API - it handles everything (auth, purchase creation/update)
            var response = await _httpClient.PostAsJsonAsync("api/scanner/scan", new
            {
                BarcodeCode = barcodeCode
            });

            if (!response.IsSuccessStatusCode)
                return new ScanResult { IsSuccess = false, ErrorMessage = "API request failed" };

            var result = await response.Content.ReadFromJsonAsync<ScanBarcodeResponse>();

            if (result == null)
                return new ScanResult { IsSuccess = false, ErrorMessage = "Invalid response" };

            if (!result.Success)
                return new ScanResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };

            // Handle login-only barcodes - don't update session
            if (result.IsLoginOnly)
            {
                return new ScanResult
                {
                    IsSuccess = true,
                    IsAdmin = result.IsAdmin,
                    IsLoginOnly = true
                };
            }

            var wasActive = IsSessionActive;
            var previousUserId = CurrentSession?.UserId;

            // Update local session from API response
            CurrentSession = new PurchaseSession
            {
                UserId = result.UserId.ToString(),
                UserName = result.Username,
                OpenAmount = result.Balance,
                LastPaymentAmount = result.LastPaymentAmount,
                LastPaymentDate = result.LastPaymentDate,
                ScannedBarcodes = result.ScannedBarcodes.Select(b => new ScannedBarcode
                {
                    BarcodeCode = b.BarcodeCode,
                    Amount = b.Amount,
                    ScannedAt = b.ScannedAt
                }).ToList(),
                StartTime = result.ScannedBarcodes.FirstOrDefault()?.ScannedAt ?? DateTime.UtcNow,
                RecentPurchases = result.RecentPurchases.Select(rp => new RecentPurchase
                {
                    PurchaseId = rp.PurchaseId,
                    TotalAmount = rp.TotalAmount,
                    CompletedAt = rp.CompletedAt,
                    ItemCount = rp.ItemCount
                }).ToList(),
                NewAchievements = result.NewAchievements.Select(a => new Achievement
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    Category = a.Category,
                    ImageUrl = a.ImageUrl,
                    EarnedAt = a.EarnedAt
                }).ToList()
            };

            // Determine if this is a new purchase or update
            if (!wasActive || previousUserId != CurrentSession.UserId)
            {
                // New session started
                StartTimeoutTimer();
                OnPurchaseStarted?.Invoke(CurrentSession);
            }
            else
            {
                // Existing session updated
                ResetTimeoutTimer();
                OnPurchaseUpdated?.Invoke(CurrentSession);
            }

            return new ScanResult
            {
                IsSuccess = true,
                IsAdmin = result.IsAdmin,
                IsLoginOnly = false
            };
        }
        catch (Exception ex)
        {
            return new ScanResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public void ResetSession()
    {
        // Reset the local session state (called when timeout expires)
        StopTimeoutTimer();
        CurrentSession = null;
        OnPurchaseTimeout?.Invoke();
    }

    public void SignalActivity()
    {
        if (IsSessionActive)
        {
            ResetTimeoutTimer();
        }
    }

    public async Task<IEnumerable<PurchaseDto>> GetMyPurchasesAsync()
    {
        if (CurrentSession == null)
            return Array.Empty<PurchaseDto>();

        var list = await _httpClient.GetFromJsonAsync<IEnumerable<PurchaseDto>>($"api/purchases/user/{CurrentSession.UserId}");
        return list ?? Array.Empty<PurchaseDto>();
    }

    public async Task<IEnumerable<PaymentDto>> GetMyPaymentsAsync()
    {
        if (CurrentSession == null)
            return Array.Empty<PaymentDto>();

        var list = await _httpClient.GetFromJsonAsync<IEnumerable<PaymentDto>>($"api/payments/user/{CurrentSession.UserId}");
        return list ?? Array.Empty<PaymentDto>();
    }

    public Task CompletePurchaseAsync()
    {
        // Complete the current purchase session
        StopTimeoutTimer();
        CurrentSession = null;
        OnPurchaseCompleted?.Invoke();
        return Task.CompletedTask;
    }

    private void StartTimeoutTimer()
    {
        _timeoutTimer = new Timer(TimeoutSeconds * 1000);
        _timeoutTimer.Elapsed += (_, _) => ResetSession();
        _timeoutTimer.AutoReset = false;
        _timeoutTimer.Start();
    }

    private void ResetTimeoutTimer()
    {
        StopTimeoutTimer();
        StartTimeoutTimer();
    }

    private void StopTimeoutTimer()
    {
        if (_timeoutTimer != null)
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Dispose();
            _timeoutTimer = null;
        }
    }
}

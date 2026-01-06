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
    #pragma warning disable CS0067
    public event Action? OnPurchaseCompleted;
    #pragma warning restore CS0067
    public event Action? OnPurchaseTimeout;

    public PurchaseSession? CurrentSession { get; private set; }
    public bool IsSessionActive => CurrentSession != null;
    public int TimeoutSeconds { get; }

    public ScannerService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        TimeoutSeconds = configuration.GetValue("Scanner:TimeoutSeconds", 60);
    }

    public async Task<ScanResult> ScanBarcodeAsync(string barcodeCode)
    {
        if (string.IsNullOrWhiteSpace(barcodeCode))
            return new ScanResult { IsSuccess = false, ErrorMessage = "Empty barcode" };

        try
        {
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

            return new ScanResult
            {
                IsSuccess = true,
                IsAdmin = result.IsAdmin,
                IsLoginOnly = result.IsLoginOnly
            };
        }
        catch (Exception ex)
        {
            return new ScanResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task ProcessBarcodeAsync(string barcodeCode)
    {
        if (string.IsNullOrWhiteSpace(barcodeCode))
            return;

            // Call the API - it handles everything (auth, purchase creation/update)
            var response = await _httpClient.PostAsJsonAsync("api/scanner/scan", new
            {
                BarcodeCode = barcodeCode
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception("API request failed");

            var result = await response.Content.ReadFromJsonAsync<ScanBarcodeResponse>();

            if (result == null)
                throw new Exception("Invalid response from server");

            if (!result.Success)
                throw new Exception(result.ErrorMessage ?? "Barcode not recognized");

            // Check if this is a login-only barcode
            if (result.IsLoginOnly)
            {
                // Login-only barcodes should be handled by the caller (redirect to login)
                // This shouldn't be called for login barcodes, but handle it gracefully
                throw new Exception("This barcode is for login only");
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
    }

    private void ResetSession()
    {
        // Reset the local session state (called when timeout expires)
        StopTimeoutTimer();
        CurrentSession = null;
        OnPurchaseTimeout?.Invoke();
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

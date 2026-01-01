using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Snackbox.Components.Models;

namespace Snackbox.Components.Services;

public class ScannerService : IScannerService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILocalizationService _localizationService;
    private System.Timers.Timer? _timeoutTimer;

    public event Action<PurchaseSession>? OnPurchaseStarted;
    public event Action<PurchaseSession>? OnPurchaseUpdated;
    public event Action? OnPurchaseCompleted;
    public event Action? OnPurchaseTimeout;

    public PurchaseSession? CurrentSession { get; private set; }
    public bool IsSessionActive => CurrentSession != null;
    public int TimeoutSeconds { get; }

    public ScannerService(HttpClient httpClient, IConfiguration configuration, ILocalizationService localizationService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _localizationService = localizationService;
        TimeoutSeconds = configuration.GetValue<int>("Scanner:TimeoutSeconds", 60);
    }

    public async Task<ScanResult?> ScanBarcodeAsync(string barcodeCode)
    {
        if (string.IsNullOrWhiteSpace(barcodeCode))
            return null;

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/scanner/scan", new
            {
                BarcodeCode = barcodeCode
            });

            if (!response.IsSuccessStatusCode)
                return new ScanResult { IsSuccess = false, ErrorMessage = "API request failed" };

            var result = await response.Content.ReadFromJsonAsync<ScanBarcodeResponseDto>();

            if (result == null)
                return new ScanResult { IsSuccess = false, ErrorMessage = "Invalid response" };

            if (!result.Success)
                return new ScanResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };

            return new ScanResult 
            { 
                IsSuccess = true, 
                IsAdmin = result.IsAdmin 
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

        try
        {
            // Call the API - it handles everything (auth, purchase creation/update)
            var response = await _httpClient.PostAsJsonAsync("api/scanner/scan", new
            {
                BarcodeCode = barcodeCode
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception("API request failed");

            var result = await response.Content.ReadFromJsonAsync<ScanBarcodeResponseDto>();

            if (result == null)
                throw new Exception("Invalid response from server");

            if (!result.Success)
                throw new Exception(result.ErrorMessage ?? "Barcode not recognized");

            var wasActive = IsSessionActive;
            var previousUserId = CurrentSession?.UserId;

            // Update local session from API response
            CurrentSession = new PurchaseSession
            {
                UserId = result.UserId.ToString(),
                UserName = result.Username,
                PreferredLanguage = result.PreferredLanguage,
                OpenAmount = result.Balance,
                LastPaymentAmount = result.LastPaymentAmount,
                LastPaymentDate = result.LastPaymentDate,
                ScannedBarcodes = result.ScannedBarcodes.Select(b => new ScannedBarcode
                {
                    BarcodeCode = b.BarcodeCode,
                    Amount = b.Amount,
                    ScannedAt = b.ScannedAt
                }).ToList(),
                StartTime = result.ScannedBarcodes.FirstOrDefault()?.ScannedAt ?? DateTime.UtcNow
            };

            // Update language if user's preferred language differs from current
            _localizationService.SetCulture(result.PreferredLanguage);

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
        catch (Exception ex)
        {
            // Re-throw so the caller can handle the error
            throw;
        }
    }

    public async Task CompletePurchaseAsync()
    {
        // Purchase is already completed on the server side
        // Just clean up local state
        StopTimeoutTimer();
        OnPurchaseCompleted?.Invoke();
        CurrentSession = null;
    }

    public void ResetSession()
    {
        StopTimeoutTimer();
        CurrentSession = null;
        
        // Reset language to English on timeout
        _localizationService.SetCulture("en");
        
        OnPurchaseTimeout?.Invoke();
    }

    private void StartTimeoutTimer()
    {
        _timeoutTimer = new System.Timers.Timer(TimeoutSeconds * 1000);
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

    // DTO classes matching API response
    private class ScanBarcodeResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string PreferredLanguage { get; set; } = "en";
        public int PurchaseId { get; set; }
        public List<ScannedBarcodeDto> ScannedBarcodes { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal LastPaymentAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    private class ScannedBarcodeDto
    {
        public string BarcodeCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ScannedAt { get; set; }
    }
}

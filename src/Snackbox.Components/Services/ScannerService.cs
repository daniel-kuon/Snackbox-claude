using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Snackbox.Api.Dtos;
using Snackbox.Components.Models;
using Snackbox.Components.Mappers;
using Refit;
using Snackbox.ApiClient;
using Timer = System.Timers.Timer;

namespace Snackbox.Components.Services;

public class ScannerService : IScannerService
{
    private readonly HttpClient _httpClient;
    private readonly IScannerApi _scannerApi;
    private readonly IPurchasesApi _purchasesApi;
    private readonly IPaymentsApi _paymentsApi;
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

        // Use Refit clients backed by the same HttpClient instance (auth headers, base URL etc.)
        _scannerApi = RestService.For<IScannerApi>(_httpClient);
        _purchasesApi = RestService.For<IPurchasesApi>(_httpClient);
        _paymentsApi = RestService.For<IPaymentsApi>(_httpClient);
    }

    public async Task<ScanResult> ProcessBarcodeAsync(string barcodeCode)
    {
        if (string.IsNullOrWhiteSpace(barcodeCode))
            return new ScanResult { IsSuccess = false, ErrorMessage = "Empty barcode" };

        try
        {
            // Call the API via Refit - it handles everything (auth, purchase creation/update)
            var result = await _scannerApi.ScanBarcodeAsync(new ScanBarcodeRequest
            {
                BarcodeCode = barcodeCode
            });

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
                ScannedBarcodes = DtoToModelMapper.ToScannedBarcodes(result.ScannedBarcodes),
                StartTime = result.ScannedBarcodes.FirstOrDefault()?.ScannedAt ?? DateTime.UtcNow,
                RecentPurchases = DtoToModelMapper.ToRecentPurchases(result.RecentPurchases),
                NewAchievements = DtoToModelMapper.ToAchievements(result.NewAchievements)
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

        // Use Refit client for API call
        var list = await _purchasesApi.GetByUserIdAsync(int.Parse(CurrentSession.UserId));
        return list ?? Array.Empty<PurchaseDto>();
    }

    public async Task<IEnumerable<PaymentDto>> GetMyPaymentsAsync()
    {
        if (CurrentSession == null)
            return Array.Empty<PaymentDto>();

        // Use Refit client for API call
        var list = await _paymentsApi.GetByUserIdAsync(int.Parse(CurrentSession.UserId));
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

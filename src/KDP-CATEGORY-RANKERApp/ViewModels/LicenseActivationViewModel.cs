using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KDP_CATEGORY_RANKERApp.Services;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class LicenseActivationViewModel : ObservableObject
{
    private readonly ILicenseKeyService _licenseService;
    private readonly ILogger<LicenseActivationViewModel> _logger;

    [ObservableProperty]
    private string licenseKey = "";

    [ObservableProperty]
    private string userEmail = "";

    [ObservableProperty]
    private bool isActivating = false;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isActivationSuccessful = false;

    [ObservableProperty]
    private LicenseInfo? currentLicense;

    [ObservableProperty]
    private LicenseFeatures availableFeatures;

    [ObservableProperty]
    private bool showActivationDialog = false;

    public LicenseActivationViewModel(ILicenseKeyService licenseService, ILogger<LicenseActivationViewModel> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
        
        _ = LoadCurrentLicenseAsync();
        UpdateFeatures();
    }

    [RelayCommand]
    private async Task ActivateLicenseAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            StatusMessage = "Please enter a license key";
            return;
        }

        if (string.IsNullOrWhiteSpace(UserEmail))
        {
            StatusMessage = "Please enter your email address";
            return;
        }

        try
        {
            IsActivating = true;
            StatusMessage = "Validating license...";

            var validationResult = await _licenseService.ValidateLicenseAsync(LicenseKey);
            
            if (!validationResult.IsValid)
            {
                StatusMessage = $"License validation failed: {validationResult.Message}";
                _logger.LogWarning("License validation failed: {Message}", validationResult.Message);
                return;
            }

            StatusMessage = "Activating license...";
            var success = await _licenseService.ActivateLicenseAsync(LicenseKey, UserEmail);
            
            if (success)
            {
                StatusMessage = "License activated successfully!";
                IsActivationSuccessful = true;
                ShowActivationDialog = false;
                
                await LoadCurrentLicenseAsync();
                UpdateFeatures();
                
                _logger.LogInformation("License activated successfully for {UserEmail}", UserEmail);
            }
            else
            {
                StatusMessage = "License activation failed. Please check your license key and try again.";
                _logger.LogWarning("License activation failed for {UserEmail}", UserEmail);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during activation: {ex.Message}";
            _logger.LogError(ex, "Error during license activation");
        }
        finally
        {
            IsActivating = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateLicenseAsync()
    {
        try
        {
            await _licenseService.DeactivateLicenseAsync();
            
            CurrentLicense = null;
            StatusMessage = "License deactivated";
            IsActivationSuccessful = false;
            UpdateFeatures();
            
            _logger.LogInformation("License deactivated");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deactivating license: {ex.Message}";
            _logger.LogError(ex, "Error deactivating license");
        }
    }

    [RelayCommand]
    private void ShowActivationDialog()
    {
        ShowActivationDialog = true;
        StatusMessage = "";
        LicenseKey = "";
        UserEmail = CurrentLicense?.UserEmail ?? "";
    }

    [RelayCommand]
    private void CloseActivationDialog()
    {
        ShowActivationDialog = false;
        StatusMessage = "";
    }

    [RelayCommand]
    private void PurchaseLicense()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://your-website.com/purchase-license", // Replace with actual purchase URL
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening purchase URL");
            StatusMessage = "Could not open purchase page";
        }
    }

    [RelayCommand]
    private async Task ValidateLicenseAsync()
    {
        if (CurrentLicense == null)
        {
            StatusMessage = "No license to validate";
            return;
        }

        try
        {
            var validationResult = await _licenseService.ValidateLicenseAsync(CurrentLicense.LicenseKey);
            
            if (validationResult.IsValid)
            {
                StatusMessage = "License is valid and active";
            }
            else
            {
                StatusMessage = $"License validation failed: {validationResult.Message}";
                // If license is invalid, we might need to deactivate it
                await DeactivateLicenseAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error validating license: {ex.Message}";
            _logger.LogError(ex, "Error validating current license");
        }
    }

    private async Task LoadCurrentLicenseAsync()
    {
        try
        {
            CurrentLicense = await _licenseService.GetCurrentLicenseAsync();
            
            if (CurrentLicense != null)
            {
                IsActivationSuccessful = _licenseService.IsLicenseValid();
                
                if (IsActivationSuccessful)
                {
                    StatusMessage = $"Licensed to: {CurrentLicense.UserEmail} ({CurrentLicense.LicenseType})";
                }
                else
                {
                    StatusMessage = "License expired or invalid";
                }
            }
            else
            {
                StatusMessage = "No license activated - running in demo mode";
                IsActivationSuccessful = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading current license");
            StatusMessage = "Error loading license information";
        }
    }

    private void UpdateFeatures()
    {
        AvailableFeatures = _licenseService.GetAvailableFeatures();
    }

    public string GetLicenseStatusText()
    {
        if (CurrentLicense == null)
            return "Demo Mode";

        var status = CurrentLicense.LicenseType.ToString();
        
        if (CurrentLicense.ExpiryDate.HasValue)
        {
            var daysRemaining = (CurrentLicense.ExpiryDate.Value - DateTime.UtcNow).Days;
            if (daysRemaining > 0)
            {
                status += $" ({daysRemaining} days remaining)";
            }
            else
            {
                status += " (EXPIRED)";
            }
        }
        else
        {
            status += " (Lifetime)";
        }

        return status;
    }

    public string GetFeatureSummary()
    {
        if (AvailableFeatures == null)
            return "Loading...";

        var features = new List<string>();
        
        if (AvailableFeatures.CanAccessCategoryRecommender) features.Add("Category Recommender");
        if (AvailableFeatures.CanAccessKeywordResearch) features.Add("Keyword Research");
        if (AvailableFeatures.CanAccessCompetitionAnalysis) features.Add("Competition Analysis");
        if (AvailableFeatures.CanAccessAmsGenerator) features.Add("AMS Generator");
        if (AvailableFeatures.CanAccessReverseAsin) features.Add("Reverse ASIN");
        if (AvailableFeatures.CanExportData) features.Add("Data Export");
        if (AvailableFeatures.CanAccessLiveData) features.Add("Live Data");

        var maxQueries = AvailableFeatures.MaxDailyQueries == int.MaxValue 
            ? "Unlimited" 
            : AvailableFeatures.MaxDailyQueries.ToString();

        return $"{string.Join(", ", features)} • {maxQueries} daily queries";
    }
}
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KDP_CATEGORY_RANKERApp.Services;

public interface ILicenseKeyService
{
    Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey);
    Task<bool> ActivateLicenseAsync(string licenseKey, string userEmail);
    Task<LicenseInfo?> GetCurrentLicenseAsync();
    Task DeactivateLicenseAsync();
    bool IsLicenseValid();
    LicenseFeatures GetAvailableFeatures();
}

public record LicenseInfo(
    string LicenseKey,
    string UserEmail,
    string UserName,
    LicenseType LicenseType,
    DateTime IssueDate,
    DateTime? ExpiryDate,
    bool IsActivated,
    string MachineId,
    int MaxActivations = 3
);

public record LicenseValidationResult(
    bool IsValid,
    string Message,
    LicenseInfo? LicenseInfo = null,
    List<string> ValidationErrors = null
);

public record LicenseFeatures(
    bool CanAccessCategoryRecommender,
    bool CanAccessKeywordResearch,
    bool CanAccessCompetitionAnalysis,
    bool CanAccessAmsGenerator,
    bool CanAccessReverseAsin,
    bool CanExportData,
    bool CanAccessLiveData,
    int MaxDailyQueries,
    int MaxCategoriesPerQuery
);

public enum LicenseType
{
    Trial = 0,
    Basic = 1,
    Professional = 2,
    Enterprise = 3
}

public class LicenseKeyService : ILicenseKeyService
{
    private readonly ILogger<LicenseKeyService> _logger;
    private readonly IPortableConfigService _configService;
    private readonly string _publicKey;
    private LicenseInfo? _currentLicense;

    // RSA public key for license verification (in a real implementation, this would be more secure)
    private const string RSA_PUBLIC_KEY = @"
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2B8Fb9UW8kF5xDGQ7vYR
H9JXJ7k8mN3pQ4zF2dL1W9vK8xA3mP5sR7NtF2dK1V8qH4cG9sT6rE3wP8qJ2kF1
X3mN7vG4dK8rQ2zA1sPxT4qV8kM9dF3nK1W8rG4pL6sQ7zA8mN1vK2dL9qF5xH3c
T6rE4wP9qJ1kF8mN3pQ2zG1dL7W8vK4xB3mP2sR9NtF5dK8V1qH7cG4sT3rE6wP
Q4qJ1kF2X9mN3vG7dK1rQ5zA4sPxT1qV4kM2dF9nK8W1rG7pL3sQ4zA1mN8vK5d
L2qF8xH9cT3rE7wPQqJ8kF1X2mN9vG4dK5rQ6zA7sPxT8qV1kMdF2nK4W8rG1pL
9sQ1zA4mN1vK8dL5qF2xH6cT9rE4wPQqJ5kF8mN9pQ1zG8dL4W1vK7xB9mP1sR
2NtF8dK4V5qH1cG7sT9rE1wPQqJ8kF5X1mN2vG7dK8rQ9zA1sPxT4qV5kM6dF
QIDAQAB
-----END PUBLIC KEY-----";

    public LicenseKeyService(ILogger<LicenseKeyService> logger, IPortableConfigService configService)
    {
        _logger = logger;
        _configService = configService;
        _publicKey = RSA_PUBLIC_KEY;
        
        _ = LoadCurrentLicenseAsync();
    }

    public async Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                return new LicenseValidationResult(false, "License key cannot be empty");
            }

            // Parse the license key (format: BASE64-ENCODED-DATA.SIGNATURE)
            var parts = licenseKey.Split('.');
            if (parts.Length != 2)
            {
                return new LicenseValidationResult(false, "Invalid license key format");
            }

            var dataBase64 = parts[0];
            var signatureBase64 = parts[1];

            // Decode the license data
            var dataBytes = Convert.FromBase64String(dataBase64);
            var dataJson = Encoding.UTF8.GetString(dataBytes);
            
            var licenseData = JsonSerializer.Deserialize<LicenseDataDto>(dataJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (licenseData == null)
            {
                return new LicenseValidationResult(false, "Invalid license data");
            }

            // Verify signature (simplified for demo - in production use proper RSA verification)
            if (!VerifySignature(dataBytes, Convert.FromBase64String(signatureBase64)))
            {
                return new LicenseValidationResult(false, "License signature verification failed");
            }

            // Validate license content
            var validationErrors = new List<string>();
            
            // Check expiry
            if (licenseData.ExpiryDate.HasValue && licenseData.ExpiryDate.Value < DateTime.UtcNow)
            {
                validationErrors.Add("License has expired");
            }

            // Check machine binding (simplified)
            var currentMachineId = GetMachineId();
            if (!string.IsNullOrEmpty(licenseData.MachineId) && licenseData.MachineId != currentMachineId)
            {
                validationErrors.Add("License is bound to a different machine");
            }

            // Create license info
            var licenseInfo = new LicenseInfo(
                LicenseKey: licenseKey,
                UserEmail: licenseData.UserEmail,
                UserName: licenseData.UserName,
                LicenseType: licenseData.LicenseType,
                IssueDate: licenseData.IssueDate,
                ExpiryDate: licenseData.ExpiryDate,
                IsActivated: true,
                MachineId: currentMachineId,
                MaxActivations: licenseData.MaxActivations
            );

            var isValid = validationErrors.Count == 0;
            var message = isValid ? "License is valid" : string.Join("; ", validationErrors);

            return new LicenseValidationResult(isValid, message, licenseInfo, validationErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return new LicenseValidationResult(false, "Error validating license: " + ex.Message);
        }
    }

    public async Task<bool> ActivateLicenseAsync(string licenseKey, string userEmail)
    {
        try
        {
            var validationResult = await ValidateLicenseAsync(licenseKey);
            
            if (!validationResult.IsValid || validationResult.LicenseInfo == null)
            {
                _logger.LogWarning("Failed to activate invalid license");
                return false;
            }

            // In a real implementation, you'd make an API call to activate the license
            // For demo purposes, we'll just store it locally
            
            _currentLicense = validationResult.LicenseInfo;
            await SaveLicenseAsync(_currentLicense);
            
            _logger.LogInformation("License activated successfully for user: {UserEmail}", userEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating license");
            return false;
        }
    }

    public async Task<LicenseInfo?> GetCurrentLicenseAsync()
    {
        if (_currentLicense != null)
            return _currentLicense;

        await LoadCurrentLicenseAsync();
        return _currentLicense;
    }

    public async Task DeactivateLicenseAsync()
    {
        try
        {
            _currentLicense = null;
            var licenseFile = Path.Combine(_configService.GetConfigDirectory(), "license.json");
            
            if (File.Exists(licenseFile))
            {
                File.Delete(licenseFile);
            }
            
            _logger.LogInformation("License deactivated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating license");
        }
    }

    public bool IsLicenseValid()
    {
        if (_currentLicense == null)
            return false;

        if (_currentLicense.ExpiryDate.HasValue && _currentLicense.ExpiryDate.Value < DateTime.UtcNow)
            return false;

        return _currentLicense.IsActivated;
    }

    public LicenseFeatures GetAvailableFeatures()
    {
        if (!IsLicenseValid() || _currentLicense == null)
        {
            // Trial/Demo features
            return new LicenseFeatures(
                CanAccessCategoryRecommender: true,
                CanAccessKeywordResearch: true,
                CanAccessCompetitionAnalysis: false,
                CanAccessAmsGenerator: false,
                CanAccessReverseAsin: false,
                CanExportData: false,
                CanAccessLiveData: false,
                MaxDailyQueries: 10,
                MaxCategoriesPerQuery: 5
            );
        }

        return _currentLicense.LicenseType switch
        {
            LicenseType.Basic => new LicenseFeatures(
                CanAccessCategoryRecommender: true,
                CanAccessKeywordResearch: true,
                CanAccessCompetitionAnalysis: true,
                CanAccessAmsGenerator: false,
                CanAccessReverseAsin: false,
                CanExportData: true,
                CanAccessLiveData: false,
                MaxDailyQueries: 100,
                MaxCategoriesPerQuery: 20
            ),
            LicenseType.Professional => new LicenseFeatures(
                CanAccessCategoryRecommender: true,
                CanAccessKeywordResearch: true,
                CanAccessCompetitionAnalysis: true,
                CanAccessAmsGenerator: true,
                CanAccessReverseAsin: true,
                CanExportData: true,
                CanAccessLiveData: true,
                MaxDailyQueries: 1000,
                MaxCategoriesPerQuery: 100
            ),
            LicenseType.Enterprise => new LicenseFeatures(
                CanAccessCategoryRecommender: true,
                CanAccessKeywordResearch: true,
                CanAccessCompetitionAnalysis: true,
                CanAccessAmsGenerator: true,
                CanAccessReverseAsin: true,
                CanExportData: true,
                CanAccessLiveData: true,
                MaxDailyQueries: int.MaxValue,
                MaxCategoriesPerQuery: int.MaxValue
            ),
            _ => GetAvailableFeatures() // Fallback to trial
        };
    }

    private async Task LoadCurrentLicenseAsync()
    {
        try
        {
            var licenseFile = Path.Combine(_configService.GetConfigDirectory(), "license.json");
            
            if (!File.Exists(licenseFile))
                return;

            var json = await File.ReadAllTextAsync(licenseFile);
            _currentLicense = JsonSerializer.Deserialize<LicenseInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogInformation("Loaded license for user: {UserEmail}", _currentLicense?.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading license");
        }
    }

    private async Task SaveLicenseAsync(LicenseInfo license)
    {
        try
        {
            _configService.EnsureDirectoriesExist();
            var licenseFile = Path.Combine(_configService.GetConfigDirectory(), "license.json");
            
            var json = JsonSerializer.Serialize(license, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(licenseFile, json);
            _logger.LogInformation("License saved for user: {UserEmail}", license.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving license");
            throw;
        }
    }

    private bool VerifySignature(byte[] data, byte[] signature)
    {
        try
        {
            // Simplified signature verification for demo
            // In production, use proper RSA signature verification
            var expectedSignature = ComputeSimpleHash(data);
            return Convert.ToBase64String(expectedSignature) == Convert.ToBase64String(signature);
        }
        catch
        {
            return false;
        }
    }

    private byte[] ComputeSimpleHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    private string GetMachineId()
    {
        try
        {
            // Create a simple machine identifier
            var machineInfo = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo));
            return Convert.ToBase64String(hash)[..16]; // Use first 16 characters
        }
        catch
        {
            return "UNKNOWN-MACHINE";
        }
    }
}

// DTO for license data serialization
internal record LicenseDataDto(
    string UserEmail,
    string UserName,
    LicenseType LicenseType,
    DateTime IssueDate,
    DateTime? ExpiryDate,
    string MachineId,
    int MaxActivations
);
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KDP_CATEGORY_RANKERApp.Services;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class UpdateNotificationViewModel : ObservableObject
{
    private readonly IAutoUpdateService _updateService;
    private readonly ILogger<UpdateNotificationViewModel> _logger;

    [ObservableProperty]
    private UpdateInfo? availableUpdate;

    [ObservableProperty]
    private bool isUpdateAvailable;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private int downloadProgress;

    [ObservableProperty]
    private string downloadStatus = "";

    [ObservableProperty]
    private bool showUpdateDialog;

    public UpdateNotificationViewModel(IAutoUpdateService updateService, ILogger<UpdateNotificationViewModel> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Manually checking for updates...");
            
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            
            if (updateInfo != null && _updateService.IsUpdateAvailable(updateInfo))
            {
                AvailableUpdate = updateInfo;
                IsUpdateAvailable = true;
                ShowUpdateDialog = true;
                
                _logger.LogInformation("Update available: {Version}", updateInfo.Version);
            }
            else
            {
                _logger.LogInformation("No updates available");
                // Could show a "No updates available" message
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (AvailableUpdate == null) return;

        try
        {
            IsDownloading = true;
            ShowUpdateDialog = false;
            DownloadStatus = "Preparing download...";

            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadProgress = p.ProgressPercentage;
                DownloadStatus = p.Status;
            });

            var success = await _updateService.DownloadAndInstallUpdateAsync(AvailableUpdate, progress);
            
            if (!success)
            {
                DownloadStatus = "Update failed. Please try again later.";
                _logger.LogError("Update installation failed");
            }
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error during update installation");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        ShowUpdateDialog = false;
        IsUpdateAvailable = false;
        
        // Save dismissal to avoid showing again for this version
        if (AvailableUpdate != null)
        {
            SaveDismissedVersion(AvailableUpdate.Version);
        }
    }

    [RelayCommand]
    private void RemindLater()
    {
        ShowUpdateDialog = false;
        // Don't set IsUpdateAvailable = false, so the notification badge remains
    }

    private void SaveDismissedVersion(string version)
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "KDP Category Ranker");
            Directory.CreateDirectory(appFolder);
            
            var dismissedFile = Path.Combine(appFolder, "dismissed-updates.txt");
            File.AppendAllText(dismissedFile, $"{version}\n");
            
            _logger.LogInformation("Dismissed update version {Version}", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save dismissed version");
        }
    }

    public async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            // Don't check too frequently - only if last check was more than 24 hours ago
            var lastCheckFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KDP Category Ranker", 
                "last-update-check.txt"
            );

            if (File.Exists(lastCheckFile))
            {
                var lastCheckText = File.ReadAllText(lastCheckFile);
                if (DateTime.TryParse(lastCheckText, out var lastCheck))
                {
                    if (DateTime.Now - lastCheck < TimeSpan.FromHours(24))
                    {
                        _logger.LogInformation("Skipping update check - last check was {LastCheck}", lastCheck);
                        return;
                    }
                }
            }

            var updateInfo = await _updateService.CheckForUpdatesAsync();
            
            if (updateInfo != null && _updateService.IsUpdateAvailable(updateInfo))
            {
                // Check if this version was previously dismissed
                if (!IsVersionDismissed(updateInfo.Version))
                {
                    AvailableUpdate = updateInfo;
                    IsUpdateAvailable = true;
                    
                    // Auto-show dialog for critical updates
                    if (updateInfo.IsCritical)
                    {
                        ShowUpdateDialog = true;
                    }
                    
                    _logger.LogInformation("Update available on startup: {Version} (Critical: {IsCritical})", 
                        updateInfo.Version, updateInfo.IsCritical);
                }
            }

            // Save last check time
            Directory.CreateDirectory(Path.GetDirectoryName(lastCheckFile)!);
            File.WriteAllText(lastCheckFile, DateTime.Now.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup update check");
        }
    }

    private bool IsVersionDismissed(string version)
    {
        try
        {
            var dismissedFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KDP Category Ranker", 
                "dismissed-updates.txt"
            );

            if (!File.Exists(dismissedFile)) return false;
            
            var dismissedVersions = File.ReadAllLines(dismissedFile);
            return dismissedVersions.Contains(version);
        }
        catch
        {
            return false;
        }
    }
}
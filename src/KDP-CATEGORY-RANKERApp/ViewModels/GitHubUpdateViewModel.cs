using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KDP_CATEGORY_RANKERApp.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class GitHubUpdateViewModel : ObservableObject
{
    private readonly IGitHubUpdateService _gitHubUpdateService;
    private readonly ILogger<GitHubUpdateViewModel> _logger;

    [ObservableProperty]
    private bool isCheckingForUpdates = false;

    [ObservableProperty]
    private bool isUpdateAvailable = false;

    [ObservableProperty]
    private bool isDownloadingUpdate = false;

    [ObservableProperty]
    private int downloadProgress = 0;

    [ObservableProperty]
    private string currentVersion = "Loading...";

    [ObservableProperty]
    private string latestVersion = "";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private GitHubRelease? latestRelease;

    [ObservableProperty]
    private bool showUpdateDialog = false;

    [ObservableProperty]
    private string repositoryUrl = "";

    public GitHubUpdateViewModel(IGitHubUpdateService gitHubUpdateService, ILogger<GitHubUpdateViewModel> logger)
    {
        _gitHubUpdateService = gitHubUpdateService;
        _logger = logger;
        
        RepositoryUrl = _gitHubUpdateService.GetRepositoryUrl();
        
        _ = LoadCurrentVersionAsync();
        _ = CheckForUpdatesOnStartupAsync();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates) return;

        try
        {
            IsCheckingForUpdates = true;
            StatusMessage = "Checking for updates...";

            var release = await _gitHubUpdateService.GetLatestReleaseAsync();
            
            if (release != null)
            {
                LatestRelease = release;
                LatestVersion = release.TagName.TrimStart('v');
                
                var isAvailable = await _gitHubUpdateService.IsUpdateAvailableAsync();
                IsUpdateAvailable = isAvailable;
                
                if (isAvailable)
                {
                    StatusMessage = $"Update available: v{LatestVersion}";
                    ShowUpdateDialog = true;
                    _logger.LogInformation("Update available: {Version}", LatestVersion);
                }
                else
                {
                    StatusMessage = "You're using the latest version";
                    _logger.LogInformation("No updates available");
                }
            }
            else
            {
                StatusMessage = "Could not check for updates";
                _logger.LogWarning("Failed to fetch latest release");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error checking for updates: {ex.Message}";
            _logger.LogError(ex, "Error during update check");
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallUpdateAsync()
    {
        if (IsDownloadingUpdate || LatestRelease == null) return;

        try
        {
            IsDownloadingUpdate = true;
            DownloadProgress = 0;
            StatusMessage = "Preparing download...";

            var downloadInfo = await _gitHubUpdateService.GetUpdateDownloadInfoAsync();
            
            if (downloadInfo == null)
            {
                StatusMessage = "Could not find download for this update";
                return;
            }

            StatusMessage = $"Downloading {downloadInfo.FileName}...";
            
            var success = await DownloadFileWithProgressAsync(downloadInfo);
            
            if (success)
            {
                StatusMessage = "Download completed! Installing update...";
                await InstallUpdateAsync(downloadInfo);
            }
            else
            {
                StatusMessage = "Download failed. Please try again.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error downloading update: {ex.Message}";
            _logger.LogError(ex, "Error during update download");
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        ShowUpdateDialog = false;
        IsUpdateAvailable = false;
        StatusMessage = "Update dismissed";
        
        // Save dismissal preference
        SaveUpdateDismissal();
    }

    [RelayCommand]
    private void RemindLater()
    {
        ShowUpdateDialog = false;
        StatusMessage = "Will remind you later";
    }

    [RelayCommand]
    private void OpenGitHubReleases()
    {
        try
        {
            var releasesUrl = $"{RepositoryUrl}/releases";
            Process.Start(new ProcessStartInfo
            {
                FileName = releasesUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening GitHub releases page");
            StatusMessage = "Could not open releases page";
        }
    }

    [RelayCommand]
    private void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening repository");
            StatusMessage = "Could not open repository";
        }
    }

    private async Task LoadCurrentVersionAsync()
    {
        try
        {
            CurrentVersion = await _gitHubUpdateService.GetCurrentVersionAsync();
            _logger.LogInformation("Current version: {Version}", CurrentVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading current version");
            CurrentVersion = "Unknown";
        }
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        // Don't check immediately on startup, wait a bit
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        try
        {
            // Check if we should auto-check (not too frequently)
            var lastCheckFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KDP Category Ranker",
                "last-update-check.txt"
            );

            var shouldCheck = true;
            if (File.Exists(lastCheckFile))
            {
                var lastCheckText = await File.ReadAllTextAsync(lastCheckFile);
                if (DateTime.TryParse(lastCheckText, out var lastCheck))
                {
                    // Only check once per day
                    shouldCheck = DateTime.Now - lastCheck > TimeSpan.FromHours(24);
                }
            }

            if (shouldCheck)
            {
                var isAvailable = await _gitHubUpdateService.IsUpdateAvailableAsync();
                
                if (isAvailable)
                {
                    var release = await _gitHubUpdateService.GetLatestReleaseAsync();
                    if (release != null && !IsUpdateDismissed(release.TagName))
                    {
                        LatestRelease = release;
                        LatestVersion = release.TagName.TrimStart('v');
                        IsUpdateAvailable = true;
                        StatusMessage = $"Update available: v{LatestVersion}";
                        
                        // Don't auto-show dialog on startup, just set the flag
                        _logger.LogInformation("Background update check found version {Version}", LatestVersion);
                    }
                }

                // Save last check time
                Directory.CreateDirectory(Path.GetDirectoryName(lastCheckFile)!);
                await File.WriteAllTextAsync(lastCheckFile, DateTime.Now.ToString("O"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup update check");
        }
    }

    private async Task<bool> DownloadFileWithProgressAsync(UpdateDownloadInfo downloadInfo)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(downloadInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? downloadInfo.FileSize;
            var downloadedBytes = 0L;

            var tempDir = Path.Combine(Path.GetTempPath(), "KDP-Category-Ranker-Update");
            Directory.CreateDirectory(tempDir);
            
            var downloadPath = Path.Combine(tempDir, downloadInfo.FileName);

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            var bytesRead = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                var progressPercentage = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0;
                DownloadProgress = progressPercentage;
                StatusMessage = $"Downloaded {downloadedBytes:N0} / {totalBytes:N0} bytes ({progressPercentage}%)";
            }

            _logger.LogInformation("Downloaded update to {DownloadPath}", downloadPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update file");
            return false;
        }
    }

    private async Task InstallUpdateAsync(UpdateDownloadInfo downloadInfo)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "KDP-Category-Ranker-Update");
            var downloadPath = Path.Combine(tempDir, downloadInfo.FileName);
            
            if (!File.Exists(downloadPath))
            {
                StatusMessage = "Downloaded file not found";
                return;
            }

            var currentExePath = Environment.ProcessPath ?? 
                               Process.GetCurrentProcess().MainModule?.FileName ??
                               System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrEmpty(currentExePath))
            {
                StatusMessage = "Could not determine current executable path";
                return;
            }

            // Create batch file to handle the update after this process exits
            var updateBatchPath = Path.Combine(tempDir, "update.bat");
            var batchContent = $@"
@echo off
echo Updating KDP Category Ranker...
timeout /t 3 /nobreak >nul

rem Backup current version
if exist ""{currentExePath}"" (
    copy ""{currentExePath}"" ""{currentExePath}.backup"" >nul
)

rem Replace with new version
copy ""{downloadPath}"" ""{currentExePath}"" >nul
if %errorlevel% == 0 (
    echo Update completed successfully!
    
    rem Clean up
    del ""{downloadPath}"" >nul 2>&1
    del ""{currentExePath}.backup"" >nul 2>&1
    
    rem Restart application
    echo Restarting application...
    timeout /t 2 /nobreak >nul
    start """" ""{currentExePath}""
) else (
    echo Update failed! Restoring backup...
    if exist ""{currentExePath}.backup"" (
        copy ""{currentExePath}.backup"" ""{currentExePath}"" >nul
        del ""{currentExePath}.backup"" >nul
    )
    pause
)

rem Clean up batch file
del ""%~f0"" >nul 2>&1
";

            await File.WriteAllTextAsync(updateBatchPath, batchContent);

            // Start the update process
            Process.Start(new ProcessStartInfo
            {
                FileName = updateBatchPath,
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = false
            });

            // Exit current application
            StatusMessage = "Installing update and restarting...";
            await Task.Delay(1000); // Give UI time to update
            
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing update");
            StatusMessage = $"Installation failed: {ex.Message}";
        }
    }

    private void SaveUpdateDismissal()
    {
        try
        {
            if (LatestRelease == null) return;
            
            var dismissedFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KDP Category Ranker",
                "dismissed-updates.txt"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(dismissedFile)!);
            File.AppendAllText(dismissedFile, $"{LatestRelease.TagName}\n");
            
            _logger.LogInformation("Dismissed update {Version}", LatestRelease.TagName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving update dismissal");
        }
    }

    private bool IsUpdateDismissed(string version)
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace KDP_CATEGORY_RANKERApp.Services;

public interface IAutoUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<DownloadProgress>? progress = null);
    string GetCurrentVersion();
    bool IsUpdateAvailable(UpdateInfo updateInfo);
}

public record UpdateInfo(
    string Version,
    string DownloadUrl,
    string ReleaseNotes,
    DateTime ReleaseDate,
    long FileSize,
    string Checksum,
    bool IsCritical = false
);

public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    int ProgressPercentage,
    string Status
);

public class AutoUpdateService : IAutoUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AutoUpdateService> _logger;
    private readonly IPortableConfigService _configService;
    private readonly IConfiguration _configuration;

    private readonly string _updateCheckUrl;
    private const string CURRENT_VERSION = "1.0.0"; // This should be read from assembly or config

    public AutoUpdateService(
        HttpClient httpClient,
        ILogger<AutoUpdateService> logger,
        IPortableConfigService configService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configService = configService;
        _configuration = configuration;

        // Get repository info from configuration with fallbacks
        var owner = _configuration["GitHub:Owner"] ??
                     DetectRepositoryOwner() ??
                     Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_OWNER") ??
                     "kdp-category-ranker";
        var repository = _configuration["GitHub:Repository"] ?? "KDP-category-ranker";
        _updateCheckUrl = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "KDP-Category-Ranker-AutoUpdater/1.0");

        _logger.LogInformation("Auto-update service configured for {Owner}/{Repository}", owner, repository);
    }

    public string GetCurrentVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? CURRENT_VERSION;
        }
        catch
        {
            return CURRENT_VERSION;
        }
    }

    public bool IsUpdateAvailable(UpdateInfo updateInfo)
    {
        try
        {
            var currentVersion = new Version(GetCurrentVersion());
            var latestVersion = new Version(updateInfo.Version);
            
            return latestVersion > currentVersion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions: Current={Current}, Latest={Latest}", 
                GetCurrentVersion(), updateInfo.Version);
            return false;
        }
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates...");
            
            var response = await _httpClient.GetAsync(_updateCheckUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to check for updates. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release == null)
            {
                _logger.LogWarning("Failed to parse release information");
                return null;
            }

            // Find the Windows executable asset
            var asset = release.Assets?.FirstOrDefault(a => 
                a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true ||
                a.Name?.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) == true);

            if (asset == null)
            {
                _logger.LogWarning("No downloadable assets found in release");
                return null;
            }

            var updateInfo = new UpdateInfo(
                Version: release.TagName?.TrimStart('v') ?? "Unknown",
                DownloadUrl: asset.BrowserDownloadUrl ?? "",
                ReleaseNotes: release.Body ?? "No release notes available",
                ReleaseDate: release.PublishedAt,
                FileSize: asset.Size,
                Checksum: "", // GitHub doesn't provide checksums by default
                IsCritical: release.Body?.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase) == true
            );

            _logger.LogInformation("Found release: {Version} ({Size:N0} bytes)", 
                updateInfo.Version, updateInfo.FileSize);

            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<DownloadProgress>? progress = null)
    {
        try
        {
            _logger.LogInformation("Starting download of update {Version}", updateInfo.Version);
            
            var tempDir = Path.Combine(Path.GetTempPath(), "KDP-Category-Ranker-Update");
            Directory.CreateDirectory(tempDir);
            
            var fileName = Path.GetFileName(new Uri(updateInfo.DownloadUrl).LocalPath);
            var downloadPath = Path.Combine(tempDir, fileName);

            // Download the update
            progress?.Report(new DownloadProgress(0, updateInfo.FileSize, 0, "Starting download..."));
            
            using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSize;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            var bytesRead = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                var progressPercentage = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0;
                progress?.Report(new DownloadProgress(
                    downloadedBytes, 
                    totalBytes, 
                    progressPercentage, 
                    $"Downloaded {downloadedBytes:N0} / {totalBytes:N0} bytes"
                ));
            }

            progress?.Report(new DownloadProgress(downloadedBytes, totalBytes, 100, "Download completed. Preparing installation..."));

            // Install the update
            await InstallUpdateAsync(downloadPath, fileName);
            
            _logger.LogInformation("Update {Version} installed successfully", updateInfo.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading/installing update");
            progress?.Report(new DownloadProgress(0, 0, 0, $"Error: {ex.Message}"));
            return false;
        }
    }

    private async Task InstallUpdateAsync(string updatePath, string fileName)
    {
        try
        {
            if (fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                // MSI installer
                await InstallMsiAsync(updatePath);
            }
            else if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // Portable executable - replace current exe
                await InstallPortableAsync(updatePath);
            }
            else
            {
                throw new NotSupportedException($"Unsupported update file type: {fileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during installation of {UpdatePath}", updatePath);
            throw;
        }
    }

    private async Task InstallMsiAsync(string msiPath)
    {
        _logger.LogInformation("Installing MSI update: {MsiPath}", msiPath);
        
        // Create a batch file to install after this process exits
        var batchPath = Path.Combine(Path.GetTempPath(), "kdp-update.bat");
        var batchContent = $@"
@echo off
echo Installing KDP Category Ranker update...
timeout /t 2 /nobreak >nul
msiexec /i ""{msiPath}"" /quiet /norestart
if %errorlevel% == 0 (
    echo Update installed successfully!
    timeout /t 3 /nobreak >nul
) else (
    echo Update installation failed!
    pause
)
del ""{msiPath}""
del ""%~f0""
";

        await File.WriteAllTextAsync(batchPath, batchContent);
        
        // Start the batch file and exit this application
        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            WindowStyle = ProcessWindowStyle.Normal,
            CreateNoWindow = false
        });
        
        // Exit current application to allow update
        System.Windows.Application.Current.Shutdown();
    }

    private async Task InstallPortableAsync(string exePath)
    {
        _logger.LogInformation("Installing portable update: {ExePath}", exePath);
        
        var currentExePath = Environment.ProcessPath ?? 
                           Process.GetCurrentProcess().MainModule?.FileName ??
                           System.Reflection.Assembly.GetExecutingAssembly().Location;
        
        if (string.IsNullOrEmpty(currentExePath))
        {
            throw new InvalidOperationException("Unable to determine current executable path");
        }

        var backupPath = currentExePath + ".backup";
        var updateBatchPath = Path.Combine(Path.GetTempPath(), "kdp-portable-update.bat");
        
        // Create batch file to handle the update after this process exits
        var batchContent = $@"
@echo off
echo Updating KDP Category Ranker...
timeout /t 2 /nobreak >nul

rem Backup current version
if exist ""{currentExePath}"" (
    copy ""{currentExePath}"" ""{backupPath}"" >nul
)

rem Replace with new version
copy ""{exePath}"" ""{currentExePath}"" >nul
if %errorlevel% == 0 (
    echo Update completed successfully!
    rem Clean up
    del ""{exePath}"" >nul 2>&1
    del ""{backupPath}"" >nul 2>&1
    
    rem Restart application
    echo Restarting application...
    timeout /t 2 /nobreak >nul
    start """" ""{currentExePath}""
) else (
    echo Update failed! Restoring backup...
    if exist ""{backupPath}"" (
        copy ""{backupPath}"" ""{currentExePath}"" >nul
        del ""{backupPath}"" >nul
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
        System.Windows.Application.Current.Shutdown();
    }

    private string? DetectRepositoryOwner()
    {
        try
        {
            // Try to detect from git remote origin if available
            var gitConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".git", "config");
            if (File.Exists(gitConfigPath))
            {
                var gitConfig = File.ReadAllText(gitConfigPath);
                var match = System.Text.RegularExpressions.Regex.Match(
                    gitConfig,
                    @"url = https://github\.com/([^/]+)/",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not detect repository owner");
            return null;
        }
    }
}

// GitHub API response models
internal record GitHubRelease(
    string? TagName,
    string? Name,
    string? Body,
    bool Draft,
    bool Prerelease,
    DateTime PublishedAt,
    GitHubAsset[]? Assets
);

internal record GitHubAsset(
    string? Name,
    string? BrowserDownloadUrl,
    long Size,
    string? ContentType
);
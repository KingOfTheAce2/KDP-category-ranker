using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Reflection;

namespace KDP_CATEGORY_RANKERApp.Services;

public interface IGitHubUpdateService
{
    Task<GitHubRelease?> GetLatestReleaseAsync();
    Task<bool> IsUpdateAvailableAsync();
    Task<string> GetCurrentVersionAsync();
    Task<UpdateDownloadInfo?> GetUpdateDownloadInfoAsync();
    string GetRepositoryUrl();
}

public record GitHubRelease(
    string TagName,
    string Name,
    string Body,
    bool Prerelease,
    DateTime PublishedAt,
    GitHubAsset[] Assets
);

public record GitHubAsset(
    string Name,
    string BrowserDownloadUrl,
    long Size,
    string ContentType
);

public record UpdateDownloadInfo(
    string Version,
    string DownloadUrl,
    long FileSize,
    string FileName,
    bool IsPortable
);

public class GitHubUpdateService : IGitHubUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubUpdateService> _logger;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;
    private readonly string _apiUrl;

    public GitHubUpdateService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<GitHubUpdateService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        // Get repository info from configuration or detect from assembly
        _repositoryOwner = _configuration["GitHub:Owner"] ?? DetectRepositoryOwner();
        _repositoryName = _configuration["GitHub:Repository"] ?? "KDP-category-ranker";
        _apiUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases/latest";
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "KDP-Category-Ranker-UpdateChecker/1.0");
        
        _logger.LogInformation("GitHub update service initialized for {Owner}/{Repository}", _repositoryOwner, _repositoryName);
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            _logger.LogInformation("Checking for latest release at {ApiUrl}", _apiUrl);
            
            var response = await _httpClient.GetAsync(_apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, options);
            
            if (release != null)
            {
                _logger.LogInformation("Found release {Version} published on {Date}", 
                    release.TagName, release.PublishedAt);
            }
            
            return release;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest release from GitHub");
            return null;
        }
    }

    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            var latestRelease = await GetLatestReleaseAsync();
            if (latestRelease == null) return false;
            
            var currentVersion = await GetCurrentVersionAsync();
            var latestVersion = latestRelease.TagName.TrimStart('v');
            
            var current = new Version(currentVersion);
            var latest = new Version(latestVersion);
            
            var isAvailable = latest > current;
            
            _logger.LogInformation("Update check: Current={Current}, Latest={Latest}, Available={Available}", 
                currentVersion, latestVersion, isAvailable);
                
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return false;
        }
    }

    public async Task<string> GetCurrentVersionAsync()
    {
        try
        {
            // Try to get version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            
            // Fallback to configuration
            var configVersion = _configuration["Application:Version"];
            if (!string.IsNullOrEmpty(configVersion))
            {
                return configVersion;
            }
            
            // Final fallback
            return "1.0.0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current version");
            return "1.0.0";
        }
    }

    public async Task<UpdateDownloadInfo?> GetUpdateDownloadInfoAsync()
    {
        try
        {
            var release = await GetLatestReleaseAsync();
            if (release == null) return null;
            
            // Determine if we're running the portable or optimized version
            var currentExePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var isOptimized = currentExePath.Contains("Optimized", StringComparison.OrdinalIgnoreCase);
            var isPortable = !isOptimized; // Default to portable if not clearly optimized
            
            // Find the appropriate asset
            GitHubAsset? targetAsset = null;
            
            if (isOptimized)
            {
                targetAsset = release.Assets.FirstOrDefault(a => 
                    a.Name.Contains("Optimized", StringComparison.OrdinalIgnoreCase) && 
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            }
            
            if (targetAsset == null && isPortable)
            {
                targetAsset = release.Assets.FirstOrDefault(a => 
                    a.Name.Contains("Portable", StringComparison.OrdinalIgnoreCase) && 
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            }
            
            // Fallback to any .exe file
            if (targetAsset == null)
            {
                targetAsset = release.Assets.FirstOrDefault(a => 
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            }
            
            if (targetAsset == null)
            {
                _logger.LogWarning("No suitable download asset found in release {Version}", release.TagName);
                return null;
            }
            
            var downloadInfo = new UpdateDownloadInfo(
                Version: release.TagName.TrimStart('v'),
                DownloadUrl: targetAsset.BrowserDownloadUrl,
                FileSize: targetAsset.Size,
                FileName: targetAsset.Name,
                IsPortable: targetAsset.Name.Contains("Portable", StringComparison.OrdinalIgnoreCase)
            );
            
            _logger.LogInformation("Update download info: {FileName} ({Size:N0} bytes)", 
                downloadInfo.FileName, downloadInfo.FileSize);
                
            return downloadInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting update download info");
            return null;
        }
    }

    public string GetRepositoryUrl()
    {
        return $"https://github.com/{_repositoryOwner}/{_repositoryName}";
    }

    private string DetectRepositoryOwner()
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
            
            // Fallback from configuration
            return _configuration["GitHub:DefaultOwner"] ?? "your-username";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not detect repository owner, using default");
            return "your-username";
        }
    }
}
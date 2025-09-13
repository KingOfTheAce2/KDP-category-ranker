using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KDP_CATEGORY_RANKERApp.Services;

public interface IPortableConfigService
{
    string GetDataDirectory();
    string GetConfigDirectory();
    bool IsPortableMode();
    void EnsureDirectoriesExist();
    Task<T?> LoadConfigAsync<T>(string fileName) where T : class;
    Task SaveConfigAsync<T>(string fileName, T config) where T : class;
}

public class PortableConfigService : IPortableConfigService
{
    private readonly ILogger<PortableConfigService> _logger;
    private readonly string _applicationDirectory;
    private readonly string _portableDataDirectory;
    private readonly string _roamingDataDirectory;

    public PortableConfigService(ILogger<PortableConfigService> logger)
    {
        _logger = logger;
        _applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _portableDataDirectory = Path.Combine(_applicationDirectory, "Data");
        _roamingDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KDP Category Ranker"
        );
    }

    public bool IsPortableMode()
    {
        var portableMarkerFile = Path.Combine(_applicationDirectory, "portable.txt");
        var hasPortableMarker = File.Exists(portableMarkerFile);
        
        var isInProgramFiles = _applicationDirectory.Contains("Program Files", StringComparison.OrdinalIgnoreCase);
        
        var isPortable = hasPortableMarker || !isInProgramFiles;
        
        _logger.LogInformation("Portable mode detection: {IsPortable} (Marker: {HasMarker}, ProgramFiles: {InProgramFiles})", 
            isPortable, hasPortableMarker, isInProgramFiles);
            
        return isPortable;
    }

    public string GetDataDirectory()
    {
        return IsPortableMode() ? _portableDataDirectory : _roamingDataDirectory;
    }

    public string GetConfigDirectory()
    {
        return GetDataDirectory();
    }

    public void EnsureDirectoriesExist()
    {
        var dataDir = GetDataDirectory();
        var configDir = GetConfigDirectory();
        
        try
        {
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(configDir);
            
            _logger.LogInformation("Ensured directories exist: Data={DataDir}, Config={ConfigDir}", dataDir, configDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directories: Data={DataDir}, Config={ConfigDir}", dataDir, configDir);
            throw;
        }
    }

    public async Task<T?> LoadConfigAsync<T>(string fileName) where T : class
    {
        var configFile = Path.Combine(GetConfigDirectory(), fileName);
        
        if (!File.Exists(configFile))
        {
            _logger.LogInformation("Config file not found: {ConfigFile}", configFile);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configFile);
            var config = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
            
            _logger.LogInformation("Loaded config from: {ConfigFile}", configFile);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config from: {ConfigFile}", configFile);
            return null;
        }
    }

    public async Task SaveConfigAsync<T>(string fileName, T config) where T : class
    {
        EnsureDirectoriesExist();
        
        var configFile = Path.Combine(GetConfigDirectory(), fileName);
        
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(configFile, json);
            _logger.LogInformation("Saved config to: {ConfigFile}", configFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config to: {ConfigFile}", configFile);
            throw;
        }
    }
}
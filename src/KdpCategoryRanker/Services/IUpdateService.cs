namespace KdpCategoryRanker.Services;

public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync();
    Task<string> GetLatestVersionAsync();
}
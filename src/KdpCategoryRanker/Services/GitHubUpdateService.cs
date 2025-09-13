using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

#nullable enable

namespace KdpCategoryRanker.Services;

public class GitHubUpdateService : IUpdateService
{
    private const string CurrentVersion = "1.0.0";
    private const string GitHubApiUrl = "https://api.github.com/repos/KingOfTheAce2/KDP-category-ranker/releases/latest";

    private readonly HttpClient _httpClient;

    public GitHubUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "KdpCategoryRanker/1.0.0");
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var latestVersion = await GetLatestVersionAsync();
            return !string.IsNullOrEmpty(latestVersion) && latestVersion != CurrentVersion;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetLatestVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl);
            var release = JsonConvert.DeserializeObject<GitHubRelease>(response);
            return release?.TagName?.TrimStart('v') ?? CurrentVersion;
        }
        catch
        {
            return CurrentVersion;
        }
    }

    private class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string? TagName { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
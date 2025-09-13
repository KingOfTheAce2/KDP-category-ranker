using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERScraping.Models;

namespace KDP_CATEGORY_RANKERScraping.Interfaces;

public interface IAmazonMarketClient
{
    Market Market { get; }
    Task<SearchResultsResponse> GetSearchResultsAsync(string keyword, int page = 1);
    Task<CategoryPageResponse> GetCategoryPageAsync(string categoryId, int page = 1);
    Task<BookDetailResponse> GetBookDetailAsync(string asin);
    Task<AlsoBoughtResponse> GetAlsoBoughtAsync(string asin);
    Task<BreadcrumbResponse> GetBreadcrumbAndNodeAsync(string url);
    Task<AutocompleteResponse> GetAutocompleteAsync(string keyword);
}

public interface IAmazonScrapingService
{
    Task<List<ScrapedBook>> SearchBooksAsync(string keyword, Market market, int maxResults = 30);
    Task<List<ScrapedBook>> GetCategoryBooksAsync(string categoryId, Market market, int maxResults = 30);
    Task<ScrapedBook?> GetBookDetailsAsync(string asin, Market market);
    Task<List<ScrapedCategory>> GetCategoriesAsync(Market market);
    Task<List<string>> GetAutocompleteKeywordsAsync(string keyword, Market market);
    Task<List<string>> GetAlsoBoughtAsinsAsync(string asin, Market market);
}

public interface IRateLimitService
{
    Task WaitIfNeededAsync();
    Task<T> ExecuteWithRateLimitAsync<T>(Func<Task<T>> operation);
    void UpdateSettings(int maxRequestsPerMinute, int delayBetweenRequestsMs);
}

public interface IScrapingCacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task ClearAsync();
    string GenerateKey(string operation, params object[] parameters);
}
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERScraping.Interfaces;
using KDP_CATEGORY_RANKERScraping.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERScraping.Services;

public class AmazonScrapingService : IAmazonScrapingService
{
    private readonly Dictionary<Market, IAmazonMarketClient> _marketClients;
    private readonly IRateLimitService _rateLimitService;
    private readonly IScrapingCacheService _cacheService;
    private readonly ILogger<AmazonScrapingService> _logger;
    private readonly bool _respectSiteRules;

    public AmazonScrapingService(
        IEnumerable<IAmazonMarketClient> marketClients,
        IRateLimitService rateLimitService,
        IScrapingCacheService cacheService,
        IConfiguration configuration,
        ILogger<AmazonScrapingService> logger)
    {
        _marketClients = marketClients.ToDictionary(c => c.Market, c => c);
        _rateLimitService = rateLimitService;
        _cacheService = cacheService;
        _logger = logger;
        _respectSiteRules = configuration.GetValue<bool>("Scraping:RespectRobotsTxt", true);
    }

    public async Task<List<ScrapedBook>> SearchBooksAsync(string keyword, Market market, int maxResults = 30)
    {
        _logger.LogInformation("Searching for books with keyword: {Keyword} in market: {Market}", keyword, market);

        if (!_marketClients.TryGetValue(market, out var client))
        {
            _logger.LogWarning("No client available for market: {Market}", market);
            return new List<ScrapedBook>();
        }

        var cacheKey = _cacheService.GenerateKey("search", keyword, market.ToString(), maxResults);
        var cached = await _cacheService.GetAsync<List<ScrapedBook>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached search results for: {Keyword}", keyword);
            return cached;
        }

        try
        {
            var allBooks = new List<ScrapedBook>();
            var page = 1;
            var resultsPerPage = Math.Min(30, maxResults);

            while (allBooks.Count < maxResults)
            {
                var response = await _rateLimitService.ExecuteWithRateLimitAsync(async () =>
                    await client.GetSearchResultsAsync(keyword, page));

                if (!response.Books.Any())
                {
                    _logger.LogDebug("No more results found on page {Page} for keyword: {Keyword}", page, keyword);
                    break;
                }

                allBooks.AddRange(response.Books);

                if (!response.HasNextPage || allBooks.Count >= maxResults)
                {
                    break;
                }

                page++;
            }

            var results = allBooks.Take(maxResults).ToList();
            await _cacheService.SetAsync(cacheKey, results, TimeSpan.FromHours(1));
            
            _logger.LogInformation("Found {Count} books for keyword: {Keyword}", results.Count, keyword);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for books with keyword: {Keyword}", keyword);
            return new List<ScrapedBook>();
        }
    }

    public async Task<List<ScrapedBook>> GetCategoryBooksAsync(string categoryId, Market market, int maxResults = 30)
    {
        _logger.LogInformation("Getting books from category: {CategoryId} in market: {Market}", categoryId, market);

        if (!_marketClients.TryGetValue(market, out var client))
        {
            return new List<ScrapedBook>();
        }

        var cacheKey = _cacheService.GenerateKey("category", categoryId, market.ToString(), maxResults);
        var cached = await _cacheService.GetAsync<List<ScrapedBook>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var allBooks = new List<ScrapedBook>();
            var page = 1;

            while (allBooks.Count < maxResults)
            {
                var response = await _rateLimitService.ExecuteWithRateLimitAsync(async () =>
                    await client.GetCategoryPageAsync(categoryId, page));

                if (!response.Books.Any())
                {
                    break;
                }

                allBooks.AddRange(response.Books);

                if (!response.HasNextPage)
                {
                    break;
                }

                page++;
            }

            var results = allBooks.Take(maxResults).ToList();
            await _cacheService.SetAsync(cacheKey, results, TimeSpan.FromHours(2));
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category books: {CategoryId}", categoryId);
            return new List<ScrapedBook>();
        }
    }

    public async Task<ScrapedBook?> GetBookDetailsAsync(string asin, Market market)
    {
        if (!_marketClients.TryGetValue(market, out var client))
        {
            return null;
        }

        var cacheKey = _cacheService.GenerateKey("book", asin, market.ToString());
        var cached = await _cacheService.GetAsync<ScrapedBook>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var response = await _rateLimitService.ExecuteWithRateLimitAsync(async () =>
                await client.GetBookDetailAsync(asin));

            await _cacheService.SetAsync(cacheKey, response.Book, TimeSpan.FromHours(6));
            return response.Book;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book details for ASIN: {ASIN}", asin);
            return null;
        }
    }

    public async Task<List<ScrapedCategory>> GetCategoriesAsync(Market market)
    {
        // TODO: Implement category crawling
        // This would be a complex operation to crawl all categories
        _logger.LogInformation("Category crawling not yet implemented for market: {Market}", market);
        return new List<ScrapedCategory>();
    }

    public async Task<List<string>> GetAutocompleteKeywordsAsync(string keyword, Market market)
    {
        if (!_marketClients.TryGetValue(market, out var client))
        {
            return new List<string>();
        }

        var cacheKey = _cacheService.GenerateKey("autocomplete", keyword, market.ToString());
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var response = await _rateLimitService.ExecuteWithRateLimitAsync(async () =>
                await client.GetAutocompleteAsync(keyword));

            await _cacheService.SetAsync(cacheKey, response.Suggestions, TimeSpan.FromHours(24));
            return response.Suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete for keyword: {Keyword}", keyword);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetAlsoBoughtAsinsAsync(string asin, Market market)
    {
        if (!_marketClients.TryGetValue(market, out var client))
        {
            return new List<string>();
        }

        var cacheKey = _cacheService.GenerateKey("alsobought", asin, market.ToString());
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var response = await _rateLimitService.ExecuteWithRateLimitAsync(async () =>
                await client.GetAlsoBoughtAsync(asin));

            var asins = response.AlsoBoughtBooks.Select(b => b.Asin).ToList();
            await _cacheService.SetAsync(cacheKey, asins, TimeSpan.FromHours(12));
            return asins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting also-bought for ASIN: {ASIN}", asin);
            return new List<string>();
        }
    }
}
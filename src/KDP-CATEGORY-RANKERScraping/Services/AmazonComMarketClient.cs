using AngleSharp;
using AngleSharp.Html.Dom;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERScraping.Interfaces;
using KDP_CATEGORY_RANKERScraping.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text.RegularExpressions;

namespace KDP_CATEGORY_RANKERScraping.Services;

public class AmazonComMarketClient : IAmazonMarketClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AmazonComMarketClient> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly string[] _userAgents;

    public Market Market => Market.AmazonCom;

    public AmazonComMarketClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AmazonComMarketClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _userAgents = configuration.GetSection("Scraping:UserAgents").Get<string[]>() ?? new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
        };

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, duration, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} after {Duration}ms due to: {Exception}",
                        retryCount, duration.TotalMilliseconds, outcome.Exception?.Message);
                });
    }

    public async Task<SearchResultsResponse> GetSearchResultsAsync(string keyword, int page = 1)
    {
        _logger.LogDebug("Searching for keyword: {Keyword}, page: {Page}", keyword, page);

        // For demo purposes, return mock data
        // TODO: Implement actual scraping logic with AngleSharp
        return new SearchResultsResponse
        {
            Books = GenerateMockBooks(keyword, page),
            TotalResults = Random.Shared.Next(100, 10000),
            CurrentPage = page,
            HasNextPage = page < 10,
            Keyword = keyword
        };
    }

    public async Task<CategoryPageResponse> GetCategoryPageAsync(string categoryId, int page = 1)
    {
        _logger.LogDebug("Getting category page: {CategoryId}, page: {Page}", categoryId, page);

        // For demo purposes, return mock data
        return new CategoryPageResponse
        {
            Books = GenerateMockBooks($"category_{categoryId}", page),
            Category = new ScrapedCategory
            {
                CategoryId = categoryId,
                Breadcrumb = $"Books > Sample Category {categoryId}",
                Name = $"Sample Category {categoryId}",
                Url = $"https://amazon.com/category/{categoryId}"
            },
            CurrentPage = page,
            HasNextPage = page < 5
        };
    }

    public async Task<BookDetailResponse> GetBookDetailAsync(string asin)
    {
        _logger.LogDebug("Getting book details for ASIN: {ASIN}", asin);

        // For demo purposes, return mock data
        var book = new ScrapedBook
        {
            Asin = asin,
            Title = $"Sample Book {asin}",
            Author = "Sample Author",
            Price = Random.Shared.Next(299, 2999) / 100m,
            Format = (BookFormat)Random.Shared.Next(0, 4),
            PagesOrMinutes = Random.Shared.Next(150, 400),
            RatingAverage = Random.Shared.NextDouble() * 2 + 3,
            RatingsCount = Random.Shared.Next(50, 5000),
            IsKindleUnlimited = Random.Shared.NextDouble() > 0.6,
            BestSellerRank = Random.Shared.Next(1000, 1000000),
            CategoryIds = new List<string> { "category_1", "category_2" },
            ImageUrl = $"https://images-na.ssl-images-amazon.com/images/I/{asin}.jpg",
            ProductUrl = $"https://amazon.com/dp/{asin}"
        };

        return new BookDetailResponse
        {
            Book = book,
            AlsoBoughtAsins = GenerateRandomAsins(10),
            ProductDetails = new Dictionary<string, string>
            {
                ["Publisher"] = "Sample Publisher",
                ["Publication Date"] = DateTime.Now.AddMonths(-Random.Shared.Next(1, 60)).ToString("yyyy-MM-dd"),
                ["Language"] = "English"
            }
        };
    }

    public async Task<AlsoBoughtResponse> GetAlsoBoughtAsync(string asin)
    {
        _logger.LogDebug("Getting also-bought for ASIN: {ASIN}", asin);

        return new AlsoBoughtResponse
        {
            SourceAsin = asin,
            AlsoBoughtBooks = GenerateMockBooks($"related_{asin}", 1, 10)
        };
    }

    public async Task<BreadcrumbResponse> GetBreadcrumbAndNodeAsync(string url)
    {
        _logger.LogDebug("Getting breadcrumb for URL: {URL}", url);

        var nodeMatch = Regex.Match(url, @"node=(\d+)");
        var nodeId = nodeMatch.Success ? nodeMatch.Groups[1].Value : "unknown";

        return new BreadcrumbResponse
        {
            Url = url,
            Breadcrumb = "Books > Sample Category > Subcategory",
            NodeId = nodeId,
            CategoryPath = new List<string> { "Books", "Sample Category", "Subcategory" }
        };
    }

    public async Task<AutocompleteResponse> GetAutocompleteAsync(string keyword)
    {
        _logger.LogDebug("Getting autocomplete for keyword: {Keyword}", keyword);

        var suggestions = new List<string>
        {
            $"{keyword} book",
            $"{keyword} guide",
            $"{keyword} manual",
            $"best {keyword}",
            $"{keyword} for beginners",
            $"{keyword} advanced",
            $"{keyword} complete",
            $"{keyword} handbook"
        };

        return new AutocompleteResponse
        {
            Query = keyword,
            Suggestions = suggestions
        };
    }

    private List<ScrapedBook> GenerateMockBooks(string context, int page, int count = 20)
    {
        var books = new List<ScrapedBook>();
        var baseRank = (page - 1) * count + 1;

        for (int i = 0; i < count; i++)
        {
            var asin = $"B{Random.Shared.Next(10000000, 99999999):X8}";
            var bsr = baseRank + i + Random.Shared.Next(0, 100);

            books.Add(new ScrapedBook
            {
                Asin = asin,
                Title = $"Book {i + 1} for {context}",
                Author = $"Author {Random.Shared.Next(1, 100)}",
                Price = Random.Shared.Next(299, 2999) / 100m,
                Format = (BookFormat)Random.Shared.Next(0, 4),
                PagesOrMinutes = Random.Shared.Next(150, 400),
                RatingAverage = Random.Shared.NextDouble() * 2 + 3,
                RatingsCount = Random.Shared.Next(10, 5000),
                IsKindleUnlimited = Random.Shared.NextDouble() > 0.7,
                PublisherType = Random.Shared.NextDouble() > 0.8 ? "Big 5" : "Indie",
                BestSellerRank = bsr,
                CategoryIds = new List<string> { $"category_{Random.Shared.Next(1, 20)}" },
                ImageUrl = $"https://images-na.ssl-images-amazon.com/images/I/{asin}.jpg",
                ProductUrl = $"https://amazon.com/dp/{asin}"
            });
        }

        return books;
    }

    private List<string> GenerateRandomAsins(int count)
    {
        var asins = new List<string>();
        for (int i = 0; i < count; i++)
        {
            asins.Add($"B{Random.Shared.Next(10000000, 99999999):X8}");
        }
        return asins;
    }
}
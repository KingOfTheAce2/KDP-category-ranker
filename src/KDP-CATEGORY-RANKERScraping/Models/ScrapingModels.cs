using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERScraping.Models;

public record ScrapedBook
{
    public string Asin { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public BookFormat Format { get; init; }
    public int PagesOrMinutes { get; init; }
    public double RatingAverage { get; init; }
    public int RatingsCount { get; init; }
    public bool IsKindleUnlimited { get; init; }
    public string PublisherType { get; init; } = "Unknown";
    public int BestSellerRank { get; init; }
    public List<string> CategoryIds { get; init; } = new();
    public string ImageUrl { get; init; } = string.Empty;
    public string ProductUrl { get; init; } = string.Empty;
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record ScrapedCategory
{
    public string CategoryId { get; init; } = string.Empty;
    public string Breadcrumb { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public bool IsGhost { get; init; }
    public string? ParentCategoryId { get; init; }
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record SearchResultsResponse
{
    public List<ScrapedBook> Books { get; init; } = new();
    public int TotalResults { get; init; }
    public int CurrentPage { get; init; }
    public bool HasNextPage { get; init; }
    public string Keyword { get; init; } = string.Empty;
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record CategoryPageResponse
{
    public List<ScrapedBook> Books { get; init; } = new();
    public ScrapedCategory Category { get; init; } = new();
    public int CurrentPage { get; init; }
    public bool HasNextPage { get; init; }
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record BookDetailResponse
{
    public ScrapedBook Book { get; init; } = new();
    public List<string> AlsoBoughtAsins { get; init; } = new();
    public Dictionary<string, string> ProductDetails { get; init; } = new();
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record AlsoBoughtResponse
{
    public string SourceAsin { get; init; } = string.Empty;
    public List<ScrapedBook> AlsoBoughtBooks { get; init; } = new();
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record BreadcrumbResponse
{
    public string Url { get; init; } = string.Empty;
    public string Breadcrumb { get; init; } = string.Empty;
    public string NodeId { get; init; } = string.Empty;
    public List<string> CategoryPath { get; init; } = new();
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}

public record AutocompleteResponse
{
    public string Query { get; init; } = string.Empty;
    public List<string> Suggestions { get; init; } = new();
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}
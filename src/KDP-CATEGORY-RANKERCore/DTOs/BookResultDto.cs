using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.DTOs;

public record BookResultDto
{
    public string Asin { get; init; } = string.Empty;
    public Market Market { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public BookFormat Format { get; init; }
    public decimal Price { get; init; }
    public int PagesOrMinutes { get; init; }
    public double RatingAverage { get; init; }
    public int RatingsCount { get; init; }
    public bool IsKindleUnlimited { get; init; }
    public string PublisherType { get; init; } = string.Empty;
    public int BestSellerRank { get; init; }
    public int EstimatedDailySales { get; init; }
    public decimal EstimatedMonthlyEarnings { get; init; }
    public List<string> CategoryIds { get; init; } = new();
    public string ImageUrl { get; init; } = string.Empty;
    public string AmazonUrl { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}

public record BookSnapshotDto
{
    public string Asin { get; init; } = string.Empty;
    public Market Market { get; init; }
    public DateTime CapturedAt { get; init; }
    public int BestSellerRank { get; init; }
    public int EstDailySales { get; init; }
    public decimal EstMonthlyEarnings { get; init; }
    public List<string> CategoryIds { get; init; } = new();
}
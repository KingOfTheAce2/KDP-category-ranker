using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.DTOs;

public record KeywordResultDto
{
    public string Keyword { get; init; } = string.Empty;
    public int CompetitiveScore { get; init; }
    public CompetitionLevel CompetitionLevel => CompetitionLevelExtensions.FromScore(CompetitiveScore);
    public int EstimatedSearchesPerMonth { get; init; }
    public decimal AvgMonthlyEarnings { get; init; }
    public TrendDirection Trend { get; init; }
    public List<Market> MarketsPresent { get; init; } = new();
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}

public record KeywordMetricDto
{
    public string Keyword { get; init; } = string.Empty;
    public Market Market { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public int Searches { get; init; }
    public int CompetitiveScore { get; init; }
    public decimal AvgMonthlyEarnings { get; init; }
    public DateTime SnapshotAt { get; init; }
}
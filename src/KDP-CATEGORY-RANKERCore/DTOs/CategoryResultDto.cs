using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.DTOs;

public record CategoryResultDto
{
    public string CategoryId { get; init; } = string.Empty;
    public string Breadcrumb { get; init; } = string.Empty;
    public Market Market { get; init; }
    public int SalesToReachRank1 { get; init; }
    public int SalesToReachRank10 { get; init; }
    public decimal AvgPriceIndie { get; init; }
    public decimal AvgPriceBig5 { get; init; }
    public double AvgRating { get; init; }
    public int AvgPageCount { get; init; }
    public int AvgAgeDays { get; init; }
    public double PercentageLargePublisher { get; init; }
    public double PercentageKindleUnlimited { get; init; }
    public bool IsGhost { get; init; }
    public bool IsDuplicate { get; init; }
    public string? DuplicateGroupId { get; init; }
    public TrendDirection Trend { get; init; }
    public double GrowthPercentage { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}

public record CategorySnapshotDto
{
    public string CategoryId { get; init; } = string.Empty;
    public Market Market { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public int SalesToNo1 { get; init; }
    public int SalesToNo10 { get; init; }
    public decimal AvgPriceIndie { get; init; }
    public decimal AvgPriceBig { get; init; }
    public double AvgRating { get; init; }
    public int AvgPageCount { get; init; }
    public int AvgAgeDays { get; init; }
    public double KuPercentage { get; init; }
    public double LargePublisherPercentage { get; init; }
    public DateTime SnapshotAt { get; init; }
}
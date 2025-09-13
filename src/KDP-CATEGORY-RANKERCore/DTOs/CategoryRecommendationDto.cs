using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.DTOs;

public record CategoryRecommendationDto
{
    public string CategoryId { get; init; } = string.Empty;
    public string Breadcrumb { get; init; } = string.Empty;
    public Market Market { get; init; }
    
    // Difficulty Metrics
    public int DifficultyScore { get; init; } // 0-100 (0 = easiest, 100 = hardest)
    public DifficultyLevel DifficultyLevel => DifficultyLevelExtensions.FromScore(DifficultyScore);
    
    // Bestseller Requirements
    public int DailySalesForBestseller { get; init; } // Sales needed for #1 spot
    public int DailySalesForTop10 { get; init; } // Sales needed for top 10 (front page)
    public int DailySalesForTop50 { get; init; } // Sales needed for first page visibility
    
    // Market Metrics
    public decimal AveragePrice { get; init; }
    public int TotalBooksInCategory { get; init; }
    public int NewReleasesPerDay { get; init; }
    public double VolatilityScore { get; init; } // How often rankings change (0-1)
    
    // Revenue Potential
    public decimal EstimatedMonthlyRevenue { get; init; }
    public decimal EstimatedMonthlyRevenueTop10 { get; init; }
    
    // Competition Analysis
    public double BigPublisherPercentage { get; init; }
    public double KindleUnlimitedPercentage { get; init; }
    public double AverageRating { get; init; }
    public int AverageReviewCount { get; init; }
    
    // Timing Intelligence
    public SeasonalTrend SeasonalTrend { get; init; }
    public string BestReleaseMonths { get; init; } = string.Empty;
    public int DaysUntilOptimalRelease { get; init; }
    
    // Recommendation Score (0-100, higher = better opportunity)
    public int RecommendationScore { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
    
    // Historical Performance
    public TrendDirection SalesVolumetrend { get; init; }
    public double GrowthPercentage { get; init; }
    
    // Flags
    public bool IsGhostCategory { get; init; }
    public bool IsDuplicateCategory { get; init; }
    public bool IsHighlyCompetitive { get; init; }
    public bool IsEmerging { get; init; }
    public bool IsDecline { get; init; }
    
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}

public enum DifficultyLevel
{
    VeryEasy = 1,    // 0-20: Easy to reach bestseller
    Easy = 2,        // 21-40: Moderate effort required  
    Medium = 3,      // 41-60: Significant competition
    Hard = 4,        // 61-80: Very competitive
    VeryHard = 5     // 81-100: Extremely difficult
}

public static class DifficultyLevelExtensions
{
    public static DifficultyLevel FromScore(int score) => score switch
    {
        >= 0 and <= 20 => DifficultyLevel.VeryEasy,
        >= 21 and <= 40 => DifficultyLevel.Easy,
        >= 41 and <= 60 => DifficultyLevel.Medium,
        >= 61 and <= 80 => DifficultyLevel.Hard,
        >= 81 and <= 100 => DifficultyLevel.VeryHard,
        _ => DifficultyLevel.Medium
    };

    public static string ToDisplayName(this DifficultyLevel level) => level switch
    {
        DifficultyLevel.VeryEasy => "Very Easy",
        DifficultyLevel.Easy => "Easy",
        DifficultyLevel.Medium => "Medium",
        DifficultyLevel.Hard => "Hard",
        DifficultyLevel.VeryHard => "Very Hard",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
    
    public static string ToDescription(this DifficultyLevel level) => level switch
    {
        DifficultyLevel.VeryEasy => "Low competition, easy to reach bestseller status",
        DifficultyLevel.Easy => "Moderate competition, achievable with good marketing",
        DifficultyLevel.Medium => "Balanced competition, requires solid strategy",
        DifficultyLevel.Hard => "High competition, needs strong marketing and quality",
        DifficultyLevel.VeryHard => "Extremely competitive, dominated by established authors",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}

public enum SeasonalTrend
{
    Stable,          // No strong seasonal pattern
    SpringPeak,      // March-May peak
    SummerPeak,      // June-August peak  
    FallPeak,        // September-November peak
    WinterPeak,      // December-February peak
    HolidayDriven,   // Strong December spike
    BackToSchool,    // August-September spike
    NewYear         // January spike
}

public record CategoryRecommendationRequest
{
    public string BookTitle { get; init; } = string.Empty;
    public string BookDescription { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public BookFormat Format { get; init; } = BookFormat.Kindle;
    public decimal Price { get; init; }
    public Market TargetMarket { get; init; } = Market.AmazonCom;
    public int MaxDailySalesTarget { get; init; } = 10; // Max daily sales author thinks they can achieve
    public DateTime PlannedReleaseDate { get; init; } = DateTime.Now.AddMonths(1);
    public bool IncludeHighCompetition { get; init; } = false;
    public bool ExcludeGhostCategories { get; init; } = true;
    public DifficultyLevel MaxDifficultyLevel { get; init; } = DifficultyLevel.Medium;
}

public record BestsellerPlanningDto
{
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public Market Market { get; init; }
    
    // Current Requirements
    public int CurrentDailySalesForBestseller { get; init; }
    public int CurrentDailySalesForTop10 { get; init; }
    public int CurrentDailySalesForTop50 { get; init; }
    
    // Historical Requirements (12 month average)
    public int AverageDailySalesForBestseller { get; init; }
    public int AverageDailySalesForTop10 { get; init; }
    public int AverageDailySalesForTop50 { get; init; }
    
    // Projected Requirements (based on trends)
    public int ProjectedDailySalesForBestseller { get; init; }
    public int ProjectedDailySalesForTop10 { get; init; }
    public int ProjectedDailySalesForTop50 { get; init; }
    
    // Seasonal Adjustments
    public Dictionary<string, int> MonthlyBestsellerRequirements { get; init; } = new();
    public string EasiestMonth { get; init; } = string.Empty;
    public string HardestMonth { get; init; } = string.Empty;
    public int DaysUntilEasiestPeriod { get; init; }
    
    // Revenue Projections
    public decimal DailyRevenueAtBestseller { get; init; }
    public decimal MonthlyRevenueAtBestseller { get; init; }
    public decimal DailyRevenueAtTop10 { get; init; }
    public decimal MonthlyRevenueAtTop10 { get; init; }
    
    // Success Probability
    public double SuccessProbability { get; init; } // 0-1 based on user's target sales vs requirements
    public string RecommendedStrategy { get; init; } = string.Empty;
    public List<string> ActionItems { get; init; } = new();
    
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}
namespace KDP_CATEGORY_RANKERCore.Enums;

public enum CompetitionLevel
{
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    VeryHigh = 5
}

public static class CompetitionLevelExtensions
{
    public static CompetitionLevel FromScore(int score) => score switch
    {
        >= 0 and <= 20 => CompetitionLevel.VeryLow,
        >= 21 and <= 40 => CompetitionLevel.Low,
        >= 41 and <= 60 => CompetitionLevel.Medium,
        >= 61 and <= 80 => CompetitionLevel.High,
        >= 81 and <= 100 => CompetitionLevel.VeryHigh,
        _ => throw new ArgumentOutOfRangeException(nameof(score))
    };

    public static string ToDisplayName(this CompetitionLevel level) => level switch
    {
        CompetitionLevel.VeryLow => "Very Low",
        CompetitionLevel.Low => "Low",
        CompetitionLevel.Medium => "Medium",
        CompetitionLevel.High => "High",
        CompetitionLevel.VeryHigh => "Very High",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}
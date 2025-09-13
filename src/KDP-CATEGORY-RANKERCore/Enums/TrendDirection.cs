namespace KDP_CATEGORY_RANKERCore.Enums;

public enum TrendDirection
{
    RapidlyDeclining = -2,
    SignificantlyDeclining = -1,
    Flat = 0,
    Growing = 1,
    RapidlyGrowing = 2
}

public static class TrendDirectionExtensions
{
    public static TrendDirection FromGrowthPercentage(double growthPercentage) => growthPercentage switch
    {
        < -20.0 => TrendDirection.RapidlyDeclining,
        >= -20.0 and < -5.0 => TrendDirection.SignificantlyDeclining,
        >= -5.0 and <= 5.0 => TrendDirection.Flat,
        > 5.0 and <= 15.0 => TrendDirection.Growing,
        > 15.0 => TrendDirection.RapidlyGrowing
    };

    public static string ToDisplayName(this TrendDirection trend) => trend switch
    {
        TrendDirection.RapidlyDeclining => "Rapidly Declining",
        TrendDirection.SignificantlyDeclining => "Significantly Declining", 
        TrendDirection.Flat => "Flat",
        TrendDirection.Growing => "Growing",
        TrendDirection.RapidlyGrowing => "Rapidly Growing",
        _ => throw new ArgumentOutOfRangeException(nameof(trend))
    };

    public static string ToPercentageRange(this TrendDirection trend) => trend switch
    {
        TrendDirection.RapidlyDeclining => "(<-20%)",
        TrendDirection.SignificantlyDeclining => "(-20% to -5%)",
        TrendDirection.Flat => "(-5% to 5%)",
        TrendDirection.Growing => "(5% to 15%)",
        TrendDirection.RapidlyGrowing => "(>15%)",
        _ => throw new ArgumentOutOfRangeException(nameof(trend))
    };
}
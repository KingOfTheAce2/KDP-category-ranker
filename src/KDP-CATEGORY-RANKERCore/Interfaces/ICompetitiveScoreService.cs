using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.Interfaces;

public interface ICompetitiveScoreService
{
    Task<int> CalculateCompetitiveScoreAsync(string keyword, Market market);
    Task<CompetitiveScoreBreakdownDto> GetScoreBreakdownAsync(string keyword, Market market);
}

public record CompetitiveScoreBreakdownDto
{
    public string Keyword { get; init; } = string.Empty;
    public Market Market { get; init; }
    public int TotalScore { get; init; }
    public int SerpIntensityScore { get; init; } // 0-30
    public int KeywordUsageScore { get; init; } // 0-20
    public int BsrToughnessScore { get; init; } // 0-25
    public int SaturationScore { get; init; } // 0-15
    public int SearchVolumeScore { get; init; } // 0-10
    public CompetitionLevel Level => CompetitionLevelExtensions.FromScore(TotalScore);
    public string Explanation { get; init; } = string.Empty;
}
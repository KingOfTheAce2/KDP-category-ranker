using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERCore.Services;

public class CompetitiveScoreService : ICompetitiveScoreService
{
    private readonly ILogger<CompetitiveScoreService> _logger;
    private readonly ICompetitionAnalysisService _competitionService;
    
    private readonly double _serpIntensityWeight;
    private readonly double _keywordUsageWeight;
    private readonly double _bsrToughnessWeight;
    private readonly double _saturationWeight;
    private readonly double _searchVolumeWeight;

    public CompetitiveScoreService(
        IConfiguration configuration,
        ILogger<CompetitiveScoreService> logger,
        ICompetitionAnalysisService competitionService)
    {
        _logger = logger;
        _competitionService = competitionService;
        
        _serpIntensityWeight = configuration.GetValue<double>("CompetitiveScore:SerpIntensityWeight", 0.3);
        _keywordUsageWeight = configuration.GetValue<double>("CompetitiveScore:KeywordUsageWeight", 0.2);
        _bsrToughnessWeight = configuration.GetValue<double>("CompetitiveScore:BsrToughnessWeight", 0.25);
        _saturationWeight = configuration.GetValue<double>("CompetitiveScore:SaturationWeight", 0.15);
        _searchVolumeWeight = configuration.GetValue<double>("CompetitiveScore:SearchVolumeFactor", 0.1);
    }

    public async Task<int> CalculateCompetitiveScoreAsync(string keyword, Market market)
    {
        var breakdown = await GetScoreBreakdownAsync(keyword, market);
        return breakdown.TotalScore;
    }

    public async Task<CompetitiveScoreBreakdownDto> GetScoreBreakdownAsync(string keyword, Market market)
    {
        _logger.LogInformation("Calculating competitive score for keyword: {Keyword} in market: {Market}", 
            keyword, market);

        try
        {
            // Get top 30 competitors for this keyword
            var competitors = await _competitionService.AnalyzeCompetitionAsync(keyword, market, 30);
            
            if (!competitors.Any())
            {
                _logger.LogWarning("No competitors found for keyword: {Keyword}", keyword);
                return CreateEmptyBreakdown(keyword, market, "No competitors found");
            }

            var top10 = competitors.Take(10).ToList();
            
            // Calculate individual score components
            var serpIntensityScore = CalculateSerpIntensity(top10);
            var keywordUsageScore = CalculateKeywordUsage(keyword, competitors);
            var bsrToughnessScore = CalculateBsrToughness(top10);
            var saturationScore = CalculateSaturation(keyword, competitors);
            var searchVolumeScore = CalculateSearchVolume(keyword, competitors.Count);
            
            var totalScore = Math.Min(100, 
                (int)(serpIntensityScore * _serpIntensityWeight * 100 +
                      keywordUsageScore * _keywordUsageWeight * 100 +
                      bsrToughnessScore * _bsrToughnessWeight * 100 +
                      saturationScore * _saturationWeight * 100 +
                      searchVolumeScore * _searchVolumeWeight * 100));

            return new CompetitiveScoreBreakdownDto
            {
                Keyword = keyword,
                Market = market,
                TotalScore = totalScore,
                SerpIntensityScore = (int)(serpIntensityScore * 30),
                KeywordUsageScore = (int)(keywordUsageScore * 20),
                BsrToughnessScore = (int)(bsrToughnessScore * 25),
                SaturationScore = (int)(saturationScore * 15),
                SearchVolumeScore = (int)(searchVolumeScore * 10),
                Explanation = GenerateExplanation(totalScore, competitors.Count)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating competitive score for keyword: {Keyword}", keyword);
            return CreateEmptyBreakdown(keyword, market, $"Error: {ex.Message}");
        }
    }

    private double CalculateSerpIntensity(List<BookResultDto> top10)
    {
        if (!top10.Any()) return 0.0;
        
        var medianRatingCount = GetMedian(top10.Select(b => (double)b.RatingsCount));
        var avgRating = top10.Average(b => b.RatingAverage);
        
        // Scale rating count (0-30 points) and rating average (0-10 points)
        var ratingCountScore = Math.Min(30.0, medianRatingCount / 1000.0 * 30.0);
        var ratingAvgScore = Math.Max(0.0, (avgRating - 3.0) * 2.5); // 3.0-5.0 -> 0-5.0, then scale to 0-10
        
        return (ratingCountScore + ratingAvgScore) / 40.0; // Normalize to 0-1
    }

    private double CalculateKeywordUsage(string keyword, List<BookResultDto> competitors)
    {
        var keywordLower = keyword.ToLowerInvariant();
        var usageCount = competitors.Count(b => 
            b.Title.ToLowerInvariant().Contains(keywordLower) ||
            (!string.IsNullOrEmpty(b.Title) && ExtractSubtitle(b.Title).ToLowerInvariant().Contains(keywordLower)));
        
        return Math.Min(1.0, usageCount / (double)competitors.Count * 2.0);
    }

    private double CalculateBsrToughness(List<BookResultDto> top10)
    {
        if (!top10.Any()) return 0.0;
        
        var medianBsr = GetMedian(top10.Select(b => (double)b.BestSellerRank));
        
        // Lower BSR = higher toughness (inverse relationship)
        var toughnessScore = Math.Max(0.0, 1.0 - (medianBsr / 1000000.0));
        return Math.Min(1.0, toughnessScore);
    }

    private double CalculateSaturation(string keyword, List<BookResultDto> competitors)
    {
        var keywordLower = keyword.ToLowerInvariant();
        var exactMatches = competitors.Count(b => 
            b.Title.ToLowerInvariant().Contains(keywordLower));
        
        // More exact matches = higher saturation
        return Math.Min(1.0, exactMatches / 15.0);
    }

    private double CalculateSearchVolume(string keyword, int competitorCount)
    {
        // Simple heuristic: more competitors suggests higher search volume
        return Math.Min(1.0, competitorCount / 100.0);
    }

    private string GenerateExplanation(int score, int competitorCount)
    {
        var level = CompetitionLevelExtensions.FromScore(score);
        return $"{level.ToDisplayName()} competition based on {competitorCount} competitors analyzed";
    }

    private CompetitiveScoreBreakdownDto CreateEmptyBreakdown(string keyword, Market market, string explanation)
    {
        return new CompetitiveScoreBreakdownDto
        {
            Keyword = keyword,
            Market = market,
            TotalScore = 0,
            SerpIntensityScore = 0,
            KeywordUsageScore = 0,
            BsrToughnessScore = 0,
            SaturationScore = 0,
            SearchVolumeScore = 0,
            Explanation = explanation
        };
    }

    private static double GetMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (!sorted.Any()) return 0.0;
        
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static string ExtractSubtitle(string fullTitle)
    {
        var colonIndex = fullTitle.IndexOf(':');
        return colonIndex > 0 && colonIndex < fullTitle.Length - 1 
            ? fullTitle[(colonIndex + 1)..].Trim() 
            : string.Empty;
    }
}
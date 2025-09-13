using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.Interfaces;

public interface IKeywordResearchService
{
    Task<List<KeywordResultDto>> ResearchKeywordAsync(string seedKeyword, Market market);
    Task<List<KeywordResultDto>> ResearchKeywordsAsync(List<string> seedKeywords, Market market);
    Task<List<string>> GetAutocompleteKeywordsAsync(string seedKeyword, Market market);
    Task<List<KeywordResultDto>> GetKeywordHistoryAsync(string keyword, Market market, int months = 12);
    Task ExportKeywordsAsync(List<KeywordResultDto> keywords, string filePath);
}

public interface ICategoryAnalysisService
{
    Task<List<CategoryResultDto>> GetCategoriesAsync(Market market, string? searchTerm = null);
    Task<CategoryResultDto?> GetCategoryAsync(string categoryId, Market market);
    Task<List<CategorySnapshotDto>> GetCategoryHistoryAsync(string categoryId, Market market, int months = 12);
    Task<List<CategoryResultDto>> GetGhostCategoriesAsync(Market market);
    Task<List<CategoryResultDto>> GetDuplicateCategoriesAsync(Market market);
    Task<TrendAnalysisDto> CalculateTrendAsync(string categoryId, Market market);
    Task RefreshCategoryDataAsync(string categoryId, Market market);
}

public record TrendAnalysisDto
{
    public string CategoryId { get; init; } = string.Empty;
    public Market Market { get; init; }
    public TrendDirection Direction { get; init; }
    public double GrowthPercentage { get; init; }
    public double VolatilityScore { get; init; }
    public bool IsHighVariation { get; init; }
    public List<MonthlyDataPoint> DataPoints { get; init; } = new();
}

public record MonthlyDataPoint
{
    public int Month { get; init; }
    public int Year { get; init; }
    public int SalesVolume { get; init; }
    public DateTime Date => new(Year, Month, 1);
}
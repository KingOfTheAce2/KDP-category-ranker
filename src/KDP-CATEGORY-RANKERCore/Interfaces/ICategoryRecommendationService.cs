using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.Interfaces;

public interface ICategoryRecommendationService
{
    /// <summary>
    /// Get recommended categories based on book details and author goals
    /// </summary>
    Task<List<CategoryRecommendationDto>> GetCategoryRecommendationsAsync(CategoryRecommendationRequest request);
    
    /// <summary>
    /// Analyze a specific category for bestseller requirements and planning
    /// </summary>
    Task<BestsellerPlanningDto> GetBestsellerPlanningAsync(string categoryId, Market market, decimal bookPrice, DateTime plannedReleaseDate);
    
    /// <summary>
    /// Calculate difficulty score for a category (0-100)
    /// </summary>
    Task<int> CalculateCategoryDifficultyAsync(string categoryId, Market market);
    
    /// <summary>
    /// Get daily sales requirements for different bestseller positions
    /// </summary>
    Task<Dictionary<int, int>> GetSalesRequirementsAsync(string categoryId, Market market);
    
    /// <summary>
    /// Find the easiest categories to reach bestseller status
    /// </summary>
    Task<List<CategoryRecommendationDto>> GetEasiestCategoriesAsync(Market market, BookFormat format, int maxDailySales = 10);
    
    /// <summary>
    /// Get seasonal trends and optimal release timing for categories
    /// </summary>
    Task<Dictionary<string, SeasonalTrend>> GetSeasonalTrendsAsync(List<string> categoryIds, Market market);
    
    /// <summary>
    /// Calculate recommendation score based on multiple factors
    /// </summary>
    Task<int> CalculateRecommendationScoreAsync(string categoryId, Market market, CategoryRecommendationRequest request);
    
    /// <summary>
    /// Get ghost categories that should be avoided
    /// </summary>
    Task<List<string>> GetGhostCategoriesAsync(Market market);
    
    /// <summary>
    /// Find similar successful categories based on keywords and metrics
    /// </summary>
    Task<List<CategoryRecommendationDto>> FindSimilarCategoriesAsync(List<string> keywords, Market market, int maxResults = 20);
}

public interface IBestsellerCalculatorService
{
    /// <summary>
    /// Calculate daily sales needed to reach specific BSR
    /// </summary>
    int CalculateDailySalesForBSR(int targetBSR, BookFormat format, Market market);
    
    /// <summary>
    /// Calculate BSR achievable with specific daily sales
    /// </summary>
    int CalculateBSRForDailySales(int dailySales, BookFormat format, Market market);
    
    /// <summary>
    /// Get current BSR requirements for bestseller positions in category
    /// </summary>
    Task<Dictionary<string, int>> GetCategoryBSRRequirementsAsync(string categoryId, Market market);
    
    /// <summary>
    /// Calculate revenue projections based on sales and position
    /// </summary>
    decimal CalculateRevenueProjection(int dailySales, decimal bookPrice, int marketPosition, double revenueFactor = 0.6);
    
    /// <summary>
    /// Get historical bestseller requirements for trend analysis
    /// </summary>
    Task<List<MonthlyBestsellerRequirement>> GetHistoricalRequirementsAsync(string categoryId, Market market, int months = 12);
}

public record MonthlyBestsellerRequirement
{
    public string CategoryId { get; init; } = string.Empty;
    public Market Market { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public int DailySalesForBestseller { get; init; }
    public int DailySalesForTop10 { get; init; }
    public int DailySalesForTop50 { get; init; }
    public DateTime SnapshotDate { get; init; }
}
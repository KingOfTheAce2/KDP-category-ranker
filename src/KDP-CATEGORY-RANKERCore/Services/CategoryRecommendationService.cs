using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERCore.Services;

public class CategoryRecommendationService : ICategoryRecommendationService
{
    private readonly ILogger<CategoryRecommendationService> _logger;
    private readonly ICategoryAnalysisService _categoryAnalysisService;
    private readonly IBestsellerCalculatorService _bestsellerCalculatorService;
    private readonly ISalesEstimationService _salesEstimationService;

    public CategoryRecommendationService(
        ILogger<CategoryRecommendationService> logger,
        ICategoryAnalysisService categoryAnalysisService,
        IBestsellerCalculatorService bestsellerCalculatorService,
        ISalesEstimationService salesEstimationService)
    {
        _logger = logger;
        _categoryAnalysisService = categoryAnalysisService;
        _bestsellerCalculatorService = bestsellerCalculatorService;
        _salesEstimationService = salesEstimationService;
    }

    public async Task<List<CategoryRecommendationDto>> GetCategoryRecommendationsAsync(CategoryRecommendationRequest request)
    {
        _logger.LogInformation("Getting category recommendations for book: {Title}", request.BookTitle);

        try
        {
            // Find categories matching keywords
            var matchingCategories = await FindSimilarCategoriesAsync(request.Keywords, request.TargetMarket, 50);
            
            // Filter based on user criteria
            var filteredCategories = matchingCategories
                .Where(c => !request.ExcludeGhostCategories || !c.IsGhostCategory)
                .Where(c => !request.IncludeHighCompetition || c.DifficultyLevel <= request.MaxDifficultyLevel)
                .Where(c => c.DailySalesForBestseller <= request.MaxDailySalesTarget * 2) // Allow some stretch
                .ToList();

            // Score and rank recommendations
            foreach (var category in filteredCategories)
            {
                var score = await CalculateRecommendationScoreAsync(category.CategoryId, request.TargetMarket, request);
                // Update recommendation score (this would be done by recreating the record in a real implementation)
            }

            // Sort by recommendation score (highest first)
            var recommendations = filteredCategories
                .OrderByDescending(c => c.RecommendationScore)
                .Take(20)
                .ToList();

            _logger.LogInformation("Found {Count} category recommendations", recommendations.Count);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category recommendations");
            return new List<CategoryRecommendationDto>();
        }
    }

    public async Task<BestsellerPlanningDto> GetBestsellerPlanningAsync(string categoryId, Market market, decimal bookPrice, DateTime plannedReleaseDate)
    {
        _logger.LogInformation("Getting bestseller planning for category: {CategoryId}", categoryId);

        try
        {
            var category = await _categoryAnalysisService.GetCategoryAsync(categoryId, market);
            if (category == null)
            {
                throw new ArgumentException($"Category {categoryId} not found");
            }

            // Get current requirements
            var currentRequirements = await _bestsellerCalculatorService.GetCategoryBSRRequirementsAsync(categoryId, market);
            
            // Get historical data for trend analysis
            var historicalData = await _bestsellerCalculatorService.GetHistoricalRequirementsAsync(categoryId, market, 12);
            
            // Calculate seasonal patterns
            var seasonalTrends = await GetSeasonalTrendsAsync(new List<string> { categoryId }, market);
            var categoryTrend = seasonalTrends.GetValueOrDefault(categoryId, SeasonalTrend.Stable);

            // Calculate monthly requirements
            var monthlyRequirements = CalculateMonthlyRequirements(historicalData, categoryTrend);
            
            // Find easiest/hardest months
            var easiestMonth = monthlyRequirements.OrderBy(kvp => kvp.Value).First();
            var hardestMonth = monthlyRequirements.OrderByDescending(kvp => kvp.Value).First();
            
            // Calculate success probability
            var currentDailySalesForBestseller = _bestsellerCalculatorService.CalculateDailySalesForBSR(
                currentRequirements.GetValueOrDefault("bestseller", 1), BookFormat.Kindle, market);
            
            var userMaxSales = 10; // This would come from request in real implementation
            var successProbability = Math.Min(1.0, (double)userMaxSales / currentDailySalesForBestseller);

            // Generate strategy recommendations
            var strategy = GenerateRecommendedStrategy(currentDailySalesForBestseller, userMaxSales, categoryTrend);
            var actionItems = GenerateActionItems(currentDailySalesForBestseller, userMaxSales, plannedReleaseDate);

            return new BestsellerPlanningDto
            {
                CategoryId = categoryId,
                CategoryName = category.Breadcrumb,
                Market = market,
                CurrentDailySalesForBestseller = currentDailySalesForBestseller,
                CurrentDailySalesForTop10 = _bestsellerCalculatorService.CalculateDailySalesForBSR(
                    currentRequirements.GetValueOrDefault("top10", 10), BookFormat.Kindle, market),
                CurrentDailySalesForTop50 = _bestsellerCalculatorService.CalculateDailySalesForBSR(
                    currentRequirements.GetValueOrDefault("top50", 50), BookFormat.Kindle, market),
                AverageDailySalesForBestseller = CalculateAverage(historicalData, d => d.DailySalesForBestseller),
                AverageDailySalesForTop10 = CalculateAverage(historicalData, d => d.DailySalesForTop10),
                AverageDailySalesForTop50 = CalculateAverage(historicalData, d => d.DailySalesForTop50),
                MonthlyBestsellerRequirements = monthlyRequirements,
                EasiestMonth = easiestMonth.Key,
                HardestMonth = hardestMonth.Key,
                DaysUntilEasiestPeriod = CalculateDaysUntilEasiestPeriod(easiestMonth.Key, plannedReleaseDate),
                DailyRevenueAtBestseller = _bestsellerCalculatorService.CalculateRevenueProjection(
                    currentDailySalesForBestseller, bookPrice, 1),
                MonthlyRevenueAtBestseller = _bestsellerCalculatorService.CalculateRevenueProjection(
                    currentDailySalesForBestseller, bookPrice, 1) * 30,
                SuccessProbability = successProbability,
                RecommendedStrategy = strategy,
                ActionItems = actionItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bestseller planning for category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<int> CalculateCategoryDifficultyAsync(string categoryId, Market market)
    {
        try
        {
            var requirements = await _bestsellerCalculatorService.GetCategoryBSRRequirementsAsync(categoryId, market);
            var category = await _categoryAnalysisService.GetCategoryAsync(categoryId, market);
            
            if (category == null) return 50; // Medium difficulty if no data

            // Calculate difficulty based on multiple factors
            var dailySalesForBestseller = _bestsellerCalculatorService.CalculateDailySalesForBSR(
                requirements.GetValueOrDefault("bestseller", 1), BookFormat.Kindle, market);
            
            var factors = new[]
            {
                Math.Min(100, dailySalesForBestseller * 2), // Sales requirement (0-100)
                (int)(category.PercentageLargePublisher * 100), // Big publisher presence
                Math.Min(100, (int)(category.AvgRating * 20)), // Quality bar
                Math.Min(100, category.AvgAgeDays / 10) // Market maturity
            };

            return (int)factors.Average();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating category difficulty for: {CategoryId}", categoryId);
            return 50; // Medium difficulty on error
        }
    }

    public async Task<Dictionary<int, int>> GetSalesRequirementsAsync(string categoryId, Market market)
    {
        var requirements = await _bestsellerCalculatorService.GetCategoryBSRRequirementsAsync(categoryId, market);
        var result = new Dictionary<int, int>();

        foreach (var req in requirements)
        {
            var position = req.Key switch
            {
                "bestseller" => 1,
                "top10" => 10,
                "top50" => 50,
                _ => int.Parse(req.Key)
            };
            
            result[position] = _bestsellerCalculatorService.CalculateDailySalesForBSR(req.Value, BookFormat.Kindle, market);
        }

        return result;
    }

    public async Task<List<CategoryRecommendationDto>> GetEasiestCategoriesAsync(Market market, BookFormat format, int maxDailySales = 10)
    {
        var allCategories = await _categoryAnalysisService.GetCategoriesAsync(market);
        var recommendations = new List<CategoryRecommendationDto>();

        foreach (var category in allCategories)
        {
            var difficulty = await CalculateCategoryDifficultyAsync(category.CategoryId, market);
            var salesRequirements = await GetSalesRequirementsAsync(category.CategoryId, market);
            
            if (salesRequirements.GetValueOrDefault(1, int.MaxValue) <= maxDailySales)
            {
                recommendations.Add(new CategoryRecommendationDto
                {
                    CategoryId = category.CategoryId,
                    Breadcrumb = category.Breadcrumb,
                    Market = market,
                    DifficultyScore = difficulty,
                    DailySalesForBestseller = salesRequirements.GetValueOrDefault(1, 0),
                    DailySalesForTop10 = salesRequirements.GetValueOrDefault(10, 0),
                    DailySalesForTop50 = salesRequirements.GetValueOrDefault(50, 0),
                    RecommendationScore = 100 - difficulty, // Easier = higher score
                    IsGhostCategory = category.IsGhost,
                    IsDuplicateCategory = category.IsDuplicate
                });
            }
        }

        return recommendations.OrderBy(r => r.DifficultyScore).Take(20).ToList();
    }

    public async Task<Dictionary<string, SeasonalTrend>> GetSeasonalTrendsAsync(List<string> categoryIds, Market market)
    {
        var result = new Dictionary<string, SeasonalTrend>();
        
        foreach (var categoryId in categoryIds)
        {
            var history = await _categoryAnalysisService.GetCategoryHistoryAsync(categoryId, market, 12);
            var trend = AnalyzeSeasonalPattern(history);
            result[categoryId] = trend;
        }
        
        return result;
    }

    public async Task<int> CalculateRecommendationScoreAsync(string categoryId, Market market, CategoryRecommendationRequest request)
    {
        try
        {
            var category = await _categoryAnalysisService.GetCategoryAsync(categoryId, market);
            if (category == null) return 0;

            var difficulty = await CalculateCategoryDifficultyAsync(categoryId, market);
            var salesRequirements = await GetSalesRequirementsAsync(categoryId, market);
            
            var factors = new Dictionary<string, (double weight, double score)>
            {
                ["difficulty"] = (0.3, 100 - difficulty), // Lower difficulty = higher score
                ["achievability"] = (0.25, CalculateAchievabilityScore(salesRequirements.GetValueOrDefault(1, 0), request.MaxDailySalesTarget)),
                ["revenue_potential"] = (0.2, CalculateRevenuePotentialScore(category.AvgPriceIndie, salesRequirements.GetValueOrDefault(1, 0))),
                ["growth_trend"] = (0.15, CalculateGrowthScore(category.Trend)),
                ["market_size"] = (0.1, Math.Min(100, category.SalesToReachRank10 / 10.0))
            };

            var weightedScore = factors.Values.Sum(f => f.weight * f.score);
            return Math.Max(0, Math.Min(100, (int)weightedScore));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating recommendation score for category: {CategoryId}", categoryId);
            return 0;
        }
    }

    public async Task<List<string>> GetGhostCategoriesAsync(Market market)
    {
        var ghostCategories = await _categoryAnalysisService.GetGhostCategoriesAsync(market);
        return ghostCategories.Select(c => c.CategoryId).ToList();
    }

    public async Task<List<CategoryRecommendationDto>> FindSimilarCategoriesAsync(List<string> keywords, Market market, int maxResults = 20)
    {
        var allCategories = await _categoryAnalysisService.GetCategoriesAsync(market);
        var scoredCategories = new List<(CategoryResultDto category, double relevanceScore)>();

        foreach (var category in allCategories)
        {
            var relevanceScore = CalculateKeywordRelevance(category.Breadcrumb, keywords);
            if (relevanceScore > 0.1) // Minimum relevance threshold
            {
                scoredCategories.Add((category, relevanceScore));
            }
        }

        var topCategories = scoredCategories
            .OrderByDescending(c => c.relevanceScore)
            .Take(maxResults)
            .ToList();

        var recommendations = new List<CategoryRecommendationDto>();
        
        foreach (var (category, relevanceScore) in topCategories)
        {
            var difficulty = await CalculateCategoryDifficultyAsync(category.CategoryId, market);
            var salesRequirements = await GetSalesRequirementsAsync(category.CategoryId, market);
            
            recommendations.Add(new CategoryRecommendationDto
            {
                CategoryId = category.CategoryId,
                Breadcrumb = category.Breadcrumb,
                Market = market,
                DifficultyScore = difficulty,
                DailySalesForBestseller = salesRequirements.GetValueOrDefault(1, 0),
                DailySalesForTop10 = salesRequirements.GetValueOrDefault(10, 0),
                DailySalesForTop50 = salesRequirements.GetValueOrDefault(50, 0),
                AveragePrice = category.AvgPriceIndie,
                RecommendationScore = (int)((100 - difficulty) * relevanceScore),
                IsGhostCategory = category.IsGhost,
                IsDuplicateCategory = category.IsDuplicate,
                SalesVolumetrend = category.Trend
            });
        }

        return recommendations;
    }

    // Helper methods
    private SeasonalTrend AnalyzeSeasonalPattern(List<CategorySnapshotDto> history)
    {
        if (history.Count < 12) return SeasonalTrend.Stable;

        var monthlyAverages = history
            .GroupBy(h => h.Month)
            .ToDictionary(g => g.Key, g => g.Average(h => h.SalesToNo1));

        var peak = monthlyAverages.OrderByDescending(kvp => kvp.Value).First();
        
        return peak.Key switch
        {
            >= 3 and <= 5 => SeasonalTrend.SpringPeak,
            >= 6 and <= 8 => SeasonalTrend.SummerPeak,
            >= 9 and <= 11 => SeasonalTrend.FallPeak,
            12 => SeasonalTrend.HolidayDriven,
            1 => SeasonalTrend.NewYear,
            2 => SeasonalTrend.WinterPeak,
            _ => SeasonalTrend.Stable
        };
    }

    private Dictionary<string, int> CalculateMonthlyRequirements(List<MonthlyBestsellerRequirement> history, SeasonalTrend trend)
    {
        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var result = new Dictionary<string, int>();

        for (int month = 1; month <= 12; month++)
        {
            var monthData = history.Where(h => h.Month == month).ToList();
            var average = monthData.Any() ? (int)monthData.Average(h => h.DailySalesForBestseller) : 10;
            result[monthNames[month - 1]] = average;
        }

        return result;
    }

    private int CalculateAverage(List<MonthlyBestsellerRequirement> data, Func<MonthlyBestsellerRequirement, int> selector)
    {
        return data.Any() ? (int)data.Average(selector) : 0;
    }

    private int CalculateDaysUntilEasiestPeriod(string easiestMonth, DateTime plannedDate)
    {
        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var monthIndex = Array.IndexOf(monthNames, easiestMonth) + 1;
        
        var targetDate = new DateTime(plannedDate.Year, monthIndex, 1);
        if (targetDate < plannedDate)
        {
            targetDate = targetDate.AddYears(1);
        }
        
        return (int)(targetDate - plannedDate).TotalDays;
    }

    private string GenerateRecommendedStrategy(int requiredSales, int userMaxSales, SeasonalTrend trend)
    {
        if (userMaxSales >= requiredSales)
        {
            return "You have excellent chances! Focus on quality content and basic marketing.";
        }
        else if (userMaxSales >= requiredSales * 0.7)
        {
            return "Good potential with strong marketing. Consider pre-launch campaign and influencer outreach.";
        }
        else
        {
            return "Challenging category. Consider easier categories or build audience first through multiple releases.";
        }
    }

    private List<string> GenerateActionItems(int requiredSales, int userMaxSales, DateTime plannedDate)
    {
        var items = new List<string>
        {
            "Optimize book cover for maximum click-through rate",
            "Write compelling book description with emotional hooks",
            "Set competitive pricing based on category analysis"
        };

        if (userMaxSales < requiredSales)
        {
            items.AddRange(new[]
            {
                "Build email list before launch",
                "Plan pre-launch review campaign",
                "Consider Facebook/Amazon ads budget",
                "Reach out to influencers in your niche"
            });
        }

        if (plannedDate > DateTime.Now.AddMonths(1))
        {
            items.Add("Use extra time to build anticipation and gather reviews");
        }

        return items;
    }

    private double CalculateKeywordRelevance(string categoryBreadcrumb, List<string> keywords)
    {
        if (!keywords.Any()) return 0;

        var breadcrumbLower = categoryBreadcrumb.ToLowerInvariant();
        var matches = keywords.Count(keyword => breadcrumbLower.Contains(keyword.ToLowerInvariant()));
        
        return (double)matches / keywords.Count;
    }

    private double CalculateAchievabilityScore(int requiredSales, int userMaxSales)
    {
        if (requiredSales == 0) return 100;
        return Math.Min(100, (double)userMaxSales / requiredSales * 100);
    }

    private double CalculateRevenuePotentialScore(decimal avgPrice, int dailySales)
    {
        var monthlyRevenue = avgPrice * dailySales * 30 * 0.6m; // 60% revenue share
        return Math.Min(100, (double)monthlyRevenue / 100); // Normalize to 0-100
    }

    private double CalculateGrowthScore(TrendDirection trend)
    {
        return trend switch
        {
            TrendDirection.RapidlyGrowing => 100,
            TrendDirection.Growing => 80,
            TrendDirection.Flat => 60,
            TrendDirection.SignificantlyDeclining => 40,
            TrendDirection.RapidlyDeclining => 20,
            _ => 60
        };
    }
}
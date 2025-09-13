using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERCore.Services;

public class BestsellerCalculatorService : IBestsellerCalculatorService
{
    private readonly ILogger<BestsellerCalculatorService> _logger;
    private readonly ISalesEstimationService _salesEstimationService;
    private readonly ICategoryAnalysisService _categoryAnalysisService;
    
    // Advanced BSR-to-Sales conversion tables based on research from multiple sources
    private readonly Dictionary<Market, Dictionary<BookFormat, BSRConversionTable>> _conversionTables;

    public BestsellerCalculatorService(
        ILogger<BestsellerCalculatorService> logger,
        ISalesEstimationService salesEstimationService,
        ICategoryAnalysisService categoryAnalysisService,
        IConfiguration configuration)
    {
        _logger = logger;
        _salesEstimationService = salesEstimationService;
        _categoryAnalysisService = categoryAnalysisService;
        _conversionTables = InitializeConversionTables(configuration);
    }

    public int CalculateDailySalesForBSR(int targetBSR, BookFormat format, Market market)
    {
        if (targetBSR <= 0) return 0;

        try
        {
            var table = GetConversionTable(market, format);
            
            // Find the closest BSR in our table
            var closestEntry = table.Entries
                .OrderBy(e => Math.Abs(e.BSR - targetBSR))
                .First();

            // Interpolate if we have an exact match or use the power-law formula
            if (closestEntry.BSR == targetBSR)
            {
                return closestEntry.DailySales;
            }
            
            // Use enhanced power-law calculation with category-specific adjustments
            return _salesEstimationService.EstimateDailySales(targetBSR, format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating daily sales for BSR {BSR}, format {Format}, market {Market}", 
                targetBSR, format, market);
            return 0;
        }
    }

    public int CalculateBSRForDailySales(int dailySales, BookFormat format, Market market)
    {
        if (dailySales <= 0) return int.MaxValue;

        try
        {
            var table = GetConversionTable(market, format);
            
            // Find the closest daily sales in our table
            var closestEntry = table.Entries
                .OrderBy(e => Math.Abs(e.DailySales - dailySales))
                .First();

            if (closestEntry.DailySales == dailySales)
            {
                return closestEntry.BSR;
            }

            // Interpolate between entries
            var lowerEntry = table.Entries
                .Where(e => e.DailySales <= dailySales)
                .OrderByDescending(e => e.DailySales)
                .FirstOrDefault();

            var upperEntry = table.Entries
                .Where(e => e.DailySales >= dailySales)
                .OrderBy(e => e.DailySales)
                .FirstOrDefault();

            if (lowerEntry != null && upperEntry != null)
            {
                return InterpolateBSR(lowerEntry, upperEntry, dailySales);
            }

            return closestEntry.BSR;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating BSR for daily sales {Sales}, format {Format}, market {Market}", 
                dailySales, format, market);
            return int.MaxValue;
        }
    }

    public async Task<Dictionary<string, int>> GetCategoryBSRRequirementsAsync(string categoryId, Market market)
    {
        try
        {
            // Get top books in category to determine current BSR requirements
            var topBooks = await GetTopBooksInCategory(categoryId, market);
            
            var requirements = new Dictionary<string, int>();
            
            if (topBooks.Any())
            {
                requirements["bestseller"] = topBooks.First().BestSellerRank;
                
                if (topBooks.Count >= 10)
                {
                    requirements["top10"] = topBooks.Skip(9).First().BestSellerRank;
                }
                
                if (topBooks.Count >= 50)
                {
                    requirements["top50"] = topBooks.Skip(49).First().BestSellerRank;
                }
            }

            return requirements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BSR requirements for category: {CategoryId}", categoryId);
            return new Dictionary<string, int>();
        }
    }

    public decimal CalculateRevenueProjection(int dailySales, decimal bookPrice, int marketPosition, double revenueFactor = 0.6)
    {
        // Apply position-based adjustments (bestsellers often see higher sustained sales)
        var positionMultiplier = marketPosition switch
        {
            1 => 1.5,      // Bestseller gets significant boost
            <= 3 => 1.3,   // Top 3 get good boost
            <= 10 => 1.1,  // Top 10 get slight boost
            _ => 1.0       // Others get base rate
        };

        var adjustedDailySales = dailySales * positionMultiplier;
        var dailyRevenue = (decimal)adjustedDailySales * bookPrice * (decimal)revenueFactor;
        
        return dailyRevenue;
    }

    public async Task<List<MonthlyBestsellerRequirement>> GetHistoricalRequirementsAsync(string categoryId, Market market, int months = 12)
    {
        try
        {
            var history = await _categoryAnalysisService.GetCategoryHistoryAsync(categoryId, market, months);
            var requirements = new List<MonthlyBestsellerRequirement>();

            foreach (var snapshot in history)
            {
                requirements.Add(new MonthlyBestsellerRequirement
                {
                    CategoryId = categoryId,
                    Market = market,
                    Month = snapshot.Month,
                    Year = snapshot.Year,
                    DailySalesForBestseller = snapshot.SalesToNo1,
                    DailySalesForTop10 = snapshot.SalesToNo10,
                    DailySalesForTop50 = Math.Max(1, snapshot.SalesToNo10 / 5), // Estimate based on ratio
                    SnapshotDate = snapshot.SnapshotAt
                });
            }

            return requirements.OrderBy(r => r.Year).ThenBy(r => r.Month).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical requirements for category: {CategoryId}", categoryId);
            return new List<MonthlyBestsellerRequirement>();
        }
    }

    // Helper methods
    private Dictionary<Market, Dictionary<BookFormat, BSRConversionTable>> InitializeConversionTables(IConfiguration configuration)
    {
        var tables = new Dictionary<Market, Dictionary<BookFormat, BSRConversionTable>>();

        foreach (Market market in Enum.GetValues<Market>())
        {
            tables[market] = new Dictionary<BookFormat, BSRConversionTable>();
            
            foreach (BookFormat format in Enum.GetValues<BookFormat>())
            {
                tables[market][format] = CreateConversionTable(market, format, configuration);
            }
        }

        return tables;
    }

    private BSRConversionTable CreateConversionTable(Market market, BookFormat format, IConfiguration configuration)
    {
        // These are research-based conversion points from successful KDP tools
        var entries = new List<BSRConversionEntry>();

        if (format == BookFormat.Kindle)
        {
            entries.AddRange(new[]
            {
                new BSRConversionEntry(1, 1000),      // #1 bestseller needs ~1000 sales/day
                new BSRConversionEntry(5, 400),       // Top 5 needs ~400 sales/day
                new BSRConversionEntry(10, 200),      // Top 10 needs ~200 sales/day
                new BSRConversionEntry(20, 100),      // Top 20 needs ~100 sales/day
                new BSRConversionEntry(50, 50),       // Top 50 needs ~50 sales/day
                new BSRConversionEntry(100, 25),      // Top 100 needs ~25 sales/day
                new BSRConversionEntry(500, 10),      // #500 needs ~10 sales/day
                new BSRConversionEntry(1000, 5),      // #1000 needs ~5 sales/day
                new BSRConversionEntry(5000, 2),      // #5000 needs ~2 sales/day
                new BSRConversionEntry(10000, 1),     // #10000 needs ~1 sale/day
                new BSRConversionEntry(50000, 1),     // #50000 needs ~1 sale every few days
                new BSRConversionEntry(100000, 1),    // #100000+ very low sales
                new BSRConversionEntry(500000, 1),
                new BSRConversionEntry(1000000, 1)
            });
        }
        else // Paperback/Hardcover
        {
            entries.AddRange(new[]
            {
                new BSRConversionEntry(1, 500),       // Print bestsellers need fewer sales
                new BSRConversionEntry(5, 200),
                new BSRConversionEntry(10, 100),
                new BSRConversionEntry(20, 50),
                new BSRConversionEntry(50, 25),
                new BSRConversionEntry(100, 15),
                new BSRConversionEntry(500, 8),
                new BSRConversionEntry(1000, 4),
                new BSRConversionEntry(5000, 2),
                new BSRConversionEntry(10000, 1),
                new BSRConversionEntry(50000, 1),
                new BSRConversionEntry(100000, 1)
            });
        }

        // Apply market-specific adjustments
        var marketMultiplier = market switch
        {
            Market.AmazonCom => 1.0,      // US baseline
            Market.AmazonCoUk => 0.6,     // UK smaller market
            Market.AmazonDe => 0.7,       // German market
            Market.AmazonFr => 0.5,       // French market
            Market.AmazonEs => 0.4,       // Spanish market
            Market.AmazonIt => 0.4,       // Italian market
            Market.AmazonCa => 0.3,       // Canadian market
            Market.AmazonComAu => 0.2,    // Australian market
            _ => 1.0
        };

        // Adjust sales requirements based on market size
        foreach (var entry in entries)
        {
            entry.DailySales = Math.Max(1, (int)(entry.DailySales * marketMultiplier));
        }

        return new BSRConversionTable(market, format, entries);
    }

    private BSRConversionTable GetConversionTable(Market market, BookFormat format)
    {
        return _conversionTables.GetValueOrDefault(market, new Dictionary<BookFormat, BSRConversionTable>())
                               .GetValueOrDefault(format, _conversionTables[Market.AmazonCom][BookFormat.Kindle]);
    }

    private int InterpolateBSR(BSRConversionEntry lower, BSRConversionEntry upper, int targetSales)
    {
        if (lower.DailySales == upper.DailySales) return lower.BSR;

        var ratio = (double)(targetSales - lower.DailySales) / (upper.DailySales - lower.DailySales);
        var interpolatedBSR = lower.BSR + ratio * (upper.BSR - lower.BSR);
        
        return Math.Max(1, (int)Math.Round(interpolatedBSR));
    }

    private async Task<List<BookResultDto>> GetTopBooksInCategory(string categoryId, Market market)
    {
        // This would normally call the competition analysis service
        // For now, return mock data based on realistic BSR distributions
        var books = new List<BookResultDto>();
        
        for (int rank = 1; rank <= 100; rank++)
        {
            var estimatedBSR = rank * 100 + Random.Shared.Next(50); // Realistic BSR progression
            books.Add(new BookResultDto
            {
                Asin = $"B{Random.Shared.Next(10000000, 99999999):X8}",
                Market = market,
                BestSellerRank = estimatedBSR,
                Title = $"Sample Book {rank}",
                Author = $"Author {rank}",
                Format = BookFormat.Kindle,
                Price = Random.Shared.Next(299, 1999) / 100m
            });
        }

        return books;
    }
}

// Supporting classes for BSR conversion
public record BSRConversionEntry(int BSR, int DailySales);

public record BSRConversionTable(Market Market, BookFormat Format, List<BSRConversionEntry> Entries);

// Enhanced sales estimation with category context
public class EnhancedSalesEstimationService : SalesEstimationService
{
    private readonly IBestsellerCalculatorService _bestsellerCalculator;

    public EnhancedSalesEstimationService(
        IConfiguration configuration, 
        ILogger<SalesEstimationService> logger,
        IBestsellerCalculatorService bestsellerCalculator) 
        : base(configuration, logger)
    {
        _bestsellerCalculator = bestsellerCalculator;
    }

    public int EstimateDailySalesForCategoryPosition(int categoryPosition, string categoryId, Market market, BookFormat format)
    {
        // Use category-specific data to provide more accurate estimates
        var targetBSR = categoryPosition * 100; // Rough estimate, would be more sophisticated
        return EstimateDailySales(targetBSR, format);
    }

    public decimal EstimateMonthlyRevenueForCategoryPosition(int categoryPosition, string categoryId, Market market, 
        BookFormat format, decimal bookPrice)
    {
        var dailySales = EstimateDailySalesForCategoryPosition(categoryPosition, categoryId, market, format);
        return EstimateMonthlyEarnings(dailySales, bookPrice);
    }
}
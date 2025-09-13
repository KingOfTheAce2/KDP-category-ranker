using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERCore.Services;

public class SalesEstimationService : ISalesEstimationService
{
    private readonly ILogger<SalesEstimationService> _logger;
    private readonly Dictionary<BookFormat, (double A, double B)> _coefficients;

    public SalesEstimationService(IConfiguration configuration, ILogger<SalesEstimationService> logger)
    {
        _logger = logger;
        _coefficients = new Dictionary<BookFormat, (double, double)>
        {
            [BookFormat.Kindle] = (
                configuration.GetValue<double>("SalesEstimation:Kindle:CoefficientA", 5500.0),
                configuration.GetValue<double>("SalesEstimation:Kindle:CoefficientB", -0.83)
            ),
            [BookFormat.Paperback] = (
                configuration.GetValue<double>("SalesEstimation:Print:CoefficientA", 2600.0),
                configuration.GetValue<double>("SalesEstimation:Print:CoefficientB", -0.75)
            ),
            [BookFormat.Hardcover] = (
                configuration.GetValue<double>("SalesEstimation:Print:CoefficientA", 2600.0),
                configuration.GetValue<double>("SalesEstimation:Print:CoefficientB", -0.75)
            ),
            [BookFormat.AudioBook] = (
                configuration.GetValue<double>("SalesEstimation:Kindle:CoefficientA", 5500.0),
                configuration.GetValue<double>("SalesEstimation:Kindle:CoefficientB", -0.83)
            )
        };
    }

    public int EstimateDailySales(int bestSellerRank, BookFormat format)
    {
        if (bestSellerRank <= 0)
        {
            _logger.LogWarning("Invalid BSR: {BSR}", bestSellerRank);
            return 0;
        }

        var (a, b) = _coefficients[format];
        var sales = a * Math.Pow(bestSellerRank, b);
        var result = Math.Max(0.0, sales);
        
        _logger.LogDebug("Estimated daily sales for BSR {BSR} ({Format}): {Sales}", 
            bestSellerRank, format, result);
        
        return (int)Math.Round(result);
    }

    public decimal EstimateMonthlyEarnings(int dailySales, decimal price, double revenueFactor = 0.6)
    {
        var monthlyEarnings = dailySales * price * 30m * (decimal)revenueFactor;
        return Math.Max(0m, monthlyEarnings);
    }

    public void UpdateCoefficients(BookFormat format, double coefficientA, double coefficientB)
    {
        _coefficients[format] = (coefficientA, coefficientB);
        _logger.LogInformation("Updated coefficients for {Format}: A={A}, B={B}", 
            format, coefficientA, coefficientB);
    }

    public (double coefficientA, double coefficientB) GetCoefficients(BookFormat format)
    {
        return _coefficients.TryGetValue(format, out var coefficients) 
            ? coefficients 
            : (5500.0, -0.83);
    }
}
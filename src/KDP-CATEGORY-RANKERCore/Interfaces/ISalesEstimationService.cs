using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.Interfaces;

public interface ISalesEstimationService
{
    int EstimateDailySales(int bestSellerRank, BookFormat format);
    decimal EstimateMonthlyEarnings(int dailySales, decimal price, double revenueFactor = 0.6);
    void UpdateCoefficients(BookFormat format, double coefficientA, double coefficientB);
    (double coefficientA, double coefficientB) GetCoefficients(BookFormat format);
}
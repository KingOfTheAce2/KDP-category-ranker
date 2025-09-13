namespace KdpCategoryRanker.Models;

public class CategoryRecommendation
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int RequiredDailySales { get; set; }
    public string SuccessProbability { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
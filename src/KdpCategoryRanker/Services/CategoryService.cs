using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KdpCategoryRanker.Models;

namespace KdpCategoryRanker.Services;

public class CategoryService : ICategoryService
{
    private static readonly List<CategoryRecommendation> SampleCategories = new()
    {
        new CategoryRecommendation
        {
            Name = "Business & Money > Entrepreneurship > Small Business",
            Score = 85,
            RequiredDailySales = 15,
            SuccessProbability = "High (85%)",
            Description = "Great opportunity with moderate competition"
        },
        new CategoryRecommendation
        {
            Name = "Self-Help > Personal Development",
            Score = 72,
            RequiredDailySales = 25,
            SuccessProbability = "Good (72%)",
            Description = "Popular category but higher competition"
        },
        new CategoryRecommendation
        {
            Name = "Health & Fitness > Exercise & Fitness",
            Score = 68,
            RequiredDailySales = 30,
            SuccessProbability = "Moderate (68%)",
            Description = "Seasonal trends, best in January/summer"
        },
        new CategoryRecommendation
        {
            Name = "Computers & Technology > Programming",
            Score = 91,
            RequiredDailySales = 8,
            SuccessProbability = "Excellent (91%)",
            Description = "Highly targeted audience, lower competition"
        },
        new CategoryRecommendation
        {
            Name = "Education & Reference > Study Guides",
            Score = 77,
            RequiredDailySales = 20,
            SuccessProbability = "Good (77%)",
            Description = "Steady demand throughout the year"
        }
    };

    public async Task<List<CategoryRecommendation>> GetCategoryRecommendationsAsync(
        string bookTitle,
        string keywords,
        decimal price,
        int dailySalesTarget)
    {
        // Simulate async operation
        await Task.Delay(1500);

        // Filter and score categories based on user input
        var recommendations = SampleCategories
            .Where(c => c.RequiredDailySales <= dailySalesTarget * 2) // Show categories within reasonable range
            .OrderByDescending(c => c.Score)
            .Take(5)
            .ToList();

        // Adjust scores based on user's daily sales target
        foreach (var recommendation in recommendations)
        {
            if (recommendation.RequiredDailySales <= dailySalesTarget)
            {
                recommendation.Score = Math.Min(100, recommendation.Score + 10);
                recommendation.SuccessProbability = $"Excellent ({recommendation.Score}%)";
            }
            else if (recommendation.RequiredDailySales <= dailySalesTarget * 1.5)
            {
                recommendation.SuccessProbability = $"Good ({recommendation.Score}%)";
            }
            else
            {
                recommendation.Score = Math.Max(0, recommendation.Score - 10);
                recommendation.SuccessProbability = $"Challenging ({recommendation.Score}%)";
            }
        }

        return recommendations;
    }
}
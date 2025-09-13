using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KdpCategoryRanker.Models;

namespace KdpCategoryRanker.Services;

public interface ICategoryService
{
    Task<List<CategoryRecommendation>> GetCategoryRecommendationsAsync(
        string bookTitle,
        string keywords,
        decimal price,
        int dailySalesTarget);
}
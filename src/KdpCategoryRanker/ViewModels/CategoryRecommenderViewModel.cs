using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdpCategoryRanker.Models;
using KdpCategoryRanker.Services;

namespace KdpCategoryRanker.ViewModels;

public partial class CategoryRecommenderViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;

    [ObservableProperty]
    private string _bookTitle = string.Empty;

    [ObservableProperty]
    private string _keywords = string.Empty;

    [ObservableProperty]
    private string _price = "9.99";

    [ObservableProperty]
    private string _dailySalesTarget = "10";

    [ObservableProperty]
    private string _statusMessage = "Enter your book details and click 'Find Best Categories'";

    [ObservableProperty]
    private ObservableCollection<CategoryRecommendation> _categoryRecommendations = new();

    public ICommand FindCategoriesCommand { get; }

    public CategoryRecommenderViewModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
        FindCategoriesCommand = new AsyncRelayCommand(FindCategoriesAsync);
    }

    private async Task FindCategoriesAsync()
    {
        try
        {
            StatusMessage = "Analyzing categories...";
            CategoryRecommendations.Clear();

            if (string.IsNullOrWhiteSpace(BookTitle))
            {
                StatusMessage = "Please enter a book title.";
                return;
            }

            var recommendations = await _categoryService.GetCategoryRecommendationsAsync(
                BookTitle,
                Keywords,
                decimal.TryParse(Price, out var price) ? price : 9.99m,
                int.TryParse(DailySalesTarget, out var target) ? target : 10);

            foreach (var recommendation in recommendations)
            {
                CategoryRecommendations.Add(recommendation);
            }

            StatusMessage = $"Found {recommendations.Count} recommended categories.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
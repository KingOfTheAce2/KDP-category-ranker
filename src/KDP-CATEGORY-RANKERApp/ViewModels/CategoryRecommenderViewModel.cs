using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;
using KDP_CATEGORY_RANKERCore.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class CategoryRecommenderViewModel : ObservableObject
{
    private readonly ICategoryRecommendationService _categoryRecommendationService;
    private readonly IBestsellerCalculatorService _bestsellerCalculatorService;

    [ObservableProperty]
    private string bookTitle = string.Empty;

    [ObservableProperty]
    private string bookDescription = string.Empty;

    [ObservableProperty]
    private string keywords = string.Empty;

    [ObservableProperty]
    private decimal bookPrice = 9.99m;

    [ObservableProperty]
    private BookFormat selectedFormat = BookFormat.Kindle;

    [ObservableProperty]
    private Market selectedMarket = Market.AmazonCom;

    [ObservableProperty]
    private int maxDailySalesTarget = 10;

    [ObservableProperty]
    private DateTime plannedReleaseDate = DateTime.Now.AddMonths(1);

    [ObservableProperty]
    private DifficultyLevel maxDifficultyLevel = DifficultyLevel.Medium;

    [ObservableProperty]
    private bool includeHighCompetition = false;

    [ObservableProperty]
    private bool excludeGhostCategories = true;

    [ObservableProperty]
    private bool isSearching = false;

    [ObservableProperty]
    private string statusMessage = "Ready to find the best categories for your book";

    [ObservableProperty]
    private CategoryRecommendationDto? selectedRecommendation;

    [ObservableProperty]
    private BestsellerPlanningDto? bestsellerPlan;

    public ObservableCollection<CategoryRecommendationDto> Recommendations { get; } = new();
    public ObservableCollection<CategoryRecommendationDto> EasiestCategories { get; } = new();

    public ICollectionView RecommendationsView { get; }
    public ICollectionView EasiestCategoriesView { get; }

    public Array BookFormats => Enum.GetValues<BookFormat>();
    public Array Markets => Enum.GetValues<Market>();
    public Array DifficultyLevels => Enum.GetValues<DifficultyLevel>();

    public CategoryRecommenderViewModel(
        ICategoryRecommendationService categoryRecommendationService,
        IBestsellerCalculatorService bestsellerCalculatorService)
    {
        _categoryRecommendationService = categoryRecommendationService;
        _bestsellerCalculatorService = bestsellerCalculatorService;

        RecommendationsView = CollectionViewSource.GetDefaultView(Recommendations);
        EasiestCategoriesView = CollectionViewSource.GetDefaultView(EasiestCategories);

        // Set up sorting
        RecommendationsView.SortDescriptions.Add(new SortDescription(nameof(CategoryRecommendationDto.RecommendationScore), ListSortDirection.Descending));
        EasiestCategoriesView.SortDescriptions.Add(new SortDescription(nameof(CategoryRecommendationDto.DifficultyScore), ListSortDirection.Ascending));
    }

    [RelayCommand]
    private async Task SearchRecommendationsAsync()
    {
        if (string.IsNullOrWhiteSpace(BookTitle))
        {
            StatusMessage = "Please enter a book title";
            return;
        }

        IsSearching = true;
        StatusMessage = "Searching for the best categories...";
        Recommendations.Clear();

        try
        {
            var request = new CategoryRecommendationRequest
            {
                BookTitle = BookTitle,
                BookDescription = BookDescription,
                Keywords = Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(k => k.Trim())
                                  .ToList(),
                Format = SelectedFormat,
                Price = BookPrice,
                TargetMarket = SelectedMarket,
                MaxDailySalesTarget = MaxDailySalesTarget,
                PlannedReleaseDate = PlannedReleaseDate,
                IncludeHighCompetition = IncludeHighCompetition,
                ExcludeGhostCategories = ExcludeGhostCategories,
                MaxDifficultyLevel = MaxDifficultyLevel
            };

            var recommendations = await _categoryRecommendationService.GetCategoryRecommendationsAsync(request);
            
            foreach (var recommendation in recommendations)
            {
                Recommendations.Add(recommendation);
            }

            StatusMessage = recommendations.Count > 0 
                ? $"Found {recommendations.Count} recommended categories"
                : "No categories found matching your criteria. Try adjusting your filters.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching categories: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task LoadEasiestCategoriesAsync()
    {
        IsSearching = true;
        StatusMessage = "Loading easiest categories to rank in...";
        EasiestCategories.Clear();

        try
        {
            var easiest = await _categoryRecommendationService.GetEasiestCategoriesAsync(
                SelectedMarket, SelectedFormat, MaxDailySalesTarget);
            
            foreach (var category in easiest)
            {
                EasiestCategories.Add(category);
            }

            StatusMessage = $"Found {easiest.Count} easy categories for your target sales volume";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading easy categories: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task AnalyzeSelectedCategoryAsync()
    {
        if (SelectedRecommendation == null)
        {
            StatusMessage = "Please select a category to analyze";
            return;
        }

        StatusMessage = "Analyzing bestseller requirements...";

        try
        {
            var plan = await _bestsellerCalculatorService.GetBestsellerPlanningAsync(
                SelectedRecommendation.CategoryId,
                SelectedRecommendation.Market,
                BookPrice,
                PlannedReleaseDate);

            BestsellerPlan = plan;
            StatusMessage = "Bestseller analysis complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error analyzing category: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearResults()
    {
        Recommendations.Clear();
        EasiestCategories.Clear();
        SelectedRecommendation = null;
        BestsellerPlan = null;
        StatusMessage = "Results cleared";
    }

    [RelayCommand]
    private async Task ExportRecommendationsAsync()
    {
        if (!Recommendations.Any())
        {
            StatusMessage = "No recommendations to export";
            return;
        }

        try
        {
            // This would open a save dialog and export to CSV
            // For now, just show a message
            StatusMessage = $"Would export {Recommendations.Count} recommendations to CSV";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting: {ex.Message}";
        }
    }

    partial void OnSelectedRecommendationChanged(CategoryRecommendationDto? value)
    {
        if (value != null)
        {
            _ = AnalyzeSelectedCategoryAsync();
        }
    }

    partial void OnMaxDailySalesTargetChanged(int value)
    {
        if (value < 1) MaxDailySalesTarget = 1;
        if (value > 1000) MaxDailySalesTarget = 1000;
    }

    partial void OnBookPriceChanged(decimal value)
    {
        if (value < 0.99m) BookPrice = 0.99m;
        if (value > 999.99m) BookPrice = 999.99m;
    }
}
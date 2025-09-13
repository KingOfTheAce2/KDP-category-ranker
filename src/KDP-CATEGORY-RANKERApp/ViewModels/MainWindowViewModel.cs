using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private DateTime? lastSyncTime = DateTime.Now;

    [ObservableProperty]
    private string selectedMarket = "Amazon.com";

    public ObservableCollection<string> AvailableMarkets { get; } = new()
    {
        "Amazon.com",
        "Amazon.co.uk",
        "Amazon.de",
        "Amazon.fr",
        "Amazon.es",
        "Amazon.it",
        "Amazon.ca",
        "Amazon.com.au"
    };

    [RelayCommand]
    private void NavigateToCategoryRecommender()
    {
        StatusMessage = "Opening Category Recommender - Find the best categories for your book!";
    }

    [RelayCommand]
    private void NavigateToKeywordResearch()
    {
        StatusMessage = "Navigating to Keyword Research...";
    }

    [RelayCommand]
    private void NavigateToCategoryAnalyzer()
    {
        StatusMessage = "Navigating to Category Analyzer...";
    }

    [RelayCommand]
    private void NavigateToCompetitionAnalyzer()
    {
        StatusMessage = "Navigating to Competition Analyzer...";
    }

    [RelayCommand]
    private void NavigateToAmsGenerator()
    {
        StatusMessage = "Navigating to AMS Generator...";
    }

    [RelayCommand]
    private void OpenSettings()
    {
        StatusMessage = "Opening Settings...";
    }

    [RelayCommand]
    private void OpenAmazonAdsCourse()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://kindlepreneur.com/amazon-kdp-sales-rank-calculator/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening link: {ex.Message}";
        }
    }
}
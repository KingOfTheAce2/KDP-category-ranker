using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERApp.ViewModels;

public partial class FirstRunSetupViewModel : ObservableObject
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FirstRunSetupViewModel> _logger;

    [ObservableProperty]
    private string selectedMarket = "Amazon.com";

    [ObservableProperty]
    private bool useDemoMode = true;

    [ObservableProperty]
    private bool useLiveMode = false;

    [ObservableProperty]
    private string authorName = "";

    [ObservableProperty]
    private string selectedGenre = "Fiction";

    [ObservableProperty]
    private string selectedExperience = "Beginner";

    [ObservableProperty]
    private string selectedSalesGoal = "1-10 books/month";

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

    public ObservableCollection<string> PrimaryGenres { get; } = new()
    {
        "Fiction",
        "Non-Fiction",
        "Children's Books",
        "Romance",
        "Mystery & Thriller",
        "Science Fiction & Fantasy",
        "Self-Help",
        "Business & Economics",
        "Health & Fitness",
        "Cookbooks",
        "Biography & Memoir",
        "History",
        "Religion & Spirituality",
        "Travel",
        "Poetry",
        "Other"
    };

    public ObservableCollection<string> ExperienceLevels { get; } = new()
    {
        "Beginner (0-2 books published)",
        "Intermediate (3-10 books published)",
        "Advanced (11-50 books published)",
        "Expert (50+ books published)"
    };

    public ObservableCollection<string> SalesGoals { get; } = new()
    {
        "1-10 books/month",
        "11-50 books/month", 
        "51-100 books/month",
        "101-500 books/month",
        "500+ books/month"
    };

    public FirstRunSetupViewModel(IConfiguration configuration, ILogger<FirstRunSetupViewModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [RelayCommand]
    private void OpenHelp()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/your-repo/kdp-category-ranker/wiki",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open help documentation");
        }
    }

    [RelayCommand]
    private void CompleteSetup()
    {
        try
        {
            SaveUserPreferences();
            
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow is Views.MainWindow mw)
            {
                mw.CompleteFirstRunSetup();
            }
            
            _logger.LogInformation("First-run setup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete first-run setup");
        }
    }

    private void SaveUserPreferences()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "KDP Category Ranker");
        Directory.CreateDirectory(appFolder);
        
        var preferencesFile = Path.Combine(appFolder, "user-preferences.json");
        
        var preferences = new
        {
            DefaultMarket = SelectedMarket,
            DataMode = UseDemoMode ? "Demo" : "Live",
            AuthorProfile = new
            {
                Name = AuthorName,
                PrimaryGenre = SelectedGenre,
                Experience = SelectedExperience,
                SalesGoal = SelectedSalesGoal
            },
            SetupCompleted = true,
            SetupDate = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(preferences, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        File.WriteAllText(preferencesFile, json);
        
        _logger.LogInformation("User preferences saved to {PreferencesFile}", preferencesFile);
    }

    partial void OnUseDemoModeChanged(bool value)
    {
        if (value)
        {
            UseLiveMode = false;
        }
    }

    partial void OnUseLiveModeChanged(bool value)
    {
        if (value)
        {
            UseDemoMode = false;
        }
    }
}
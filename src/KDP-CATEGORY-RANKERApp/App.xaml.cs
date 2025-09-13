using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using KDP_CATEGORY_RANKERApp.ViewModels;
using KDP_CATEGORY_RANKERApp.Views;
using KDP_CATEGORY_RANKERApp.Services;
using KDP_CATEGORY_RANKERCore.Interfaces;
using KDP_CATEGORY_RANKERCore.Services;
using KDP_CATEGORY_RANKERData;
using KDP_CATEGORY_RANKERScraping.Services;
using Microsoft.EntityFrameworkCore;

namespace KDP_CATEGORY_RANKERApp;

public partial class App : Application
{
    private IHost? _host;
    
    public IServiceProvider? Services => _host?.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var builder = Host.CreateApplicationBuilder();
        
        // Configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        
        // Logging
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        
        // Services
        RegisterServices(builder.Services);
        
        _host = builder.Build();
        
        // Ensure portable directories exist
        var portableConfig = _host.Services.GetRequiredService<IPortableConfigService>();
        portableConfig.EnsureDirectoriesExist();
        
        await _host.StartAsync();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }

    private void RegisterServices(IServiceCollection services)
    {
        // Portable configuration service
        services.AddSingleton<IPortableConfigService, PortableConfigService>();
        
        // Database with portable path
        services.AddDbContext<KdpContext>((serviceProvider, options) =>
        {
            var portableConfig = serviceProvider.GetRequiredService<IPortableConfigService>();
            var databasePath = Path.Combine(portableConfig.GetDataDirectory(), "kdp-data.db");
            options.UseSqlite($"Data Source={databasePath}");
        });

        // Core services
        services.AddScoped<ISalesEstimationService, SalesEstimationService>();
        services.AddScoped<ICompetitiveScoreService, CompetitiveScoreService>();
        services.AddScoped<IKeywordResearchService, KeywordResearchService>();
        services.AddScoped<ICategoryAnalysisService, CategoryAnalysisService>();
        services.AddScoped<ICompetitionAnalysisService, CompetitionAnalysisService>();
        services.AddScoped<IReverseAsinService, ReverseAsinService>();
        services.AddScoped<IAmsKeywordService, AmsKeywordService>();
        
        // NEW: Category Recommendation services
        services.AddScoped<ICategoryRecommendationService, CategoryRecommendationService>();
        services.AddScoped<IBestsellerCalculatorService, BestsellerCalculatorService>();

        // Scraping services
        services.AddScoped<IAmazonScrapingService, AmazonScrapingService>();
        services.AddHttpClient();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CategoryRecommenderViewModel>(); // NEW: Featured view model
        services.AddTransient<KeywordResearchViewModel>();
        services.AddTransient<CategoryAnalyzerViewModel>();
        services.AddTransient<CompetitionAnalyzerViewModel>();
        services.AddTransient<ReverseAsinViewModel>();
        services.AddTransient<AmsGeneratorViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<FirstRunSetupWindow>();
        
        // ViewModels for setup
        services.AddTransient<FirstRunSetupViewModel>();
        
        // Auto-update services
        services.AddScoped<IAutoUpdateService, AutoUpdateService>();
        services.AddTransient<UpdateNotificationViewModel>();
        
        // License services
        services.AddSingleton<ILicenseKeyService, LicenseKeyService>();
        services.AddTransient<LicenseActivationViewModel>();
        
        // GitHub integration services
        services.AddScoped<IGitHubUpdateService, GitHubUpdateService>();
        services.AddTransient<GitHubUpdateViewModel>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }
}
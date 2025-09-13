using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using KdpCategoryRanker.Services;
using KdpCategoryRanker.ViewModels;

namespace KdpCategoryRanker;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<ICategoryService, CategoryService>();
                services.AddSingleton<IKeywordService, KeywordService>();
                services.AddSingleton<IUpdateService, GitHubUpdateService>();

                // Register ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<CategoryRecommenderViewModel>();

                // Register Views
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
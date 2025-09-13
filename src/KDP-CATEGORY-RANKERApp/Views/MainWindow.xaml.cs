using MahApps.Metro.Controls;
using KDP_CATEGORY_RANKERApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace KDP_CATEGORY_RANKERApp.Views;

public partial class MainWindow : MetroWindow
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
        
        CheckFirstRunSetup();
    }

    private void CheckFirstRunSetup()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "KDP Category Ranker");
        var preferencesFile = Path.Combine(appFolder, "user-preferences.json");
        
        if (!File.Exists(preferencesFile))
        {
            ShowFirstRunSetup();
        }
    }

    private void ShowFirstRunSetup()
    {
        Hide();
        
        var setupWindow = _serviceProvider.GetRequiredService<FirstRunSetupWindow>();
        setupWindow.ShowDialog();
        
        Show();
        Activate();
    }

    public void CompleteFirstRunSetup()
    {
        foreach (var window in Application.Current.Windows.OfType<FirstRunSetupWindow>())
        {
            window.Close();
        }
    }
}
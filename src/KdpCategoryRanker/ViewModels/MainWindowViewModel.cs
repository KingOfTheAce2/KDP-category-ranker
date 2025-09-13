using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdpCategoryRanker.Services;
using System.Windows.Input;

namespace KdpCategoryRanker.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string _updateStatus = "Ready";

    public ICommand CheckForUpdatesCommand { get; }

    public MainWindowViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateStatus = "Checking for updates...";
            var updateAvailable = await _updateService.CheckForUpdatesAsync();

            if (updateAvailable)
            {
                UpdateStatus = "Update available! Check GitHub releases.";
            }
            else
            {
                UpdateStatus = "You're running the latest version.";
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update check failed: {ex.Message}";
        }
    }
}
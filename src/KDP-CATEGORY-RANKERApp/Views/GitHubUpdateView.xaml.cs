using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using KDP_CATEGORY_RANKERApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KDP_CATEGORY_RANKERApp.Views;

public partial class GitHubUpdateView : UserControl
{
    public GitHubUpdateView()
    {
        InitializeComponent();
        
        if (System.Windows.Application.Current is App app && app.Services != null)
        {
            DataContext = app.Services.GetService<GitHubUpdateViewModel>();
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore navigation errors
        }
        
        e.Handled = true;
    }
}
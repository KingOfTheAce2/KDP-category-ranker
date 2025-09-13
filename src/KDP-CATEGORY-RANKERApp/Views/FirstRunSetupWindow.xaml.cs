using MahApps.Metro.Controls;
using KDP_CATEGORY_RANKERApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KDP_CATEGORY_RANKERApp.Views;

public partial class FirstRunSetupWindow : MetroWindow
{
    public FirstRunSetupWindow()
    {
        InitializeComponent();
        
        if (System.Windows.Application.Current is App app && app.Services != null)
        {
            DataContext = app.Services.GetService<FirstRunSetupViewModel>();
        }
    }
}
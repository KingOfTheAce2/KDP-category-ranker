using System.Windows.Controls;
using KDP_CATEGORY_RANKERApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KDP_CATEGORY_RANKERApp.Views;

public partial class CategoryRecommenderView : UserControl
{
    public CategoryRecommenderView()
    {
        InitializeComponent();
        
        // Set DataContext if available (will be injected at runtime)
        if (System.Windows.Application.Current is App app && app.Services != null)
        {
            DataContext = app.Services.GetService<CategoryRecommenderViewModel>();
        }
    }
}
using System;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using KdpCategoryRanker.ViewModels;

namespace KdpCategoryRanker;

public partial class MainWindow : MetroWindow
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
    }
}
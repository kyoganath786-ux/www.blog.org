using System.Windows;
using NickAI.App.ViewModels;

namespace NickAI.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

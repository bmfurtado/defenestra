using System.Windows;
using Defenestra.ViewModels;

namespace Defenestra.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        e.Cancel = true;
        Hide();
    }

    private void WindowComboBox_DropDownOpened(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RefreshWindows();
    }
}

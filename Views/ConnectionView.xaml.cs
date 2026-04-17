using System.Windows;
using System.Windows.Controls;
using SqlStressRunner.ViewModels;

namespace SqlStressRunner.Views;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}

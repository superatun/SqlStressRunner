using System.Windows.Controls;
using SqlStressRunner.ViewModels;

namespace SqlStressRunner.Views;

public partial class TestConfigurationView : UserControl
{
    public TestConfigurationView()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is TestConfigurationViewModel vm)
            {
                var mainViewModel = System.Windows.Application.Current.MainWindow?.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.ParameterMappingViewModel.PropertyChanged += (_, __) =>
                    {
                        vm.CacheMappings(mainViewModel.ParameterMappingViewModel.GetMappings());
                    };
                }
            }
        };
    }
}

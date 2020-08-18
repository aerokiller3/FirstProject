using System;
using System.Windows.Controls;
using RevitOpening.ViewModels;

namespace RevitOpening.UI
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public MainControl()
        {
            InitializeComponent();
        }

        private void TasksGrid_OnCurrentCellChanged(object sender, EventArgs e)
        {
            var mainVM = DataContext as MainVM;
            mainVM?.OnCurrentCellChanged(sender, e);
        }
    }
}

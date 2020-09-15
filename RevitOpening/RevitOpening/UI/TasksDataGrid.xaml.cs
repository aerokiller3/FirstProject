using System;
using System.Windows.Controls;
using RevitOpening.ViewModels;

namespace RevitOpening.UI
{
    /// <summary>
    /// Interaction logic for TasksDataGrid.xaml
    /// </summary>
    public partial class TasksDataGrid : UserControl
    {
        public TasksDataGrid()
        {
            InitializeComponent();
        }

        private void OnCurrentCellChanged(object sender, EventArgs e)
        {
            var updater = (IDataGridUpdater) DataContext;
            updater.ShowItemFromGrid(sender, e);
        }
    }
}

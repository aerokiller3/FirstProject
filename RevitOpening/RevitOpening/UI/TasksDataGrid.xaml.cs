using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

        private void TasksGrid_OnCurrentCellChanged(object sender, EventArgs e)
        {
            var mainVM = DataContext as IDataGridUpdater;
            mainVM?.OnCurrentCellChanged(sender, e);
        }
    }
}

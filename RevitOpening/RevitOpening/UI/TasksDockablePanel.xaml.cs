using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Models;
using RevitOpening.ViewModels;

namespace RevitOpening.UI
{
    public partial class TasksDockablePanel : UserControl, IDockablePaneProvider
    {
        public TasksDockablePanel()
        {
            InitializeComponent();
            var vm = DataContext as TaskDockablePanelVM;
            vm.Window = this;
        }

        private void TasksGrid_OnCurrentCellChanged(object sender, EventArgs e)
        {
            var taskDockablePanelVm = DataContext as TaskDockablePanelVM;
            taskDockablePanelVm?.OnCurrentCellChanged(sender, e);
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }
    }
}

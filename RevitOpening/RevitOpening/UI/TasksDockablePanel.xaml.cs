using Autodesk.Revit.UI;
using RevitOpening.ViewModels;
using System;
using System.Windows.Controls;

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

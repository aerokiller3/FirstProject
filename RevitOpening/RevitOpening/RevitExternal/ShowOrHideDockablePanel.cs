using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using RevitOpening.UI;
using RevitOpening.ViewModels;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class ShowOrHideDockablePanel : IExternalCommand
    {
        public ShowOrHideDockablePanel()
        {
            //Subscribe this event in case add-in manager fails to locate the Revit.Async.dll
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //RevitTask.Initialize();
            //RevitTask.RegisterGlobal(new TaskShowerExternalEventHandler());
            var pane = commandData.Application.ActiveUIDocument.Application.GetDockablePane(
                new DockablePaneId(new Guid(OpeningPanel.DockablePanelGuid)));
            pane.Hide();
            pane.Show();

            var tasksDockablePanel = new TasksDockablePanel();
            (tasksDockablePanel.DataContext as TaskDockablePanelVM).UpdateList(commandData.Application.Application.Documents.Cast<Document>());
            var window = new Window
            {
                Title = "Альтек Отверстия",
                Content = tasksDockablePanel,
                Width = 1000,
                MinWidth = 1000,
                Height = 450,
                MinHeight = 450,
            };

            window.Show();
            return Result.Succeeded;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            return name.Name == "Revit.Async"
                ? Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Revit.Async.dll"))
                : null;
        }
    }
}

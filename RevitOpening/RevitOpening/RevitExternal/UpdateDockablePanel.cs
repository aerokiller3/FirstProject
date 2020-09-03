using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.UI;
using RevitOpening.ViewModels;
using System;
using System.Linq;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class UpdateDockablePanel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var pane = commandData.Application.ActiveUIDocument.Application.GetDockablePane(
                new DockablePaneId(new Guid(OpeningPanel.DockablePanelGuid)));
            //pane.Hide();

            var tasksDockablePanel = new TasksDockablePanel();
            (tasksDockablePanel.DataContext as TaskDockablePanelVM)
                .UpdateList(commandData.Application.Application.Documents.Cast<Document>(),
                    commandData.Application.ActiveUIDocument.Document);
            pane.Show();
            return Result.Succeeded;
        }
    }
}

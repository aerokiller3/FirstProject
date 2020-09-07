namespace RevitOpening.RevitExternal
{
    using System;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using UI;
    using ViewModels;

    [Transaction(TransactionMode.Manual)]
    public class UpdateDockablePanel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var pane = commandData.Application.ActiveUIDocument.Application.GetDockablePane(
                new DockablePaneId(new Guid(OpeningPanel.DockablePanelGuid)));
            var app = commandData.Application;
            var documents = app.Application.Documents.Cast<Document>()
                               .ToList();
            var currentDocument = app.ActiveUIDocument.Document;
            var tasksDockablePanel = new TasksDockablePanel();
            ((TaskDockablePanelVM) tasksDockablePanel.DataContext).UpdateList(documents, currentDocument);
            pane.Show();
            return Result.Succeeded;
        }
    }
}
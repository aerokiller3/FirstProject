namespace RevitOpening.RevitExternal
{
    using System;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using EventHandlers;
    using Logic;
    using Revit.Async;
    using UI;
    using ViewModels;

    [Transaction(TransactionMode.Manual)]
    public class UpdateDockablePanel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var app = commandData.Application;
            var documents = app.Application.Documents.Cast<Document>()
                               .ToList();   
            var currentDocument = app.ActiveUIDocument.Document;

            Transactions.UpdateTasksInfo(currentDocument, documents, Extensions.Settings.Offset, Extensions.Settings.Diameter);

            var pane = commandData.Application.ActiveUIDocument.Application.GetDockablePane(
                new DockablePaneId(new Guid(OpeningPanel.DockablePanelGuid)));
            pane.Show();
            return Result.Succeeded;
        }
    }
}
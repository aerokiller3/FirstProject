namespace RevitOpening.RevitExternal
{
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Extensions;
    using LoggerClient;
    using Logic;
    using Settings = Extensions.Settings;

    [Transaction(TransactionMode.Manual)]
    public class CombineTwoBoxes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ModuleLogger.SendFunctionUseData(nameof(CombineTwoBoxes), nameof(RevitOpening));
            var app = commandData.Application;
            var currentDocument = app.ActiveUIDocument.Document;
            Transactions.LoadFamiliesToProject(currentDocument);
            var documents = app.Application.Documents.Cast<Document>()
                               .ToList();
            var selected = app.ActiveUIDocument.Selection.GetSelectedTasks(currentDocument);

            if (selected == null)
                return Result.Cancelled;

            var selectedList = selected.ToList();
            if (selectedList.Count != 2)
            {
                MessageBox.Show("Необходимо выбрать два задания");
                return Result.Failed;
            }

            Transactions.UpdateTaskInfo(currentDocument, documents, selectedList[0], Settings.Offset, Settings.Diameter);
            Transactions.UpdateTaskInfo(currentDocument, documents, selectedList[1], Settings.Offset, Settings.Diameter);
            var newTask = Transactions
               .CombineSelectedTasks(currentDocument, documents, selectedList[0], selectedList[1]);

            if (newTask == null)
            {
                MessageBox.Show("Недопустимый вариант для автоматического объединения");
                return Result.Failed;
            }

            Transactions.UpdateTaskInfo(currentDocument, documents, newTask, Settings.Offset, Settings.Diameter);
            return Result.Succeeded;
        }
    }
}
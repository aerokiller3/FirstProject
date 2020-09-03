using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using System.Linq;
using System.Windows;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class CombineTwoBoxes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var currentDocument = commandData.Application.ActiveUIDocument.Document;
            var documents = commandData.Application.Application.Documents.Cast<Document>();
            var selected = commandData.Application.ActiveUIDocument.Selection.GetSelectedTasks(currentDocument);

            if (selected == null)
                return Result.Cancelled;

            if (selected.Count != 2)
            {
                MessageBox.Show("Необходимо выбрать два задания");
                return Result.Failed;
            }

            Transactions.CombineSelectedTasks(currentDocument, documents, selected[0], selected[1], out var newTask);

            if (newTask == null)
            {
                MessageBox.Show("Недопустимый вариант для автоматического объединения");
                return Result.Failed;
            }

            Transactions.UpdateTaskInfo(currentDocument, documents, newTask);

            return Result.Succeeded;
        }
    }
}
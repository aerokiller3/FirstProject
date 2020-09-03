using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class ChangeSelectedTasksToOpenings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var currentDocument = commandData.Application.ActiveUIDocument.Document;
            var selected = commandData.Application.ActiveUIDocument.Selection.GetSelectedTasks(currentDocument);
            if (selected == null)
                return Result.Cancelled;

            var openings = new List<Element>();
            var statuses = selected.Select(el => el.LookupParameter("Несогласованно")
                .AsInteger());

            if (statuses.Any(s => s == 1))
            {
                var box = MessageBox.Show("Одно или более заданий не согласованы!\nПродолжить выполнение?",
                    "Вырезание отверстий", MessageBoxButton.YesNo);
                if (box == MessageBoxResult.No)
                    return Result.Cancelled;
            }

            Transactions.CreateOpeningInSelectedTask(currentDocument, openings, selected);
            Transactions.Drawing(currentDocument, openings);

            return Result.Succeeded;
        }
    }
}

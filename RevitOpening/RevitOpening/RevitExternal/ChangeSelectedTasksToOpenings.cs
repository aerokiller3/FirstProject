using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitOpening.Extensions;
using RevitOpening.Logic;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class ChangeSelectedTasksToOpenings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var currentDocument = commandData.Application.ActiveUIDocument.Document;
            var documents = commandData.Application.Application.Documents.Cast<Document>();
            var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes(currentDocument, documents, 0, 0);
            var select = commandData.Application.ActiveUIDocument.Selection;
            var selected = select.PickObjects(ObjectType.Element, new SelectionFilter(x => x.IsTask(),
                    (x, _) => true))
                .Select(x => currentDocument.GetElement(x))
                .ToArray();
            var openings = new List<Element>();
            using (var t = new Transaction(currentDocument, "Change Tasks To Openings"))
            {
                t.Start();
                var statuses = selected.Select(el => el.LookupParameter("Несогласованно")
                    .AsInteger());

                if (statuses.Any(s => s == 1))
                {
                    var box = MessageBox.Show("Одно или более заданий не согласованы!\nПродолжить выполнение?",
                        "Вырезание отверстий",
                        MessageBoxButton.YesNo);
                    if (box == MessageBoxResult.No)
                        return Result.Cancelled;
                }

                openings.AddRange(createOpeningInTaskBoxes.SwapTasksToOpenings(selected.Cast<FamilyInstance>()));
                t.Commit();
            }

            using (var t = new Transaction(currentDocument, "Drawing"))
            {
                t.Start();
                foreach (var el in openings)
                {
                    var v = el.LookupParameter("Отверстие_Дисциплина").AsString();
                    el.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                    el.LookupParameter("Отверстие_Дисциплина").Set(v);
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
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
    public class CombineTwoBoxes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData.Application.ActiveUIDocument.Document;
            var documents = commandData.Application.Application.Documents.Cast<Document>();

            var select = commandData.Application.ActiveUIDocument.Selection;
            var selected = select.PickObjects(ObjectType.Element, new SelectionFilter(x => x.IsTask(),
                    (x, _) => true))
                .Select(x => document.GetElement(x))
                .ToArray();
            FamilyInstance newTask; 
            using (var t = new Transaction(document, "Объединение заданий"))
            {
                t.Start();
                newTask = BoxCombiner.CombineTwoBoxes(documents, document, selected[0], selected[1]);
                t.Commit();
            }

            if (newTask == null)
            {
                MessageBox.Show("Недопустимый вариант для автоматического объединения");
                return Result.Failed;
            }

            using (var t = new Transaction(document, "Обновление информации об элементе"))
            {
                t.Start();
                var data = newTask.GetParentsData();
                var walls = documents.GetAllElementsOfClass<Wall>();
                var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
                var tasks = documents.GetAllTasks();
                newTask.AnalyzeElement(data, walls, floors, tasks, documents, 0, 0);
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
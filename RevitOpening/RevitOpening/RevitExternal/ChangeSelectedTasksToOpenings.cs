namespace RevitOpening.RevitExternal
{
    using System;
    using System.Collections.Generic;
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
    public class ChangeSelectedTasksToOpenings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ModuleLogger.SendFunctionUseData(nameof(ChangeSelectedTasksToOpenings), nameof(RevitOpening));
            var currentDocument = commandData.Application.ActiveUIDocument.Document;
            var documents = commandData.Application.Application.Documents.Cast<Document>()
                                       .ToList();
            Transactions.LoadFamiliesToProject(currentDocument);
            var selected = commandData.Application.ActiveUIDocument.Selection
                                      .GetSelectedTasks(currentDocument);
            if (selected == null)
                return Result.Cancelled;

            var selectedList = selected.ToList();
            var openings = new List<Element>();
            var statuses = selectedList.Select(el => el.LookupParameter("Несогласованно")
                                                       .AsInteger());

            if (statuses.Any(s => s == 1))
            {
                var box = MessageBox.Show("Одно или более заданий не согласованы!\nПродолжить выполнение?",
                    "Вырезание отверстий", MessageBoxButton.YesNo);
                if (box == MessageBoxResult.No)
                    return Result.Cancelled;
            }

            try
            {
                if (selectedList.Count == 1)
                    Transactions.UpdateTaskInfo(currentDocument, documents, selectedList[0], Settings.Offset, Settings.Diameter);
                var elementsData = selectedList.Select(el => el.GetParentsDataFromSchema());
                if (elementsData.Any(d => d.BoxData.HostsGeometries.Count == 0
                    || d.BoxData.PipesGeometries.Count == 0 || d.BoxData.Collisions.Count > 0))
                {
                    var result = MessageBox.Show("Одно или более отверстий невозможно вырезать автоматически\n" +
                        "Всё равно попытаться вырезать?",
                        "Вырезание", MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes)
                        return Result.Failed;
                }
            }
            catch
            {
                MessageBox.Show("Обновите информацию об отверстиях");
                return Result.Failed;
            }

            Transactions.CreateOpeningInSelectedTask(currentDocument, openings, selectedList);
            Transactions.Drawing(currentDocument, openings);

            return Result.Succeeded;
        }
    }
}
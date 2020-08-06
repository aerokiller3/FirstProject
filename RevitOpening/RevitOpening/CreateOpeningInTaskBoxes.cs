using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class CreateOpeningInTaskBoxes : IExternalCommand
    {
        private Document _document;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            var allDocs = commandData.Application.Application
                .Documents.Cast<Document>();
            new FamilyLoader(_document).LoadAllFamiliesToProject();
            var wallRectTasks = GetTasksFromDocument(Families.WallRectTaskFamily);
            var wallRoundTasks = GetTasksFromDocument(Families.WallRoundTaskFamily);
            var floorRectTasks = GetTasksFromDocument(Families.FloorRectTaskFamily); ;

            var chekedWallRectTasks = GetCheckedBoxes(wallRectTasks);

            return Result.Succeeded;
        }

        private IEnumerable<Element> GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            return wallRectTasks.Where(wallRectTask => CheckElement(wallRectTask));
        }

        private bool CheckElement(Element wallRectTask)
        {
            var parentsData = JsonConvert.DeserializeObject(wallRectTask.LookupParameter("Info").AsValueString(),
                typeof(OpeningParentsData)) as OpeningParentsData;


        }

        private IEnumerable<Element> GetTasksFromDocument(FamilyParameters familyParameters)
        {
            return new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .Where(e => e.Name == familyParameters.Name);
        }
    }
}

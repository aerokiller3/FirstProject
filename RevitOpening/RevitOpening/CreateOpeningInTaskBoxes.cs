using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
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
            var wallRectTasks = GetTasksFromDocument(Families.WallRectTaskFamily).ToList();
            var wallRoundTasks = GetTasksFromDocument(Families.WallRoundTaskFamily);
            var floorRectTasks = GetTasksFromDocument(Families.FloorRectTaskFamily); ;

            var chekedWallRectTasks = GetCheckedBoxes(wallRectTasks).ToList();

            return Result.Succeeded;
        }

        private IEnumerable<Element> GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            return wallRectTasks.Where(wallRectTask => CheckElement(wallRectTask)).ToList();
        }

        private bool CheckElement(Element wallRectTask)
        {
            var json = wallRectTask.LookupParameter("Info").AsString();
            try
            {

                var parentsData = JsonConvert.DeserializeObject<OpeningParentsData>(json);
                var oldPipe = _document.GetElement(new ElementId(parentsData.PipeId));
                switch (parentsData.PipeType)
                {
                }
            }
            catch
            {

            }
            return false;
        }

        private bool CheckPipeParametrs(Element oldPipe)
        {
            return false;
        }

        private IEnumerable<Element> GetTasksFromDocument(FamilyParameters familyParameters)
        {
            var collector = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance));

            return  collector.Where(e => e.Name == familyParameters.InstanseName)
                .ToList();
        }
    }
}

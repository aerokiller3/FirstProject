using System;
using System.Collections.Generic;
using System.Linq;
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

        private AltecJsonSchema _schema;

        private IEnumerable<Document> _documents;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _schema = new AltecJsonSchema();
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application
                .Documents.Cast<Document>();
            new FamilyLoader(_document).LoadAllFamiliesToProject();
            var wallRectTasks = GetTasksFromDocument(Families.WallRectTaskFamily);
            var wallRoundTasks = GetTasksFromDocument(Families.WallRoundTaskFamily);
            var floorRectTasks = GetTasksFromDocument(Families.FloorRectTaskFamily);

            var chekedWallRectTasks = GetCheckedBoxes(wallRectTasks);
            var chekedWallRoundTasks = GetCheckedBoxes(wallRoundTasks);
            var chekedFloorRectTasks = GetCheckedBoxes(floorRectTasks);

            SwapTasksToOpenings(chekedWallRectTasks);
            SwapTasksToOpenings(chekedWallRoundTasks);
            SwapTasksToOpenings(chekedFloorRectTasks);

            return Result.Succeeded;
        }

        private void SwapTasksToOpenings(IEnumerable<Element> elements)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create opening");
                foreach (var task in elements.Cast<FamilyInstance>())
                {
                    if (task.Name == Families.WallRoundTaskFamily.InstanseName)
                    {
                        var familyData = Families.GetDataFromInstanseName(task.Name);
                        var familySymbol = Families.GetFamilySymbol(_document, familyData.SymbolName);
                        var parentsData = GetParentsData(task);
                        var collector = new FilteredElementCollector(_document)
                            .OfClass(parentsData.HostType);
                        var host = _document.GetElement(new ElementId(parentsData.HostId));
                        BoxCreator.CreateTaskBox(familyData,familySymbol,host, parentsData.BoxData, parentsData,_document,_schema);
                    }  
                    else if (task.Name == Families.WallRectTaskFamily.InstanseName)
                    {

                    }
                    else if (task.Name == Families.FloorRectTaskFamily.InstanseName)
                    {

                    }
                    else
                    {
                        throw new Exception("Неизвестный экземпляр семейства");
                    }
                }
            }
        }

        private IEnumerable<Element> GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            return wallRectTasks.Where(wallRectTask => CheckElement(wallRectTask)).ToList();
        }

        private bool CheckElement(Element element)
        {
            var parentsData = GetParentsData(element);
            var pipe = Extensions.GetElementFromDocuments(_documents, parentsData.PipeId);
            var wall = Extensions.GetElementFromDocuments(_documents, parentsData.HostId);
            var isOldPipe = CheckElementParametrs(pipe, parentsData.BoxData.PipeGeometry);
            var isOldWall = CheckElementParametrs(wall, parentsData.BoxData.WallGeometry);
            var isOldBox = CheckBoxParametrs(element, parentsData.BoxData);
            return isOldBox && isOldPipe && isOldWall;
        }

        private bool CheckBoxParametrs(Element wallRectTask, OpeningParametrs boxData)
        {
            var toleranse = Math.Pow(10, -7);
            var familyInstanse = wallRectTask as FamilyInstance;
            var familyData = Families.GetDataFromInstanseName(familyInstanse.Name);
            var locPoint = new MyXYZ((familyInstanse.Location as LocationPoint).Point);
            var width = wallRectTask.LookupParameter(familyData.WidthName).AsDouble();
            var height = wallRectTask.LookupParameter(familyData.HeightName).AsDouble();
            return locPoint.Equals(boxData.IntersectionCenter) &&
                   Math.Abs(width - boxData.Width) < toleranse &&
                   Math.Abs(height - boxData.Heigth) < toleranse;
        }

        private OpeningParentsData GetParentsData(Element element)
        {
            return JsonConvert.DeserializeObject<OpeningParentsData>(_schema.GetJson(element));
        }

        private bool CheckElementParametrs(Element element, ElementGeometry oldData)
        {
            var isOld = oldData.Equals(new ElementGeometry(element));
            return isOld;
        }

        private IEnumerable<Element> GetTasksFromDocument(FamilyParameters familyParameters)
        {
            var collector = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance));

            return collector.Where(e => e.Name == familyParameters.InstanseName)
                .ToList();
        }
    }
}
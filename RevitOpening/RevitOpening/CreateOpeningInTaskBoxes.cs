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

        private IEnumerable<Document> _documents;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
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

            //SwapTasksToOpenings(chekedWallRectTasks);
            //SwapTasksToOpenings(chekedWallRoundTasks);
            //SwapTasksToOpenings(chekedFloorRectTasks);

            return Result.Succeeded;
        }

        //private void SwapTasksToOpenings(IEnumerable<Element> elements)
        //{
        //    using (var transaction = new Transaction(_document))
        //    {
        //        transaction.Start("Create opening");
        //        foreach (var task in elements.Cast<FamilyInstance>())
        //        {
        //            switch (task.Name)
        //            {

        //            }
        //        }
        //        switch (@enum)
        //        {

        //        }

        //    }
        //}

        private IEnumerable<Element> GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            return wallRectTasks.Where(wallRectTask => CheckElement(wallRectTask)).ToList();
        }

        private bool CheckElement(Element wallRectTask)
        {
            var json = wallRectTask.LookupParameter("Info").AsString();
            
            var parentsData = JsonConvert.DeserializeObject<OpeningParentsData>(json);
            var pipe = Extensions.GetElementFromDocuments(_documents, parentsData.PipeId);
            var wall = Extensions.GetElementFromDocuments(_documents, parentsData.WallId);
            var isOldPipe = CheckElementParametrs(pipe, parentsData.BoxData.PipeGeometry);
            var isOldWall = CheckElementParametrs(wall, parentsData.BoxData.WallGeometry);
            var isOldBox = CheckBoxParametrs(wallRectTask, parentsData.BoxData);
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
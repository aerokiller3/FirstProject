using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class CreateOpeningInTaskBoxes : IExternalCommand
    {
        private double _offset;
        private double _maxDiametr;
        private Document _document;

        private IEnumerable<Document> _documents;

        private AltecJsonSchema _schema;

        public void SetTasksParametrs(string offset, string diametr)
        {
            _offset = double.Parse(offset);
            _maxDiametr = double.Parse(diametr);
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _schema = new AltecJsonSchema();
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application
                .Documents.Cast<Document>();
            new FamilyLoader(_document).LoadAllFamiliesToProject();
            var wallRectTasks = _document.GetTasksFromDocument(Families.WallRectTaskFamily);
            var wallRoundTasks = _document.GetTasksFromDocument(Families.WallRoundTaskFamily);
            var floorRectTasks = _document.GetTasksFromDocument(Families.FloorRectTaskFamily);

            var chekedWallRectTasks = GetCheckedBoxes(wallRectTasks);
            var chekedWallRoundTasks = GetCheckedBoxes(wallRoundTasks);
            var chekedFloorRectTasks = GetCheckedBoxes(floorRectTasks);

            SwapTasksToOpenings(chekedWallRectTasks.Item1);
            SwapTasksToOpenings(chekedWallRoundTasks.Item1);
            SwapTasksToOpenings(chekedFloorRectTasks.Item1);

            return Result.Succeeded;
        }

        private void SwapTasksToOpenings(IEnumerable<Element> elements)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create opening");
                foreach (var task in elements.Cast<FamilyInstance>())
                {
                    var familyData = ChooseFamily(task.Name);
                    var parentsData = task.GetParentsData(_schema);
                    parentsData.BoxData.FamilyName = familyData.SymbolName;
                    _document.Delete(task.Id);
                    //
                    // BoxCalculator fix
                    //
                    //if (familyData == Families.WallRectOpeningFamily)
                    //parentsData.BoxData.IntersectionCenter += new MyXYZ(0, 0, parentsData.BoxData.Heigth / 2);
                    BoxCreator.CreateTaskBox(parentsData, _document, _schema);
                }

                transaction.Commit();
            }
        }

        private FamilyParameters ChooseFamily(string taskName)
        {
            FamilyParameters familyData;
            if (taskName == Families.WallRoundTaskFamily.InstanseName)
                familyData = Families.WallRoundOpeningFamily;
            else if (taskName == Families.WallRectTaskFamily.InstanseName)
                familyData = Families.WallRectOpeningFamily;
            else if (taskName == Families.FloorRectTaskFamily.InstanseName)
                familyData = Families.FloorRectOpeningFamily;
            else
                throw new Exception("Неизвестный экземпляр семейства");

            return familyData;
        }

        private (IEnumerable<Element>, IEnumerable<Element>) GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            var cheked = new List<Element>();
            var uncheked = new List<Element>();
            foreach (var element in wallRectTasks)
                if (CheckElement(element))
                    cheked.Add(element);
                else
                    uncheked.Add(element);
            return (cheked, uncheked);
        }

        private bool CheckElement(Element element)
        {
            var isAgreed = ChekcAgreed(element);
            if (!isAgreed)
                return false;

            var parentsData = element.GetParentsData(_schema);
            var pipe = _documents.GetElementFromDocuments(parentsData.PipeId);
            var wall = _documents.GetElementFromDocuments(parentsData.HostId);
            var isOldPipe = parentsData.BoxData.PipeGeometry.Equals(new ElementGeometry(pipe));
            var isOldWall = parentsData.BoxData.WallGeometry.Equals(new ElementGeometry(wall));
            var isOldBox = CheckBoxParametrs(element, parentsData.BoxData);
            var isImmutable = isOldBox && isOldPipe && isOldWall;
            if (!isImmutable)
                isImmutable = MatchOldAndNewTask(pipe, wall, parentsData);
            return isImmutable;
        }

        private bool MatchOldAndNewTask(Element pipeElement, Element hostElement, OpeningParentsData parentsData)
        {
            var boxCalculator = new BoxCalculator();
            var pipe = pipeElement as MEPCurve;
            switch (hostElement)
            {
                case CeilingAndFloor floor:
                    return MatchTasks(boxCalculator, pipe, floor, parentsData);
                case Wall wall:
                    return MatchTasks(boxCalculator, pipe, wall, parentsData);
                default:
                    throw new Exception("Неизвестный тип хост-элемента");
            }
        }

        private bool MatchTasks(BoxCalculator boxCalculator, MEPCurve pipeElement, Element host,
            OpeningParentsData parentsData)
        {
            var parametrs = boxCalculator.CalculateBoxInElement(host, pipeElement, _offset, _maxDiametr);
            return parametrs != null && parentsData.BoxData.Equals(parametrs);
        }

        private bool ChekcAgreed(Element box)
        {
            var parametrN = box.LookupParameter("Несогласованно");
            var ni = parametrN?.AsInteger();
            return ni == 0;
        }

        private bool CheckBoxParametrs(Element wallRectTask, OpeningData boxData)
        {
            var toleranse = Math.Pow(10, -7);
            var familyInstanse = wallRectTask as FamilyInstance;
            var familyData = Families.GetDataFromInstanseName(familyInstanse.Name);
            var locPoint = new MyXYZ((familyInstanse.Location as LocationPoint).Point);
            double width, height;
            try
            {
                width = wallRectTask.LookupParameter(familyData.WidthName).AsDouble();
                height = wallRectTask.LookupParameter(familyData.HeightName).AsDouble();
            }
            catch
            {
                width = height = wallRectTask.LookupParameter(familyData.DiametrName).AsDouble();
            }

            return locPoint.Equals(boxData.IntersectionCenter) &&
                   Math.Abs(width - boxData.Width) < toleranse &&
                   Math.Abs(height - boxData.Heigth) < toleranse;
        }
    }
}
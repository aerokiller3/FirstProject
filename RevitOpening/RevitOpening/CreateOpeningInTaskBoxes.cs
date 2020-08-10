using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
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

        private double _offset = 300;

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
                    var (familyData, familySymbol) = ChooseFamily(task.Name);
                    var parentsData = GetParentsData(task);
                    var host = _documents.GetElementFromDocuments(parentsData.HostId);
                    _document.Delete(task.Id);

                    //
                    // WallBoxCalculator fix
                    //
                    if (familyData == Families.WallRectOpeningFamily)
                        parentsData.BoxData.IntersectionCenter += new MyXYZ(0, 0, parentsData.BoxData.Heigth / 2);

                    BoxCreator.CreateTaskBox(familyData, familySymbol, host, parentsData.BoxData, parentsData, _document, _schema);
                }

                transaction.Commit();
            }
        }

        private (FamilyParameters, FamilySymbol) ChooseFamily(string taskName)
        {
            FamilyParameters familyData;
            FamilySymbol familySymbol;
            if (taskName == Families.WallRoundTaskFamily.InstanseName)
            {
                familyData = Families.WallRoundOpeningFamily;
                familySymbol = Families.GetFamilySymbol(_document, familyData.SymbolName);
            }
            else if (taskName == Families.WallRectTaskFamily.InstanseName)
            {
                familyData = Families.WallRectOpeningFamily;
                familySymbol = Families.GetFamilySymbol(_document, familyData.SymbolName);
            }
            else if (taskName == Families.FloorRectTaskFamily.InstanseName)
            {
                familyData = Families.FloorRectOpeningFamily;
                familySymbol = Families.GetFamilySymbol(_document, familyData.SymbolName);
            }
            else
            {
                throw new Exception("Неизвестный экземпляр семейства");
            }

            return (familyData, familySymbol);
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
            return (cheked,uncheked);
        }

        private bool CheckElement(Element element)
        {
            var isAgreed = ChekcAgreed(element);
            if (!isAgreed)
                return false;
            var parentsData = GetParentsData(element);
            var pipe = _documents.GetElementFromDocuments(parentsData.PipeId);
            var wall = _documents.GetElementFromDocuments(parentsData.HostId);
            var isOldPipe = CheckElementParametrs(pipe, parentsData.BoxData.PipeGeometry);
            var isOldWall = CheckElementParametrs(wall, parentsData.BoxData.WallGeometry);
            var isOldBox = CheckBoxParametrs(element, parentsData.BoxData);
            var isImmutable = isOldBox && isOldPipe && isOldWall;
            if (!isImmutable)
                isImmutable = MatchOldAndNewTask(pipe, wall, parentsData);
            return isImmutable;
        }

        private bool MatchOldAndNewTask(Element pipeElement, Element hostElement, OpeningParentsData parentsData)
        {
            switch (hostElement)
            {
                case CeilingAndFloor floor:
                    return MatchTasks(new FloorBoxCalculator(), pipeElement as MEPCurve, floor, parentsData);
                case Wall wall:
                    return MatchTasks(new WallBoxCalculator(), pipeElement as MEPCurve, wall, parentsData);
                default:
                    throw new Exception("Неизвестный тип хост-элемента");
            }
        }

        private bool MatchTasks(IBoxCalculator boxCalculator, MEPCurve pipeElement, Element host, OpeningParentsData parentsData)
        {
            var parametrs = boxCalculator.CalculateBoxInElement(host, pipeElement, _offset,
                Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName));
            return parametrs != null && IsParametrsEquals(parentsData.BoxData, parametrs);
        }

        private bool ChekcAgreed(Element box)
        {
            var parametrN = box.LookupParameter("Несогласованно");
            var ni = parametrN?.AsInteger();

            return ni == 0;
        }

        private bool IsParametrsEquals(OpeningParametrs oldParametrs, OpeningParametrs parametrs)
        {
            var toleranse = Math.Pow(10, -7);
            return Math.Abs(parametrs.Depth - oldParametrs.Depth) < toleranse
                   && Math.Abs(parametrs.Heigth - oldParametrs.Heigth) < toleranse
                   && Math.Abs(parametrs.Width - oldParametrs.Width) < toleranse
                   && parametrs.Direction.Equals(oldParametrs.Direction)
                   && parametrs.IntersectionCenter.Equals(oldParametrs.IntersectionCenter)
                   && parametrs.FamilyName.Equals(oldParametrs.FamilyName);
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
            return oldData.Equals(new ElementGeometry(element));
        }
    }
}
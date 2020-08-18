using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class CreateOpeningInTaskBoxes : IExternalCommand
    {
        private Document _document;
        private IEnumerable<Document> _documents;
        private double _maxDiameter;
        private double _offset;

        private AltecJsonSchema _schema;

        public CreateOpeningInTaskBoxes(Document document, IEnumerable<Document> documents)
        {
            _document = document;
            _documents = documents;
            _schema = new AltecJsonSchema();
        }

        public CreateOpeningInTaskBoxes()
        {
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

            var checkedWallRectTasks = GetCheckedBoxes(wallRectTasks);
            var checkedWallRoundTasks = GetCheckedBoxes(wallRoundTasks);
            var checkedFloorRectTasks = GetCheckedBoxes(floorRectTasks);

            SwapTasksToOpenings(checkedWallRectTasks.Item1);
            SwapTasksToOpenings(checkedWallRoundTasks.Item1);
            SwapTasksToOpenings(checkedFloorRectTasks.Item1);

            return Result.Succeeded;
        }

        public void SetTasksParameters(string offset, string diameter)
        {
            _offset = double.Parse(offset);
            _maxDiameter = double.Parse(diameter);
        }

        public void SwapTasksToOpenings(IEnumerable<Element> elements)
        {
            var elementList = new List<Element>();
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
                    //parentsData.BoxData.IntersectionCenter += new MyXYZ(0, 0, parentsData.BoxData.Height / 2);
                    var el = BoxCreator.CreateTaskBox(parentsData, _document, _schema);
                    elementList.Add(el);
                }

                transaction.Commit();
            }

            using (var t = new Transaction(_document))
            {
                t.Start("UP");
                foreach (var el in elementList)
                {
                    var v = el.LookupParameter("Отверстие_Дисциплина").AsString();
                    el.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                    el.LookupParameter("Отверстие_Дисциплина").Set(v);
                }

                t.Commit();
            }
        }

        private FamilyParameters ChooseFamily(string taskName)
        {
            FamilyParameters familyData;
            if (taskName == Families.WallRoundTaskFamily.InstanceName)
                familyData = Families.WallRoundOpeningFamily;
            else if (taskName == Families.WallRectTaskFamily.InstanceName)
                familyData = Families.WallRectOpeningFamily;
            else if (taskName == Families.FloorRectTaskFamily.InstanceName)
                familyData = Families.FloorRectOpeningFamily;
            else
                throw new Exception("Неизвестный экземпляр семейства");

            return familyData;
        }

        private (IEnumerable<Element>, IEnumerable<Element>) GetCheckedBoxes(IEnumerable<Element> wallRectTasks)
        {
            var checkedElements = new List<Element>();
            var uncheckedElements = new List<Element>();
            foreach (var element in wallRectTasks)
                if (CheckElement(element))
                {
                    checkedElements.Add(element);
                }
                else
                {
                    uncheckedElements.Add(element);
                    element.LookupParameter("Несогласованно").Set(0);
                    var data = element.GetParentsData(_schema);
                    data.BoxData.Collisions.Add(Collisions.TaskNotActual);
                }

            return (checkedElements, uncheckedElements);
        }

        private bool CheckElement(Element element)
        {
            var isAgreed = CheckAgreed(element);
            if (!isAgreed)
                return false;

            var parentsData = element.GetParentsData(_schema);
            var pipe = _documents.GetElementFromDocuments(parentsData.PipeId);
            var wall = _documents.GetElementFromDocuments(parentsData.HostId);
            var isOldPipe = parentsData.BoxData.PipeGeometry.Equals(new ElementGeometry(pipe,
                new MyXYZ(((Line) ((LocationCurve) pipe.Location).Curve).Direction)));
            var isOldWall = parentsData.BoxData.WallGeometry.Equals(new ElementGeometry(wall,
                wall is Wall wall1 ? new MyXYZ(wall1.Orientation) : new MyXYZ(0,0,-1)));
            var isOldBox = CheckBoxParametrs(element, parentsData.BoxData);
            var isImmutable = isOldBox && isOldPipe && isOldWall;
            if (!isImmutable)
                isImmutable = MatchOldAndNewTask(pipe, wall, parentsData);
            return isImmutable;
        }

        private bool MatchOldAndNewTask(Element pipeElement, Element host, OpeningParentsData parentsData)
        {
            var boxCalculator = new BoxCalculator();
            var pipe = pipeElement as MEPCurve;
            var parameters = boxCalculator.CalculateBoxInElement(host, pipe, _offset, _maxDiameter);
            return parameters != null && parentsData.BoxData.Equals(parameters);
        }

        private bool CheckAgreed(Element box)
        {
            var parameterN = box.LookupParameter("Несогласованно");
            var ni = parameterN?.AsInteger();
            return ni == 0;
        }

        private bool CheckBoxParametrs(Element wallRectTask, OpeningData boxData)
        {
            var tolerance = Math.Pow(10, -7);
            var familyInstance = wallRectTask as FamilyInstance;
            var familyParameters = Families.GetDataFromInstanseName(familyInstance.Name);
            var locPoint = new MyXYZ(((LocationPoint) familyInstance.Location).Point);
            double width, height;
            try
            {
                if (familyParameters == Families.FloorRectTaskFamily)
                {
                    width = wallRectTask.LookupParameter(familyParameters.HeightName).AsDouble();
                    height = wallRectTask.LookupParameter(familyParameters.WidthName).AsDouble();
                }
                else
                {
                    height = wallRectTask.LookupParameter(familyParameters.HeightName).AsDouble();
                    width = wallRectTask.LookupParameter(familyParameters.WidthName).AsDouble();
                }
            }
            catch
            {
                width = height = wallRectTask.LookupParameter(familyParameters.DiameterName).AsDouble();
            }

            return locPoint.Equals(boxData.IntersectionCenter) &&
                   Math.Abs(width - boxData.Width) < tolerance &&
                   Math.Abs(height - boxData.Height) < tolerance;
        }
    }
}
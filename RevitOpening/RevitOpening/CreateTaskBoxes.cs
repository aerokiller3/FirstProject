using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class CreateTaskBoxes : IExternalCommand
    {
        private Document _document;

        private AltecJsonSchema _schema;

        private double _offset = 300;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _schema = new AltecJsonSchema();

            var allDocs = commandData.Application.Application
                .Documents.Cast<Document>().ToList();
            new FamilyLoader(_document).LoadAllFamiliesToProject();

            var walls = GetElementsList<Wall>(allDocs);
            var floors = GetElementsList<CeilingAndFloor>(allDocs);
            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);

            var ductsInWalls = FindIntersectionsWith(walls, ducts);
            var pipesInWalls = FindIntersectionsWith(walls, pipes);
            var ductsInFloors = FindIntersectionsWith(floors, ducts);
            var pipesInFloors = FindIntersectionsWith(floors, pipes);

            var wallBoxCalculator = new WallBoxCalculator();
            var floorBoxCalculator = new FloorBoxCalculator();

            CreateAllTaskBoxes(ductsInWalls, wallBoxCalculator, Families.WallRoundTaskFamily, _offset);
            CreateAllTaskBoxes(pipesInWalls, wallBoxCalculator, Families.WallRoundTaskFamily, _offset);
            CreateAllTaskBoxes(ductsInFloors, floorBoxCalculator, Families.FloorRectTaskFamily, _offset);
            CreateAllTaskBoxes(pipesInFloors, floorBoxCalculator, Families.FloorRectTaskFamily, _offset);

            //CreateAllTaskBoxes(ductsInWalls, wallBoxCalculator, Families.WallRectOpeningFamily, _offset);
            //CreateAllTaskBoxes(pipesInWalls, wallBoxCalculator, Families.WallRectOpeningFamily, _offset);
            //CreateAllTaskBoxes(ductsInFloors, floorBoxCalculator, Families.FloorRectOpeningFamily, _offset);
            //CreateAllTaskBoxes(pipesInFloors, floorBoxCalculator, Families.FloorRectOpeningFamily, _offset);

            CombineTasks(Families.WallRectTaskFamily);
            CombineTasks(Families.FloorRectTaskFamily);

            return Result.Succeeded;
        }

        private void CombineTasks(FamilyParameters familyData)
        {
            var tasks = _document.GetTasksFromDocument(familyData);
            FindTaskIntersections(tasks);
        }

        private void FindTaskIntersections(IEnumerable<Element> tasks)
        {
            var elements = tasks.ToList();
            for (var i = 0; i < elements.Count; i++)
            {
                var filtered = new ElementIntersectsElementFilter(elements[i]);
                for (var j = i + 1; j < elements.Count; j++)
                    if (filtered.PassesFilter(elements[j]))
                        CreateUnitedTask(elements[i] as FamilyInstance, elements[j] as FamilyInstance);
            }
        }

        private void CreateUnitedTask(FamilyInstance el1, FamilyInstance el2)
        {
            using (var t = new Transaction(_document))
            {
                t.Start("United");
                var s1 = (el1.Location as LocationPoint).Point;
                var s2 = (el2.Location as LocationPoint).Point;
                var data = JsonConvert.DeserializeObject<OpeningParentsData>(new AltecJsonSchema().GetJson(el1));
                var middle = (s1 + s2) / 2;
                var w1 = el1.LookupParameter("Отверстие_Ширина").AsDouble();
                var h1 = el1.LookupParameter("Отверстие_Высота").AsDouble();
                var w2 = el2.LookupParameter("Отверстие_Ширина").AsDouble();
                var h2 = el2.LookupParameter("Отверстие_Высота").AsDouble();
                var newBox = _document.Create.NewFamilyInstance(middle,
                    Families.GetFamilySymbol(_document, data.BoxData.FamilyName),
                    _document.GetElement(new ElementId(data.HostId)),
                    StructuralType.NonStructural);
                var family = Families.GetDataFromSymbolName(data.BoxData.FamilyName);

                if (family.DiametrName != null)
                {
                    newBox.LookupParameter(family.DiametrName).Set(Math.Max(w1 + w2, h1 + h2));
                }
                else
                {
                    newBox.LookupParameter(family.HeightName).Set(h1 + h2);
                    newBox.LookupParameter(family.WidthName).Set(h1 + h2);
                }

                newBox.LookupParameter(family.DepthName).Set(data.BoxData.Depth);
                t.Commit();
            }
        }

        public List<T> GetElementsList<T>(IEnumerable<Document> allDocs)
        {
            return allDocs
                .SelectMany(document => new FilteredElementCollector(document)
                .OfClass(typeof(T))
                .Cast<T>())
                .ToList();
        }

        private Dictionary<Element, List<MEPCurve>> FindIntersectionsWith(IEnumerable<Element> elements,
            IEnumerable<MEPCurve> curves)
        {
            var intersections = new Dictionary<Element, List<MEPCurve>>();
            foreach (var intersectionElement in elements)
            {
                var intersection = new ElementIntersectsElementFilter(intersectionElement);
                var currentInetsections = curves
                    .Where(el => intersection.PassesFilter(el))
                    .ToList();
                if (currentInetsections.Count > 0)
                    intersections[intersectionElement] = currentInetsections;
            }

            return intersections;
        }

        private void CreateAllTaskBoxes(Dictionary<Element, List<MEPCurve>> pipesInElements, IBoxCalculator boxCalculator,
            FamilyParameters familyParameters, double offset)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create task box");
                var familySymbol = Families.GetFamilySymbol(_document, familyParameters.SymbolName);
                foreach (var ductsInWall in pipesInElements)
                    foreach (var curve in ductsInWall.Value)
                    {

                        var openingParametrs =
                            boxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, offset, familyParameters);
                        if (openingParametrs == null)
                            continue;
                        var parentsData = new OpeningParentsData(ductsInWall.Key.Id.IntegerValue, curve.Id.IntegerValue,
                            ductsInWall.Key.GetType(), curve.GetType(), openingParametrs);
                        BoxCreator.CreateTaskBox(familyParameters, familySymbol, ductsInWall.Key, openingParametrs, parentsData, _document, _schema);
                    }

                transaction.Commit();
            }
        }
    }
}
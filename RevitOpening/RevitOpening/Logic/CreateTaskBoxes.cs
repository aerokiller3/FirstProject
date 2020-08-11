using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class CreateTaskBoxes : IExternalCommand
    {
        private readonly double _offset = 300;
        private Document _document;
        private AltecJsonSchema _schema;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _schema = new AltecJsonSchema();

            var allDocs = commandData.Application.Application
                .Documents.Cast<Document>()
                .ToList();
            new FamilyLoader(_document).LoadAllFamiliesToProject();

            var walls = GetElementsList<Wall>(allDocs);
            var floors = GetElementsList<CeilingAndFloor>(allDocs);
            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);

            var ductsInWalls = FindIntersectionsWith(walls, ducts);
            var pipesInWalls = FindIntersectionsWith(walls, pipes);
            var ductsInFloors = FindIntersectionsWith(floors, ducts);
            var pipesInFloors = FindIntersectionsWith(floors, pipes);

            CreateAllTaskBoxes(ductsInWalls, Families.WallRectTaskFamily, _offset);
            CreateAllTaskBoxes(pipesInWalls, Families.WallRectTaskFamily, _offset);
            CreateAllTaskBoxes(ductsInFloors, Families.FloorRectTaskFamily, _offset);
            CreateAllTaskBoxes(pipesInFloors, Families.FloorRectTaskFamily, _offset);
            var boxCombiner = new BoxCombiner(_document, _schema);
            boxCombiner.CombineAllBoxs();

            return Result.Succeeded;
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

        private void CreateAllTaskBoxes(Dictionary<Element, List<MEPCurve>> pipesInElements,
            FamilyParameters familyParameters, double offset)
        {
            using (var transaction = new Transaction(_document))
            {
                var boxCalculator = new BoxCalculator();
                transaction.Start("Create task box");
                foreach (var ductsInWall in pipesInElements)
                foreach (var curve in ductsInWall.Value)
                {
                    var openingParametrs =
                        boxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, offset, familyParameters); 
                    if (openingParametrs == null)
                        continue;

                    openingParametrs.Level = ((Level) _document.GetElement(ductsInWall.Key.LevelId)).Name;
                    var parentsData = new OpeningParentsData(ductsInWall.Key.Id.IntegerValue, curve.Id.IntegerValue,
                        ductsInWall.Key.GetType(), curve.GetType(), openingParametrs);
                    BoxCreator.CreateTaskBox(parentsData, _document, _schema);
                }

                transaction.Commit();
            }
        }
    }
}
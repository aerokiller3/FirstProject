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
        private bool _combineAll;

        private Document _document;
        private IEnumerable<Document> _documents;
        private double _maxDiameter;
        private double _offset;
        private HashSet<MyXYZ> _openings;
        private AltecJsonSchema _schema;
        private HashSet<MyXYZ> _tasks;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _schema = new AltecJsonSchema();
            _documents = commandData.Application.Application
                .Documents.Cast<Document>()
                .ToList();
            new FamilyLoader(_document).LoadAllFamiliesToProject();

            var walls = GetElementsList<Wall>(_documents);
            var floors = GetElementsList<CeilingAndFloor>(_documents);
            var pipes = GetElementsList<Pipe>(_documents);
            var ducts = GetElementsList<Duct>(_documents);
            CreateAllTaskBoxes(FindIntersectionsWith(walls, ducts));
            CreateAllTaskBoxes(FindIntersectionsWith(walls, pipes));
            CreateAllTaskBoxes(FindIntersectionsWith(floors, ducts));
            CreateAllTaskBoxes(FindIntersectionsWith(floors, pipes));
            if (_combineAll)
                new BoxCombiner(_document, _schema,_documents).CombineAllBoxes();

            return Result.Succeeded;
        }

        public void SetTasksParameters(string offset, string diameter, bool combineAll, List<OpeningData> tasks,
            List<OpeningData> opening)
        {
            _openings = opening.Select(o => o.IntersectionCenter).ToHashSet();
            _tasks = tasks.Select(o => o.IntersectionCenter).ToHashSet();
            _offset = double.Parse(offset);
            _maxDiameter = double.Parse(diameter);
            _combineAll = combineAll;
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
                var currentIntersections = curves
                    .Where(el => intersection.PassesFilter(el))
                    .ToList();
                if (currentIntersections.Count > 0)
                    intersections[intersectionElement] = currentIntersections;
            }

            return intersections;
        }

        private void CreateAllTaskBoxes(Dictionary<Element, List<MEPCurve>> pipesInElements)
        {
            using (var transaction = new Transaction(_document))
            {
                var boxCalculator = new BoxCalculator();
                transaction.Start("Create task box");
                foreach (var ductsInWall in pipesInElements)
                foreach (var curve in ductsInWall.Value)
                {
                    var openingParameters =
                        boxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, _offset, _maxDiameter);
                    if (openingParameters == null)
                        continue;

                    openingParameters.Level = _documents
                        .GetElementFromDocuments(ductsInWall.Key.LevelId.IntegerValue).Name;
                    var parentsData = new OpeningParentsData(ductsInWall.Key.Id.IntegerValue, curve.Id.IntegerValue,
                        ductsInWall.Key.GetType(), curve.GetType(), openingParameters);

                    if ((_tasks?.Contains(openingParameters.IntersectionCenter) ?? false)
                        || (_openings?.Contains(openingParameters.IntersectionCenter) ?? false))
                        continue;

                    BoxCreator.CreateTaskBox(parentsData, _document, _schema);
                }

                transaction.Commit();
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.ExternalCommands
{
    public class CreateTaskBoxes
    {
        private readonly bool _isCombineAll;
        private readonly double _maxDiameter;
        private readonly double _offsetRatio;
        private readonly HashSet<MyXYZ> _openings;
        private readonly HashSet<MyXYZ> _tasks;

        public CreateTaskBoxes(string offset, string diameter, bool combineAll, List<OpeningData> tasks,
            List<OpeningData> opening)
        {
            _openings = opening.Select(o => o.IntersectionCenter).ToHashSet();
            _tasks = tasks.Select(o => o.IntersectionCenter).ToHashSet();
            _offsetRatio = double.Parse(offset);
            _maxDiameter = double.Parse(diameter);
            _isCombineAll = combineAll;
        }

        public void Execute(IEnumerable<Document> documents, Document currentDocument)
        {
            FamilyLoader.LoadAllFamiliesToProject(currentDocument);

            var walls = documents.GetAllElementsOfClass<Wall>();
            var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
            var pipes = documents.GetAllElementsOfClass<Pipe>();
            var ducts = documents.GetAllElementsOfClass<Duct>();
            var trays = documents.GetAllElementsOfClass<CableTrayConduitBase>();

            CreateAllTaskBoxes(FindIntersectionsWith(walls, ducts), currentDocument);
            CreateAllTaskBoxes(FindIntersectionsWith(walls, pipes), currentDocument);
            CreateAllTaskBoxes(FindIntersectionsWith(walls, trays), currentDocument);
            CreateAllTaskBoxes(FindIntersectionsWith(floors, ducts), currentDocument);
            CreateAllTaskBoxes(FindIntersectionsWith(floors, pipes), currentDocument);
            CreateAllTaskBoxes(FindIntersectionsWith(floors, trays), currentDocument);
            if (_isCombineAll)
                BoxCombiner.CombineAllBoxes(documents, currentDocument);
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

        private void CreateAllTaskBoxes(Dictionary<Element, List<MEPCurve>> pipesInElements, Document currentDocument)
        {
            foreach (var ductsInWall in pipesInElements)
            foreach (var curve in ductsInWall.Value)
            {
                var openingParameters =
                    BoxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, _offsetRatio, _maxDiameter);
                if (openingParameters == null)
                    continue;

                var parentsData = new OpeningParentsData(new List<int> {ductsInWall.Key.Id.IntegerValue},
                    new List<int> {curve.Id.IntegerValue}, openingParameters);

                if ((_tasks?.Contains(openingParameters.IntersectionCenter) ?? false)
                    || (_openings?.Contains(openingParameters.IntersectionCenter) ?? false))
                    continue;

                BoxCreator.CreateTaskBox(parentsData, currentDocument);
            }
        }
    }
}
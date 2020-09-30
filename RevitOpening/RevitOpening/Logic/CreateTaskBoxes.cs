namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Electrical;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Extensions;
    using Models;

    internal class CreateTaskBoxes
    {
        private readonly Document _currentDocument;
        private readonly ICollection<Document> _documents;
        private readonly double _maxDiameter;
        private readonly double _offsetRatio;
        private readonly HashSet<MyXYZ> _openingsCenters;
        private readonly List<FamilyInstance> _tasks;
        private readonly HashSet<MyXYZ> _tasksCenters;

        public CreateTaskBoxes(List<OpeningData> tasks, List<OpeningData> opening,
            Document currentDocument, ICollection<Document> documents,
            double maxDiameter, double offsetRatio)
        {
            _openingsCenters = opening.Select(o => o.IntersectionCenter).ToHashSet();
            _tasksCenters = tasks.Select(o => o.IntersectionCenter).ToHashSet();
            _currentDocument = currentDocument;
            _documents = documents;
            _maxDiameter = maxDiameter;
            _offsetRatio = offsetRatio;
            _tasks = documents.GetAllTasks();
        }

        public void Execute()
        {
            var mepCurves = GetAllMepCurves();
            var elements = GetAllHostElements();
            var intersections = elements.FindIntersectionsWith(mepCurves);
            var boxes = CalculateBoxes(intersections);
            var createTasks = CreateTaskBoxesByParameters(boxes).ToArray();
            _currentDocument.Regenerate();
            var tasksForDelete = GetIntersectionsForRemove(createTasks);
            RemoveIntersectedTasks(tasksForDelete);
            _currentDocument.Regenerate();
            BoxCombiner.CombineAllBoxes(_documents, _currentDocument, true);
        }

        private List<MEPCurve> GetAllMepCurves()
        {
            var pipes = _documents.GetAllElementsOfClass<Pipe>();
            var ducts = _documents.GetAllElementsOfClass<Duct>();
            var trays = _documents.GetAllElementsOfClass<CableTrayConduitBase>();
            var mepCurves = new List<MEPCurve>();
            mepCurves.AddRange(pipes);
            mepCurves.AddRange(ducts);
            mepCurves.AddRange(trays);
            return mepCurves;
        }

        private List<Element> GetAllHostElements()
        {
            var walls = _documents.GetAllElementsOfClass<Wall>();
            var floors = _documents.GetAllElementsOfClass<CeilingAndFloor>();
            var elements = new List<Element>();
            elements.AddRange(walls);
            elements.AddRange(floors);
            return elements;
        }

        private void RemoveIntersectedTasks(IEnumerable<FamilyInstance> tasksForDelete)
        {
            foreach (var task in tasksForDelete)
                _currentDocument.Delete(task.Id);
        }

        private IEnumerable<FamilyInstance> GetIntersectionsForRemove(IEnumerable<FamilyInstance> createdTasks)
        {
            foreach (var task in createdTasks)
            {
                var filter = new ElementIntersectsElementFilter(task);
                if (_tasks.Any(t => filter.PassesFilter(t)))
                    yield return task;
            }
        }

        private IEnumerable<FamilyInstance> CreateTaskBoxesByParameters(IEnumerable<OpeningParentsData> openingsParameters)
        {
            foreach (var parentsData in openingsParameters)
            {
                var level = _documents
                   .GetElement(_documents
                              .GetElement(parentsData.HostsIds
                                                     .FirstOrDefault()).LevelId.IntegerValue) as Level;
                var levelOffset = level?.Elevation ?? 0;
                var offsetFromLevel = parentsData.BoxData.Height - levelOffset;
                parentsData.BoxData.LevelOffset = levelOffset;
                parentsData.BoxData.OffsetFromLevel = offsetFromLevel;
                var newBox = BoxCreator.CreateTaskBox(parentsData, _currentDocument);
                if (newBox != null)
                    yield return newBox;
            }
        }

        private IEnumerable<OpeningParentsData> CalculateBoxes(IDictionary<Element, List<MEPCurve>> pipesInElements)
        {
            var parameters = new ConcurrentBag<OpeningParentsData>();
            Parallel.ForEach(pipesInElements, pipesInHost =>
            {
                foreach (var curve in pipesInHost.Value)
                {
                    var openingParameters = BoxCalculator
                       .CalculateBoxInElement(pipesInHost.Key, curve, _offsetRatio, _maxDiameter);
                    if (openingParameters == null) continue;

                    openingParameters.Level = _documents.GetElement(pipesInHost.Key.LevelId.IntegerValue).Name;
                    var parentsData = new OpeningParentsData(new List<string> {pipesInHost.Key.UniqueId},
                        new List<string> {curve.UniqueId}, openingParameters);

                    if ((_tasksCenters?.Contains(openingParameters.IntersectionCenter) ?? false)
                        || (_openingsCenters?.Contains(openingParameters.IntersectionCenter) ?? false))
                        continue;

                    parameters.Add(parentsData);
                }
            });
            return parameters;
        }
    }
}
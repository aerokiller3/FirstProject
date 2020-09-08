namespace RevitOpening.Logic
{
    using System.Collections.Generic;
    using System.Linq;
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
            FamilyLoader.LoadAllFamiliesToProject(_currentDocument);

            var walls = _documents.GetAllElementsOfClass<Wall>();
            var floors = _documents.GetAllElementsOfClass<CeilingAndFloor>();
            var pipes = _documents.GetAllElementsOfClass<Pipe>();
            var ducts = _documents.GetAllElementsOfClass<Duct>();
            var trays = _documents.GetAllElementsOfClass<CableTrayConduitBase>();
            var mepCurves = new List<MEPCurve>();
            mepCurves.AddRange(pipes);
            mepCurves.AddRange(ducts);
            mepCurves.AddRange(trays);
            CreateTaskBoxesIn(walls.FindIntersectionsWith(mepCurves));
            CreateTaskBoxesIn(floors.FindIntersectionsWith(mepCurves));
            BoxCombiner.CombineAllBoxes(_documents,_currentDocument, true);
        }

        private void CreateTaskBoxesIn(Dictionary<Element, List<MEPCurve>> pipesInElements)
        {
            foreach (var ductsInWall in pipesInElements)
                foreach (var curve in ductsInWall.Value)
                {
                    var openingParameters =
                        BoxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, _offsetRatio, _maxDiameter);
                    if (openingParameters == null)
                        continue;

                    openingParameters.Level = _documents.GetElement(ductsInWall.Key.LevelId.IntegerValue).Name;
                    var parentsData = new OpeningParentsData(new List<string> {ductsInWall.Key.UniqueId},
                        new List<string> {curve.UniqueId}, openingParameters);

                    if ((_tasksCenters?.Contains(openingParameters.IntersectionCenter) ?? false)
                        || (_openingsCenters?.Contains(openingParameters.IntersectionCenter) ?? false))
                        continue;

                    var createElement = BoxCreator.CreateTaskBox(parentsData, _currentDocument);
                    _currentDocument.Regenerate();
                    var filter = new ElementIntersectsElementFilter(createElement);
                    if (_tasks.Any(t => filter.PassesFilter(t)))
                        _currentDocument.Delete(createElement.Id);
                    //else
                    //    yield return createElement;
                }
        }
    }
}
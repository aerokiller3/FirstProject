namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Electrical;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.Exceptions;
    using Extensions;
    using LoggerClient;
    using Models;

    internal static class BoxAnalyzer
    {
        public static void ExecuteAnalysis(List<Document> documents, double offset, double maxDiameter)
        {
            var walls = documents.GetAllElementsOfClass<Wall>();
            var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
            var tasksAndOpenings = GetAllTasksAndOpenings(documents);
            var mepCurves = GetAllMEPCurves(documents);
            AnalyzeElements(walls, floors, mepCurves, tasksAndOpenings, offset,
                maxDiameter, documents);
        }

        private static List<MEPCurve> GetAllMEPCurves(List<Document> documents)
        {
            var mepCurves = new List<MEPCurve>();
            mepCurves.AddRange(documents.GetAllElementsOfClass<Pipe>());
            mepCurves.AddRange(documents.GetAllElementsOfClass<Duct>());
            mepCurves.AddRange(documents.GetAllElementsOfClass<CableTrayConduitBase>());
            return mepCurves;
        }

        private static List<FamilyInstance> GetAllTasksAndOpenings(List<Document> documents)
        {
            var tasksAndOpenings = new List<FamilyInstance>();
            var tasks = documents.GetAllTasks();
            var openings = documents.GetAllOpenings();
            tasksAndOpenings.AddRange(tasks);
            tasksAndOpenings.AddRange(openings);
            return tasksAndOpenings;
        }

        private static void AnalyzeElements(List<Wall> walls, List<CeilingAndFloor> floors,
            List<MEPCurve> mepCurves, List<FamilyInstance> elementsToAnalyze, double offset,
            double maxDiameter, ICollection<Document> documents)
        {
            foreach (var el in elementsToAnalyze)
            {
                var data = el.GetParentsDataFromSchema();
                el.SetParentsData(UpdateElementInformation(
                    el, data, walls, floors,
                    elementsToAnalyze, documents, offset, maxDiameter, mepCurves));
            }
        }

        public static OpeningParentsData UpdateElementInformation(Element task, OpeningParentsData data, List<Wall> walls,
            ICollection<CeilingAndFloor> floors, ICollection<FamilyInstance> tasks, ICollection<Document> documents,
            double offset, double maxDiameter, List<MEPCurve> mepCurves)
        {
            if (data != null)
                data.BoxData.Collisions = new Collisions();
            var isTask = task.IsTask();
            if (!isTask)
            {
                data = new OpeningParentsData();
                data.BoxData.Collisions.MarkUnSupported();
                return data;
            }

            using (var filter = new ElementIntersectsElementFilter(task))
            {
                if (!data.IsActualTask(task, documents, offset, maxDiameter,
                    walls, floors, mepCurves, out var newData) || data == null)
                    data = newData;
                if (data?.IsTaskIntersectManyWall(walls, filter) ?? false)
                    data.BoxData.Collisions.Add(Collisions.TaskIntersectManyWalls);
                if (data?.IsWallTaskIntersectFloor(floors, filter) ?? false)
                    data.BoxData.Collisions.Add(Collisions.WallTaskIntersectFloor);
                if (data?.IsFloorTaskIntersectWall(walls, filter) ?? false)
                    data.BoxData.Collisions.Add(Collisions.FloorTaskIntersectWall);
                if (data?.IsHostNotPerpendicularPipe(documents) ?? false)
                    data.BoxData.Collisions.Add(Collisions.PipeNotPerpendicularHost);
                if (task.IsTaskIntersectTask(tasks, filter))
                    data?.BoxData.Collisions.Add(Collisions.TaskIntersectTask);
            }

            if (data?.BoxData.Collisions.Count > 0)
                data.BoxData.Collisions.MarkUnSupported();


            //var intAgreedValue = CalculateAgreedValue(currentCollisions);
            //Менять спец. атрибут
            //task.LookupParameter("Несогласованно").Set(intAgreedValue);

            return data;
        }

        private static int CalculateAgreedValue(Collisions currentCollisions)
        {
            return currentCollisions.Count == 0 ? 0 : 1;
        }

        private static bool IsTaskIntersectTask(this Element box,
            IEnumerable<FamilyInstance> tasks, ElementIntersectsElementFilter filter)
        {
            return tasks.Where(element => element.Id != box.Id)
                        .Any(filter.PassesFilter);
        }

        private static bool IsActualTask(this OpeningParentsData parentsData, Element element,
            ICollection<Document> documents, double offset, double maxDiameter,
            ICollection<Wall> walls, ICollection<CeilingAndFloor> floors,
            ICollection<MEPCurve> mepCurves, out OpeningParentsData newData)
        {
            var currentData = element.GetParentsDataFromParameters(walls, floors, offset, maxDiameter, mepCurves, documents);
            if (parentsData == null)
            {
                newData = currentData;
                return false;
            }

            var isOldBox = CompareActualAndOldBoxData(parentsData.BoxData, currentData.BoxData);
            var isOldPipes = parentsData.BoxData.PipesGeometries.AlmostEqualTo(currentData.BoxData.PipesGeometries);
            var isOldHosts = parentsData.BoxData.HostsGeometries.AlmostEqualTo(currentData.BoxData.HostsGeometries);
            var isImmutable = isOldBox && isOldPipes && isOldHosts;
            newData = parentsData;
            if (!isImmutable)
                newData = currentData;
            else if (currentData.PipesIds.Count != 1 && currentData.HostsIds.Count != 1)
                parentsData.BoxData.Collisions.MarkUnSupported();

            return isImmutable;
        }

        private static bool CompareActualAndOldBoxData(OpeningData oldData, OpeningData currentData)
        {
            const double tolerance = 0.000_000_1;
            return currentData.IntersectionCenter.Equals(oldData.IntersectionCenter) &&
                Math.Abs(currentData.Depth - oldData.Width) < tolerance &&
                Math.Abs(currentData.Depth - oldData.Height) < tolerance &&
                Math.Abs(currentData.Depth - oldData.Depth) < tolerance;
        }

        private static bool IsFloorTaskIntersectWall(this OpeningParentsData data, IEnumerable<Wall> walls,
            ElementIntersectsElementFilter filter)
        {
            return data.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName && walls.Any(filter.PassesFilter);
        }

        private static bool IsWallTaskIntersectFloor(this OpeningParentsData data, IEnumerable<CeilingAndFloor> floors,
            ElementIntersectsElementFilter filter)
        {
            return data.BoxData.FamilyName != Families.FloorRectTaskFamily.SymbolName &&
                floors.Any(filter.PassesFilter);
        }

        private static bool IsTaskIntersectManyWall(this OpeningParentsData data, IEnumerable<Wall> walls,
            ElementIntersectsElementFilter filter)
        {
            if (data.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName)
                return false;

            var wallsIntersect = walls
                                .Where(filter
                                    .PassesFilter)
                                .ToList();
            return wallsIntersect.Count != data.HostsIds.Count;
        }

        private static readonly object DocumentLock = new object();

        private static bool IsHostNotPerpendicularPipe(this OpeningParentsData data, ICollection<Document> documents)
        {
            const double toleranceInDegrees = 3;
            if (data.BoxData.HostsGeometries.Count == 0 || data.BoxData.PipesGeometries.Count == 0)
            {
                data.BoxData.Collisions.MarkUnSupported();
                return false;
            }

            var element = documents.GetElement(data.HostsIds.FirstOrDefault());
            var pipe = documents.GetElement(data.PipesIds.First());

            switch (element)
            {
                case Wall _:
                    var pipeGeometry = data.BoxData.PipesGeometries.FirstOrDefault();
                    var pipeVec = pipeGeometry?.End.XYZ - pipeGeometry?.Start.XYZ;
                    var wallGeometry = data.BoxData.HostsGeometries.FirstOrDefault();
                    var wallVec = wallGeometry?.End.XYZ - wallGeometry?.Start.XYZ;
                    return !(Math.Abs(pipeVec.AngleTo(wallVec) - Math.PI / 2) < toleranceInDegrees * Math.PI / 180);
                case CeilingAndFloor _:
                    pipeGeometry = data.BoxData.PipesGeometries.FirstOrDefault();
                    pipeVec = pipeGeometry?.End.XYZ - pipeGeometry?.Start.XYZ;
                    pipeVec = pipeVec.Normalize();

                    var floorVec = ((MEPCurve) pipe)
                                  .ConnectorManager.Connectors
                                  .Cast<Connector>()
                                  .FirstOrDefault()?
                                  .CoordinateSystem.BasisX.CrossProduct(XYZ.BasisZ.Negate());

                    return !(Math.Abs(pipeVec.AngleTo(floorVec) - Math.PI / 2) <
                        toleranceInDegrees * Math.PI / 180);
                default:
                    ModuleLogger.SendErrorData("Необработанный тип хост элемента",
                        element.Category.Name, nameof(BoxAnalyzer),
                        Environment.StackTrace, nameof(RevitOpening));
                    return false;
            }
        }
    }
}
namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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
        public static void ExecuteAnalysis(List<Document> documents, Document currentDocument, double offset, double maxDiameter)
        {
            var walls = documents.GetAllElementsOfClass<Wall>();
            var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();

            var tasks = documents.GetAllTasks();
            var openings = documents.GetAllOpenings();

            var mepCurves = new List<MEPCurve>();
            mepCurves.AddRange(documents.GetAllElementsOfClass<Pipe>());
            mepCurves.AddRange(documents.GetAllElementsOfClass<Duct>());
            mepCurves.AddRange(documents.GetAllElementsOfClass<CableTrayConduitBase>());
            AnalyzeElements(walls, floors, mepCurves, tasks, offset, maxDiameter, documents, currentDocument);
            AnalyzeElements(walls, floors, mepCurves, openings, offset, maxDiameter, documents, currentDocument);
        }

        private static void AnalyzeElements(List<Wall> walls, List<CeilingAndFloor> floors,
            List<MEPCurve> mepCurves, List<FamilyInstance> elementsToAnalyze, double offset,
            double maxDiameter, ICollection<Document> documents, Document currentDocument)
        {
            foreach (var task in elementsToAnalyze)
            {
                var data = task.GetOrInitData(walls, floors, offset, maxDiameter, mepCurves,
                    currentDocument, documents);
                AnalyzeElement(task, data, walls, floors, elementsToAnalyze, documents, offset,
                    maxDiameter, mepCurves, currentDocument);
            }
        }

        public static void AnalyzeElement(Element task, OpeningParentsData data, List<Wall> walls,
            ICollection<CeilingAndFloor> floors, ICollection<FamilyInstance> tasks, ICollection<Document> documents,
            double offset, double maxDiameter, List<MEPCurve> mepCurves, Document currentDocument)
        {
            data.BoxData.Collisions = new Collisions();
            var isTask = task.IsTask();

            using (var filter = new ElementIntersectsElementFilter(task))
            {
                if (isTask && !data.IsActualTask(task, documents, offset, maxDiameter, filter, walls, floors, mepCurves,
                    currentDocument, out var newData))
                    data = newData;
                if (data.IsTaskIntersectManyWall(walls, filter))
                    data.BoxData.Collisions.Add(Collisions.TaskIntersectManyWalls);
                if (data.IsWallTaskIntersectFloor(floors, filter))
                    data.BoxData.Collisions.Add(Collisions.WallTaskIntersectFloor);
                if (data.IsFloorTaskIntersectWall(walls, filter))
                    data.BoxData.Collisions.Add(Collisions.FloorTaskIntersectWall);
                if (data.IsHostNotPerpendicularPipe(documents))
                    data.BoxData.Collisions.Add(Collisions.PipeNotPerpendicularHost);
                if (isTask && task.IsTaskIntersectTask(tasks, filter))
                    data.BoxData.Collisions.Add(Collisions.TaskIntersectTask);
            }

            if (data.BoxData.Collisions.Count > 0)
                data.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);


            //var intAgreedValue = CalculateAgreedValue(currentCollisions);
            //Менять спец. атрибут
            //task.LookupParameter("Несогласованно").Set(intAgreedValue);

            task.SetParentsData(data);
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
            ElementIntersectsElementFilter filter, ICollection<Wall> walls,
            ICollection<CeilingAndFloor> floors, ICollection<MEPCurve> mepCurves,
            Document currentDocument, out OpeningParentsData newData)
        {
            var pipes = mepCurves
                       .Where(filter.PassesFilter)
                       .ToList();
            var hosts = new List<Element>();
            hosts.AddRange(floors
               .Where(filter.PassesFilter));
            hosts.AddRange(walls
               .Where(filter.PassesFilter));

            var pipesGeometries = new List<ElementGeometry>(pipes.Select(p => new ElementGeometry(p)));
            var hostGeometries = new List<ElementGeometry>(hosts.Select(h => new ElementGeometry(h)));
            var isOldPipes = parentsData.BoxData.PipesGeometries.AlmostEqualTo(pipesGeometries);
            var isOldHosts = parentsData.BoxData.HostsGeometries.AlmostEqualTo(hostGeometries);
            var isOldBox = CompareActualAndOldBoxData((FamilyInstance)element, parentsData.BoxData);
            newData = null;
            var isImmutable = isOldBox && isOldPipes && isOldHosts;
            if (!isImmutable)
                newData = element.InitData(walls, floors, offset, maxDiameter,
                    mepCurves, documents);
            else if (pipesGeometries.Count != 1 && hostGeometries.Count != 1)
                parentsData.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);
            return isImmutable;
        }

        private static bool CompareActualAndOldBoxData(FamilyInstance oldTask, OpeningData boxData)
        {
            const double tolerance = 0.000_000_1;
            var familyParameters = Families.GetDataFromSymbolName(oldTask.Symbol.FamilyName);
            var locPoint = new MyXYZ(((LocationPoint) oldTask.Location).Point);
            double width, height;
            if (familyParameters == Families.FloorRectTaskFamily)
            {
                width = oldTask.LookupParameter(familyParameters.HeightName).AsDouble();
                height = oldTask.LookupParameter(familyParameters.WidthName).AsDouble();
            }
            else if (familyParameters == Families.WallRectTaskFamily)
            {
                height = oldTask.LookupParameter(familyParameters.HeightName).AsDouble();
                width = oldTask.LookupParameter(familyParameters.WidthName).AsDouble();
            }
            else if (familyParameters == Families.WallRoundTaskFamily)
            {
                width = height = oldTask.LookupParameter(familyParameters.DiameterName).AsDouble();
            }
            else
            {
                ModuleLogger.SendErrorData("Не поддерживаемый тип элемента для анализа",
                    $"Name: {oldTask.Name} Type: {oldTask.Symbol.FamilyName}",
                    nameof(BoxAnalyzer), Environment.StackTrace, nameof(RevitOpening));
                //throw new ArgumentException("Не поддерживаемый тип");
                return true;
            }

            var depth = oldTask.LookupParameter(familyParameters.DepthName).AsDouble();

            return locPoint.Equals(boxData.IntersectionCenter) &&
                Math.Abs(width - boxData.Width) < tolerance &&
                Math.Abs(height - boxData.Height) < tolerance &&
                Math.Abs(depth - boxData.Depth) < tolerance;
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

        private static bool IsHostNotPerpendicularPipe(this OpeningParentsData data, ICollection<Document> documents)
        {
            const double toleranceInDegrees = 3;
            if (data.BoxData.HostsGeometries.Count == 0 || data.BoxData.PipesGeometries.Count == 0)
            {
                data.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);
                return false;
            }

            var element = documents.GetElement(data.HostsIds.FirstOrDefault());
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
                    var floorVec = ((MEPCurve) documents
                                      .GetElement(data.PipesIds.First()))
                                  .ConnectorManager.Connectors
                                  .Cast<Connector>()
                                  .FirstOrDefault()?
                                  .CoordinateSystem.BasisX.CrossProduct(XYZ.BasisZ.Negate());
                    return !(Math.Abs(pipeVec.AngleTo(floorVec) - Math.PI / 2) < toleranceInDegrees * Math.PI / 180);
                default:
                    ModuleLogger.SendErrorData("Необработанный тип хост элемента",
                        element.Category.Name, nameof(BoxAnalyzer),
                        Environment.StackTrace, nameof(RevitOpening));
                    return false;
            }
        }
    }
}
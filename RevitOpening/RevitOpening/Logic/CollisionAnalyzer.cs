using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class CollisionAnalyzer
    {
        public static void ExecuteAnalysis(IEnumerable<Document> documents,Document currentDocument, double offset, double maxDiameter)
        {
            var walls = documents.GetAllElementsOfClass<Wall>();
            var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
            var tasks = documents.GetAllTasks();
            foreach (var task in tasks)
                AnalyzeElement(task, walls, floors, tasks, documents, currentDocument, offset, maxDiameter);
        }

        public static void AnalyzeElement(FamilyInstance task, List<Wall> walls, List<CeilingAndFloor> floors,
            List<FamilyInstance> tasks, IEnumerable<Document> documents, Document currentDocument, double offset, double maxDiameter)
        {
            var data = GetOrInitData(task,walls,floors,documents,offset, maxDiameter, currentDocument);
            var currentCollisions = new Collisions();
            var isNotProcessed = false;
            if (data.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed))
            {
                isNotProcessed = true;
                currentCollisions.Add(Collisions.TaskCouldNotBeProcessed);
            }

            using (var filter = new ElementIntersectsElementFilter(task))
            {
                if (data.IsTaskIntersectManyWall(walls, filter))
                    currentCollisions.Add(Collisions.TaskIntersectManyWalls);
                if (data.IsWallTaskIntersectFloor(floors, filter))
                    currentCollisions.Add(Collisions.WallTaskIntersectFloor);
                if (data.IsFloorTaskIntersectWall(walls, filter))
                    currentCollisions.Add(Collisions.FloorTaskIntersectWall);
                if (data.IsHostNotPerpendicularPipe(documents))
                    currentCollisions.Add(Collisions.PipeNotPerpendicularHost);
                if (data.IsTaskIntersectTask(task, tasks))
                    currentCollisions.Add(Collisions.TaskIntersectTask);
                if (!data.IsActualTask(task, documents, currentDocument, offset, maxDiameter))
                    currentCollisions.Add(Collisions.TaskNotActual);
            }

            data.BoxData.Collisions = currentCollisions;
            var intAgreedValue = CalculateAgreedValue(isNotProcessed, currentCollisions);

            //Менять спец. атрибут
            task.LookupParameter("Несогласованно").Set(intAgreedValue);
            task.SetParentsData(data);
        }

        private static OpeningParentsData GetOrInitData(FamilyInstance task, List<Wall> walls,
            List<CeilingAndFloor> floors, IEnumerable<Document> documents,
            double offset, double maxDiameter, Document currentDocument)
        {
            OpeningParentsData data;
            try
            {
                data = task.GetParentsData();
            }
            catch
            {
                data = InitData(task,walls,floors,documents,offset,maxDiameter,currentDocument);
            }

            return data;
        }

        private static OpeningParentsData InitData(FamilyInstance task, List<Wall> walls, List<CeilingAndFloor> floors,
            IEnumerable<Document> documents, double offset, double maxDiameter, Document currentDocument)
        {
            return null;
            //var filter = new ElementIntersectsElementFilter(task);
            //var intersectsWalls = walls.Where(filter.PassesFilter).ToList();
            //var intersectsFloors = floors.Where(filter.PassesFilter).ToList();
            //var


            //var openingParameters =
            //    BoxCalculator.CalculateBoxInElement(ductsInWall.Key, curve, _offsetRatio, _maxDiameter);
            //if (openingParameters == null)
            //    continue;

            //var parentsData = new OpeningParentsData(new List<int> { ductsInWall.Key.Id.IntegerValue },
            //    new List<int> { curve.Id.IntegerValue }, openingParameters);

            //if ((_tasksCenters?.Contains(openingParameters.IntersectionCenter) ?? false)
            //    || (_openingsCenters?.Contains(openingParameters.IntersectionCenter) ?? false))
            //    continue;

            //var createElement = BoxCreator.CreateTaskBox(parentsData, _currentDocument);
            //var filter = new ElementIntersectsElementFilter(createElement);
            //if (_tasks.Where(t => filter.PassesFilter(t))
            //    .Any(t => t.GetParentsData().BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed)))
            //    _currentDocument.Delete(createElement.Id);
            //var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            //var familySymbol = document.GetFamilySymbol(parentsData.BoxData.FamilyName);
            //familySymbol.Activate();
            //var center = parentsData.BoxData.IntersectionCenter.XYZ;
            //var direction = parentsData.BoxData.Direction.XYZ;
            //var host = document.GetElement(new ElementId(parentsData.HostsIds.FirstOrDefault()));
            //var newBox =
            //    document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);

            //if (familyParameters.DiameterName != null)
            //{
            //    newBox.LookupParameter(familyParameters.DiameterName)
            //        .Set(Math.Max(parentsData.BoxData.Width, parentsData.BoxData.Height));
            //}
            //else
            //{
            //    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
            //    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
            //}

            //newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            //parentsData.BoxData.Id = newBox.Id.IntegerValue;
            //newBox.SetParentsData(parentsData);

            //return newBox;
        }


        private static int CalculateAgreedValue(bool isNotProcessed, Collisions currentCollisions)
        {
            if (isNotProcessed)
                return currentCollisions.Count == 1 ? 0 : 1;

            return currentCollisions.Count == 0 ? 0 : 1;
        }

        private static bool IsTaskIntersectTask(this OpeningParentsData data, FamilyInstance box, List<FamilyInstance> tasks)
        {
            var tolerance = new XYZ(0.001, 0.001, 0.001);
            var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var unitedSolid = box.GetUnitedSolid(null, transform, tolerance);
            var filter = new ElementIntersectsSolidFilter(unitedSolid);
            return tasks
                .Where(element => element.Id != box.Id)
                .Any(element => filter.PassesFilter(element));
        }

        public static bool IsActualTask(this OpeningParentsData parentsData, FamilyInstance element, IEnumerable<Document> documents,
            Document currentDocument, double offset, double maxDiameter)
        {
            var pipes = parentsData.PipesIds
                .Select(documents.GetElement)
                .ToList();
            var hosts = parentsData.HostsIds
                .Select(documents.GetElement)
                .ToList();
            var isOldPipes = parentsData.BoxData.PipesGeometries
                .AlmostEqualTo(new List<ElementGeometry>(pipes.Select(p => new ElementGeometry(p))));
            var isOldHosts = parentsData.BoxData.HostsGeometries
                .AlmostEqualTo(new List<ElementGeometry>(hosts.Select(h => new ElementGeometry(h))));
            var isOldBox = CheckBoxParameters(element, parentsData.BoxData);
            var isImmutable = isOldBox && isOldPipes && isOldHosts;
            if (!isImmutable)
                isImmutable = MatchOldAndNewTask(pipes.OfType<MEPCurve>() as List<MEPCurve> ?? new List<MEPCurve>(), hosts, parentsData, offset, maxDiameter);
            return isImmutable;
        }

        private static bool CheckBoxParameters(FamilyInstance oldTask, OpeningData boxData)
        {
            const double tolerance = 0.000_000_1;
            var familyParameters = Families.GetDataFromSymbolName(oldTask.Symbol.FamilyName);
            var locPoint = new MyXYZ(((LocationPoint)oldTask.Location).Point);
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
                throw new Exception("Неизвестный тип задания");

            return locPoint.Equals(boxData.IntersectionCenter) &&
                   Math.Abs(width - boxData.Width) < tolerance &&
                   Math.Abs(height - boxData.Height) < tolerance;
        }

        private static bool MatchOldAndNewTask(List<MEPCurve> pipes, List<Element> hosts, OpeningParentsData parentsData, double offset, double maxDiameter)
        {
            if (pipes.Count != 1 || hosts.Count != 1)
            {
                parentsData.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);
                return true;
            }

            var parameters = BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(), pipes.FirstOrDefault(), offset, maxDiameter);
            return parameters != null && parentsData.BoxData.Equals(parameters);
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

        private static bool IsHostNotPerpendicularPipe(this OpeningParentsData data, IEnumerable<Document> documents)
        {
            const double toleranceInDegrees = 3;
            switch (documents.GetElement(data.HostsIds.FirstOrDefault()))
            {
                case Wall _:
                    var pipeGeometry = data.BoxData.PipesGeometries.FirstOrDefault();
                    var pipeVec = pipeGeometry?.End.XYZ - pipeGeometry?.Start.XYZ;
                    var wallGeometry = data.BoxData.HostsGeometries.FirstOrDefault();
                    var wallVec = wallGeometry?.End.XYZ - wallGeometry?.Start.XYZ;
                    return !(Math.Abs(pipeVec.AngleTo(wallVec) - Math.PI / 2) < toleranceInDegrees * Math.PI / 180);
                case CeilingAndFloor _:
                    return false;
                default:
                    throw new Exception("Неизвестный тип хост элемента");
            }
        }
    }
}
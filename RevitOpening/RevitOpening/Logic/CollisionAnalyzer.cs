using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class CollisionAnalyzer
    {
        public static void ExecuteAnalysis(IEnumerable<Document> documents)
        {
            var walls = documents.GetAllElementsOfClass<Wall>();
            var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
            var tasks = documents.GetAllTasks();
            foreach (var task in tasks)
                AnalyzeElement(task, walls, floors, tasks, documents);
        }

        public static void AnalyzeElement(Element box, List<Wall> walls, List<CeilingAndFloor> floors,
            List<Element> tasks,
            IEnumerable<Document> documents)
        {
            var data = box.GetParentsData();
            data.BoxData.Collisions = new Collisions();
            using (var filter = new ElementIntersectsElementFilter(box))
            {
                if (IsTaskIntersectManyWall(data, walls, filter))
                    data.BoxData.Collisions.Add(Collisions.TaskIntersectManyWalls);
                if (IsWallTaskIntersectFloor(data, floors, filter))
                    data.BoxData.Collisions.Add(Collisions.WallTaskIntersectFloor);
                if (IsFloorTaskIntersectWall(data, walls, filter))
                    data.BoxData.Collisions.Add(Collisions.FloorTaskIntersectWall);
                if (IsHostNotPerpendicularPipe(data, documents))
                    data.BoxData.Collisions.Add(Collisions.PipeNotPerpendicularHost);
                if (IsTaskIntersectTask(box, tasks))
                    data.BoxData.Collisions.Add(Collisions.TaskIntersectTask);
            }

            box.LookupParameter("Несогласованно").Set(data.BoxData.Collisions.ListOfCollisions.Count == 0 ? 0 : 1);
            box.SetParentsData(data);
        }

        private static bool IsTaskIntersectTask(Element box, List<Element> tasks)
        {
            var data = box.GetParentsData();
            var tolerance = new XYZ(0.001, 0.001, 0.001);
            var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var unitedSolid = box.GetUnitedSolid(null, transform, tolerance);
            var filter = new ElementIntersectsSolidFilter(unitedSolid);
            return tasks
                .Where(element => element.Id != box.Id)
                .Any(element => filter.PassesFilter(element));
        }

        private static bool IsFloorTaskIntersectWall(OpeningParentsData data, IEnumerable<Wall> walls,
            ElementIntersectsElementFilter filter)
        {
            return data.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName && walls.Any(filter.PassesFilter);
        }

        private static bool IsWallTaskIntersectFloor(OpeningParentsData data, IEnumerable<CeilingAndFloor> floors,
            ElementIntersectsElementFilter filter)
        {
            return data.BoxData.FamilyName != Families.FloorRectTaskFamily.SymbolName &&
                   floors.Any(filter.PassesFilter);
        }

        private static bool IsTaskIntersectManyWall(OpeningParentsData data, IEnumerable<Wall> walls,
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

        private static bool IsHostNotPerpendicularPipe(OpeningParentsData data, IEnumerable<Document> documents)
        {
            const double toleranceInDegrees = 3;
            var isPerpendicular = true;
            for (var i = 0; i < data.HostsIds.Count; i++)
                switch (documents.GetElement(data.HostsIds[i]))
                {
                    case Wall _:
                        var pipeVec = data.BoxData.PipesGeometries[i].End.XYZ -
                                      data.BoxData.PipesGeometries[i].Start.XYZ;
                        var wallVec = data.BoxData.HostsGeometries[i].End.XYZ -
                                      data.BoxData.HostsGeometries[i].Start.XYZ;
                        isPerpendicular &= Math.Abs(pipeVec.AngleTo(wallVec) - Math.PI / 2) <
                                           toleranceInDegrees * Math.PI / 180;
                        break;
                    case CeilingAndFloor _:
                        break;
                    default:
                        throw new Exception("Неизвестный тип хост элемента");
                }

            return !isPerpendicular;
        }
    }
}
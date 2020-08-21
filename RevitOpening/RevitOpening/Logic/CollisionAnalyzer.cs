using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitOpening.Extensions;
using RevitOpening.Models;
using ArgumentException = Autodesk.Revit.Exceptions.ArgumentException;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException;

namespace RevitOpening.Logic
{
    public class CollisionAnalyzer
    {
        private readonly Document _document;
        private readonly IEnumerable<Document> _documents;
        private readonly List<Element> _elements;

        public CollisionAnalyzer(Document document, IEnumerable<Document> documents)
        {
            _documents = documents;
            _document = document;
            _elements = _documents.GetAllTasks();
        }

        public void ExecuteAnalysis()
        {
            var walls = _documents.GetAllElementsOfClass<Wall>();
            var floors = _documents.GetAllElementsOfClass<CeilingAndFloor>();
            try
            {
                using (var t = new SubTransaction(_document))
                {
                    t.Start();
                    foreach (var box in _elements)
                        AnalyzeElement(box,walls,floors);
                    t.Commit();
                }
            }
            catch (InvalidOperationException)
            {
                using (var t = new Transaction(_document))
                {
                    t.Start("Analyze");
                    foreach (var box in _elements)
                        AnalyzeElement(box,walls,floors);
                    t.Commit();
                }
            }
        }

        public void AnalyzeElement(Element box,IEnumerable<Wall> walls, IEnumerable<CeilingAndFloor> floors)
        {
            var data = box.GetParentsData();
            data.BoxData.Collisions = new Collisions();
            if (IsHostNotPerpendicularPipe(box))
                data.BoxData.Collisions.Add(Collisions.PipeNotPerpendicularHost);
            if (IsTaskIntersectManyWall(box, walls, data))
                data.BoxData.Collisions.Add(Collisions.TaskIntersectManyWalls);
            if (IsWallTaskIntersectFloor(box, data, floors))
                data.BoxData.Collisions.Add(Collisions.WallTaskIntersectFloor);
            if (IsFloorTaskIntersectWall(box, data, walls))
                data.BoxData.Collisions.Add(Collisions.FloorTaskIntersectWall);
            if (IsTaskIntersectTask(box))
                data.BoxData.Collisions.Add(Collisions.TaskIntersectTask);
            if (data.BoxData.Collisions.ListOfCollisions.Count == 0)
                box.LookupParameter("Несогласованно").Set(0);
            else
                box.LookupParameter("Несогласованно").Set(1);
            box.SetParentsData(data);
        }

        private bool IsTaskIntersectTask(Element box)
        {
            var data = box.GetParentsData();
            var toleranceXYZ = new XYZ(0.001,0.001,0.001);
            var solids = box.get_Geometry(new Options())
                .GetAllSolids()
                .FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001);

            var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var tPoints = solids.Edges
                .Cast<Edge>()
                .Select(y => y.AsCurve().GetEndPoint(0))
                .Select(y => t.OfPoint(y));

            var min = new BoxCombiner(_document, _documents).GetMinPointsCoordinates(tPoints)-toleranceXYZ;
            var max = new BoxCombiner(_document, _documents).GetMaxPointsCoordinates(tPoints)+toleranceXYZ;
            var bbox = new BoundingBoxXYZ
            {
                Min = min,
                Max = max
            };
            var solid = bbox.CreateSolid();
            var backSolid = SolidUtils.CreateTransformed(solid, backT);
            using (var tr = new SubTransaction(_document))
            {
                tr.Start();
                var ds = DirectShape.CreateElement(_document, new ElementId(BuiltInCategory.OST_Doors));
                ds.SetShape(new[] { backSolid });
                tr.Commit();
            }

            var filter = new ElementIntersectsSolidFilter(backSolid);
            foreach (var element in _elements)
            {
                if (element.Id == box.Id)
                    continue;
                if (filter.PassesFilter(element))
                    return true;
            }

            return false;
        }

        private bool IsFloorTaskIntersectWall(Element box, OpeningParentsData data, IEnumerable<Wall> walls)
        {
            if (data.BoxData.FamilyName != Families.FloorRectTaskFamily.SymbolName)
                return false;

            var filterd = new ElementIntersectsElementFilter(box);
            return walls.Any(wall => filterd.PassesFilter(wall));
        }

        private bool IsWallTaskIntersectFloor(Element box, OpeningParentsData data, IEnumerable<CeilingAndFloor> floors)
        {
            if (data.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName)
                return false;

            var filterd = new ElementIntersectsElementFilter(box);
            return floors.Any(floor => filterd.PassesFilter(floor));
        }

        private bool IsTaskIntersectManyWall(Element box, IEnumerable<Wall> walls, OpeningParentsData data)
        {
            if (data.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName)
                return false;

            var filterd = new ElementIntersectsElementFilter(box);
            var wallsIntersect = walls
                .Where(wall => filterd
                    .PassesFilter(wall))
                .ToList();
            return wallsIntersect.Count != 1;
        }

        private bool IsHostNotPerpendicularPipe(Element box)
        {
            var data = box.GetParentsData();
            switch (_document.GetElement(new ElementId(data.HostId)))
            {
                case Wall _:
                    var pipeVec = data.BoxData.PipeGeometry.End.XYZ - data.BoxData.PipeGeometry.Start.XYZ;
                    var wallVec = data.BoxData.WallGeometry.End.XYZ - data.BoxData.WallGeometry.Start.XYZ;
                    return !(Math.Abs(pipeVec.AngleTo(wallVec) - Math.PI / 2) < 3 * Math.PI / 180);
                case CeilingAndFloor _:
                    return false;
                default:
                    return true;
            }
        }
    }
}
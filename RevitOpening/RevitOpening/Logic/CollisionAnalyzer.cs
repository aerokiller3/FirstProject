using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class CollisionAnalyzer
    {
        private readonly Document _document;
        private readonly IEnumerable<Document> _documents;
        private readonly List<FamilyInstance> _elements;
        private readonly AltecJsonSchema _schema;

        public CollisionAnalyzer(Document document, List<FamilyInstance> elements, IEnumerable<Document> documents)
        {
            _documents = documents;
            _document = document;
            _elements = elements;
            _schema = new AltecJsonSchema();
        }

        public void ExecuteAnalysis()
        {
            try
            {
                using (var t = new SubTransaction(_document))
                {
                    t.Start();
                    foreach (var box in _elements)
                        AnalyzeElement(box);
                    t.Commit();
                }
            }
            catch
            {
                using (var t = new Transaction(_document))
                {
                    t.Start("Analyze");
                    foreach (var box in _elements)
                        AnalyzeElement(box);
                    t.Commit();
                }
            }
        }

        public void AnalyzeElement(FamilyInstance box)
        {
            var walls = new List<Wall>();
            var floors = new List<CeilingAndFloor>();
            var data = box.GetParentsData(_schema);
            foreach (var document in _documents)
                walls.AddRange(new FilteredElementCollector(document)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>());
            foreach (var document in _documents)
                floors.AddRange(new FilteredElementCollector(document)
                    .OfClass(typeof(CeilingAndFloor))
                    .Cast<CeilingAndFloor>());

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
            box.SetParentsData(data, _schema);
        }

        private bool IsTaskIntersectTask(Element box)
        {
            var filterd = new ElementIntersectsElementFilter(box);
            return _elements.Any(element => filterd.PassesFilter(element));
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

        private bool IsHostNotPerpendicularPipe(FamilyInstance box)
        {
            var data = box.GetParentsData(_schema);
            switch (_document.GetElement(new ElementId(data.HostId)))
            {
                case Wall _:
                    var pipeVec = data.BoxData.PipeGeometry.End.XYZ - data.BoxData.PipeGeometry.Start.XYZ;
                    var wallVec = data.BoxData.WallGeometry.End.XYZ - data.BoxData.WallGeometry.Start.XYZ;
                    return !(Math.Abs(pipeVec.AngleTo(wallVec) - Math.PI / 2) < Math.Pow(10, -7));
                case CeilingAndFloor _:
                    return false;
                default:
                    return true;
            }
        }
    }
}
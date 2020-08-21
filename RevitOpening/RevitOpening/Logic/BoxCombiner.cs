using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class BoxCombiner : IExternalCommand
    {
        private Document _document;
        private IEnumerable<Document> _documents;

        public BoxCombiner()
        {
        }

        public BoxCombiner(Document document, IEnumerable<Document> documents)
        {
            _documents = documents;
            _document = document;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents.Cast<Document>();

            var select = commandData.Application.ActiveUIDocument.Selection;
            var selected = select.PickObjects(ObjectType.Element, new SelectionFilter(x =>
                        x is FamilyInstance,
                    (x, _) => true))
                .Select(x => _document.GetElement(x))
                .ToArray();
            //.GetElementIds()
            //.Select(x => _documents.GetElement(x.IntegerValue))
            //.ToArray();
            CreateUnitedTask(selected[0], selected[1]);

            return Result.Succeeded;
        }

        public void CombineAllBoxes()
        {
            var isElementsUnited1 = true;
            var isElementsUnited2 = true;
            while (isElementsUnited1 && isElementsUnited2)
            {
                isElementsUnited1 = CombineOneTypeBoxes(Families.WallRectTaskFamily);
                isElementsUnited2 = CombineOneTypeBoxes(Families.FloorRectTaskFamily);
            }
        }

        public Element CreateUnitedTask(Element el1, Element el2)
        {
            var data1 = el1.GetParentsData();
            var data2 = el2.GetParentsData();
            if (!ValidateTasksForCombine(data1, data2))
                throw new Exception("Недопустимый вариант объединения");

            OpeningData opening = null;
            if (data1.PipeId == data2.PipeId)
                opening = CalculateUnitedTaskOnOnePipe(el1, el2, data1, data2);
            else if (_documents.GetElement(data1.HostId) is Wall)
                opening = CalculateUnitedTaskInWall(el1, el2, data1, data2);
            else if (_documents.GetElement(data1.HostId) is CeilingAndFloor)
                opening = CalculateUnitedTaskInFloor(el1, el2, data1, data2);
            Element createdElement=null;

            using (var t = new Transaction(_document))
            {
                t.Start("TestCombine");
                data1.BoxData = opening;
                createdElement = BoxCreator.CreateTaskBox(data1, _document);
                _document.Delete(el1.Id);
                _document.Delete(el2.Id);

                t.Commit();
            }


            return createdElement;
        }

        private OpeningData CalculateUnitedTaskInFloor(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var unitedSolid = GetUnitedSolidForFloor((FamilyInstance) el1, (FamilyInstance) el2);
            var angle1 = XYZ.BasisX.AngleTo(data1.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle1);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle1);
            var bSolid = SolidUtils.CreateTransformed(unitedSolid, t);
            var faceOrigin = unitedSolid.Faces.Cast<PlanarFace>()
                .FirstOrDefault(f => Math.Abs(f.FaceNormal.Z + 1) < Math.Pow(10, -7))
                .YVector;
            var direction = faceOrigin;
            var minUnited = bSolid.GetBoundingBox().Min;
            var maxUnited = bSolid.GetBoundingBox().Max;


            var center1 = data1.BoxData.IntersectionCenter.XYZ;
            var center2 = data2.BoxData.IntersectionCenter.XYZ;
            var tCenter1 = t.OfPoint(center1);
            var tCenter2 = t.OfPoint(center2);
            var middleX = Math.Min(tCenter1.X, tCenter2.X);
            var middleZ = Math.Min(tCenter1.Z, tCenter2.Z);
            var middleY = (tCenter1.Y + tCenter2.Y) / 2;
            var tMiddle = new XYZ(middleX, middleY, middleZ);

            var middle = unitedSolid.ComputeCentroid();
            middle = new XYZ(middle.X, middle.Y, data1.BoxData.IntersectionCenter.Z);
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.X - minUnited.X;
            var depth = data1.BoxData.Depth;

            return new OpeningData(
                width, height, depth,
                direction, middle,
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName);
        }

        public Solid GetUnitedSolidForFloor(FamilyInstance el1, FamilyInstance el2)
        {
            var floors = new[] { el1, el2 };
            var solids = floors
                .Select(x =>
                    x.get_Geometry(new Options())
                        .GetAllSolids()
                        .FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001));

            var angle = XYZ.BasisX.AngleTo(el1.GetParentsData().BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var tPoints = solids
                .SelectMany(x => x?.Edges
                    .Cast<Edge>()
                    .Select(y => y.AsCurve().GetEndPoint(0))
                    .Select(y => t.OfPoint(y)));

            var min = GetMinPointsCoordinates(tPoints);
            var max = GetMaxPointsCoordinates(tPoints);
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

            return backSolid;
        }

        private OpeningData CalculateUnitedTaskInWall(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var unitedSolid = GetUnitedSolid((FamilyInstance) el1, (FamilyInstance) el2);
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var bSolid = SolidUtils.CreateTransformed(unitedSolid, t);
            var minUnited = bSolid.GetBoundingBox().Min;
            var maxUnited = bSolid.GetBoundingBox().Max;
            var middle = FindTasksMiddle(data1, data2, unitedSolid);
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.Z - minUnited.Z;
            var depth = data1.BoxData.Depth;

            return new OpeningData(
                width, height, depth,
                data1.BoxData.Direction.XYZ,
                middle.XYZ,
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName);
        }

        private MyXYZ FindTasksMiddle(OpeningParentsData data1, OpeningParentsData data2, Solid unitedSolid)
        {
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var edges = unitedSolid.Faces.Cast<Face>()
                .FirstOrDefault(f => Math.Abs(t.OfPoint(f.ComputeNormal(UV.BasisU)).X + 1) < Math.Pow(10, -7))
                .EdgeLoops.Cast<EdgeArray>()
                .FirstOrDefault()
                .Cast<Edge>()
                .ToArray();
            var minEdge = edges.First().AsCurve() as Line;
            foreach (var edge in edges)
            {
                var line = edge.AsCurve() as Line;
                var minZ = minEdge.GetEndPoint(0).Z > minEdge.GetEndPoint(1).Z
                    ? minEdge.GetEndPoint(1).Z
                    : minEdge.GetEndPoint(0).Z;
                if (line.GetEndPoint(0).Z < minZ)
                    minEdge = line;
            }

            return new MyXYZ((minEdge.GetEndPoint(0) + minEdge.GetEndPoint(1)) / 2);
        }

        public Solid GetUnitedSolid(FamilyInstance el1, FamilyInstance el2)
        {
            var floors = new[] {el1, el2};
            var solids = floors
                .Select(x =>
                    x.get_Geometry(new Options())
                        .GetAllSolids()
                        .FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001));

            var angle = XYZ.BasisY.Negate().AngleTo(el1.GetParentsData().BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var tPoints = solids
                .SelectMany(x => x?.Edges
                    .Cast<Edge>()
                    .Select(y => y.AsCurve().GetEndPoint(0))
                    .Select(y => t.OfPoint(y)));

            var min = GetMinPointsCoordinates(tPoints);
            var max = GetMaxPointsCoordinates(tPoints);
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

            return backSolid;
        }

        public XYZ GetMaxPointsCoordinates(IEnumerable<XYZ> tPoints)
        {
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var maxZ = double.MinValue;
            foreach (var point in tPoints)
            {
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }

            return new XYZ(maxX, maxY, maxZ);
        }

        public XYZ GetMinPointsCoordinates(IEnumerable<XYZ> tPoints)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var minZ = double.MaxValue;
            foreach (var point in tPoints)
            {
                minX = Math.Min(point.X, minX);
                minY = Math.Min(point.Y, minY);
                minZ = Math.Min(point.Z, minZ);
            }

            return new XYZ(minX, minY, minZ);
        }

        private bool CombineOneTypeBoxes(FamilyParameters familyData)
        {
            var isElementsUnited = false;
            using (var t = new TransactionGroup(_document))
            {
                t.Start("United");
                var tasks = _document.GetTasks(familyData);
                isElementsUnited |= FindTaskIntersections(tasks);
                t.Commit();
            }

            return isElementsUnited;
        }

        private bool FindTaskIntersections(IEnumerable<Element> tasks)
        {
            var elements = tasks.ToList();
            var isElementsUnited = false;
            for (var i = 0; i < elements.Count; i++)
            {
                var data = elements[i].GetParentsData();
                var toleranceXYZ = new XYZ(0.001, 0.001, 0.001);
                var solids = elements[i].get_Geometry(new Options())
                    .GetAllSolids()
                    .FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001);

                var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
                var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
                var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
                var tPoints = solids.Edges
                    .Cast<Edge>()
                    .Select(y => y.AsCurve().GetEndPoint(0))
                    .Select(y => t.OfPoint(y));

                var min = new BoxCombiner(_document, _documents).GetMinPointsCoordinates(tPoints) - toleranceXYZ;
                var max = new BoxCombiner(_document, _documents).GetMaxPointsCoordinates(tPoints) + toleranceXYZ;
                var bbox = new BoundingBoxXYZ
                {
                    Min = min,
                    Max = max
                };
                var solid = bbox.CreateSolid();
                var backSolid = SolidUtils.CreateTransformed(solid, backT);
                var filter = new ElementIntersectsSolidFilter(backSolid);
                for (var j = i + 1; j < elements.Count; j++)
                {
                    if (elements[i].Id == elements[j].Id)
                        continue;
                    if (filter.PassesFilter(elements[j]))
                    {
                        elements.Add(CreateUnitedTask(elements[i], elements[j]));
                        elements.RemoveAt(j);
                        elements.RemoveAt(i);
                        isElementsUnited = true;
                        i -= 1;
                        break;
                    }
                }
            }

            return isElementsUnited;
        }

        private OpeningData CalculateUnitedTaskOnOnePipe(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            var width = data1.BoxData.Width;
            var height = data1.BoxData.Height;
            var depth = data1.BoxData.Depth + data2.BoxData.Depth;
            var normalize = orientation.Normalize();
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);
            var middle = normalize.IsAlmostEqualTo(source)
                ? data1.BoxData.IntersectionCenter.XYZ
                : data2.BoxData.IntersectionCenter.XYZ;
            return new OpeningData(
                width, height, depth,
                data1.BoxData.Direction.XYZ,
                middle,
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName);
        }

        public bool ValidateTasksForCombine(OpeningParentsData data1, OpeningParentsData data2)
        {
            return data1.PipeId == data2.PipeId
                   || _documents.GetElement(data1.HostId) is Wall
                   || _documents.GetElement(data1.HostId) is CeilingAndFloor;
        }
    }
}
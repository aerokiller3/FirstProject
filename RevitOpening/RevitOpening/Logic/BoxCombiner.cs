using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class BoxCombiner : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _schema = new AltecJsonSchema();
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents.Cast<Document>();
            using (var t = new Transaction(_document))
            {
                t.Start("TestCombine");
                var select = commandData.Application.ActiveUIDocument.Selection;
                var selected = select.PickObjects(ObjectType.Element, new SelectionFilter((x) =>
                            x is FamilyInstance,
                        (x, _) => true))
                    .Select(x => _document.GetElement(x))
                    .ToArray();
                //.GetElementIds()
                //.Select(x => _documents.GetElementFromDocuments(x.IntegerValue))
                //.ToArray();
                CreateUnitedTask(selected[0], selected[1]);
                t.Commit();
            }

            return Result.Succeeded;
        }

        private Document _document;
        private AltecJsonSchema _schema;
        private IEnumerable<Document> _documents;

        public BoxCombiner()
        {
        }

        public BoxCombiner(Document document, AltecJsonSchema schema, IEnumerable<Document> documents)
        {
            _schema = schema;
            _documents = documents;
            _document = document;
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

        public FamilyInstance CreateUnitedTask(Element el1, Element el2)
        {
            var data1 = el1.GetParentsData(_schema);
            var data2 = el2.GetParentsData(_schema);
            OpeningData opening = null;
            if (data1.PipeId == data2.PipeId)
                opening = CalculateUnitedTaskOnOnePipe(el1, el2, data1, data2);
            else if (data1.HostId == data2.HostId)
                opening = CalculateUnitedTask(el1, el2, data1, data2);

            data1.BoxData = opening;
            _document.Delete(el1.Id);
            _document.Delete(el2.Id);
            var createEl = BoxCreator.CreateTaskBox(data1, _document, _schema);
            Clipboard.SetText(createEl.Id.ToString());
            var analyzer = new CollisionAnalyzer(_document, new List<FamilyInstance> { createEl }, _documents);
            analyzer.ExecuteAnalysis();
            return createEl;
        }

        private OpeningData CalculateUnitedTaskInOneHost(Element el1, Element el2, OpeningParentsData data1, OpeningParentsData data2)
        {
            var s1 = el1.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var s2 = el2.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var min = GetMinFromSolids(s1, s2);
            var max = GetMaxFromSolids(s1, s2);

            var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            var normal = orientation.Normalize();
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);

            var middle = (min + max) / 2;
            //middle=new XYZ(middle.X,middle.Y,data1.BoxData.IntersectionCenter.Z);
            var cat1 = max.X - min.X;
            var cat2 = max.Y - min.Y;
            var cat3 = max.Z - min.Z;

            var tx = Math.Atan2(data1.BoxData.Direction.Y, data1.BoxData.Direction.X);
            var ty = Math.Atan2(data1.BoxData.Direction.X, data1.BoxData.Direction.Y);
            var width = cat2 * tx;
            var height = cat1 * ty;
            var depth = data1.BoxData.Depth;

            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                new MyXYZ(middle),
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
        }

        private OpeningData CalculateUnitedTask(Element el1, Element el2, OpeningParentsData data1, OpeningParentsData data2)
        {
            var s1 = el1.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var s2 = el2.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var unitedSolid = GetUnitedSolid((FamilyInstance) el1, (FamilyInstance) el2);
            //var minUnited = unitedSolid.GetBoundingBox().Min;
            //var maxUnited = unitedSolid.GetBoundingBox().Max;
            var min = GetMinFromSolids(s1, s2);
            var max = GetMaxFromSolids(s1, s2);
            //var angle = XYZ.BasisX.AngleTo(data1.BoxData.Direction.XYZ);
            //var middle = unitedSolid.ComputeCentroid();
            //var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            //min = t.OfPoint(min);
            //max = t.OfPoint(max);
            //var width = max.Z - min.Z;
            //var height = max.Y - min.Y;
            //var backT = Transform.CreateRotation(XYZ.BasisZ, angle);

            //var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            //var normal = orientation.Normalize();
            //var source = data1.BoxData.Direction.XYZ;
            //source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);

            //var depth = data1.BoxData.Depth;
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var bSolid = SolidUtils.CreateTransformed(unitedSolid, t);
            var minUnited = bSolid.GetBoundingBox().Min;
            var maxUnited = bSolid.GetBoundingBox().Max;

            var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            var normal = orientation.Normalize();
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);

            var center1 = data1.BoxData.IntersectionCenter.XYZ;
            var center2 = data2.BoxData.IntersectionCenter.XYZ;
            var tCenter1 = t.OfPoint(center1);
            var tCenter2 = t.OfPoint(center2);
            var middleX = Math.Min(tCenter1.X, tCenter2.X);
            var middleZ = Math.Min(tCenter1.Z, tCenter2.Z);
            var middleY = (tCenter1.Y + tCenter2.Y) / 2 ;

            var tMiddle = new XYZ(middleX, middleY, middleZ);

            var middle = backT.OfPoint(tMiddle);

            //var middle = (center1+ center2) / 2;

            //var middle = (min + max) / 2;
            //var middle = unitedSolid.ComputeCentroid();
            ////?
            //middle=new XYZ(middle.X,middle.Y, data1.BoxData.IntersectionCenter.Z);

            //var middle =
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.Z - minUnited.Z;
            var depth = data1.BoxData.Depth;

            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                new MyXYZ(middle),
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
        }

        public Solid GetUnitedSolid(FamilyInstance el1, FamilyInstance el2)
        {
            var data1 = el1.GetParentsData(_schema);
            var data2 = el2.GetParentsData(_schema);
            var floors = new[] {el1, el2};
            var solids = floors.Select(x =>
                x.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001));

            //var angle = XYZ.BasisX.AngleTo(floors.First().Orientation);
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);

            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var tPoints = solids
                .SelectMany(x => x.Edges
                    .Cast<Edge>()
                    .Select(y => y.AsCurve().GetEndPoint(0))
                    .Select(y => t.OfPoint(y)));
            //var pointsSolids = tPoints.Select(x => SolidExtensions.GetBoxAroundPoint(x).SolidBoundingBox());

            var min = Min(tPoints);
            var max = Max(tPoints);
            var bbox = new BoundingBoxXYZ
            {
                Min = min,
                Max = max
            };
            //bbox.Transform = backT;
            var solid = bbox.SolidBoundingBox();
            var backSolid = SolidUtils.CreateTransformed(solid, backT);
            using (var tr = new SubTransaction(_document))
            {
                tr.Start();
                var ds = DirectShape.CreateElement(_document, new ElementId(BuiltInCategory.OST_Floors));
                ds.SetShape(new[] { backSolid });
                tr.Commit();
            }

            return backSolid;
        }

        private XYZ Max(IEnumerable<XYZ> tPoints)
        {
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var maxZ = double.MinValue;
            foreach (var point in tPoints)
            {
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
            }

            return new XYZ(maxX, maxY, maxZ);
        }

        private XYZ Min(IEnumerable<XYZ> tPoints)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var minZ = double.MaxValue;
            foreach (var point in tPoints)
            {
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
            }

            return new XYZ(minX, minY, minZ);
        }

        private bool CombineOneTypeBoxes(FamilyParameters familyData)
        {
            var isElementsUnited = false;
            using (var t = new Transaction(_document))
            {
                t.Start("United");
                var tasks = _document.GetTasksFromDocument(familyData);
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
                var filtered = new ElementIntersectsElementFilter(elements[i]);
                for (var j = i + 1; j < elements.Count; j++)
                    if (filtered.PassesFilter(elements[j]))
                    {
                        elements.Add(CreateUnitedTask(elements[i], elements[j]));
                        elements.RemoveAt(j);
                        elements.RemoveAt(i);
                        isElementsUnited = true;
                        i -= 1;
                        break;
                    }
            }

            return isElementsUnited;
        }

        private OpeningData CalculateUnitedTaskOnOnePipe(Element el1, Element el2, OpeningParentsData data1, OpeningParentsData data2)
        {
            var s1 = el1.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var s2 = el2.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var min = GetMinFromSolids(s1, s2);
            var max = GetMaxFromSolids(s1, s2);

            var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            XYZ middle;
            var width = data1.BoxData.Width;
            var height = data1.BoxData.Height;
            var depth = data1.BoxData.Depth + data2.BoxData.Depth;
            var intersectHalf = (max - min) / 2;
            var normalize = orientation.Normalize();
            //var line = Line.CreateBound(data1.BoxData.IntersectionCenter.GetXYZ(),
            //    data2.BoxData.IntersectionCenter.GetXYZ());
            //var line1 = Line.CreateBound(data1.BoxData.IntersectionCenter.GetXYZ()
            //    , data1.BoxData.IntersectionCenter.GetXYZ() + data1.BoxData.Direction.GetXYZ());
            //var line2 = Line.CreateBound(data2.BoxData.IntersectionCenter.GetXYZ()
            //    , data2.BoxData.IntersectionCenter.GetXYZ() + data2.BoxData.Direction.GetXYZ());
            //using (var subtr = new SubTransaction(_document))
            //{
            //    subtr.Start();
            //    var direct = DirectShape.CreateElement(_document, new ElementId(BuiltInCategory.OST_Floors));
            //    direct.SetShape(new[]
            //    {
            //        line.CreateCylindricalSolidFromLine(),
            //        line1.CreateCylindricalSolidFromLine(),
            //        line2.CreateCylindricalSolidFromLine()
            //    });
            //    Clipboard.SetText(direct.Id.ToString());
            //    subtr.Commit();
            //}
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);
            if (normalize.IsAlmostEqualTo(source))
            {
                middle = data1.BoxData.IntersectionCenter.XYZ;
            }
            else
            {
                middle = data2.BoxData.IntersectionCenter.XYZ;
            }
            //var bias = new XYZ(
            //    data1.BoxData.PipeGeometry.Orientation.X * data1.BoxData.WallGeometry.Orientation.X * intersectHalf.X,
            //    data1.BoxData.PipeGeometry.Orientation.Y * data1.BoxData.WallGeometry.Orientation.Y * intersectHalf.Y,
            //    data1.BoxData.PipeGeometry.Orientation.Z * data1.BoxData.WallGeometry.Orientation.Z * intersectHalf.Z);

            //if (Math.Abs(data1.BoxData.Direction.Y - 1) < Math.Pow(10, -7))
            //{
            //    middle = data1.BoxData.IntersectionCenter.X >= data2.BoxData.IntersectionCenter.X
            //        ? data1.BoxData.IntersectionCenter.GetXYZ()
            //        : data2.BoxData.IntersectionCenter.GetXYZ();
            //    var buf = depth;
            //    depth = width;
            //    width = height;
            //    height = buf;
            //}
            //else if (Math.Abs(data1.BoxData.Direction.X - 1) < Math.Pow(10, -7))
            //{
            //    middle = data1.BoxData.IntersectionCenter.Y < data2.BoxData.IntersectionCenter.Y
            //        ? data1.BoxData.IntersectionCenter.GetXYZ()
            //        : data2.BoxData.IntersectionCenter.GetXYZ();
            //    var buf = depth;
            //    depth = height;
            //    height = buf;
            //}
            //else if (Math.Abs(data1.BoxData.Direction.Z + 1) < Math.Pow(10, -7))
            //{
            //    var z = data1.BoxData.IntersectionCenter.Z > data2.BoxData.IntersectionCenter.Z
            //        ? data1.BoxData.IntersectionCenter.Z
            //        : data2.BoxData.IntersectionCenter.Z;
            //    middle =new XYZ(middle.X, middle.Y, z);
            //}

            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                new MyXYZ(middle),
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
        }

        private XYZ GetMaxFromSolids(Solid s1, Solid s2)
        {
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var maxZ = double.MinValue;
            foreach (var point in s1.Edges.Cast<Edge>()
                .Select(e => e.AsCurve())
                .Select(curve => curve.GetEndPoint(0)))
            {
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
            }

            foreach (var point in s2.Edges.Cast<Edge>()
                .Select(e => e.AsCurve())
                .Select(curve => curve.GetEndPoint(0)))
            {
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
            }

            return new XYZ(maxX, maxY, maxZ);
        }

        private XYZ GetMinFromSolids(Solid s1, Solid s2)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var minZ = double.MaxValue;
            foreach (var point in s1.Edges.Cast<Edge>()
                .Select(e => e.AsCurve())
                .Select(curve => curve.GetEndPoint(0)))
            {
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
            }

            foreach (var point in s2.Edges.Cast<Edge>()
                .Select(e => e.AsCurve())
                .Select(curve => curve.GetEndPoint(0)))
            {
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
            }

            return new XYZ(minX, minY, minZ);
        }
    }
}
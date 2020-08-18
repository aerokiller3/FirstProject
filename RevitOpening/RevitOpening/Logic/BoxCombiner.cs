using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class BoxCombiner
    {
        private readonly Document _document;
        private readonly AltecJsonSchema _schema;

        public BoxCombiner(Document document, AltecJsonSchema schema)
        {
            _schema = schema;
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

            //var solid = el1.get_Geometry(new Options()).FirstOrDefault() as Solid;
            //foreach (Face e in solid.Edges.Cast<Edge>().Select(x => x.AsCurve().GetEndPoint(0)))
            //{
            //    var computeNormal = e.ComputeNormal(UV.Zero);
            //    if (computeNormal != XYZ.BasisZ || computeNormal != XYZ.BasisZ.Negate())
            //}

            //BooleanOperationsUtils.ExecuteBooleanOperation(el1, null, BooleanOperationsType.Union)
            var opening = CalculateOpening(el1, el2, data1, data2);
            data1.BoxData = opening;
            _document.Delete(el1.Id);
            _document.Delete(el2.Id);
            return BoxCreator.CreateTaskBox(data1, _document, _schema);
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

        private OpeningData CalculateOpening(Element el1, Element el2, OpeningParentsData data1, OpeningParentsData data2)
        {
            var box1 = el1.get_BoundingBox(_document.ActiveView);
            var box2 = el2.get_BoundingBox(_document.ActiveView);
            var s1 = el1.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var s2 = el2.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var min = GetMinFromSolids(s1, s2);
            var max = GetMaxFromSolids(s1, s2);
            var bbox = new BoundingBoxXYZ();
            bbox.Min = min;
            bbox.Max = max;
            var solid = bbox.SolidBoundingBox();
            //using (var t = new SubTransaction(_document))
            //{
            //    t.Start();
            //    var direct = DirectShape.CreateElement(_document, new ElementId(BuiltInCategory.OST_Doors));
            //    direct.SetShape(new[] { solid });
            //    t.Commit();
            //}
            var middle = (min + max) / 2;
            var width = max.Y - min.Y;
            var height = max.Z - min.Z;
            var depth = max.X - min.X;
            var intersectHalf = (max - min) / 2;

            var bias = new XYZ(
                data1.BoxData.PipeGeometry.Orientation.X * data1.BoxData.WallGeometry.Orientation.X * intersectHalf.X,
                data1.BoxData.PipeGeometry.Orientation.Y * data1.BoxData.WallGeometry.Orientation.Y * intersectHalf.Y,
                data1.BoxData.PipeGeometry.Orientation.Z * data1.BoxData.WallGeometry.Orientation.Z * intersectHalf.Z);

            middle += bias;

            if (data1.BoxData.FamilyName == Families.WallRectTaskFamily.SymbolName)
                middle -= new XYZ(0, 0, height / 2);


            if (data1.BoxData.FamilyName == Families.FloorRectTaskFamily.SymbolName)
            {
                var buf = width;
                width = height;
                height = buf;
            }

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
            foreach (var curve in s1.Edges.Cast<Edge>().Select(e => e.AsCurve()))
            {
                var point = curve.GetEndPoint(0);
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
                point = curve.GetEndPoint(1);
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
            }

            foreach (var curve in s2.Edges.Cast<Edge>().Select(e => e.AsCurve()))
            {
                var point = curve.GetEndPoint(0);
                maxX = point.X > maxX ? point.X : maxX;
                maxY = point.Y > maxY ? point.Y : maxY;
                maxZ = point.Z > maxZ ? point.Z : maxZ;
                point = curve.GetEndPoint(1);
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
            foreach (var curve in s1.Edges.Cast<Edge>().Select(e=>e.AsCurve()))
            {
                var point = curve.GetEndPoint(0);
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
                point = curve.GetEndPoint(1);
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
            }

            foreach (var curve in s2.Edges.Cast<Edge>().Select(e => e.AsCurve()))
            {
                var point = curve.GetEndPoint(0);
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
                point = curve.GetEndPoint(1);
                minX = point.X < minX ? point.X : minX;
                minY = point.Y < minY ? point.Y : minY;
                minZ = point.Z < minZ ? point.Z : minZ;
            }

            return new XYZ(minX, minY, minZ);
        }
    }
}
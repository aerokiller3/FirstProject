using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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
                var selected = commandData.Application.ActiveUIDocument.Selection
                    .GetElementIds()
                    .Select(x => _documents.GetElementFromDocuments(x.IntegerValue))
                    .ToArray();
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
            var s1 = el1.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var s2 = el2.get_Geometry(new Options()).GetAllSolids().FirstOrDefault(s => s.Volume != 0);
            var min = GetMinFromSolids(s1, s2);
            var max = GetMaxFromSolids(s1, s2);

            var middle = (min + max) / 2;
            var width = max.Y - min.Y;
            var height = max.Z - min.Z;
            var depth = max.X - min.X;
            var intersectHalf = (max - min) / 2;

            //var bias = new XYZ(
            //    data1.BoxData.PipeGeometry.Orientation.X * data1.BoxData.WallGeometry.Orientation.X * intersectHalf.X,
            //    data1.BoxData.PipeGeometry.Orientation.Y * data1.BoxData.WallGeometry.Orientation.Y * intersectHalf.Y,
            //    data1.BoxData.PipeGeometry.Orientation.Z * data1.BoxData.WallGeometry.Orientation.Z * intersectHalf.Z);

            if (data1.BoxData.IntersectionCenter.X >= data2.BoxData.IntersectionCenter.X
                && data1.BoxData.IntersectionCenter.Y >= data2.BoxData.IntersectionCenter.Y)
                middle = data1.BoxData.IntersectionCenter.GetXYZ();
            else
                middle = data2.BoxData.IntersectionCenter.GetXYZ();


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
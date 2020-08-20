using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    [Transaction(TransactionMode.Manual)]
    public class BoxCombiner : IExternalCommand
    {
        private Document _document;
        private IEnumerable<Document> _documents;
        private AltecJsonSchema _schema;

        public BoxCombiner()
        {
        }

        public BoxCombiner(Document document, AltecJsonSchema schema, IEnumerable<Document> documents)
        {
            _schema = schema;
            _documents = documents;
            _document = document;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _schema = new AltecJsonSchema();
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents.Cast<Document>();
            using (var t = new Transaction(_document))
            {
                t.Start("TestCombine");
                var select = commandData.Application.ActiveUIDocument.Selection;
                var selected = select.PickObjects(ObjectType.Element, new SelectionFilter(x =>
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
            if(!ValidateTasksForCombine(data1,data2))
                throw new Exception("Недопустимый вариант объединения");

            OpeningData opening = null;
            if (data1.PipeId == data2.PipeId)
                opening = CalculateUnitedTaskOnOnePipe(el1, el2, data1, data2);
            else if (_documents.GetElementFromDocuments(data1.HostId) is Wall)
                opening = CalculateUnitedTaskInWall(el1, el2, data1, data2);
            else if (_documents.GetElementFromDocuments(data1.HostId) is CeilingAndFloor)
                opening = CalculateUnitedTaskInFloor(el1, el2, data1, data2);

            data1.BoxData = opening;
            _document.Delete(el1.Id);
            _document.Delete(el2.Id);
            var createEl = BoxCreator.CreateTaskBox(data1, _document, _schema);
            Clipboard.SetText(createEl.Id.ToString());
            var analyzer = new CollisionAnalyzer(_document, new List<FamilyInstance> {createEl}, _documents);
            analyzer.ExecuteAnalysis();
            return createEl;
        }

        private OpeningData CalculateUnitedTaskInFloor(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var unitedSolid = GetUnitedSolid((FamilyInstance) el1, (FamilyInstance) el2);
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var bSolid = SolidUtils.CreateTransformed(unitedSolid, t);
            var minUnited = bSolid.GetBoundingBox().Min;
            var maxUnited = bSolid.GetBoundingBox().Max;
            var middle = new MyXYZ(unitedSolid.ComputeCentroid());
            middle = new MyXYZ(middle.X, middle.Y, data1.BoxData.IntersectionCenter.Z);
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.X - minUnited.X;
            var depth = data1.BoxData.Depth;

            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                middle,
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
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

            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                middle,
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
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

            var angle = XYZ.BasisY.Negate().AngleTo(el1.GetParentsData(_schema).BoxData.Direction.XYZ);
            var t = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = Transform.CreateRotation(XYZ.BasisZ, angle);
            var tPoints = solids
                .SelectMany(x => x?.Edges
                    .Cast<Edge>()
                    .Select(y => y.AsCurve().GetEndPoint(0))
                    .Select(y => t.OfPoint(y)));

            var min = Min(tPoints);
            var max = Max(tPoints);
            var bbox = new BoundingBoxXYZ
            {
                Min = min,
                Max = max
            };
            var solid = bbox.SolidBoundingBox();
            var backSolid = SolidUtils.CreateTransformed(solid, backT);
            //using (var tr = new SubTransaction(_document))
            //{
            //    tr.Start();
            //    var ds = DirectShape.CreateElement(_document, new ElementId(BuiltInCategory.OST_Windows));
            //    ds.SetShape(new[] {backSolid});
            //    tr.Commit();
            //}

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

        private OpeningData CalculateUnitedTaskOnOnePipe(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var orientation = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            XYZ middle;
            var width = data1.BoxData.Width;
            var height = data1.BoxData.Height;
            var depth = data1.BoxData.Depth + data2.BoxData.Depth;
            var normalize = orientation.Normalize();
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);
            if (normalize.IsAlmostEqualTo(source))
                middle = data1.BoxData.IntersectionCenter.XYZ;
            else
                middle = data2.BoxData.IntersectionCenter.XYZ;
            return new OpeningData(null,
                width, height, depth,
                data1.BoxData.Direction,
                new MyXYZ(middle),
                data1.BoxData.WallGeometry,
                data1.BoxData.PipeGeometry,
                data1.BoxData.FamilyName,
                data1.BoxData.Level);
        }

        public bool ValidateTasksForCombine(OpeningParentsData data1, OpeningParentsData data2)
        {
            return data1.BoxData.Direction.Equals(data2.BoxData.Direction)
                   && (data1.PipeId == data2.PipeId
                       || _documents.GetElementFromDocuments(data1.HostId) is Wall
                       || _documents.GetElementFromDocuments(data1.HostId) is CeilingAndFloor);
        }
    }
}
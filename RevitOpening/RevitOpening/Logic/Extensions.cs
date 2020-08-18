using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class Extensions
    {
        public static Solid SolidBoundingBox(this BoundingBoxXYZ bbox)
        {
            // corners in BBox coords
            var (minX, minY, minZ) = (bbox.Min.X,bbox.Min.Y, bbox.Min.Z);
            var (maxX, maxY, maxZ) = (bbox.Max.X, bbox.Max.Y, bbox.Max.Z);
            var pt0 = new XYZ(minX, minY, minZ);
            var pt1 = new XYZ(maxX, minY, minZ);
            var pt2 = new XYZ(maxX, maxY, minZ);
            var pt3 = new XYZ(minX, maxY, minZ);
            //edges in BBox coords
            var edge0 = Line.CreateBound(pt0, pt1);
            var edge1 = Line.CreateBound(pt1, pt2);
            var edge2 = Line.CreateBound(pt2, pt3);
            var edge3 = Line.CreateBound(pt3, pt0);
            //create loop, still in BBox coords
            var height = maxZ - minZ;
            var baseLoop = CurveLoop.Create(new Curve[] {edge0, edge1, edge2, edge3});
            var preTransformBox = GeometryCreationUtilities.CreateExtrusionGeometry(new[] {baseLoop}, XYZ.BasisZ, height);
            var transformBox = SolidUtils.CreateTransformed(preTransformBox, bbox.Transform);
            return transformBox;
        }

        public static IEnumerable<Solid> GetAllSolids(this GeometryElement geometry)
        {
            foreach (GeometryObject gObject in geometry)
            {
                if (gObject is Solid solid)
                    yield return solid;
                GeometryElement geometryElement = default;
                if (gObject is GeometryInstance geometryInstance)
                    geometryElement = geometryInstance.GetInstanceGeometry();
                if (gObject is GeometryElement element)
                    geometryElement = element;
                if (geometryElement != default)
                    foreach (var deepSolid in GetAllSolids(geometryElement))
                        yield return deepSolid;
            }
        }

        public static Element GetElement(this ElementId id, IEnumerable<Document> documents)
        {
            return documents.GetElementFromDocuments(id.IntegerValue);
        }

        public static Element GetElementFromDocuments(this IEnumerable<Document> documents, int id)
        {
            return documents
                .Select(document => document
                    .GetElement(new ElementId(id)))
                .FirstOrDefault(curEl => curEl != null);
        }

        public static IEnumerable<Element> GetTasksFromDocument(this Document document,
            FamilyParameters familyParameters)
        {
            var collector = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance));

            return collector
                .Where(e => e.Name == familyParameters.InstanceName)
                .ToList();
        }

        public static double GetInFoot(this double number)
        {
            return number / 304.8;
        }

        public static double CalculateSize(double wallWidth, double angle, double ductWidth, double offset)
        {
            return wallWidth / Math.Tan(angle) + ductWidth / Math.Sin(angle) + GetInFoot(offset);
        }

        public static double SqrtOfSqrSum(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        public static OpeningParentsData GetParentsData(this Element element, AltecJsonSchema schema)
        {
            var json = schema.GetJson(element);
            return JsonConvert.DeserializeObject<OpeningParentsData>(json);
        }

        public static void SetParentsData(this Element element, OpeningParentsData parentsData, AltecJsonSchema schema)
        {
            var json = JsonConvert.SerializeObject(parentsData);
            schema.SetJson(element, json);
        }

        public static double GetAcuteAngle(double angel)
        {
            return angel > Math.PI / 2
                ? Math.PI - angel
                : angel;
        }

        public static bool IsRoundPipe(this MEPCurve pipe)
        {
            bool isRound;
            try
            {
                var p = pipe.Width;
                isRound = false;
            }
            catch
            {
                isRound = true;
            }

            return isRound;
        }


        public static double GetPipeWidth(this MEPCurve pipe)
        {
            double pipeWidth;
            try
            {
                pipeWidth = pipe.Width;
            }
            catch
            {
                pipeWidth = pipe.Diameter;
            }

            return pipeWidth;
        }

        public static double GetPipeHeight(this MEPCurve pipe)
        {
            double pipeHeight;
            try
            {
                pipeHeight = pipe.Height;
            }
            catch
            {
                pipeHeight = pipe.Diameter;
            }

            return pipeHeight;
        }
    }
}
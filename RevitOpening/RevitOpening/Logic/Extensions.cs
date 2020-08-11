using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class Extensions
    {
        public static IEnumerable<Level> GetAllLevels(this Document document)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(Level));
            return collector.Cast<Level>();
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
                .Where(e => e.Name == familyParameters.InstanseName)
                .ToList();
        }

        public static double GetOffsetInFoot(double offset)
        {
            return offset / 304.8;
        }

        public static double CalculateSize(double wallWidth, double angle, double ductWidth, double offset)
        {
            return wallWidth / Math.Tan(angle) + ductWidth / Math.Sin(angle) + GetOffsetInFoot(offset);
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
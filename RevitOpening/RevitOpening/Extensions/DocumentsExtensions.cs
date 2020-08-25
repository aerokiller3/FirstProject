using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Logic;

namespace RevitOpening.Extensions
{
    public static class DocumentsExtensions
    {
        public static Element GetElement(this IEnumerable<Document> documents, string uniqueId)
        {
            var els=documents
                .Select(document => document
                    .GetElement(uniqueId));
            return els.FirstOrDefault(curEl => curEl != null);
        }

        public static List<Element> GetAllOpenings(this IEnumerable<Document> documents)
        {
            var elements = new List<Element>();
            foreach (var document in documents)
            {
                elements.AddRange(document.GetTasks(Families.FloorRectOpeningFamily));
                elements.AddRange(document.GetTasks(Families.WallRectOpeningFamily));
                elements.AddRange(document.GetTasks(Families.WallRoundOpeningFamily));
            }

            return elements;
        }

        public static List<FamilyInstance> GetAllTasks(this IEnumerable<Document> documents)
        {
            var elements = new List<FamilyInstance>();
            foreach (var document in documents)
            {
                elements.AddRange(document.GetTasks(Families.FloorRectTaskFamily));
                elements.AddRange(document.GetTasks(Families.WallRectTaskFamily));
                elements.AddRange(document.GetTasks(Families.WallRoundTaskFamily));
            }

            return elements;
        }

        public static List<T> GetAllElementsOfClass<T>(this IEnumerable<Document> documents)
        {
            var elements = new List<T>();
            foreach (var document in documents)
                using (var collector = new FilteredElementCollector(document)
                    .OfClass(typeof(T)))
                {
                    elements.AddRange(collector.Cast<T>());
                }

            return elements;
        }
    }
}
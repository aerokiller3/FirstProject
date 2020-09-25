namespace RevitOpening.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Logic;
    using Models;

    internal static class DocumentsExtensions
    {
        public static Element GetElement(this IEnumerable<Document> documents, string uniqueId)
        {
            var els = documents
               .Select(document => document
                   .GetElement(uniqueId));
            return els.FirstOrDefault(curEl => curEl != null);
        }

        public static Element GetElement(this IEnumerable<Document> documents, int id)
        {
            var els = documents
               .Select(document => document
                   .GetElement(new ElementId(id)));
            return els.FirstOrDefault(curEl => curEl != null);
        }

        public static List<FamilyInstance> GetAllOpenings(this ICollection<Document> documents)
        {
            var elements = new List<FamilyInstance>();
            elements.AddRange(documents.GetTasksByName(Families.FloorRectOpeningFamily));
            elements.AddRange(documents.GetTasksByName(Families.WallRectOpeningFamily));
            elements.AddRange(documents.GetTasksByName(Families.WallRoundOpeningFamily));
            return elements;
        }

        public static List<FamilyInstance> GetTasksByName(this IEnumerable<Document> documents,
            FamilyParameters familyParameters)
        {
            var elements = new List<FamilyInstance>();
            foreach (var document in documents)
            {
                var collector = new FilteredElementCollector(document)
                               .OfCategory(BuiltInCategory.OST_Windows)
                               .OfClass(typeof(FamilyInstance));
                elements.AddRange(collector
                                 .Cast<FamilyInstance>()
                                 .Where(e => e.Symbol.FamilyName == familyParameters.SymbolName));
            }

            return elements;
        }

        public static List<FamilyInstance> GetAllTasks(this ICollection<Document> documents)
        {
            var elements = new List<FamilyInstance>();
            elements.AddRange(documents.GetTasksByName(Families.FloorRectTaskFamily));
            elements.AddRange(documents.GetTasksByName(Families.WallRectTaskFamily));
            elements.AddRange(documents.GetTasksByName(Families.WallRoundTaskFamily));

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
namespace RevitOpening.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Logic;

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

        public static List<FamilyInstance> GetAllOpenings(this IEnumerable<Document> documents)
        {
            var elements = new List<FamilyInstance>();
            foreach (var document in documents)
            {
                // Добавил овальные трубы
                elements.AddRange(document.GetTasksByName(Families.FloorRectOpeningFamily));
                elements.AddRange(document.GetTasksByName(Families.WallRectOpeningFamily));
                elements.AddRange(document.GetTasksByName(Families.WallRoundOpeningFamily));
                //elements.AddRange(document.GetTasksByName(Families.WallElipticalOpeningFamily));
            }

            return elements;
        }

        public static List<FamilyInstance> GetAllTasks(this IEnumerable<Document> documents)
        {
            var elements = new List<FamilyInstance>();
            foreach (var document in documents)
            {
                elements.AddRange(document.GetTasksByName(Families.FloorRectTaskFamily));
                elements.AddRange(document.GetTasksByName(Families.WallRectTaskFamily));
                elements.AddRange(document.GetTasksByName(Families.WallRoundTaskFamily));
                //elements.AddRange(document.GetTasksByName(Families.WallElipticalTaskFamily));
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
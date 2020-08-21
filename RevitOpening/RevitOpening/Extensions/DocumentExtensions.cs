using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class DocumentExtensions
    {
        public static IEnumerable<Element> GetTasks(this Document document, FamilyParameters familyParameters)
        {
            using (var collector = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance)))
            {
                return collector
                    .Where(e => e.Name == familyParameters.InstanceName)
                    .ToList();
            }
        }
    }
}
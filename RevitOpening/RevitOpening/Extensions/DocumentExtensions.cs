using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class DocumentExtensions
    {
        public static List<FamilyInstance> GetTasks(this Document document, FamilyParameters familyParameters)
        {
            using (var collector = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilyInstance)))
            {
                return collector
                    .Cast<FamilyInstance>()
                    .Where(e => e.Symbol.FamilyName == familyParameters.SymbolName)
                    .ToList();
            }
        }

        public static FamilySymbol GetFamilySymbol(this Document document, string familyName)
        {
            var collector = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilySymbol));

            var familySymbol = collector
                .Cast<FamilySymbol>()
                .FirstOrDefault(x => x.FamilyName == familyName);
            if (familySymbol == null)
                throw new Exception("Невозможно найти семейство");

            collector.Dispose();
            return familySymbol;
        }
    }
}
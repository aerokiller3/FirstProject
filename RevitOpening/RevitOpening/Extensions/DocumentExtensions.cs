namespace RevitOpening.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Models;

    internal static class DocumentExtensions
    {
        public static IEnumerable<FamilyInstance> GetTasksByName(this Document document, FamilyParameters familyParameters)
        {
            var collector = new FilteredElementCollector(document)
                           .OfCategory(BuiltInCategory.OST_Windows)
                           .OfClass(typeof(FamilyInstance));

            return collector
                  .Cast<FamilyInstance>()
                  .Where(e => e.Symbol.FamilyName == familyParameters.SymbolName);
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
                throw new Exception($"Невозможно найти семейство: {familyName}");

            collector.Dispose();
            return familySymbol;
        }
    }
}
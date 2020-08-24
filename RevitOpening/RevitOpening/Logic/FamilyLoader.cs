using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;

namespace RevitOpening.Logic
{
    public static class FamilyLoader
    {
        public static void LoadAllFamiliesToProject(Document document)
        {
            foreach (var family in Families.AllFamilies)
                LoadFamilyToProject(family.SymbolName, document);
        }

        private static void LoadFamilyToProject(string familyName, Document document)
        {
            var currentDirectory = GetCurrentDirectory();
            var fileName = $"{currentDirectory}\\Families\\{familyName}.rfa";
            var isInProj = IsFamilyInProject(familyName, document);
            var isFileExist = File.Exists(fileName);
            if (!isInProj && isFileExist)
                document.LoadFamily(fileName);
        }

        private static bool IsFamilyInProject(string familyName, Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(f => f.Name == familyName) != null;
        }

        private static string GetCurrentDirectory()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var endIndex = currentDirectory.LastIndexOf('\\');
            return currentDirectory.Substring(0, endIndex);
        }
    }
}
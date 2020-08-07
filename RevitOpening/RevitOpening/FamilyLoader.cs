using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public class FamilyLoader
    {
        private Document _document;

        public FamilyLoader(Document document)
        {
            _document = document;
        }

        public void LoadAllFamiliesToProject()
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Loading families");
                foreach (var family in Families.AllFamilies)
                    LoadFamilyToProject(family.SymbolName);
                transaction.Commit();
            }
        }

        private bool IsFamilyInProject(string familyName)
        {
            var familiesInProject = new FilteredElementCollector(_document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(f => f.Name == familyName);

            return familiesInProject != null;
        }

        private void LoadFamilyToProject(string familyName)
        {
            var currentDirectory = GetCurrentDirectory();
            var fileName = $"{currentDirectory}\\Families\\{familyName}.rfa";
            var isInProj = IsFamilyInProject(familyName);
            var exist = File.Exists(fileName);
            if (!isInProj && exist)
            {
                _document.LoadFamily(fileName);
            }
        }

        private string GetCurrentDirectory()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var endIndex = currentDirectory.LastIndexOf('\\');
            return currentDirectory.Substring(0, endIndex);
        }
    }
}

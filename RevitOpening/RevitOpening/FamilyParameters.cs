using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public class FamilyParameters
    {
        public string SymbolName { get; private set; }

        public string InstanseName { get; private set; }

        public string DepthName { get; private set; }

        public string HeightName { get; private set; }

        public string WidthName { get; private set; }

        public string DiametrName { get; private set; }

        public FamilyParameters(string symbolName, string depthName, string instanseName,
            string heightName, string widthName, string diametrName)
        {
            SymbolName = symbolName;
            DepthName = depthName;
            InstanseName = instanseName;
            HeightName = heightName;
            WidthName = widthName;
            DiametrName = diametrName;
        }
    }
}

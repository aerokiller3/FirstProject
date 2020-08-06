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
        public string Name { get; private set; }

        public string DepthParametr { get; private set; }

        public string HeightParametr { get; private set; }

        public string WidthParametr { get; private set; }

        public string DiametrParametr { get; private set; }

        public FamilyParameters(string name, string depthParametr,string heightParametr = null, string widthParametr = null,string diametrParametr = null)
        {
            Name = name;
            DepthParametr = depthParametr;
            HeightParametr = heightParametr;
            WidthParametr = widthParametr;
            DiametrParametr = diametrParametr;
        }
    }
}

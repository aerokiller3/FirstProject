using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public class OpeningParametrs
    {
        public double Width { get; private set; }

        public double Heigth { get; private set; }

        public double Depth { get; private set; }

        public XYZ Direction { get; private set; }

        public XYZ IntersectionCenter { get; private set; }

        public OpeningParametrs(double width, double heigth, double depth, XYZ direction, XYZ intersectionCenter)
        {
            Width = width;
            Heigth = heigth;
            Depth = depth;
            Direction = direction;
            IntersectionCenter = intersectionCenter;
        }
    }
}

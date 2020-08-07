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
        public double Width { get; set; }

        public double Heigth { get; set; }

        public double Depth { get; set; }

        public MyXYZ Direction { get; set; }

        public MyXYZ IntersectionCenter { get; set; }

        public ElementGeometry WallGeometry { get; set; }

        public ElementGeometry PipeGeometry { get; set; }

        public OpeningParametrs(double width, double heigth, double depth, XYZ direction,
            XYZ intersectionCenter, ElementGeometry wallGeometry, ElementGeometry pipeGeometry)
        {
            Width = width;
            Heigth = heigth;
            Depth = depth;
            Direction = new MyXYZ(direction);
            IntersectionCenter = new MyXYZ(intersectionCenter);
            WallGeometry = wallGeometry;
            PipeGeometry = pipeGeometry;
        }

        public OpeningParametrs()
        {

        }
    }
}

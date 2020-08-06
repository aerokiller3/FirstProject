using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening
{
    public class ElementGeometry
    {
        public double YLen { get; private set; }

        public double XLen { get; private set; }

        public double ZLen { get; private set; }

        [JsonIgnore]
        public Curve Curve { get; private set; }

        public XYZ Start { get; private set; }

        public XYZ End { get; private set; }

        public ElementGeometry(Element element)
        {
            Curve = (element.Location as LocationCurve)?.Curve;
            Start = Curve.GetEndPoint(0);
            End = Curve.GetEndPoint(1);
            XLen = Start.X - End.X;
            YLen = Start.Y - End.Y;
            ZLen = Start.Z - End.Z;
        }
    }
}

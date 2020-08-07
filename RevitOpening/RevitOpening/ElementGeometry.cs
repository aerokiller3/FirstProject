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
        public double YLen { get; set; }

        public double XLen { get; set; }

        public double ZLen { get; set; }

        [JsonIgnore]
        public Curve Curve { get; set; }

        public MySolidInfo SolidInfo { get; set; }

        public MyXYZ Start { get; set; }

        public MyXYZ End { get; set; }

        public ElementGeometry(Element element)
        {
            Curve = (element.Location as LocationCurve)?.Curve;
            if (Curve != null)
            {
                Start = new MyXYZ(Curve.GetEndPoint(0));
                End = new MyXYZ(Curve.GetEndPoint(1));
                XLen = Start.X - End.X;
                YLen = Start.Y - End.Y;
                ZLen = Start.Z - End.Z;
            }
            SolidInfo = new MySolidInfo(element);
        }

        public ElementGeometry()
        {
                
        }

        public override bool Equals(object obj)
        {
            var toleranse = Math.Pow(10, -7);
            if (obj is ElementGeometry geometry)
            {
                var a = Math.Abs(geometry.XLen - XLen) < toleranse;
                var b = Math.Abs(geometry.YLen - YLen) < toleranse;
                var c = Math.Abs(geometry.ZLen - ZLen) < toleranse;
                var d = geometry.Start?.Equals(Start) ?? true;
                var e = geometry.End?.Equals(End) ?? true;
                var f = geometry.SolidInfo?.Equals(SolidInfo) ?? true;

                return a && b && c && d && e && f;
            }
            return false;
        }

        protected bool Equals(ElementGeometry geometry)
        {
            var toleranse = Math.Pow(10, -7);
            var a = Math.Abs(geometry.XLen - XLen) < toleranse;
            var b = Math.Abs(geometry.YLen - YLen) < toleranse;
            var c = Math.Abs(geometry.ZLen - ZLen) < toleranse;
            var d = geometry.Start?.Equals(Start) is true;
            var e = geometry.End?.Equals(End) is true;
            var f = geometry.SolidInfo?.Equals(SolidInfo) is true;

            return a && b && c && d && e && f;
        }
    }
}

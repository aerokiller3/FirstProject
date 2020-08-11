using System;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening.Models
{
    public class ElementGeometry
    {
        public ElementGeometry(Element element)
        {
            SolidInfo = new MySolidInfo(element);
            Curve = (element.Location as LocationCurve)?.Curve;
            if (Curve == null)
                return;

            Start = new MyXYZ(Curve.GetEndPoint(0));
            End = new MyXYZ(Curve.GetEndPoint(1));
            XLen = Start.X - End.X;
            YLen = Start.Y - End.Y;
            ZLen = Start.Z - End.Z;
        }

        public ElementGeometry()
        {
        }

        public double YLen { get; set; }

        public double XLen { get; set; }

        public double ZLen { get; set; }

        [JsonIgnore]
        public Curve Curve { get; set; }

        public MySolidInfo SolidInfo { get; set; }

        public MyXYZ Start { get; set; }

        public MyXYZ End { get; set; }

        public override bool Equals(object obj)
        {
            var toleranse = Math.Pow(10, -7);
            return obj is ElementGeometry geometry
                   && Math.Abs(geometry.XLen - XLen) < toleranse
                   && Math.Abs(geometry.YLen - YLen) < toleranse
                   && Math.Abs(geometry.ZLen - ZLen) < toleranse
                   && (geometry.Start?.Equals(Start) ?? true)
                   && (geometry.End?.Equals(End) ?? true)
                   && (geometry.SolidInfo?.Equals(SolidInfo) ?? true);
        }
    }
}
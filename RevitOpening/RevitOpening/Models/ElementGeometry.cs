namespace RevitOpening.Models
{
    using System;
    using Autodesk.Revit.DB;
    using Newtonsoft.Json;

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

        [JsonIgnore] public Curve Curve { get; set; }

        public MySolidInfo SolidInfo { get; set; }

        public MyXYZ Start { get; set; }

        public MyXYZ End { get; set; }

        public override bool Equals(object obj)
        {
            const double tolerance = 0.000_000_1;
            return obj is ElementGeometry geometry
                && Math.Abs(geometry.XLen - XLen) < tolerance
                && Math.Abs(geometry.YLen - YLen) < tolerance
                && Math.Abs(geometry.ZLen - ZLen) < tolerance
                && (geometry.Start?.Equals(Start) ?? true)
                && (geometry.End?.Equals(End) ?? true)
                && (geometry.SolidInfo?.Equals(SolidInfo) ?? true);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = YLen.GetHashCode();
                hashCode = (hashCode * 397) ^ XLen.GetHashCode();
                hashCode = (hashCode * 397) ^ ZLen.GetHashCode();
                hashCode = (hashCode * 397) ^ (SolidInfo != null ? SolidInfo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Start != null ? Start.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (End != null ? End.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
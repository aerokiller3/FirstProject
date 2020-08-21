using System;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening.Models
{
    public class MyXYZ
    {
        public MyXYZ()
        {
        }

        public MyXYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public MyXYZ(XYZ point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }


        [JsonIgnore] public XYZ XYZ => new XYZ(X, Y, Z);

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public override string ToString()
        {
            return string.Format($"X: {X} Y: {Y} Z:{Z}");
        }

        public override bool Equals(object obj)
        {
            var tolerance = Math.Pow(10, -7);
            return obj is MyXYZ point
                   && Math.Abs(point.X - X) < tolerance
                   && Math.Abs(point.Y - Y) < tolerance
                   && Math.Abs(point.Z - Z) < tolerance;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }
}
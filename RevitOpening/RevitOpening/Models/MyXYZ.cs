using System;
using Autodesk.Revit.DB;

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
            Point = point;
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public XYZ Point { get; set; }

        public override string ToString()
        {
            return string.Format($"X: {X} Y: {Y} Z:{Z}");
        }

        public override bool Equals(object obj)
        {
            var toleranse = Math.Pow(10, -7);
            return obj is MyXYZ point
                   && Math.Abs(point.X - X) < toleranse
                   && Math.Abs(point.Y - Y) < toleranse
                   && Math.Abs(point.Z - Z) < toleranse;
        }

        public XYZ GetXYZ()
        {
            return new XYZ(X, Y, Z);
        }
    }
}
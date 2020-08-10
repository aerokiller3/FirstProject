using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening
{
    public class MyXYZ
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public XYZ Point { get; set; }

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

        public override string ToString()
        {
            return string.Format($"X: {X} Y: {Y} Z:{Z}");
        }

        public static MyXYZ operator +(MyXYZ p1, MyXYZ p2)
        {
            return new MyXYZ(p1.X+p2.X,p1.Y+p2.Y,p1.Z+p2.Z);
        }

        public override bool Equals(object obj)
        {
            var toleranse = Math.Pow(10, -7);
            if (obj is MyXYZ point)
                return Math.Abs(point.X - X) < toleranse && 
                       Math.Abs(point.Y - Y) < toleranse &&
                       Math.Abs(point.Z - Z) < toleranse;

            return false;
        }
    }
}

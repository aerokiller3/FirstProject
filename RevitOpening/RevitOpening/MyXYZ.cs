using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening
{
    public class MyXYZ : XYZ
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public XYZ Point { get; set; }

        public MyXYZ()
        {
        }

        public MyXYZ(double x, double y, double z)
        : base(x,y,z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public MyXYZ(XYZ point)
        :base(point.X,point.Y,point.Z)
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

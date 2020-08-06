using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public static class Extensions
    {
        public static double GetPipeWidth(this MEPCurve pipe)
        {
            double pipeWidth;
            try
            {
                pipeWidth = pipe.Width;
            }
            catch
            {
                pipeWidth = pipe.Diameter;
            }

            return pipeWidth;
        }

        public static double GetPipeHeight(this MEPCurve pipe)
        {
            double pipeHeight;
            try
            {
                pipeHeight = pipe.Height;
            }
            catch
            {
                pipeHeight = pipe.Diameter;
            }

            return pipeHeight;
        }

        public static double GetOffsetInFoot(double offset) => offset / 304.8;

        public static double CalculateSize(double wallWidth, double angle, double ductWidth, double offset)
        {
            return wallWidth / Math.Tan(angle) + ductWidth / Math.Sin(angle) + GetOffsetInFoot(offset);
        }

        public static double SqrtOfSqrSum(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        public static double GetAcuteAngle(double angel)
        {
            return angel > Math.PI / 2
                ? Math.PI - angel
                : angel;
        }
    }
}

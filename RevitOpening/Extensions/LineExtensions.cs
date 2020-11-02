namespace RevitOpening.Extensions
{
    using System;
    using Autodesk.Revit.DB;

    internal static class LineExtensions
    {
        public static Solid CreateCylindricalSolidFromLine(this Line line, double radius = 0.1)
        {
            line = line.Fix();
            var plane = Plane.CreateByNormalAndOrigin(line.Direction, line.Origin);
            var profile = CurveLoop.Create(new Curve[]
            {
                Arc.Create(plane, radius, 0, Math.PI),
                Arc.Create(plane, radius, Math.PI, 2 * Math.PI),
            });
            return GeometryCreationUtilities.CreateExtrusionGeometry(new[] {profile}, line.Direction, line.Length);
        }

        private static Line Fix(this Line line)
        {
            return line.IsBound ? Line.CreateBound(line.GetEndPoint(0), line.GetEndPoint(1)) : line;
        }
    }
}
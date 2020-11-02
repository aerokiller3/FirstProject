namespace RevitOpening.Extensions
{
    using Autodesk.Revit.DB;

    internal static class BoundingBoxExtensions
    {
        public static Solid CreateSolid(this BoundingBoxXYZ bbox)
        {
            // corners in BBox coords
            (var minX, var minY, var minZ) = (bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
            (var maxX, var maxY, var maxZ) = (bbox.Max.X, bbox.Max.Y, bbox.Max.Z);

            var pt0 = new XYZ(minX, minY, minZ);
            var pt1 = new XYZ(maxX, minY, minZ);
            var pt2 = new XYZ(maxX, maxY, minZ);
            var pt3 = new XYZ(minX, maxY, minZ);
            //edges in BBox coords
            var edge0 = Line.CreateBound(pt0, pt1);
            var edge1 = Line.CreateBound(pt1, pt2);
            var edge2 = Line.CreateBound(pt2, pt3);
            var edge3 = Line.CreateBound(pt3, pt0);
            //create loop, still in BBox coords
            var height = maxZ - minZ;
            var baseLoop = CurveLoop.Create(new Curve[] {edge0, edge1, edge2, edge3});
            var preTransformBox =
                GeometryCreationUtilities.CreateExtrusionGeometry(new[] {baseLoop}, XYZ.BasisZ, height);
            var transformBox = SolidUtils.CreateTransformed(preTransformBox, bbox.Transform);
            return transformBox;
        }
    }
}
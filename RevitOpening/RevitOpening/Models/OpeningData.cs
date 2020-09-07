namespace RevitOpening.Models
{
    using System;
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Extensions;
    using Logic;

    public class OpeningData
    {
        public OpeningData(double width, double height, double depth, XYZ direction,
            XYZ intersectionCenter, List<ElementGeometry> hostsGeometries, List<ElementGeometry> pipesGeometries,
            string familyName, double offset, double diameter, string level)
        {
            Width = width;
            Height = height;
            Depth = depth;
            Direction = new MyXYZ(direction);
            IntersectionCenter = new MyXYZ(intersectionCenter);
            Collisions = new Collisions();
            HostsGeometries = hostsGeometries;
            PipesGeometries = pipesGeometries;
            FamilyName = familyName;
            Offset = offset;
            Diameter = diameter;
            Level = level;
        }

        public OpeningData()
        {
            Direction = new MyXYZ();
            IntersectionCenter = new MyXYZ();
            HostsGeometries = new List<ElementGeometry>();
            PipesGeometries = new List<ElementGeometry>();
            FamilyName = "";
            Collisions = new Collisions();
        }

        public string Level { get; set; }

        public double Offset { get; set; }

        public double Diameter { get; set; }

        public int Id { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Depth { get; set; }

        public MyXYZ Direction { get; set; }

        public MyXYZ IntersectionCenter { get; set; }

        public List<ElementGeometry> HostsGeometries { get; set; }

        public List<ElementGeometry> PipesGeometries { get; set; }

        public string FamilyName { get; set; }

        public Collisions Collisions { get; set; }

        public override bool Equals(object obj)
        {
            const double tolerance = 0.000_000_1;
            return obj is OpeningData parameters
                && (parameters.HostsGeometries?.AlmostEqualTo(HostsGeometries) ?? true)
                && (parameters.PipesGeometries?.AlmostEqualTo(PipesGeometries) ?? true)
                && (parameters.FamilyName?.Equals(FamilyName) ?? true)
                && Math.Abs(parameters.Height - Height) < tolerance
                && Math.Abs(parameters.Width - Width) < tolerance
                && Math.Abs(parameters.Depth - Depth) < tolerance
                && (parameters.Direction?.Equals(Direction) ?? true)
                && (parameters.IntersectionCenter?.Equals(IntersectionCenter) ?? true);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                hashCode = (hashCode * 397) ^ Depth.GetHashCode();
                hashCode = (hashCode * 397) ^ (Direction != null ? Direction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntersectionCenter != null ? IntersectionCenter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FamilyName != null ? FamilyName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
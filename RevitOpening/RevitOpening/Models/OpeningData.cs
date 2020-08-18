using System;
using RevitOpening.Logic;

namespace RevitOpening.Models
{
    public class OpeningData
    {
        public OpeningData(int? id, double width, double height, double depth, MyXYZ direction,
            MyXYZ intersectionCenter, ElementGeometry wallGeometry, ElementGeometry pipeGeometry, string familyName,
            string level)
        {
            Id = id;
            Width = width;
            Height = height;
            Depth = depth;
            Direction = direction;
            IntersectionCenter = intersectionCenter;
            WallGeometry = wallGeometry;
            PipeGeometry = pipeGeometry;
            FamilyName = familyName;
            Level = level;
            Collisions = new Collisions();
        }

        public OpeningData()
        {
        }

        public int? Id { get; set; }

        public string Level { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Depth { get; set; }

        public MyXYZ Direction { get; set; }

        public MyXYZ IntersectionCenter { get; set; }

        public ElementGeometry WallGeometry { get; set; }

        public ElementGeometry PipeGeometry { get; set; }

        public string FamilyName { get; set; }

        public Collisions Collisions { get; set; }

        public override bool Equals(object obj)
        {
            var tolerance = Math.Pow(10, -7);
            return obj is OpeningData parameters
                   && parameters.WallGeometry.Equals(WallGeometry)
                   && parameters.PipeGeometry.Equals(PipeGeometry)
                   && parameters.FamilyName.Equals(FamilyName)
                   && Math.Abs(parameters.Height - Height) < tolerance
                   && Math.Abs(parameters.Width - Width) < tolerance
                   && Math.Abs(parameters.Depth - Depth) < tolerance
                   && parameters.Direction.Equals(Direction)
                   && parameters.IntersectionCenter.Equals(IntersectionCenter);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Level != null ? Level.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                hashCode = (hashCode * 397) ^ Depth.GetHashCode();
                hashCode = (hashCode * 397) ^ (Direction != null ? Direction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntersectionCenter != null ? IntersectionCenter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (WallGeometry != null ? WallGeometry.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PipeGeometry != null ? PipeGeometry.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FamilyName != null ? FamilyName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
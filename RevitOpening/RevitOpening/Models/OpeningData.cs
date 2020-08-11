using System;

namespace RevitOpening.Models
{
    public class OpeningData
    {
        public OpeningData(int? id, double width, double heigth, double depth, MyXYZ direction,
            MyXYZ intersectionCenter, ElementGeometry wallGeometry, ElementGeometry pipeGeometry, string familyName, string level)
        {
            Id = id;
            Width = width;
            Heigth = heigth;
            Depth = depth;
            Direction = direction;
            IntersectionCenter = intersectionCenter;
            WallGeometry = wallGeometry;
            PipeGeometry = pipeGeometry;
            FamilyName = familyName;
            Level = level;
        }

        public OpeningData()
        {
        }

        public int? Id { get; set; }

        public string Level { get; set; }

        public double Width { get; set; }

        public double Heigth { get; set; }

        public double Depth { get; set; }

        public MyXYZ Direction { get; set; }

        public MyXYZ IntersectionCenter { get; set; }

        public ElementGeometry WallGeometry { get; set; }

        public ElementGeometry PipeGeometry { get; set; }

        public string FamilyName { get; set; }

        public override bool Equals(object obj)
        {
            var toleranse = Math.Pow(10, -7);
            return obj is OpeningData parametrs
                   && parametrs.WallGeometry.Equals(WallGeometry)
                   && parametrs.PipeGeometry.Equals(PipeGeometry)
                   && parametrs.FamilyName.Equals(FamilyName)
                   && Math.Abs(parametrs.Heigth - Heigth) < toleranse
                   && Math.Abs(parametrs.Width - Width) < toleranse
                   && Math.Abs(parametrs.Depth - Depth) < toleranse
                   && parametrs.Direction.Equals(Direction)
                   && parametrs.IntersectionCenter.Equals(IntersectionCenter);
        }
    }
}
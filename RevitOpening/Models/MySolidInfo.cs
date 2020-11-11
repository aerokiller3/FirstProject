namespace RevitOpening.Models
{
    using System.Linq;
    using Annotations;
    using Autodesk.Revit.DB;

    public class MySolidInfo
    {
        public MySolidInfo(Element element)
        {
            var solid = element.get_Geometry(new Options())
                               .FirstOrDefault() as Solid;
            var boundingBox = element
                             .get_Geometry(new Options())
                             .GetBoundingBox();
            Min = new MyXYZ(boundingBox.Min);
            Max = new MyXYZ(boundingBox.Max);
            FacesCount = solid.Faces.Size;
            EdgesCount = solid.Edges.Size;
        }

        /// <summary>
        /// Пустой конструктор для json.
        /// </summary>
        public MySolidInfo()
        {
        }

        public int FacesCount { get; set; }

        public int EdgesCount { get; set; }

        public MyXYZ Min { get; set; }

        public MyXYZ Max { get; set; }

        public override bool Equals(object obj)
        {
            return obj is MySolidInfo info
                && info.Min.Equals(Min)
                && info.Max.Equals(Max)
                && info.FacesCount.Equals(FacesCount)
                && info.EdgesCount.Equals(EdgesCount);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FacesCount;
                hashCode = (hashCode * 397) ^ EdgesCount;
                hashCode = (hashCode * 397) ^ (Min != null ? Min.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Max != null ? Max.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
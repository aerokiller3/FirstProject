using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening.Models
{
    public class MySolidInfo
    {
        public MySolidInfo(Element element)
        {
            Solid = element.get_Geometry(new Options())
                .FirstOrDefault() as Solid;
            var geometry = element
                .get_Geometry(new Options())
                .GetBoundingBox();
            Min = new MyXYZ(geometry.Min);
            Max = new MyXYZ(geometry.Max);
            FacesCount = Solid.Faces.Size;
            EdgesCount = Solid.Faces.Size;
        }

        public MySolidInfo()
        {
        }

        [JsonIgnore]
        public Solid Solid { get; set; }

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
    }
}
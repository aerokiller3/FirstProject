using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening
{
    public class MySolidInfo
    {
        [JsonIgnore]
        public Solid Solid { get; set; }

        public int FacesCount { get; set; }

        public int EdgesCount { get; set; }

        public MyXYZ Min { get; set; }

        public MyXYZ Max { get; set; }


        public MySolidInfo(Element element)
        {
            Solid = element.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var geometry = element.get_Geometry(new Options()).GetBoundingBox();
            Min = new MyXYZ(geometry.Min);
            Max = new MyXYZ(geometry.Max);
            FacesCount = Solid.Faces.Size;
            EdgesCount = Solid.Faces.Size;
        }

        public MySolidInfo()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is MySolidInfo info)
            {
                var toleranse = Math.Pow(10, -7);
                var a = info.Min.Equals(Min);
                var b = info.Max.Equals(Max);
                var c = info.FacesCount.Equals(FacesCount);
                var d = info.EdgesCount.Equals(EdgesCount);

                return a && b && c && d;
            }
                return false;
        }

        protected bool Equals(MySolidInfo info)
        {
            var toleranse = Math.Pow(10, -7);
            var a = info.Min.Equals(Min);
            var b = info.Max.Equals(Max);
            var c = info.FacesCount.Equals(FacesCount);
            var d = info.EdgesCount.Equals(EdgesCount);

            return a && b && c && d;
        }
    }
}

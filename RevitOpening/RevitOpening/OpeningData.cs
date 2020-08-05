using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    class OpeningData
    {
    internal int WallId { get; set; }
    internal int PipeId { get; set; }

    //var e = new OpeningData {PipeId = 1, WallId = 1};
    //var json = JsonConvert.SerializeObject(e);
    //pipe.LookupParameter("MyInfo").Set(json);
    }
}

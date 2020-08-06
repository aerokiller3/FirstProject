using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening
{
    public class OpeningParentsData
    {
        public OpeningParentsData(int wallId, int pipeId, ElementGeometry wallData, ElementGeometry pipeData)
        {
            WallId = wallId;
            PipeId = pipeId;
            WallData = wallData;
            PipeData = pipeData;
        }

        public int WallId { get; set; }

        public int PipeId { get; set; }

        public ElementGeometry WallData { get; set; }

        public ElementGeometry PipeData { get; set; }
    }
}

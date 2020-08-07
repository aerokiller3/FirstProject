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
        public OpeningParentsData(int wallId, int pipeId, ElementGeometry hostData, ElementGeometry pipeData, Type hostType, Type pipeType)
        {
            WallId = wallId;
            PipeId = pipeId;
            HostData = hostData;
            PipeData = pipeData;
            HostType = hostType;
            PipeType = pipeType;
        }

        public int WallId { get; set; }

        public int PipeId { get; set; }

        public ElementGeometry HostData { get; set; }

        public Type HostType { get; set; }

        public ElementGeometry PipeData { get; set; }

        public Type PipeType { get; set; }

        public XYZ LocationPoint { get; set; }
    }
}

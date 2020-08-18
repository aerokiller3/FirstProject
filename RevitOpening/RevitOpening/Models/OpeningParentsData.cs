using System;

namespace RevitOpening.Models
{
    public class OpeningParentsData
    {
        public OpeningParentsData(int hostId, int pipeId, Type hostType, Type pipeType, OpeningData boxData)
        {
            HostId = hostId;
            PipeId = pipeId;
            HostType = hostType;
            PipeType = pipeType;
            BoxData = boxData;
        }

        public int HostId { get; set; }

        public int PipeId { get; set; }

        public Type HostType { get; set; }

        public Type PipeType { get; set; }

        public MyXYZ LocationPoint { get; set; }

        public OpeningData BoxData { get; set; }
    }
}
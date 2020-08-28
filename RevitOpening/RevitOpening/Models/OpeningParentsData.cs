using RevitOpening.Extensions;
using System.Collections.Generic;

namespace RevitOpening.Models
{
    public class OpeningParentsData
    {
        public OpeningParentsData()
        {
            HostsIds = new List<string>();
            PipesIds = new List<string>();
            BoxData = new OpeningData();
        }

        public OpeningParentsData(List<string> hostsIds, List<string> pipesIds, OpeningData boxData)
        {
            HostsIds = hostsIds;
            PipesIds = pipesIds;
            BoxData = boxData;
        }

        public List<string> HostsIds { get; set; }

        public List<string> PipesIds { get; set; }

        public OpeningData BoxData { get; set; }

        public override bool Equals(object obj)
        {
            return obj is OpeningParentsData data
                   && (HostsIds?.AlmostEqualTo(data.HostsIds) ?? true)
                   && (PipesIds?.AlmostEqualTo(data.PipesIds) ?? true)
                   && (BoxData?.Equals(data.BoxData) ?? true);
        }

        public override int GetHashCode()
        {
            return BoxData != null ? BoxData.GetHashCode() : 0;
        }
    }
}
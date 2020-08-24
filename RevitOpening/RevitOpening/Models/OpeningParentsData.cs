using System.Collections.Generic;
using RevitOpening.Extensions;

namespace RevitOpening.Models
{
    public class OpeningParentsData
    {
        public OpeningParentsData()
        {
        }

        public OpeningParentsData(List<int> hostsIds, List<int> pipesIds, OpeningData boxData)
        {
            HostsIds = hostsIds;
            PipesIds = pipesIds;
            BoxData = boxData;
        }

        public List<int> HostsIds { get; set; }

        public List<int> PipesIds { get; set; }

        public OpeningData BoxData { get; set; }

        public override bool Equals(object obj)
        {
            return obj is OpeningParentsData data
                   && HostsIds.AlmostEqualTo(data.HostsIds)
                   && PipesIds.AlmostEqualTo(data.PipesIds)
                   && BoxData.Equals(data.BoxData);
        }

        public override int GetHashCode()
        {
            return BoxData != null ? BoxData.GetHashCode() : 0;
        }
    }
}
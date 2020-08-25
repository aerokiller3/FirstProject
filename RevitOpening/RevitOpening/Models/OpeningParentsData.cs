using System.Collections.Generic;
using RevitOpening.Extensions;

namespace RevitOpening.Models
{
    public class OpeningParentsData
    {
        public OpeningParentsData()
        {
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
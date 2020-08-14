namespace RevitOpening.Models
{
    public class FamilyParameters
    {
        public FamilyParameters(string symbolName, string depthName, string instanceName,
            string heightName, string widthName, string diameterName)
        {
            SymbolName = symbolName;
            DepthName = depthName;
            InstanceName = instanceName;
            HeightName = heightName;
            WidthName = widthName;
            DiameterName = diameterName;
        }

        public string SymbolName { get; }

        public string InstanceName { get; }

        public string DepthName { get; }

        public string HeightName { get; }

        public string WidthName { get; }

        public string DiameterName { get; }
    }
}
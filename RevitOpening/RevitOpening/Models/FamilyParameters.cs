namespace RevitOpening.Models
{
    public class FamilyParameters
    {
        public FamilyParameters(string symbolName, string depthName, string instanseName,
            string heightName, string widthName, string diametrName)
        {
            SymbolName = symbolName;
            DepthName = depthName;
            InstanseName = instanseName;
            HeightName = heightName;
            WidthName = widthName;
            DiametrName = diametrName;
        }

        public string SymbolName { get; }

        public string InstanseName { get; }

        public string DepthName { get; }

        public string HeightName { get; }

        public string WidthName { get; }

        public string DiametrName { get; }
    }
}
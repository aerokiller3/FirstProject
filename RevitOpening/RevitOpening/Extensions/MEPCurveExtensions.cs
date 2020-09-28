namespace RevitOpening.Extensions
{
    using System.Linq;
    using Autodesk.Revit.DB;

    internal static class MEPCurveExtensions
    {
        public static bool IsRoundPipe(this MEPCurve pipe)
        {
            var connector = pipe.ConnectorManager.Connectors
                                .Cast<Connector>()
                                .FirstOrDefault();
            if (connector != null && connector.Shape != ConnectorProfileType.Invalid)
                return connector.Shape == ConnectorProfileType.Oval || connector.Shape == ConnectorProfileType.Round;

            try
            {
                var width = pipe.Width;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static double GetPipeWidth(this MEPCurve pipe)
        {
            return pipe.IsRoundPipe() ? pipe.Diameter : pipe.Width;
        }

        public static double GetPipeHeight(this MEPCurve pipe)
        {
            return pipe.IsRoundPipe() ? pipe.Diameter : pipe.Height;
        }
    }
}
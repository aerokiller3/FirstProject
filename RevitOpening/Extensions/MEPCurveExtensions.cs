namespace RevitOpening.Extensions
{
    using Autodesk.Revit.DB;

    internal static class MEPCurveExtensions
    {
        //TODO: заменить try catch на что-то другое
        public static bool IsRoundPipe(this MEPCurve pipe)
        {
            bool isRound;
            try
            {
                var width = pipe.Width;
                isRound = false;
            }
            catch
            {
                isRound = true;
            }

            return isRound;
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
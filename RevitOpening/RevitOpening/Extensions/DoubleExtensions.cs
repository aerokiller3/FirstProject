namespace RevitOpening.Extensions
{
    internal static class DoubleExtensions
    {
        public static double GetInFoot(this double number)
        {
            return number / 304.8;
        }
    }
}
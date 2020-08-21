namespace RevitOpening.Extensions
{
    public static class DoubleExtensions
    {
        public static double GetInFoot(this double number)
        {
            return number / 304.8;
        }
    }
}
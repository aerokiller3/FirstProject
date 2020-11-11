namespace RevitOpening.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    internal static class CollectionExtensions
    {
        public static bool AlmostEqualTo<T>(this ICollection<T> thisList, ICollection<T> otherList)
        {
            return thisList.Count == otherList.Count
                && thisList.All(otherList.Contains);
        }
    }
}
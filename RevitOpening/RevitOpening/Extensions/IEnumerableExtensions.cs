using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitOpening.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsOnlyTasks(this IEnumerable<Element> tasks)
        {
            return tasks.All(t => t.IsTask());
        }

        public static bool AlmostEqualTo<T>(this IEnumerable<T> thisList,
            IEnumerable<T> otherList)
        {
            return thisList.Count() == otherList.Count()
                   && thisList.All(otherList.Contains);
        }

        public static XYZ GetMaxPointsCoordinates(this IEnumerable<XYZ> tPoints)
        {
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var maxZ = double.MinValue;
            foreach (var point in tPoints)
            {
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }

            return new XYZ(maxX, maxY, maxZ);
        }

        public static XYZ GetMinPointsCoordinates(this IEnumerable<XYZ> tPoints)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var minZ = double.MaxValue;
            foreach (var point in tPoints)
            {
                minX = Math.Min(point.X, minX);
                minY = Math.Min(point.Y, minY);
                minZ = Math.Min(point.Z, minZ);
            }

            return new XYZ(minX, minY, minZ);
        }

        public static Dictionary<Element, List<MEPCurve>> FindIntersectionsWith(this IEnumerable<Element> elements,
            IEnumerable<MEPCurve> curves)
        {
            var intersections = new Dictionary<Element, List<MEPCurve>>();
            foreach (var intersectionElement in elements)
            {
                var intersection = new ElementIntersectsElementFilter(intersectionElement);
                var currentIntersections = curves
                    .Where(el => intersection.PassesFilter(el))
                    .ToList();
                if (currentIntersections.Count > 0)
                    intersections[intersectionElement] = currentIntersections;
            }

            return intersections;
        }
    }
}
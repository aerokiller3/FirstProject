namespace RevitOpening.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autodesk.Revit.DB;

    internal static class EnumerableExtensions
    {
        public static bool IsOnlyTasks(this IEnumerable<Element> tasks)
        {
            return tasks.All(t => t.IsTask());
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

        public static IDictionary<Element, List<MEPCurve>> FindIntersectionsWith(this IEnumerable<Element> elements,
            ICollection<MEPCurve> curves)
        {
            var intersections = new ConcurrentDictionary<Element, List<MEPCurve>>();
            Task.WaitAll(elements
                        .Select(intersectionElement => Task.Run(() =>
                         {
                             using (var intersection = new ElementIntersectsElementFilter(intersectionElement))
                             {
                                 var currentIntersections = curves
                                                           .Where(intersection.PassesFilter)
                                                           .ToList();
                                 if (currentIntersections.Count > 0) intersections[intersectionElement] = currentIntersections;
                             }
                         }))
                        .ToArray());
            return intersections;
        }
    }
}
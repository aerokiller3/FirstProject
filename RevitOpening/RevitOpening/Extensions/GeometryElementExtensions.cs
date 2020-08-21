using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitOpening.Extensions
{
    public static class GeometryElementExtensions
    {
        public static IEnumerable<Solid> GetAllSolids(this GeometryElement geometry)
        {
            foreach (var gObject in geometry)
            {
                if (gObject is Solid solid)
                    yield return solid;

                GeometryElement geometryElement = default;
                switch (gObject)
                {
                    case GeometryInstance geometryInstance:
                        geometryElement = geometryInstance.GetInstanceGeometry();
                        break;
                    case GeometryElement element:
                        geometryElement = element;
                        break;
                }

                if (geometryElement != default)
                    foreach (var deepSolid in geometryElement.GetAllSolids())
                        yield return deepSolid;
            }
        }
    }
}
using System;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class ElementExtensions
    {
        public static OpeningParentsData GetParentsData(this Element element)
        {
            var json = AltecJsonSchema.GetJson(element);
            return JsonConvert.DeserializeObject<OpeningParentsData>(json);
        }

        public static void SetParentsData(this Element element, OpeningParentsData parentsData)
        {
            var json = JsonConvert.SerializeObject(parentsData);
            AltecJsonSchema.SetJson(element, json);
        }

        public static Solid GetSolid(this Element task)
        {
            var data = task.GetParentsData();
            Solid solid;
            if (data.BoxData.FamilyName == Families.WallRoundTaskFamily.SymbolName)
                solid = task.get_BoundingBox(null).CreateSolid();
            else
                solid = task.get_Geometry(new Options())
                    .GetAllSolids()
                    .FirstOrDefault(s => Math.Abs(s.Volume) > 0.0000001);
            return solid;
        }

        public static Solid GetUnitedSolid(this Element task, Element otherTask, Transform transform, XYZ tolerance = null)
        {
            tolerance = tolerance ?? XYZ.Zero;

            var tasks = new [] {task, otherTask};
            var solids = tasks
                .Where(el => el != null)
                .Select(el => el.GetSolid());

            var backTransform = transform.Inverse;
            var transformPoints = solids
                .SelectMany(x => x.Edges.Cast<Edge>())
                .Select(y => y.AsCurve().GetEndPoint(0))
                .Select(transform.OfPoint);

            var bbox = new BoundingBoxXYZ
            {
                Min = transformPoints.GetMinPointsCoordinates() - tolerance,
                Max = transformPoints.GetMaxPointsCoordinates() + tolerance
            };
            var solid = bbox.CreateSolid();
            var backSolid = SolidUtils.CreateTransformed(solid, backTransform);
            return backSolid;
        }
    }
}
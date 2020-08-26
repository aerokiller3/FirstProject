using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class ElementExtensions
    {
        public static OpeningParentsData InitData(this FamilyInstance task, List<Wall> walls, List<CeilingAndFloor> floors,
            double offset, double maxDiameter, List<MEPCurve> mepCurves)
        {
            var filter = new ElementIntersectsElementFilter(task);
            var intersectsWalls = walls.Where(filter.PassesFilter).ToList();
            var intersectsFloors = floors.Where(filter.PassesFilter).ToList();
            var hosts = new List<Element>();
            hosts.AddRange(intersectsFloors);
            hosts.AddRange(intersectsWalls);
            var parentsData = new OpeningParentsData(hosts.Select(h => h.UniqueId).ToList(), mepCurves.Select(c => c.UniqueId).ToList(), null);
            if (hosts.Count == 0 || mepCurves.Count == 0)
                return null;

            var openingParameters =
                BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(), mepCurves.FirstOrDefault(), offset, maxDiameter);
            if (openingParameters == null)
                return null;

            parentsData.BoxData = openingParameters;
            parentsData.BoxData.Id = task.Id.IntegerValue;
            task.SetParentsData(parentsData);

            return parentsData;
        }


        public static OpeningParentsData GetOrInitData(this FamilyInstance task, List<Wall> walls,
            List<CeilingAndFloor> floors, double offset, double maxDiameter, List<MEPCurve> mepCurves)
        {
            OpeningParentsData data;
            try
            {
                data = task.GetParentsData();
            }
            catch
            {
                data = task.InitData(walls, floors, offset, maxDiameter, mepCurves);
            }

            return data;
        }

        public static bool IsTask(this Element element)
        {
            return Families.AllFamiliesNames.Contains(((FamilyInstance) element).Symbol.FamilyName);
        }

        public static OpeningParentsData GetParentsData(this Element element)
        {
            var json = AltecJsonSchema.GetJson(element);
            if (json == null)
                throw new ArgumentNullException("Обновите информацию о заданиях перед использованием");

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
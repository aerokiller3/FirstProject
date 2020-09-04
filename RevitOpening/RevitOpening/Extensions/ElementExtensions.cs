using Autodesk.Revit.DB;
using Newtonsoft.Json;
using RevitOpening.Logic;
using RevitOpening.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
            var intersectsMEPCurves = mepCurves.Where(filter.PassesFilter).ToList();
            var hosts = new List<Element>();
            hosts.AddRange(intersectsFloors);
            hosts.AddRange(intersectsWalls);

            var parentsData = new OpeningParentsData(
                hosts.Select(h => h.UniqueId).ToList(),
                intersectsMEPCurves.Select(c => c.UniqueId).ToList(),
                null);

            if (hosts.Count == 0 || mepCurves.Count == 0)
            {
                task.SetParentsData(parentsData);
                return parentsData;
            }

            var openingParameters =
                BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(), mepCurves.FirstOrDefault(), offset, maxDiameter);

            if (openingParameters == null)
            {
                task.SetParentsData(parentsData);
                return parentsData;
            }

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
                if (data.BoxData.HostsGeometries.Count == 0 || data.BoxData.PipesGeometries.Count == 0)
                    data = task.InitData(walls, floors, offset, maxDiameter, mepCurves);
            }
            catch (ArgumentNullException)
            {
                data = task.InitData(walls, floors, offset, maxDiameter, mepCurves);
            }

            return data;
        }

        public static bool IsTask(this Element element)
        {
            return Families.AllFamiliesNames.Contains(((FamilyInstance)element).Symbol.FamilyName);
        }

        public static OpeningParentsData GetParentsData(this Element element)
        {
            var json = AltecJsonSchema.GetJson(element);
            if (json != null)
                return JsonConvert.DeserializeObject<OpeningParentsData>(json);

            throw new ArgumentNullException("Обновить информацию о заданиях перед использованием");
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

            var tasks = new[] { task, otherTask };
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
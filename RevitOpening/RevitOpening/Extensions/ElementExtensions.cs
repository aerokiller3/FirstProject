namespace RevitOpening.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Logic;
    using Models;
    using Newtonsoft.Json;

    internal static class ElementExtensions
    {
        public static OpeningParentsData InitData(this Element task, IEnumerable<Wall> walls,
            IEnumerable<CeilingAndFloor> floors, double offset, double maxDiameter,
            IEnumerable<MEPCurve> mepCurves, Document currentDocument)
        {
            var filter = new ElementIntersectsElementFilter(task);
            var intersectsWalls = walls
               .Where(filter.PassesFilter);
            var intersectsFloors = floors
               .Where(filter.PassesFilter);
            var intersectsMepCurves = mepCurves
                                     .Where(filter.PassesFilter)
                                     .ToList();

            var hosts = new List<Element>();
            hosts.AddRange(intersectsFloors);
            hosts.AddRange(intersectsWalls);

            var parentsData = new OpeningParentsData(
                hosts
                   .Select(h => h.UniqueId)
                   .ToList(),
                intersectsMepCurves
                   .Select(c => c.UniqueId)
                   .ToList(),
                null);

            if (intersectsMepCurves.Count != 1 || hosts.Count != 1)
            {
                parentsData.BoxData = new OpeningData();
                parentsData.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);
                goto SetData;
            }

            parentsData.BoxData = BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(),
                intersectsMepCurves.FirstOrDefault(), offset, maxDiameter);
            SetData:
            parentsData.BoxData.Id = task.Id.IntegerValue;
            parentsData.BoxData.FamilyName = ((FamilyInstance) task).Symbol.FamilyName;
            parentsData.BoxData.Level = currentDocument
                                       .GetElement(currentDocument
                                                  .GetElement(parentsData.HostsIds
                                                                         .FirstOrDefault()).LevelId).Name;
            task.SetParentsData(parentsData);
            filter.Dispose();
            return parentsData;
        }


        public static OpeningParentsData GetOrInitData(this Element task, List<Wall> walls,
            List<CeilingAndFloor> floors, double offset, double maxDiameter, List<MEPCurve> mepCurves,
            Document currentDocument)
        {
            return task.GetParentsData() ?? task.InitData(walls, floors, offset, maxDiameter,
                mepCurves, currentDocument);
        }

        public static bool IsTask(this Element element)
        {
            var elSymbolName = (((FamilyInstance) element).Symbol.FamilyName);
            return Families.FloorRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRoundTaskFamily.SymbolName == elSymbolName;
        }

        public static OpeningParentsData GetParentsData(this Element element)
        {
            var json = AltecJsonSchema.GetJson(element);
            return json != null ? JsonConvert.DeserializeObject<OpeningParentsData>(json) : null;
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

        public static Solid GetUnitedSolid(this Element task, Element otherTask, Transform transform,
            XYZ tolerance = null)
        {
            tolerance = tolerance ?? XYZ.Zero;

            var tasks = new[] {task, otherTask};
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
                Max = transformPoints.GetMaxPointsCoordinates() + tolerance,
            };
            var solid = bbox.CreateSolid();
            var backSolid = SolidUtils.CreateTransformed(solid, backTransform);
            return backSolid;
        }
    }
}
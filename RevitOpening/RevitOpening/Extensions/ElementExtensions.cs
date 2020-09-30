namespace RevitOpening.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Autodesk.Revit.DB;
    using Logic;
    using Models;
    using Newtonsoft.Json;

    internal static class ElementExtensions
    {
        public static OpeningParentsData GetParentsDataFromParameters(this Element element,
            IEnumerable<Wall> walls, IEnumerable<CeilingAndFloor> floors, double offset,
            double maxDiameter, IEnumerable<MEPCurve> mepCurves, ICollection<Document> documents)
        {
            var filter = new ElementIntersectsElementFilter(element);
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
            filter.Dispose();
            OpeningData parameters;
            if (intersectsMepCurves.Count != 1 || hosts.Count != 1)
            {
                parameters = new OpeningData();
                parameters.Collisions.MarkUnSupported();
            }
            else
            {
                parameters = BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(),
                    intersectsMepCurves.FirstOrDefault(), offset, maxDiameter);
            }

            if (parameters == null)
                return new OpeningParentsData
                {
                    HostsIds = hosts
                              .Select(h => h.UniqueId)
                              .ToList(),
                    PipesIds = intersectsMepCurves
                              .Select(c => c.UniqueId)
                              .ToList(),
                };

            parameters.Id = element.Id.IntegerValue;
            parameters.FamilyName = ((FamilyInstance) element).Symbol.FamilyName;
            // TASK
            parameters.Level = hosts.FirstOrDefault()?.GetLevelName(documents);
            // OR NOT
            parameters.HostsGeometries = hosts
                                        .Select(h => new ElementGeometry(h))
                                        .ToList();
            parameters.PipesGeometries = intersectsMepCurves
                                        .Select(p => new ElementGeometry(p))
                                        .ToList();
            var familyParameters = Families.GetDataFromSymbolName(parameters.FamilyName);
            double width, height;
            if (familyParameters == Families.FloorRectTaskFamily)
            {
                width = element.LookupParameter(familyParameters.HeightName).AsDouble();
                height = element.LookupParameter(familyParameters.WidthName).AsDouble();
            }
            else if (familyParameters == Families.WallRectTaskFamily)
            {
                height = element.LookupParameter(familyParameters.HeightName).AsDouble();
                width = element.LookupParameter(familyParameters.WidthName).AsDouble();
            }
            else if (familyParameters == Families.WallRoundTaskFamily)
            {
                width = height = element.LookupParameter(familyParameters.DiameterName).AsDouble();
            }
            else
            {
                throw new ArgumentException("Неизвестный экземпляр семейства");
            }

            var depth = element.LookupParameter(familyParameters.DepthName).AsDouble();

            parameters.Depth = depth;
            parameters.Width = width;
            parameters.Height = height;
            parameters.IntersectionCenter = new MyXYZ(((LocationPoint) element.Location).Point);
            return new OpeningParentsData(
                hosts
                   .Select(h => h.UniqueId)
                   .ToList(),
                intersectsMepCurves
                   .Select(c => c.UniqueId)
                   .ToList(),
                parameters);
        }

        public static string GetLevelName(this Element element, ICollection<Document> documents)
        {
            return documents.GetElement(element.LevelId.IntegerValue).Name;
        }

        public static OpeningParentsData GetOrInitData(this Element task, List<Wall> walls,
            List<CeilingAndFloor> floors, double offset, double maxDiameter, List<MEPCurve> mepCurves
            , ICollection<Document> documents)
        {
            var data = task.GetParentsDataFromSchema();
            if (data == null)
            {
                data = task.GetParentsDataFromParameters(walls, floors, offset,
                    maxDiameter, mepCurves, documents);
                task.SetParentsData(data);
            }

            return data;
        }

        public static bool IsTask(this Element element)
        {
            var elSymbolName = (((FamilyInstance) element).Symbol.FamilyName);
            return Families.FloorRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRoundTaskFamily.SymbolName == elSymbolName;
        }

        public static bool IsOpening(this Element element)
        {
            var elSymbolName = ((FamilyInstance) element).Symbol.FamilyName;
            return Families.FloorRectOpeningFamily.SymbolName == elSymbolName
                || Families.WallRectOpeningFamily.SymbolName == elSymbolName
                || Families.WallRoundOpeningFamily.SymbolName == elSymbolName;
        }

        public static OpeningParentsData GetParentsDataFromSchema(this Element element)
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
            var data = task.GetParentsDataFromSchema();
            Solid solid;
            if (data != null && data.BoxData.FamilyName == Families.WallRoundTaskFamily.SymbolName)
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
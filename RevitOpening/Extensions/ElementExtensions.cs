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
            IEnumerable<MEPCurve> mepCurves, Document currentDocument, ICollection<Document> documents)
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
                task.CalculateOldBoxParameters(intersectsMepCurves, hosts, offset, maxDiameter,
                    currentDocument, documents));

            task.SetParentsData(parentsData);
            filter.Dispose();
            return parentsData;
        }

        public static OpeningData CalculateOldBoxParameters(this Element element, ICollection<MEPCurve> pipes,
            ICollection<Element> hosts, double offset, double maxDiameter, Document currentDocument,
            ICollection<Document> documents)
        {
            OpeningData parameters;
            if (pipes.Count != 1 || hosts.Count != 1)
            {
                parameters = new OpeningData();
                parameters.Collisions.Add(Collisions.TaskCouldNotBeProcessed);
            }
            else
            {
                parameters = BoxCalculator.CalculateBoxInElement(hosts.FirstOrDefault(),
                    pipes.FirstOrDefault(), offset, maxDiameter);
            }

            parameters.Id = element.Id.IntegerValue;
            parameters.FamilyName = ((FamilyInstance) element).Symbol.FamilyName;
            parameters.Level = hosts.FirstOrDefault()?.GetLevelName(documents);
            parameters.HostsGeometries = hosts
                                        .Select(h => new ElementGeometry(h))
                                        .ToList();
            parameters.PipesGeometries = pipes
                                        .Select(p => new ElementGeometry(p))
                                        .ToList();
            var familyParameters = Families.GetDataFromSymbolName(parameters.FamilyName);
            double width, height;
            if (familyParameters == Families.FloorRectTaskFamily)
            {
                width = element.LookupParameter(familyParameters.HeightName).AsDouble();
                height = element.LookupParameter(familyParameters.WidthName).AsDouble();
            }
            else if (familyParameters == Families.WallRectTaskFamily || familyParameters == Families.WallElipticalTaskFamily)
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
                return parameters;
            }

            var depth = element.LookupParameter(familyParameters.DepthName).AsDouble();
            parameters.Depth = depth;
            parameters.Width = width;
            parameters.Height = height;
            parameters.IntersectionCenter = new MyXYZ(((LocationPoint) element.Location).Point);

            return parameters;
        }

        public static string GetLevelName(this Element element, ICollection<Document> documents)
        {
            return documents.GetElement(element.LevelId.IntegerValue).Name;
        }

        public static OpeningParentsData GetOrInitData(this Element task, List<Wall> walls,
            List<CeilingAndFloor> floors, double offset, double maxDiameter, List<MEPCurve> mepCurves,
            Document currentDocument, ICollection<Document> documents)
        {
            return task.GetParentsData() ?? task.InitData(walls, floors, offset, maxDiameter,
                mepCurves, currentDocument, documents);
        }

        public static bool IsTask(this Element element)
        {
            // Добавил овальные трубы
            var elSymbolName = (((FamilyInstance) element).Symbol.FamilyName);
            return Families.FloorRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRectTaskFamily.SymbolName == elSymbolName
                || Families.WallRoundTaskFamily.SymbolName == elSymbolName
                || Families.WallElipticalTaskFamily.SymbolName == elSymbolName;
        }

        public static bool IsOpening(this Element element)
        {
            // Добавил овальные трубы
            var elSymbolName = (((FamilyInstance) element).Symbol.FamilyName);
            return Families.FloorRectOpeningFamily.SymbolName == elSymbolName
                || Families.WallRectOpeningFamily.SymbolName == elSymbolName
                || Families.WallRoundOpeningFamily.SymbolName == elSymbolName
                || Families.WallElipticalOpeningFamily.SymbolName == elSymbolName;
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
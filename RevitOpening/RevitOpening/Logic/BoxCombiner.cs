namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Extensions;
    using Models;
    using Settings = Extensions.Settings;

    internal static class BoxCombiner
    {
        //?
        public static bool ValidateTasksForCombine(OpeningParentsData data1, OpeningParentsData data2, Element el1,
            Element el2)
        {
            return (data1.PipesIds.AlmostEqualTo(data2.PipesIds) || data1.HostsIds.AlmostEqualTo(data2.HostsIds))
                && data1.BoxData.FamilyName == data2.BoxData.FamilyName
                && !data1.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed)
                && !data2.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed)
                && el1.IsTask() && el2.IsTask();
        }

        public static void CombineAllBoxes(ICollection<Document> documents, Document currentDocument, bool isTangent)
        {
            var isElementsUnited = true;
            while (isElementsUnited)
            {
                isElementsUnited = false;
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.WallRectTaskFamily, isTangent);
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.FloorRectTaskFamily, isTangent);
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.WallRoundTaskFamily, isTangent);
                currentDocument.Regenerate();
            }
        }

        private static bool CombineOneTypeBoxes(ICollection<Document> documents, Document currentDocument,
            FamilyParameters familyData, bool isTangent)
        {
            var tasks = currentDocument
                       .GetTasksByName(familyData)
                       .ToList();
            var intersections = FindTaskIntersections(tasks, isTangent).ToList();
            for (var i = 0; i < intersections.Count; i++)
                if (CombineTwoBoxes(documents, currentDocument, intersections[i].Item1, intersections[i].Item2) == null)
                {
                    intersections.RemoveAt(i);
                    i--;
                }

            return intersections.Count > 0;
        }

        public static FamilyInstance CombineTwoBoxes(ICollection<Document> documents, Document currentDocument,
            Element el1, Element el2)
        {
            var data1 = el1.GetParentsData();
            var data2 = el2.GetParentsData();
            if (!ValidateTasksForCombine(data1, data2, el1, el2))
                return null;

            OpeningData newOpening = null;
            if (data1.PipesIds.AlmostEqualTo(data2.PipesIds))
                newOpening = CalculateUnitedTaskOnOnePipe(data1, data2);
            else if (documents.GetElement(data1.HostsIds.FirstOrDefault()) is Wall)
                newOpening = data1.BoxData.FamilyName == Families.WallRoundTaskFamily.SymbolName
                    ? CalculateUnitedTaskInWallWithRounds(data1, data2)
                    : CalculateUnitedTaskInWallWithRects(el1, el2, data1, data2);
            else if (documents.GetElement(data1.HostsIds.FirstOrDefault()) is CeilingAndFloor)
                newOpening = CalculateUnitedTaskInFloor(el1, el2, data1, data2);

            if (newOpening == null)
                return null;

            var newData = new OpeningParentsData(data1.HostsIds.Union(data2.HostsIds).ToList(),
                data1.PipesIds.Union(data2.PipesIds).ToList(), newOpening);
            newData.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);

            var createdElement = BoxCreator.CreateTaskBox(newData, currentDocument);
            currentDocument.Delete(el1.Id);
            currentDocument.Delete(el2.Id);

            return createdElement;
        }

        private static IEnumerable<(Element, Element)> FindTaskIntersections(IList<FamilyInstance> elements, bool isTangent)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                var data = elements[i].GetParentsData();
                var tolerance = new XYZ(0.01, 0.01, 0.01);
                var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
                var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
                var solidWithTolerance = elements[i].GetUnitedSolid(null, transform, tolerance);
                var solid = elements[i].GetUnitedSolid(null, transform);
                var filter = new ElementIntersectsSolidFilter(solid);
                var filterWithTolerance = new ElementIntersectsSolidFilter(solidWithTolerance);
                for (var j = i + 1; j < elements.Count; j++)
                {
                    if (elements[i].Id == elements[j].Id)
                        continue;

                    var tangent = filterWithTolerance.PassesFilter(elements[j]);
                    var intersect = filter.PassesFilter(elements[j]);
                    if (isTangent && tangent && !intersect)
                        yield return (elements[i], elements[j]);
                    else if (!isTangent && intersect)
                        yield return (elements[i], elements[j]);
                    else
                        continue;
                    elements.RemoveAt(j);
                    elements.RemoveAt(i);
                    i -= 1;
                    break;
                }
            }
        }

        private static OpeningData CalculateUnitedTaskOnOnePipe(OpeningParentsData data1, OpeningParentsData data2)
        {
            var direction = data1.BoxData.IntersectionCenter.XYZ - data2.BoxData.IntersectionCenter.XYZ;
            var depth = data1.BoxData.Depth + data2.BoxData.Depth;
            var normalDirection = direction.Normalize();
            var source = data1.BoxData.Direction.XYZ;
            source = Transform.CreateRotation(-XYZ.BasisZ, Math.PI / 2).OfVector(source);
            var center = normalDirection.IsAlmostEqualTo(source)
                ? data1.BoxData.IntersectionCenter.XYZ
                : data2.BoxData.IntersectionCenter.XYZ;
            (var hostsGeometries, var pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                data1.BoxData.Width, data1.BoxData.Height, depth,
                data1.BoxData.Direction.XYZ, center, hostsGeometries,
                pipesGeometries, data1.BoxData.FamilyName, data1.BoxData.Offset,
                data1.BoxData.Diameter, data1.BoxData.Level);
        }

        private static OpeningData CalculateUnitedTaskInFloor(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var angle = XYZ.BasisX.AngleTo(data1.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var unitedSolid = el1.GetUnitedSolid(el2, transform);
            var transformedSolid = SolidUtils.CreateTransformed(unitedSolid, transform);
            var planarFace = unitedSolid.Faces.Cast<PlanarFace>()
                                        .FirstOrDefault(f => Math.Abs(f.FaceNormal.Z + 1) < Math.Pow(10, -7));
            if (planarFace == null)
                return null;

            var direction = planarFace.YVector;
            var minUnited = transformedSolid.GetBoundingBox().Min;
            var maxUnited = transformedSolid.GetBoundingBox().Max;

            var center = unitedSolid.ComputeCentroid();
            center = new XYZ(center.X, center.Y, data1.BoxData.IntersectionCenter.Z);
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.X - minUnited.X;
            (var hostsGeometries, var pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth, direction,
                center, hostsGeometries, pipesGeometries, Families.FloorRectTaskFamily.SymbolName,
                data1.BoxData.Offset, data1.BoxData.Diameter, data1.BoxData.Level);
        }

        private static OpeningData CalculateUnitedTaskInWallWithRounds(OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var backT = transform.Inverse;
            var center1 = transform.OfPoint(data1.BoxData.IntersectionCenter.XYZ);
            var center2 = transform.OfPoint(data2.BoxData.IntersectionCenter.XYZ);
            var width = Math.Abs(transform.OfPoint(center1).Y - transform.OfPoint(center2).Y)
                + Math.Max(data1.BoxData.Width, data2.BoxData.Width);
            var height = Math.Abs(transform.OfPoint(center1).Z - transform.OfPoint(center2).Z)
                + Math.Max(data1.BoxData.Height, data2.BoxData.Height);

            var center = (center1 + center2) / 2;
            //
            //Фикс семейства WallRectTask
            center -= new XYZ(0, 0, height / 2);
            //
            center = backT.OfPoint(center);
            (var hostsGeometries, var pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth, data1.BoxData.Direction.XYZ,
                center, hostsGeometries, pipesGeometries, Families.WallRectTaskFamily.SymbolName,
                data1.BoxData.Offset, data1.BoxData.Diameter, data1.BoxData.Level);
        }

        private static OpeningData CalculateUnitedTaskInWallWithRects(Element el1, Element el2,
            OpeningParentsData data1, OpeningParentsData data2)
        {
            var angle = XYZ.BasisY.Negate().AngleTo(data1.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var unitedSolid = el1.GetUnitedSolid(el2, transform);
            var bSolid = SolidUtils.CreateTransformed(unitedSolid, transform);
            var minUnited = bSolid.GetBoundingBox().Min;
            var maxUnited = bSolid.GetBoundingBox().Max;
            var center = FindTasksCenterInWall(unitedSolid, transform);
            if (center == null)
                return null;

            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.Z - minUnited.Z;
            (var hostsGeometries, var pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth, data1.BoxData.Direction.XYZ, center.XYZ,
                hostsGeometries, pipesGeometries, Families.WallRectTaskFamily.SymbolName,
                data1.BoxData.Offset, data2.BoxData.Diameter, data1.BoxData.Level);
        }

        private static MyXYZ FindTasksCenterInWall(Solid unitedSolid, Transform transform)
        {
            const double tolerance = 0.000_000_1;
            var edges = unitedSolid?.Faces?.Cast<Face>()?
                                    .FirstOrDefault(face => Math.Abs(transform.OfPoint(
                                         face?.ComputeNormal(UV.BasisU)).X + 1) < tolerance)
                                   ?.EdgeLoops?.Cast<EdgeArray>()?
                                    .FirstOrDefault()?
                                    .Cast<Edge>()
                                    .ToArray();
            var minEdge = edges?.First().AsCurve() as Line;
            if (minEdge == null)
                return null;

            foreach (var edge in edges)
            {
                var line = edge.AsCurve() as Line;
                if (line == null)
                    return null;

                var minZ = minEdge.GetEndPoint(0).Z > minEdge.GetEndPoint(1).Z
                    ? minEdge.GetEndPoint(1).Z
                    : minEdge.GetEndPoint(0).Z;
                if (line.GetEndPoint(0).Z < minZ)
                    minEdge = line;
            }

            return new MyXYZ((minEdge.GetEndPoint(0) + minEdge.GetEndPoint(1)) / 2);
        }

        private static (List<ElementGeometry>, List<ElementGeometry>) UnionTwoData(OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var hostsGeometries = data1.BoxData.HostsGeometries
                                       .Union(data2.BoxData.HostsGeometries)
                                       .ToList();
            var pipesGeometries = data1.BoxData.PipesGeometries
                                       .Union(data2.BoxData.PipesGeometries)
                                       .ToList();
            return (hostsGeometries, pipesGeometries);
        }
    }
}
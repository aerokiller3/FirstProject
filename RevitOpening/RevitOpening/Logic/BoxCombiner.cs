using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class BoxCombiner
    {
        //?
        public static bool ValidateTasksForCombine(IEnumerable<Document> documents, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            return (data1.PipesIds.AlmostEqualTo(data2.PipesIds) || data1.HostsIds.AlmostEqualTo(data2.HostsIds))
                   && data1.BoxData.FamilyName == data2.BoxData.FamilyName
                   && (documents.GetElement(data1.HostsIds.FirstOrDefault()) is Wall
                       || documents.GetElement(data1.HostsIds.FirstOrDefault()) is CeilingAndFloor);
        }

        public static void CombineAllBoxes(IEnumerable<Document> documents, Document currentDocument)
        {
            var isElementsUnited = true;
            while (isElementsUnited)
            {
                isElementsUnited = false;
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.WallRectTaskFamily);
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.FloorRectTaskFamily);
                isElementsUnited |= CombineOneTypeBoxes(documents, currentDocument, Families.WallRoundTaskFamily);
                currentDocument.Regenerate();
            }
        }

        private static bool CombineOneTypeBoxes(IEnumerable<Document> documents, Document currentDocument,
            FamilyParameters familyData)
        {
            var tasks = currentDocument.GetTasks(familyData);
            var intersections = FindTaskIntersections(tasks).ToList();
            for (var i=0;i<intersections.Count;i++)
                if (CombineTwoBoxes(documents, currentDocument, intersections[i].Item1, intersections[i].Item2) == null)
                {
                    intersections.RemoveAt(i);
                    i--;
                }

            return intersections.Count > 0;
        }

        public static FamilyInstance CombineTwoBoxes(IEnumerable<Document> documents, Document currentDocument,
            Element el1, Element el2)
        {
            var data1 = el1.GetParentsData();
            var data2 = el2.GetParentsData();
            if (!ValidateTasksForCombine(documents, data1, data2))
            {
                return null;
                //throw new Exception("Недопустимый вариант объединения");
            }

            OpeningData newOpening = null;
            if (data1.PipesIds.AlmostEqualTo(data2.PipesIds))
                newOpening = CalculateUnitedTaskOnOnePipe(data1, data2);
            else if (documents.GetElement(data1.HostsIds.FirstOrDefault()) is Wall)
                newOpening = data1.BoxData.FamilyName == Families.WallRoundTaskFamily.SymbolName
                    ? CalculateUnitedTaskInWallWithRounds(data1, data2)
                    : CalculateUnitedTaskInWallWithRects(el1, el2, data1, data2);
            else if (documents.GetElement(data1.HostsIds.FirstOrDefault()) is CeilingAndFloor)
                newOpening = CalculateUnitedTaskInFloor(el1, el2, data1, data2);

            var newData = new OpeningParentsData(data1.HostsIds.Union(data2.HostsIds).ToList(),
                data1.PipesIds.Union(data2.PipesIds).ToList(),
                newOpening);
            newData.BoxData.Collisions.Add(Collisions.TaskCouldNotBeProcessed);

            var createdElement = BoxCreator.CreateTaskBox(newData, currentDocument);
            currentDocument.Delete(el1.Id);
            currentDocument.Delete(el2.Id);

            return createdElement;
        }

        private static IEnumerable<(Element, Element)> FindTaskIntersections(List<FamilyInstance> elements)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                var data = elements[i].GetParentsData();
                var tolerance = new XYZ(0.001, 0.001, 0.001);
                var angle = XYZ.BasisY.Negate().AngleTo(data.BoxData.Direction.XYZ);
                var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
                var solid = elements[i].GetUnitedSolid(null, transform, tolerance);
                var filter = new ElementIntersectsSolidFilter(solid);
                for (var j = i + 1; j < elements.Count; j++)
                {
                    if (elements[i].Id == elements[j].Id || !filter.PassesFilter(elements[j]))
                        continue;

                    yield return (elements[i], elements[j]);

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
            var (hostsGeometries, pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                data1.BoxData.Width, data1.BoxData.Height, depth,
                data1.BoxData.Direction.XYZ,
                center,
                hostsGeometries,
                pipesGeometries,
                data1.BoxData.FamilyName);
        }

        private static OpeningData CalculateUnitedTaskInFloor(Element el1, Element el2, OpeningParentsData data1,
            OpeningParentsData data2)
        {
            var angle = XYZ.BasisX.AngleTo(data1.BoxData.Direction.XYZ);
            var transform = Transform.CreateRotation(XYZ.BasisZ, -angle);
            var unitedSolid = el1.GetUnitedSolid(el2, transform);
            var transformedSolid = SolidUtils.CreateTransformed(unitedSolid, transform);
            var direction = unitedSolid.Faces.Cast<PlanarFace>()
                .FirstOrDefault(f => Math.Abs(f.FaceNormal.Z + 1) < Math.Pow(10, -7))
                .YVector;
            var minUnited = transformedSolid.GetBoundingBox().Min;
            var maxUnited = transformedSolid.GetBoundingBox().Max;

            var center = unitedSolid.ComputeCentroid();
            center = new XYZ(center.X, center.Y, data1.BoxData.IntersectionCenter.Z);
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.X - minUnited.X;
            var (hostsGeometries, pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth,
                direction, center,
                hostsGeometries,
                pipesGeometries,
                Families.FloorRectTaskFamily.SymbolName);
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
            var (hostsGeometries, pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth,
                data1.BoxData.Direction.XYZ,
                center,
                hostsGeometries,
                pipesGeometries,
                Families.WallRectTaskFamily.SymbolName);
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
            var width = maxUnited.Y - minUnited.Y;
            var height = maxUnited.Z - minUnited.Z;
            var (hostsGeometries, pipesGeometries) = UnionTwoData(data1, data2);
            return new OpeningData(
                width, height, data1.BoxData.Depth,
                data1.BoxData.Direction.XYZ,
                center.XYZ,
                hostsGeometries,
                pipesGeometries,
                Families.WallRectTaskFamily.SymbolName);
        }

        private static MyXYZ FindTasksCenterInWall(Solid unitedSolid, Transform transform)
        {
            const double tolerance = 0.000_000_1;
            var edges = unitedSolid?.Faces?.Cast<Face>()?
                .FirstOrDefault(face => Math.Abs(transform.OfPoint(
                    face?.ComputeNormal(UV.BasisU)).X + 1) < tolerance)
                ?.EdgeLoops?.Cast<EdgeArray>()?
                .FirstOrDefault()?
                .Cast<Edge>()?
                .ToArray();
            var minEdge = edges?.First().AsCurve() as Line;
            foreach (var edge in edges)
            {
                var line = edge.AsCurve() as Line;
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
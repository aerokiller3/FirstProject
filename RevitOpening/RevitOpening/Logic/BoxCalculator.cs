using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class BoxCalculator
    {
        public static OpeningData CalculateBoxInElement(Element element, MEPCurve pipe, double offsetRatio,
            double maxDiameter)
        {
            switch (element)
            {
                case Wall wall:
                    return CalculateBoxInWall(wall, pipe, offsetRatio, maxDiameter);
                case CeilingAndFloor floor:
                    return CalculateBoxInFloor(floor, pipe, offsetRatio);
                default:
                    throw new Exception("Неизсветный тип хост-элемента");
            }
        }

        private static OpeningData CalculateBoxInWall(Wall wall, MEPCurve pipe, double offsetRatio, double maxDiameter)
        {
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);
            var (intersectionCenter, direction) = CalculateCenterAndDirectionInWall(wallData, pipeData, wall);
            if (intersectionCenter == null || direction == null)
                return null;

            var taskWidth = CalculateWidthInWall(pipe.GetPipeWidth(), wall.Width, wallData, pipeData, offsetRatio);
            var taskHeight = CalculateHeightInWall(pipe.GetPipeHeight(), wall.Width, pipeData, offsetRatio);
            var isRoundTask = pipe.IsRoundPipe() && taskWidth <= maxDiameter.GetInFoot();
            var familyParameters = isRoundTask ? Families.WallRoundTaskFamily : Families.WallRectTaskFamily;
            var taskDepth = wall.Width;
            //
            // Фикс семейства
            if (familyParameters == Families.WallRectTaskFamily)
                intersectionCenter -= new XYZ(0, 0, taskHeight / 2);
            //

            return new OpeningData(taskWidth, taskHeight, taskDepth, direction,
                intersectionCenter, new List<ElementGeometry> {wallData},
                new List<ElementGeometry> {pipeData}, familyParameters.SymbolName,offsetRatio,maxDiameter);
        }

        private static OpeningData CalculateBoxInFloor(CeilingAndFloor floor, MEPCurve pipe, double offsetRatio)
        {
            var pipeData = new ElementGeometry(pipe);
            var floorData = new ElementGeometry(floor);
            var (intersectionCenter, direction) = CalculateCenterAndDirectionInFloor(pipeData, floor, pipe);
            if (intersectionCenter == null || direction == null)
                return null;

            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();
            var taskWidth = pipeHeight * offsetRatio;
            var taskHeight = pipeWidth * offsetRatio;
            var taskDepth = floorData.SolidInfo.Max.Z - floorData.SolidInfo.Min.Z;

            return new OpeningData(taskWidth, taskHeight, taskDepth, direction,
                intersectionCenter, new List<ElementGeometry> {floorData},
                new List<ElementGeometry> {pipeData}, Families.FloorRectTaskFamily.SymbolName, offsetRatio, 0);
        }

        private static (XYZ, XYZ) CalculateCenterAndDirectionInFloor(ElementGeometry pipeData, CeilingAndFloor floor,
            MEPCurve pipe)
        {
            var floorSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var direction = pipe.ConnectorManager.Connectors
                .Cast<Connector>()
                .FirstOrDefault()?.CoordinateSystem.BasisY;
            var curves = floorSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return (null, null);

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectVector = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var bias = new XYZ(0, 0, -intersectVector.Z / 2);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2 - bias;
            return (intersectionCenter, direction);
        }

        private static (XYZ, XYZ) CalculateCenterAndDirectionInWall(ElementGeometry wallData, ElementGeometry pipeData,
            Wall wall)
        {
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var line = (Line) wallData.Curve;
            var byLineWallOrientation = line.Direction.CrossProduct(XYZ.BasisZ.Negate());
            var bias = wall.Width * byLineWallOrientation / 2;
            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());

            if (curves == null || curves.SegmentCount == 0)
                return (null, null);

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var direction = (wallData.End.XYZ - wallData.Start.XYZ).Normalize();
            intersectionCenter -= bias;
            if (direction.X < 0 ||
                Math.Abs(direction.X) < Math.Pow(10, -7) && Math.Abs(direction.Y + 1) < Math.Pow(10, -7))
            {
                direction = direction.Negate();
                intersectionCenter += 2 * bias;
            }

            return (intersectionCenter, direction);
        }

        private static double CalculateWidthInWall(double pipeWidth, double wallWidth, ElementGeometry wallData,
            ElementGeometry pipeData, double offsetRatio)
        {
            var horizontalAngleBetweenWallAndPipe =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                          (SqrtOfSqrSum(wallData.XLen, wallData.YLen) *
                           SqrtOfSqrSum(pipeData.XLen, pipeData.YLen)));
            horizontalAngleBetweenWallAndPipe = GetAcuteAngle(horizontalAngleBetweenWallAndPipe);

            return CalculateTaskSize(wallWidth, horizontalAngleBetweenWallAndPipe, pipeWidth, offsetRatio);
        }

        private static double CalculateHeightInWall(double pipeWidth, double wallWidth, ElementGeometry pipeData,
            double offsetRatio)
        {
            var verticalAngleBetweenWallAndPipe =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            verticalAngleBetweenWallAndPipe = GetAcuteAngle(verticalAngleBetweenWallAndPipe);

            return CalculateTaskSize(wallWidth, verticalAngleBetweenWallAndPipe, pipeWidth, offsetRatio);
        }

        private static double CalculateTaskSize(double wallWidth, double angle, double ductWidth, double offsetRatio)
        {
            var r = (wallWidth / angle==0 ? 1 :Math.Tan(angle)
                + ductWidth / angle==0? 1: Math.Sin(angle)) * offsetRatio;
            return r;
        }

        private static double SqrtOfSqrSum(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        private static double GetAcuteAngle(double angel)
        {
            return angel > Math.PI / 2
                ? Math.PI - angel
                : angel;
        }
    }
}
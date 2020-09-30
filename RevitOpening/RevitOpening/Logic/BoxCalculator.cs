namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Extensions;
    using LoggerClient;
    using Models;

    internal static class BoxCalculator
    {
        public static OpeningData CalculateBoxInElement(Element element, MEPCurve pipe, double offsetRatio,
            double maxDiameter)
        {
            try
            {
                switch (element)
                {
                    case Wall wall:
                        return CalculateBoxInWall(wall, pipe, offsetRatio, maxDiameter);
                    case CeilingAndFloor floor:
                        return CalculateBoxInFloor(floor, pipe, offsetRatio);
                    default:
                        throw new ArgumentException("Unsupported host type");
                }
            }
            catch (Exception e)
            {
                ModuleLogger.SendErrorData(e.Message,
                    $"Element id: {element?.Id?.IntegerValue}", nameof(BoxCalculator),
                    e.StackTrace, nameof(RevitOpening));
                return null;
            }
        }

        private static OpeningData CalculateBoxInWall(Wall wall, MEPCurve pipe, double offsetRatio, double maxDiameter)
        {
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);
            (var intersectionCenter, var direction) = CalculateCenterAndDirectionInWall(wallData, pipeData, wall);
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
                new List<ElementGeometry> {pipeData}, familyParameters.SymbolName,
                offsetRatio, maxDiameter, null);
        }

        private static bool IsNotNormalNumber(double number)
        {
            return double.IsNaN(number) || double.IsInfinity(number);
        }

        private static OpeningData CalculateBoxInFloor(CeilingAndFloor floor, MEPCurve pipe, double offsetRatio)
        {
            var pipeData = new ElementGeometry(pipe);
            var floorData = new ElementGeometry(floor);
            (var intersectionCenter, var direction) = CalculateCenterAndDirectionInFloor(pipeData, floor, pipe);
            if (intersectionCenter == null || direction == null)
                return null;

            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();
            var taskWidth = pipeHeight * offsetRatio;
            var taskHeight = pipeWidth * offsetRatio;
            var taskDepth = floorData.SolidInfo.Max.Z - floorData.SolidInfo.Min.Z;

            return new OpeningData(taskWidth, taskHeight, taskDepth, direction,
                intersectionCenter, new List<ElementGeometry> {floorData},
                new List<ElementGeometry> {pipeData}, Families.FloorRectTaskFamily.SymbolName,
                offsetRatio, 0, null);
        }

        private static (XYZ, XYZ) CalculateCenterAndDirectionInFloor(ElementGeometry pipeData,
            CeilingAndFloor floor, MEPCurve pipe)
        {
            var floorSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var direction = pipe.ConnectorManager.Connectors
                                .Cast<Connector>()
                                .FirstOrDefault()?
                                .CoordinateSystem.BasisX
                                .CrossProduct(XYZ.BasisZ.Negate());
            var curves = floorSolid?
               .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return (null, null);

            var intersectCurve = (Line) curves.GetCurveSegment(0);
            var intersectVector = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var bias = new XYZ(0, 0, -intersectVector.Z);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            intersectionCenter -= bias;
            if (intersectCurve.Direction.Z < 0)
            {
                direction = direction.Negate();
                intersectionCenter += 2 * bias;
            }

            return (intersectionCenter, direction);
        }

        private static (XYZ, XYZ) CalculateCenterAndDirectionInWall(ElementGeometry wallData, ElementGeometry pipeData,
            Wall wall)
        {
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var direction = wallData.Curve.Direction;
            var byLineWallOrientation = direction.CrossProduct(XYZ.BasisZ.Negate());
            var bias = wall.Width * byLineWallOrientation / 2;
            var curves = geomSolid?
               .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());

            if (curves == null || curves.SegmentCount == 0)
                return (null, null);

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
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
            var size = CalculateTaskSize(wallWidth, horizontalAngleBetweenWallAndPipe, pipeWidth, offsetRatio);
            return IsNotNormalNumber(size)
                ? size
                : pipeWidth * offsetRatio;
        }

        private static double CalculateHeightInWall(double pipeHeight, double wallWidth, ElementGeometry pipeData,
            double offsetRatio)
        {
            var verticalAngleBetweenWallAndPipe =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                    pipeData.ZLen * pipeData.ZLen));
            verticalAngleBetweenWallAndPipe = GetAcuteAngle(verticalAngleBetweenWallAndPipe);
            var size = CalculateTaskSize(wallWidth, verticalAngleBetweenWallAndPipe, pipeHeight, offsetRatio);
            return IsNotNormalNumber(size)
                ? size
                : pipeHeight * offsetRatio;
        }

        private static double CalculateTaskSize(double wallWidth, double angle, double pipeSize, double offsetRatio)
        {
            return (wallWidth / Math.Tan(angle)
                + pipeSize / Math.Sin(angle)) * offsetRatio;
        }

        private static double SqrtOfSqrSum(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        private static double GetAcuteAngle(double angel)
        {
            angel = angel > Math.PI / 2
                ? Math.PI - angel
                : angel;
            if (IsNotNormalNumber(angel) || Math.Abs(angel) < 0.001)
                return Math.PI / 2;

            return angel;
        }
    }
}
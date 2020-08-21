using System;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class BoxCalculator
    {
        public static OpeningData CalculateBoxInElement(Element element, MEPCurve pipe, double offsetRatio, double maxDiameter)
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
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var line = (Line) ((LocationCurve) wall.Location).Curve;
            var byLineWallOrientation = line.Direction.CrossProduct(XYZ.BasisZ.Negate());
            var bias = wall.Width * byLineWallOrientation / 2;
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());

            if (curves == null || curves.SegmentCount == 0)
                return null;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var direction = (wallData.End.XYZ - wallData.Start.XYZ).Normalize();
            intersectionCenter -= bias;
            if (direction.X < 0 || Math.Abs(direction.X) < Math.Pow(10, -7) && Math.Abs(direction.Y + 1) < Math.Pow(10, -7))
            {
                direction = direction.Negate();
                intersectionCenter += 2 * bias;
            }


            var width = CalculateWidthInWall(pipe.GetPipeWidth(), wall.Width, wallData, pipeData, offsetRatio);
            var height = CalculateHeightInWall(pipe.GetPipeHeight(), wall.Width, pipeData, offsetRatio);
            var isRound = pipe.IsRoundPipe() && width <= maxDiameter.GetInFoot();
            var familyParameters = isRound ? Families.WallRoundTaskFamily : Families.WallRectTaskFamily;
            //
            // Фикс сдвига
            //
            if (familyParameters.SymbolName == Families.WallRectTaskFamily.SymbolName)
                intersectionCenter -= new XYZ(0, 0, height / 2);

            var depth = wall.Width;

            return new OpeningData(null, width, height, depth, new MyXYZ(direction),
                new MyXYZ(intersectionCenter), wallData, pipeData, familyParameters.SymbolName, null);
        }

        private static OpeningData CalculateBoxInFloor(CeilingAndFloor floor, MEPCurve pipe, double offsetRatio)
        {
            var floorSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var pipeData = new ElementGeometry(pipe);
            var floorData = new ElementGeometry(floor);
            var direction = pipe.ConnectorManager.Connectors
                .Cast<Connector>()
                .FirstOrDefault()?.CoordinateSystem.BasisY;
            var curves = floorSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return null;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectVector = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var bias = new XYZ(0, 0, -intersectVector.Z / 2);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2 - bias;

            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();
            var taskWidth = pipeHeight * offsetRatio;
            var taskHeight = pipeWidth * offsetRatio;
            var taskDepth = Math.Abs(intersectVector.Z);

            return new OpeningData(null, taskWidth, taskHeight, taskDepth, new MyXYZ(direction),
                new MyXYZ(intersectionCenter), floorData, pipeData, Families.FloorRectTaskFamily.SymbolName, null);
        }

        private static double CalculateWidthInWall(double pipeWidth, double wallWidth, ElementGeometry wallData,
            ElementGeometry pipeData, double offsetRatio)
        {
            var horizontalAngleBetweenWallAndPipe =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                          (SqrtOfSqrSum(wallData.XLen, wallData.YLen) *
                           SqrtOfSqrSum(pipeData.XLen, pipeData.YLen)));
            horizontalAngleBetweenWallAndPipe = GetAcuteAngle(horizontalAngleBetweenWallAndPipe);

            return CalculateSize(wallWidth, horizontalAngleBetweenWallAndPipe, pipeWidth, offsetRatio);
        }

        private static double CalculateHeightInWall(double pipeWidth, double wallWidth, ElementGeometry pipeData,
            double offsetRatio)
        {
            var verticalAngleBetweenWallAndPipe =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            verticalAngleBetweenWallAndPipe = GetAcuteAngle(verticalAngleBetweenWallAndPipe);

            return CalculateSize(wallWidth, verticalAngleBetweenWallAndPipe, pipeWidth, offsetRatio);
        }

        private static double CalculateSize(double wallWidth, double angle, double ductWidth, double offsetRatio)
        {
            return (wallWidth / Math.Tan(angle) + ductWidth / Math.Sin(angle)) * offsetRatio;
        }

        public static double SqrtOfSqrSum(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        public static double GetAcuteAngle(double angel)
        {
            return angel > Math.PI / 2
                ? Math.PI - angel
                : angel;
        }
    }
}
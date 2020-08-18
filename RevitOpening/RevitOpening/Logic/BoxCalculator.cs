using System;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class BoxCalculator
    {
        public OpeningData CalculateBoxInElement(Element element, MEPCurve pipe, double offset, double maxDiameter)
        {
            switch (element)
            {
                case Wall wall:
                    return CalculateBoxInWall(wall, pipe, offset, maxDiameter);
                case CeilingAndFloor floor:
                    return CalculateBoxInFloor(floor, pipe, offset);
                default:
                    throw new Exception("Неизсветный тип хост-элемента");
            }
        }

        public OpeningData CalculateBoxInWall(Wall wall, MEPCurve pipe, double offset, double maxDiameter)
        {
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall, new MyXYZ(wall.Orientation));
            var pipeData = new ElementGeometry(pipe, new MyXYZ(((Line) ((LocationCurve) pipe.Location).Curve).Direction));
            var direction = wallData.End.GetXYZ() - wallData.Start.GetXYZ();
            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());

            if (curves == null || curves.SegmentCount == 0)
                return null;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectHalf = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var bias = new XYZ(
                pipeData.Orientation.X * wallData.Orientation.X * intersectHalf.X,
                pipeData.Orientation.Y * wallData.Orientation.Y * intersectHalf.Y,
                pipeData.Orientation.Z * wallData.Orientation.Z * intersectHalf.Z);

            intersectionCenter -= bias;
            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();
            var width = pipeWidth + offset.GetInFoot();
            var height = pipeHeight + offset.GetInFoot();
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

        public OpeningData CalculateBoxInFloor(CeilingAndFloor floor, MEPCurve pipe, double offset)
        {
            var geomSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var pipeData = new ElementGeometry(pipe, new MyXYZ(((Line) ((LocationCurve) pipe.Location).Curve).Direction));
            var floorData = new ElementGeometry(floor, new MyXYZ(0, 0, -1));
            var direction = new XYZ();

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return null;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectHalf = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var bias = new XYZ(
                pipeData.Orientation.X * floorData.Orientation.X * intersectHalf.X,
                pipeData.Orientation.Y * floorData.Orientation.Y * intersectHalf.Y,
                pipeData.Orientation.Z * floorData.Orientation.Z * intersectHalf.Z);

            intersectionCenter -= bias;

            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();
            var width = pipeWidth + Extensions.GetInFoot(offset);
            var height = pipeHeight + Extensions.GetInFoot(offset);
            var boundBox = (floor.get_Geometry(new Options()).FirstOrDefault() as Solid).GetBoundingBox();
            var depth = boundBox.Max.Z - boundBox.Min.Z;

            return new OpeningData(null, height, width, depth, new MyXYZ(direction),
                new MyXYZ(intersectionCenter), floorData, pipeData, Families.FloorRectTaskFamily.SymbolName, null);
        }

        private double CalculateOpeningWidthInWall(double pipeWidth, double wallWidth, ElementGeometry wallData,
            ElementGeometry pipeData, double offset)
        {
            var horAngleBetweenWallAndDuct =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                          (Extensions.SqrtOfSqrSum(wallData.XLen, wallData.YLen) *
                           Extensions.SqrtOfSqrSum(pipeData.XLen, pipeData.YLen)));
            horAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(horAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, horAngleBetweenWallAndDuct, pipeWidth, offset);
        }

        private double CalculateOpeningHeightInWall(double pipeWidth, double wallWidth, ElementGeometry pipeData,
            double offset)
        {
            var verAngleBetweenWallAndDuct =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            verAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(verAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, verAngleBetweenWallAndDuct, pipeWidth, offset);
        }
    }
}
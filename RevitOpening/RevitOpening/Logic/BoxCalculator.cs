using System;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class BoxCalculator
    {
        public OpeningData CalculateBoxInElement(Element element, MEPCurve pipe, double offset,
            FamilyParameters familyParameters)
        {
            switch (element)
            {
                case Wall wall:
                    return CalculateBoxInElement(wall, pipe, offset, familyParameters);
                case CeilingAndFloor floor:
                    return CalculateBoxInElement(floor, pipe, offset, familyParameters);
                default:
                    throw new Exception("Неизсветный тип хост-элемента");
            }
        }

        public OpeningData CalculateBoxInElement(Wall wall, MEPCurve pipe, double offset,
            FamilyParameters familyParameters)
        {
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);
            var direction = wallData.End.GetXYZ() - wallData.Start.GetXYZ();
            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());

            if (curves == null || curves.SegmentCount == 0)
                return null;

            var wallOrentation = wall.Orientation;
            var pipeOrentation = ((pipe.Location as LocationCurve).Curve as Line).Direction;
            var intersectCurve = curves.GetCurveSegment(0);
            var intersectHalf = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var bias = new XYZ(
                pipeOrentation.X * wallOrentation.X * intersectHalf.X,
                pipeOrentation.Y * wallOrentation.Y * intersectHalf.Y,
                pipeOrentation.Z * wallOrentation.Z * intersectHalf.Z);

            intersectionCenter -= bias;
            var pipeWidth = pipe.GetPipeWidth();

            var width = CalculateOpeningWidthInWall(pipeWidth, wall.Width, wallData, pipeData, offset);
            var height = CalculateOpeningHeightInWall(pipeWidth, wall.Width, pipeData, offset);
            //
            // Фикс сдвига
            //
            if (familyParameters.SymbolName == Families.WallRectTaskFamily.SymbolName)
                intersectionCenter -= new XYZ(0, 0, height / 2);


            var depth = wall.Width;

            return new OpeningData(width, height, depth, new MyXYZ(direction),
                new MyXYZ(intersectionCenter), wallData, pipeData, familyParameters.SymbolName);
        }

        public OpeningData CalculateBoxInElement(CeilingAndFloor floor, MEPCurve pipe, double offset,
            FamilyParameters familyParameters)
        {
            var geomSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var pipeData = new ElementGeometry(pipe);
            var wallData = new ElementGeometry(floor);
            var direction = new XYZ();

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return null;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectHalf = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2
                                     + new XYZ(0, 0, intersectHalf.Z);
            var pipeWidth = pipe.GetPipeWidth();

            var width = pipeWidth + Extensions.GetOffsetInFoot(offset);
            var height = pipeWidth + Extensions.GetOffsetInFoot(offset);
            var boundBox = (floor.get_Geometry(new Options()).FirstOrDefault() as Solid).GetBoundingBox();
            var depth = boundBox.Max.Z - boundBox.Min.Z;

            return new OpeningData(width, height, depth, new MyXYZ(direction),
                new MyXYZ(intersectionCenter), wallData, pipeData, familyParameters.SymbolName);
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
            var vertAngleBetweenWallAndDuct =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            vertAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(vertAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, vertAngleBetweenWallAndDuct, pipeWidth, offset);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitOpening
{
    public class WallBoxCalculator : IBoxCalculator
    {
        public OpeningParametrs CalculateBoxInElement(Element element, MEPCurve pipe, double offset, FamilyParameters familyParameters)
        {
            return CalculateBoxInWall(element as Wall, pipe, offset, familyParameters);
        }

        private OpeningParametrs CalculateBoxInWall(Wall wall, MEPCurve pipe, double offset, FamilyParameters familyParameters)
        {
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);
            var direction = new XYZ(wallData.End.X - wallData.Start.X, wallData.End.Y - wallData.Start.Y, 0);

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0)
                return null;
            var wallOrentation = wall.Orientation;
            var pipeOrentation = ((pipe.Location as LocationCurve).Curve as Line).Direction;
            var intersectCurve = curves.GetCurveSegment(0);
            var intersectHalf = (intersectCurve.GetEndPoint(1) - intersectCurve.GetEndPoint(0)) / 2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var bias = new XYZ(pipeOrentation.X*wallOrentation.X * intersectHalf.X, 
                                         pipeOrentation.Y * wallOrentation.Y * intersectHalf.Y, 
                                         pipeOrentation.Z * wallOrentation.Z * intersectHalf.Z);
            intersectionCenter -= bias;
            var pipeWidth = pipe.GetPipeWidth();

            var width = CalculateWidth(pipeWidth, wall.Width, wallData, pipeData, offset);
            var height = CalculateHeight(pipeWidth, wall.Width, pipeData, offset);

            if(familyParameters.SymbolName == Families.WallRectTaskFamily.SymbolName)
                intersectionCenter -= new XYZ(0,0 , height / 2);

            var depth = wall.Width;

            return new OpeningParametrs(width,height,depth,direction, intersectionCenter, wallData,pipeData);
        }

        private double CalculateWidth(double pipeWidth, double wallWidth, ElementGeometry wallData,
            ElementGeometry pipeData, double offset)
        {
            var horAngleBetweenWallAndDuct =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                          (Extensions.SqrtOfSqrSum(wallData.XLen, wallData.YLen) * Extensions.SqrtOfSqrSum(pipeData.XLen, pipeData.YLen)));
            horAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(horAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, horAngleBetweenWallAndDuct, pipeWidth, offset);
        }

        private double CalculateHeight(double pipeWidth, double wallWidth, ElementGeometry pipeData, double offset)
        {
            var vertAngleBetweenWallAndDuct =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            vertAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(vertAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, vertAngleBetweenWallAndDuct, pipeWidth, offset);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public class FloorBoxCalculator : IBoxCalculator
    {
        public OpeningParametrs CalculateBoxInElement(Element element, MEPCurve pipe, double offset, FamilyParameters familyParameters)
        {
            return CalculateBoxInFloor(element as Floor, pipe, offset, familyParameters);
        }
        private OpeningParametrs CalculateBoxInFloor(Floor floor, MEPCurve pipe, double offset, FamilyParameters familyParameters)
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
            var intersectHalf = (intersectCurve.GetEndPoint(1)-intersectCurve.GetEndPoint(0))/2;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2 + new XYZ(0,0, intersectHalf.Z);
            var pipeWidth = pipe.GetPipeWidth();




            var width = CalculateWidth(pipeWidth, offset);
            var height = CalculateHeight(pipeWidth, offset);
            var boundBox = (floor.get_Geometry(new Options()).FirstOrDefault() as Solid).GetBoundingBox();
            var depth = boundBox.Max.Z - boundBox.Min.Z;

            return new OpeningParametrs(width,height,depth,direction,intersectionCenter,wallData,pipeData);
        }

        private double CalculateWidth(double pipeWidth, double offset)
        {
            return pipeWidth + Extensions.GetOffsetInFoot(offset);
        }

        private double CalculateHeight(double pipeHeight, double offset)
        {
            return pipeHeight + Extensions.GetOffsetInFoot(offset);
        }
    }
}

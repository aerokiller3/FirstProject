using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class CutOpening : IExternalCommand
    {
        private Document _document;

        private IEnumerable<Document> _linkedDocuments;

        private readonly double _offset = 300;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            var allDocs = commandData.Application.Application
                .Documents.Cast<Document>();
            _linkedDocuments = allDocs.Skip(1);

            var walls = GetElementsList<Wall>(allDocs);
            var floors = GetElementsList<Floor>(allDocs);

            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);
            //Можно добавить ещё коммуникаций


            FindIntersectionsWith(walls, ducts);
            FindIntersectionsWith(walls, pipes);
            FindIntersectionsWith(floors, ducts);
            FindIntersectionsWith(floors, pipes);

            return Result.Succeeded;
        }
        private List<T> GetElementsList<T>(IEnumerable<Document> allDocs)
        {
            var elements = new List<T>();
            foreach (var document in allDocs)
            {
                var currentDucs = new FilteredElementCollector(document)
                    .OfClass(typeof(T))
                    .ToElements()
                    .Cast<T>();
                elements.AddRange(currentDucs);
            }

            return elements;
        }

        private void FindIntersectionsWith(IEnumerable<Element> walls, IReadOnlyCollection<MEPCurve> pipes)
        {
            foreach (var intersectionElement in walls)
            {
                var intersection = new ElementIntersectsElementFilter(intersectionElement);

                foreach (var element in pipes
                    .Where(el => intersection.PassesFilter(el)))
                {
                    switch (intersectionElement)
                    {
                        case Wall wall:
                            CalculateBoxInWall(wall, element);
                            break;
                        case Floor floor:
                            CalculateBoxInFloor(floor, element);
                            break;
                    }
                }
            }
        }

        private void CalculateBoxInFloor(Floor floor, MEPCurve pipe)
        {
            var familySymbol = GetFamilySymbol(_document,Families.FloorRectTaskFamily.Name);

            var geomSolid = floor.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(floor);
            var pipeData = new ElementGeometry(pipe);

            var dir = new XYZ(wallData.End.X - wallData.Start.X, wallData.End.Y - wallData.Start.Y, 0);

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0 || familySymbol == null)
                return;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var pipeWidth = pipe.GetPipeWidth();
            var pipeHeight = pipe.GetPipeHeight();

            var geomdPipeSolidFaces = (pipe.get_Geometry(new Options()).FirstOrDefault() as Solid).Faces;
            foreach (Face solidFace in geomdPipeSolidFaces)
            {
                var faceNormal = solidFace;
            }

            //var floorWidth = floor.
            //var width = CalculateWidth(pipeWidth, floor. , wallData, pipeData);
            //var height = CalculateHeight(pipeWidth, , pipeData);

            //CreateOpening(intersectionCenter, familySymbol, floor, height, width, pipe.Category.Name, dir);
        }
        public FamilySymbol GetFamilySymbol(Document document, string familyName)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows);

            var familySymbol = collector
                .ToElements()
                .Cast<FamilySymbol>()
                .FirstOrDefault(x => x.FamilyName == familyName);

            collector.Dispose();
            return familySymbol;
        }

        private void CalculateBoxInWall(Wall wall, MEPCurve pipe)
        {
            //Families.WallRoundOpeningFamily /*работает*/
            //Families.WallRoundTaskFamily /*(Вылазит)*/
            //Families.WallRectTaskFamily  /*(Вылазит)*/
            //Families.WallRectOpeningFamily /*работает*/
            var currentFamily = Families.WallRoundTaskFamily;
            var familySymbol = GetFamilySymbol(_document, currentFamily.Name);
            
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);
            var direction = new XYZ(wallData.End.X - wallData.Start.X, wallData.End.Y - wallData.Start.Y, 0);

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves == null || curves.SegmentCount == 0 || familySymbol == null)
                return;

            var intersectCurve = curves.GetCurveSegment(0);
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            var pipeWidth = pipe.GetPipeWidth();

            var width = CalculateWidth(pipeWidth, wall.Width, wallData, pipeData);
            var height = CalculateHeight(pipeWidth, wall.Width, pipeData);
            var depth = wall.Width;

            CreateOpening(intersectionCenter, familySymbol, wall, height, width, pipe.Category.Name,direction, depth,currentFamily);
        }

        private double CalculateWidth(double pipeWidth, double wallWidth, ElementGeometry wallData,
            ElementGeometry pipeData)
        {
            var horAngleBetweenWallAndDuct =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                          (Extensions.SqrtOfSqrSum(wallData.XLen, wallData.YLen) * Extensions.SqrtOfSqrSum(pipeData.XLen, pipeData.YLen)));
            horAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(horAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, horAngleBetweenWallAndDuct, pipeWidth, _offset);
        }

        private double CalculateHeight(double pipeWidth, double wallWidth, ElementGeometry pipeData)
        {
            var vertAngleBetweenWallAndDuct =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen * pipeData.XLen + pipeData.YLen * pipeData.YLen +
                                                    pipeData.ZLen * pipeData.ZLen));
            vertAngleBetweenWallAndDuct = Extensions.GetAcuteAngle(vertAngleBetweenWallAndDuct);

            return Extensions.CalculateSize(wallWidth, vertAngleBetweenWallAndDuct, pipeWidth, _offset);
        }

        private void CreateOpening(XYZ intersectionCenter, FamilySymbol familySymbol, Element wall,
            double minHeight, double minWidth, string categoryName, XYZ direction, double depth, TOFamily familyData)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create box");
                familySymbol.Activate();
                var newBox = _document.Create.NewFamilyInstance(intersectionCenter, familySymbol,
                    direction, wall, StructuralType.NonStructural);
                if (familyData.DiametrParametr != null)
                    newBox.LookupParameter(familyData.DiametrParametr).Set(Math.Max(minWidth, minHeight));
                else
                {
                    newBox.LookupParameter(familyData.HeightParametr).Set(minHeight);
                    newBox.LookupParameter(familyData.WidthParametr).Set(minWidth);
                }

                newBox.LookupParameter(familyData.DepthParametr).Set(depth);

                transaction.Commit();
            }
        }
    }
}
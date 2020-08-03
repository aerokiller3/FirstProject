using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class Class1 : IExternalCommand
    {
        private Document _document;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            var allDocs = commandData.Application.Application.Documents;
            var walls = GetElementsList<Wall>(allDocs);
            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);
            FindIntersectionsWith(walls, ducts);
            FindIntersectionsWith(walls, pipes);


            return Result.Succeeded;

        }

        private void FindIntersectionsWith(List<Wall> walls, IReadOnlyCollection<Element> elements)
        {
            foreach (var wall in walls)
            {
                var intersection = new ElementIntersectsElementFilter(wall);
                foreach (var element in elements
                    .Where(el=>intersection.PassesFilter(el)))
                    CreateBox(wall, element);
            }
        }

        private void CreateBox(Wall wall, Element communication)
        {
            switch (communication)
            {
                case Duct duct:
                    CreateDuctBox(wall, duct);
                    break;
                case Pipe pipe:
                    CreatePipeBox(wall, pipe);
                    break;
            }
        }

        private void CreatePipeBox(Wall wall, Pipe pipe)
        {
        }

        private void CreateDuctBox(Wall wall, Duct duct)
        {
            var collector = new FilteredElementCollector(_document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows);

            var familySymbol = collector
                .ToElements()
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName == "Проём прямоугольный")
                .FirstOrDefault();

            collector.Dispose();


            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallCurve = (wall.Location as LocationCurve)?.Curve;
            var startWall = wallCurve?.GetEndPoint(0);
            var endWall = wallCurve?.GetEndPoint(1);
            var wallXLen = startWall.X - endWall.X;
            var wallYLen = startWall.Y - endWall.Y;
            var wallZLen = startWall.Z - endWall.Z;


            var ductLine = duct.Location as LocationCurve;
            var curve = geomSolid?
                .IntersectWithCurve(ductLine?.Curve, new SolidCurveIntersectionOptions())
                .GetCurveSegment(0);
            
            var startDuct = curve?.GetEndPoint(0);
            var endDuct = curve?.GetEndPoint(1);
            var ductXLen = startDuct.X - endDuct.X;
            var ductYLen = startDuct.Y - endDuct.Y;
            var ductZLen = startDuct.Z - endDuct.Z;

            var intersectionCenter = (startDuct + endDuct) / 2;
            double ductWidth, ductHeight;
            try
            {
                ductHeight = duct.Height;
                ductWidth = duct.Width;
            }
            catch
            {
                ductWidth = duct.Diameter;
                ductHeight = duct.Diameter;
            }
            var horAngleBetweenWallAndDuct =
                Math.Acos(wallXLen * ductXLen + wallYLen * ductYLen) / (SqrtOfSqrSum(wallXLen,wallYLen) * SqrtOfSqrSum(ductXLen,ductYLen));
            horAngleBetweenWallAndDuct = GetAcuteAngle(horAngleBetweenWallAndDuct);
            var vertAngleBetweenWallAndDuct =
                Math.Acos(ductZLen / Math.Sqrt(ductXLen * ductXLen + ductYLen * ductYLen + ductZLen * ductZLen));
            vertAngleBetweenWallAndDuct = GetAcuteAngle(vertAngleBetweenWallAndDuct);

            var minWidth = CalculateMinSize(wall.Width, horAngleBetweenWallAndDuct, ductWidth);
            var minHeight = CalculateMinSize(wall.Width, vertAngleBetweenWallAndDuct, ductWidth);

            var currentLevel = _document.GetElement(wall.LevelId);
            using (Transaction transaction = new Transaction(_document))
            {
                transaction.Start("Create box");
                var newBox = _document.Create.NewFamilyInstance(intersectionCenter, familySymbol, wall,
                    (Level)currentLevel, StructuralType.NonStructural);
                newBox.LookupParameter("Ширина проёма").Set(minWidth);
                newBox.LookupParameter("Высота проёма").Set(minHeight);
                newBox.LookupParameter("Дисциплина проёма").Set(duct.Category.Name);
                transaction.Commit();
            }
        }

        private double CalculateMinSize(double wallWidth, double angle, double ductWidth) =>
            Math.Round(wallWidth / Math.Tan(angle)) + ductWidth / Math.Sin(angle);

        private double SqrtOfSqrSum(double a, double b) => Math.Sqrt(a * a + b * b);

        private double GetAcuteAngle(double angel) => angel > Math.PI / 2
            ? Math.PI - angel
            : angel;

        private List<T> GetElementsList<T>(DocumentSet allDocs)
        {
            var elements = new List<T>();
            foreach (Document document in allDocs)
            {
                var currentDucs = new FilteredElementCollector(document)
                    .OfClass(typeof(T))
                    .ToElements()
                    .Cast<T>();
                elements.AddRange(currentDucs);
            }

            return elements;
        }
    }
}

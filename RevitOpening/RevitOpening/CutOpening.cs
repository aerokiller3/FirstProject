using System;
using System.Windows;
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
    public class CutOpening : IExternalCommand
    {
        private Document _document;

        private Document _linkedDocument;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            var allDocs = commandData.Application.Application.Documents;
            var linked = allDocs.GetEnumerator();
            linked.MoveNext();
            linked.MoveNext();
            _linkedDocument=linked.Current as Document;
            var walls = GetElementsList<Wall>(allDocs);
            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);
            FindIntersectionsWith(walls, ducts);
            FindIntersectionsWith(walls, pipes);


            return Result.Succeeded;

        }

        private void FindIntersectionsWith(List<Wall> walls, IReadOnlyCollection<MEPCurve> elements)
        {
            foreach (var wall in walls)
            {
                var intersection = new ElementIntersectsElementFilter(wall);

                foreach (var element in elements
                    .Where(el => intersection.PassesFilter(el)))
                {
                    //element.
                    CreateBox(wall, element);
                }
            }
        }

        private FamilySymbol GetApertureSymbol(string categoryName, BuiltInCategory builtInCategory)
        {
            var collector = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(builtInCategory);

            var familySymbol = collector
                .ToElements()
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName == categoryName)
                .FirstOrDefault();

            collector.Dispose();
            return familySymbol;
        }

        private void CreateBox(Wall wall, MEPCurve pipe)
        {
            const string famName2 = "Задание_Стена_Прямоугольник_БезОсновы";
            var familySymbol = GetApertureSymbol(famName2, BuiltInCategory.OST_Windows);


            var offset = 100;
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall);
            var ductData = new ElementGeometry(pipe);

            double ductWidth, ductHeight;
            try
            {
                ductHeight = pipe.Height;
                ductWidth = pipe.Width;
            }
            catch
            {
                ductHeight = pipe.Diameter;
                ductWidth = pipe.Diameter;
            }

            var curves = geomSolid?
                .IntersectWithCurve(ductData.Curve, new SolidCurveIntersectionOptions());
            if (curves.SegmentCount == 0)
            {
                return;
            }


            var intersectCurve =  curves.GetCurveSegment(0);

            if(intersectCurve==null || familySymbol==null)
                return;

            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            intersectionCenter=new XYZ(intersectionCenter.X-ductHeight / 2, intersectionCenter.Y-ductWidth,intersectionCenter.Z);
            var horAngleBetweenWallAndDuct =
                Math.Acos((wallData.XLen * ductData.XLen + wallData.YLen * ductData.YLen) /
                (SqrtOfSqrSum(wallData.XLen,wallData.YLen) * SqrtOfSqrSum(ductData.XLen,ductData.YLen)));
            horAngleBetweenWallAndDuct = GetAcuteAngle(horAngleBetweenWallAndDuct);
            var vertAngleBetweenWallAndDuct =
                Math.Acos(ductData.ZLen / Math.Sqrt(ductData.XLen*ductData.XLen + ductData.YLen*ductData.YLen + ductData.ZLen*ductData.ZLen));
            vertAngleBetweenWallAndDuct = GetAcuteAngle(vertAngleBetweenWallAndDuct);

            var width = CalculateMinSize(wall.Width, horAngleBetweenWallAndDuct, ductWidth,offset);
            var height = CalculateMinSize(wall.Width, vertAngleBetweenWallAndDuct, ductWidth, offset);

            CreateAperture(intersectionCenter,familySymbol,wall,height,width,pipe.Category.Name);
        }

        private void CreateAperture(XYZ intersectionCenter, FamilySymbol familySymbol, Wall wall,
            double minHeight, double minWidth, string categoryName)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create box");
                familySymbol.Activate();

                var oldBox = _document.Create.NewFamilyInstance(intersectionCenter, familySymbol, wall,
                    StructuralType.NonStructural);

                oldBox.LookupParameter("Отверстие_Ширина").Set(minWidth);
                oldBox.LookupParameter("Отверстие_Высота").Set(minHeight);

                transaction.Commit();
            }
        }

        private double CalculateMinSize(double wallWidth, double angle, double ductWidth, double offset) =>
            wallWidth / Math.Tan(angle) + ductWidth / Math.Sin(angle) + offset/304.8;

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

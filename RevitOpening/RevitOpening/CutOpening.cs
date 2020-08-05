using System;
using System.Windows;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            var allDocs = commandData.Application.Application.Documents/*.Cast<Document>()*/;
            //var secondDoc = allDocs.ElementAt(1);
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
            var famName1 = "Отверстие_Круглое_Стена";
            var famName2 = "Отверстие_Прямоуг_Перекр";
            var famName3 = "Отверстие_Прямоуг_Перекр_БезЗаливки";
            var famName4 = "Отверстие_Прямоуг_Стена";
            var famName5 = "Задание_Стена_Прямоугольник_БезОсновы";
            var famName6 = "Задание_Круглая_Стена_БезОсновы";
            var famName7 = "Задание_Стена_Перекрытие_БезОсновы";
            var familySymbol = GetApertureSymbol(famName5, BuiltInCategory.OST_Windows);


            var offset = 100;
            var geomSolid = wall.get_Geometry(new Options()).FirstOrDefault() as Solid;
            var wallData = new ElementGeometry(wall);
            var pipeData = new ElementGeometry(pipe);

            double pipeWidth, pipeHeight;
            try
            {
                pipeHeight = pipe.Height;
                pipeWidth = pipe.Width;
            }
            catch
            {
                pipeHeight = pipe.Diameter;
                pipeWidth = pipe.Diameter;
            }

            var curves = geomSolid?
                .IntersectWithCurve(pipeData.Curve, new SolidCurveIntersectionOptions());
            if (curves.SegmentCount == 0)
            {
                return;
            }


            var intersectCurve =  curves.GetCurveSegment(0);

            if(intersectCurve==null || familySymbol==null)
                return;
            var intersectionCenter = (intersectCurve.GetEndPoint(0) + intersectCurve.GetEndPoint(1)) / 2;
            intersectionCenter -= new XYZ(0, pipeHeight, 0);
            var horAngleBetweenWallAndDuct =
                Math.Acos((wallData.XLen * pipeData.XLen + wallData.YLen * pipeData.YLen) /
                (SqrtOfSqrSum(wallData.XLen,wallData.YLen) * SqrtOfSqrSum(pipeData.XLen,pipeData.YLen)));
            horAngleBetweenWallAndDuct = GetAcuteAngle(horAngleBetweenWallAndDuct);
            var vertAngleBetweenWallAndDuct =
                Math.Acos(pipeData.ZLen / Math.Sqrt(pipeData.XLen*pipeData.XLen + pipeData.YLen*pipeData.YLen + pipeData.ZLen*pipeData.ZLen));
            vertAngleBetweenWallAndDuct = GetAcuteAngle(vertAngleBetweenWallAndDuct);

            var width = CalculateMinSize(wall.Width, horAngleBetweenWallAndDuct, pipeWidth,offset);
            var height = CalculateMinSize(wall.Width, vertAngleBetweenWallAndDuct, pipeWidth, offset);

            //var direction = , wallData.Curve.
            CreateAperture(intersectionCenter,familySymbol,wall,height,width,pipe.Category.Name);
        }

        private void CreateAperture(XYZ intersectionCenter, FamilySymbol familySymbol, Wall wall,
            double minHeight, double minWidth, string categoryName)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create box");
                familySymbol.Activate();

                var newBox = _document.Create.NewFamilyInstance(intersectionCenter, familySymbol,wall,
                    StructuralType.NonStructural);

                //"Отверстие_Круглое_Стена"
                //newBox.LookupParameter("Размер_Диаметр").Set(Math.Max(minWidth, minHeight));

                //Отверстие_Прямоуг_Перекр
                newBox.LookupParameter("Отверстие_Ширина").Set(minWidth);
                newBox.LookupParameter("Отверстие_Высота").Set(minHeight);

                transaction.Commit();

                //if (transaction.HasEnded())
                //    transaction.Commit();
                //else
                //{
                //    Thread.Sleep(10);
                //    transaction.Commit();
                //}
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

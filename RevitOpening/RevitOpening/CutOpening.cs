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
            new FamilyLoader(_document).LoadAllFamiliesToProject();

            var walls = GetElementsList<Wall>(allDocs);
            var floors = GetElementsList<Floor>(allDocs);
            var pipes = GetElementsList<Pipe>(allDocs);
            var ducts = GetElementsList<Duct>(allDocs);

            var ductsInWalls = FindIntersectionsWith(walls, ducts);
            var pipesInWalls = FindIntersectionsWith(walls, pipes);
            var ductsInFloors = FindIntersectionsWith(floors, ducts);
            var pipesInFloors = FindIntersectionsWith(floors, pipes);

            var wallCutter = new WallCutter();
            var floorCutter = new FloorCutter();

            CreateAllOpening(ductsInWalls, wallCutter, Families.WallRectTaskFamilyData);
            CreateAllOpening(pipesInWalls, wallCutter, Families.WallRectTaskFamilyData);
            CreateAllOpening(ductsInFloors, floorCutter, Families.FloorRectTaskFamilyData);
            CreateAllOpening(pipesInFloors, floorCutter, Families.FloorRectTaskFamilyData);

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

        private Dictionary<Element,List<MEPCurve>> FindIntersectionsWith(IEnumerable<Element> elements, IReadOnlyCollection<MEPCurve> curves)
        {
            var intersections = new Dictionary<Element,List<MEPCurve>>();
            foreach (var intersectionElement in elements)
            {
                var currentInetsections = new List<MEPCurve>();
                var intersection = new ElementIntersectsElementFilter(intersectionElement);
                foreach (var element in curves
                    .Where(el => intersection.PassesFilter(el)))
                    currentInetsections.Add(element);
                if (currentInetsections.Count > 0)
                    intersections[intersectionElement] = currentInetsections;
            }

            return intersections;
        }
        private void CreateAllOpening(Dictionary<Element, List<MEPCurve>> pipesInElements, ICutter cutter, FamilyData familyData)
        {
            var familySymbol = Families.GetFamilySymbol(_document, familyData.Name);
            foreach (var ductsInWall in pipesInElements)
            foreach (var curve in ductsInWall.Value)
            {
                var openingParametrs = cutter.CalculateBoxInElement(ductsInWall.Key, curve, _offset);
                if (openingParametrs == null)
                    continue;
                CreateOpening(familyData, familySymbol, ductsInWall.Key, openingParametrs);
            }

        }

        private void CreateOpening(FamilyData familyData,FamilySymbol familySymbol, Element hostElement, OpeningParametrs parametrs)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create box");
                familySymbol.Activate();
                var newBox = _document.Create.NewFamilyInstance(parametrs.IntersectionCenter, familySymbol,
                    parametrs.Direction, hostElement, StructuralType.NonStructural);
                if (familyData.DiametrParametr != null)
                    newBox.LookupParameter(familyData.DiametrParametr).Set(Math.Max(parametrs.Width, parametrs.Heigth));
                else
                {
                    newBox.LookupParameter(familyData.HeightParametr).Set(parametrs.Heigth);
                    newBox.LookupParameter(familyData.WidthParametr).Set(parametrs.Width);
                }

                newBox.LookupParameter(familyData.DepthParametr).Set(parametrs.Depth);

                transaction.Commit();
            }
        }
    }
}
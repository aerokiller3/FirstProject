using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class CreateTaskBoxes : IExternalCommand
    {
        private Document _document;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            var allDocs = commandData.Application.Application
                .Documents.Cast<Document>().ToList();
            new FamilyLoader(_document).LoadAllFamiliesToProject(); 
            var offset = 300;

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

            CreateAllTaskBoxes(ductsInWalls, wallCutter, Families.WallRectTaskFamily, offset);
            CreateAllTaskBoxes(pipesInWalls, wallCutter, Families.WallRectTaskFamily, offset);
            CreateAllTaskBoxes(ductsInFloors, floorCutter, Families.FloorRectTaskFamily, offset);
            CreateAllTaskBoxes(pipesInFloors, floorCutter, Families.FloorRectTaskFamily, offset);

            return Result.Succeeded;
        }

        public List<T> GetElementsList<T>(IEnumerable<Document> allDocs)
        {
            return allDocs
                .SelectMany(document => new FilteredElementCollector(document)
                .OfClass(typeof(T))
                .Cast<T>())
                .ToList();
        }

        private Dictionary<Element, List<MEPCurve>> FindIntersectionsWith(IEnumerable<Element> elements,
            IEnumerable<MEPCurve> curves)
        {
            var intersections = new Dictionary<Element, List<MEPCurve>>();
            foreach (var intersectionElement in elements)
            {
                var intersection = new ElementIntersectsElementFilter(intersectionElement);
                var currentInetsections = curves
                    .Where(el => intersection.PassesFilter(el))
                    .ToList();
                if (currentInetsections.Count > 0)
                    intersections[intersectionElement] = currentInetsections;
            }

            return intersections;
        }

        private void CreateAllTaskBoxes(Dictionary<Element, List<MEPCurve>> pipesInElements, ICutter cutter,
            FamilyParameters familyParameters, double offset)
        {
            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Create task box");
                var familySymbol = Families.GetFamilySymbol(_document, familyParameters.SymbolName);
                foreach (var ductsInWall in pipesInElements)
                foreach (var curve in ductsInWall.Value)
                {

                    var openingParametrs =
                        cutter.CalculateBoxInElement(ductsInWall.Key, curve, offset, familyParameters);
                    if (openingParametrs == null)
                        continue;
                    var parentsData = new OpeningParentsData(ductsInWall.Key.Id.IntegerValue, curve.Id.IntegerValue,
                        openingParametrs.WallGeometry, openingParametrs.PipeGeometry,ductsInWall.Key.GetType(), curve.GetType());
                    CreateTaskBox(familyParameters, familySymbol, ductsInWall.Key, openingParametrs, parentsData);
                }

                transaction.Commit();
            }
        }

        private void CreateTaskBox(FamilyParameters family, FamilySymbol familySymbol, Element hostElement,
            OpeningParametrs opening, OpeningParentsData parentsData)
        {
            familySymbol.Activate();
            var newBox = _document.Create.NewFamilyInstance(opening.IntersectionCenter, familySymbol,
                opening.Direction, hostElement, StructuralType.NonStructural);
            if (family.DiametrName != null)
            {
                newBox.LookupParameter(family.DiametrName).Set(Math.Max(opening.Width, opening.Heigth));
            }
            else
            {
                newBox.LookupParameter(family.HeightName).Set(opening.Heigth);
                newBox.LookupParameter(family.WidthName).Set(opening.Width);
            }

            newBox.LookupParameter(family.DepthName).Set(opening.Depth);
            parentsData.LocationPoint = (newBox.Location as LocationPoint).Point;

            var json = JsonConvert.SerializeObject(parentsData);
            newBox.LookupParameter("Info").Set(json);
        }
    }
}
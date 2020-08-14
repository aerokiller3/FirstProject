using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class BoxCreator
    {
        public static FamilyInstance CreateTaskBox(OpeningParentsData parentsData, Document document,
            AltecJsonSchema schema)
        {
            var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            var familySymbol = Families.GetFamilySymbol(document, parentsData.BoxData.FamilyName);
            familySymbol.Activate();
            var center = parentsData.BoxData.IntersectionCenter.GetXYZ();
            var direction = parentsData.BoxData.Direction.GetXYZ();
            var host = document.GetElement(new ElementId(parentsData.HostId));
            var newBox = document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);
            if (familyParameters.DiameterName != null)
            {
                newBox.LookupParameter(familyParameters.DiameterName)
                    .Set(Math.Max(parentsData.BoxData.Width, parentsData.BoxData.Height));
            }
            else
            {
                if (familyParameters == Families.FloorRectTaskFamily)
                {
                    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Width);
                    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Height);
                }
                else
                {
                    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
                }
            }

            //newBox.LookupParameter("Несогласованно").Set(0);
            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            parentsData.LocationPoint = new MyXYZ((newBox.Location as LocationPoint).Point);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData, schema);

            return newBox;
        }
    }
}
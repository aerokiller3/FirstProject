#define small
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
            var center = parentsData.BoxData.IntersectionCenter.XYZ;
            var direction = parentsData.BoxData.Direction.XYZ;
            var host = document.GetElement(new ElementId(parentsData.HostId));
            var newBox =
                document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);

            if (familyParameters.DiameterName != null)
            {
                newBox.LookupParameter(familyParameters.DiameterName)
                    .Set(Math.Max(parentsData.BoxData.Width, parentsData.BoxData.Height));
            }
            else
            {
                newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
            }

            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
#if !small
            if (familyParameters.DiameterName != null)
            {
                newBox.LookupParameter(familyParameters.DiameterName)
                    .Set(20.0.GetInFoot());
            }
            else
            {
                newBox.LookupParameter(familyParameters.HeightName).Set(20.0.GetInFoot());
                newBox.LookupParameter(familyParameters.WidthName).Set(20.0.GetInFoot());
            }

            newBox.LookupParameter(familyParameters.DepthName).Set(20.0.GetInFoot());
#endif
            parentsData.LocationPoint = new MyXYZ(((LocationPoint) newBox.Location).Point);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData, schema);

            return newBox;
        }
    }
}
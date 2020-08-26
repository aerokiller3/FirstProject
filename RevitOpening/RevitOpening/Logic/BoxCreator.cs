using System;
using System.Linq;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class BoxCreator
    {
        public static FamilyInstance CreateTaskBox(OpeningParentsData parentsData, Document document)
        {
            var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            var familySymbol = document.GetFamilySymbol(parentsData.BoxData.FamilyName);
            var center = parentsData.BoxData.IntersectionCenter.XYZ;
            var direction = parentsData.BoxData.Direction.XYZ;
            var host = document.GetElement(parentsData.HostsIds.FirstOrDefault());
            var newBox = document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);

            if (familyParameters.DiameterName != null)
            {
                newBox.LookupParameter(familyParameters.DiameterName)
                    .Set(Math.Max(parentsData.BoxData.Width, parentsData.BoxData.Height));
            }
            else
            {
                if (familyParameters == Families.FloorRectOpeningFamily)
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

            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData);

            return newBox;
        }
    }
}
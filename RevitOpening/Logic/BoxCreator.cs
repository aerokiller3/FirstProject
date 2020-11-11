namespace RevitOpening.Logic
{
    using System;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.DB.Structure;
    using Extensions;
    using Models;

    internal static class BoxCreator
    {
        public static FamilyInstance CreateTaskBox(OpeningParentsData parentsData, Document document)
        {
            var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            var familySymbol = document.GetFamilySymbol(parentsData.BoxData.FamilyName);
            var center = parentsData.BoxData.IntersectionCenter.XYZ;
            var direction = parentsData.BoxData.Direction.XYZ;
            var host = document.GetElement(parentsData.HostsIds.FirstOrDefault());
            var newBox =
                document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);

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


            //настроить семейство круглых
            if (newBox.IsTask())
                newBox.LookupParameter("Несогласованно").Set(1);

            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            var rand = new Random();
            parentsData.BoxData.Number = rand.Next(0, 100);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData);

            return newBox;
        }

        public static FamilyInstance CreateCombineTaskBox(OpeningParentsData parentsData, Document document, XYZ cent, double width, double height)
        {
            XYZ center = null;
            var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            var familySymbol = document.GetFamilySymbol(parentsData.BoxData.FamilyName);

            center = new XYZ(parentsData.BoxData.IntersectionCenter.X, parentsData.BoxData.IntersectionCenter.Y, parentsData.BoxData.IntersectionCenter.Z);

            var direction = parentsData.BoxData.Direction.XYZ;
            var host = document.GetElement(parentsData.HostsIds.FirstOrDefault());
            var newBox =
                document.Create.NewFamilyInstance(center, familySymbol, direction, host, StructuralType.NonStructural);

            if (familyParameters.DiameterName != null)
            {
                newBox.LookupParameter(familyParameters.DiameterName)
                      .Set(Math.Max(width, parentsData.BoxData.Height));
            }
            else
            {
                if (familyParameters == Families.FloorRectTaskFamily)
                {
                    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
                    /*
                    if (width != 0 && height != 0)
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(width);
                    }
                    else
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(width);
                    }*/
                }
                //TODO: уменьшить условие
                else
                {
                    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
                    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                }
            }

            if (newBox.IsTask())
                newBox.LookupParameter("Несогласованно").Set(1);

            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            var rand = new Random();
            parentsData.BoxData.Number = rand.Next(0, 100);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData);

            return newBox;
        }
    }
}
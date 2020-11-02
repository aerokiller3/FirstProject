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
                    newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                    newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
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
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData);

            return newBox;
        }

        public static FamilyInstance CreateCombineTaskBox(OpeningParentsData parentsData, Document document, XYZ cent, double width, double height)
        {
            XYZ center = null;
            var familyParameters = Families.GetDataFromSymbolName(parentsData.BoxData.FamilyName);
            var familySymbol = document.GetFamilySymbol(parentsData.BoxData.FamilyName);

            /*center = familyParameters == Families.FloorRectTaskFamily ? new XYZ(cent.X, parentsData.BoxData.IntersectionCenter.Y, parentsData.BoxData.IntersectionCenter.Z) : new XYZ(cent.X, parentsData.BoxData.IntersectionCenter.Y, cent.Z);*/

            //center = new XYZ(cent.X, (cent.Y + parentsData.BoxData.IntersectionCenter.Y) / 2, parentsData.BoxData.IntersectionCenter.Z);

            // TODO: решить всё с объединением отверстий в потолке (размеры и центр)
            if (familyParameters == Families.FloorRectTaskFamily)
            {
                center = new XYZ(cent.X, parentsData.BoxData.IntersectionCenter.Y, parentsData.BoxData.IntersectionCenter.Z);
            }
            else
            {
                center = new XYZ(cent.X, parentsData.BoxData.IntersectionCenter.Y, cent.Z);
            }

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
                if (familyParameters == Families.FloorRectTaskFamily)
                {
                    if (width != 0 && height != 0)
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(width);
                    }
                    else
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
                    }
                }
                //TODO: уменьшить условие
                else
                {
                    if (height == 0 && width != 0)
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(parentsData.BoxData.Height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(width);
                    }
                    else if (width == 0 && height != 0)
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(parentsData.BoxData.Width);
                    }
                    else
                    {
                        newBox.LookupParameter(familyParameters.HeightName).Set(height);
                        newBox.LookupParameter(familyParameters.WidthName).Set(width);
                    }
                }
            }

            if (newBox.IsTask())
                newBox.LookupParameter("Несогласованно").Set(1);

            newBox.LookupParameter(familyParameters.DepthName).Set(parentsData.BoxData.Depth);
            parentsData.BoxData.Id = newBox.Id.IntegerValue;
            newBox.SetParentsData(parentsData);

            return newBox;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json;

namespace RevitOpening
{
    public static class BoxCreator
    {
        public static void CreateTaskBox(FamilyParameters family, FamilySymbol familySymbol, Element hostElement,
            OpeningParametrs opening, OpeningParentsData parentsData, Document document, AltecJsonSchema schema)
        {
            familySymbol.Activate();
            var newBox = document.Create.NewFamilyInstance(
                    new XYZ(opening.IntersectionCenter.X, opening.IntersectionCenter.Y, opening.IntersectionCenter.Z),
                    familySymbol,new XYZ(opening.Direction.X, opening.Direction.Y, opening.Direction.Z),
                    hostElement, StructuralType.NonStructural);
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
            
            if (parentsData!=null)
                parentsData.LocationPoint = new MyXYZ((newBox.Location as LocationPoint).Point);
            var json = JsonConvert.SerializeObject(parentsData);
            schema.SetJson(newBox, json);
        }
    }
}

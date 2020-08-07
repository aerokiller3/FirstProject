using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public interface IBoxCalculator
    {
        OpeningParametrs CalculateBoxInElement(Element element, MEPCurve pipe, double offset, FamilyParameters familyParameters);
    }
}

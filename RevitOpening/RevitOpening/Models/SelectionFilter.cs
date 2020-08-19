using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitOpening.Models
{
    public class SelectionFilter : ISelectionFilter
    {
        private Func<Element, bool> _allowElement;
        private Func<Reference, XYZ, bool> _allowReference;

        public SelectionFilter(Func<Element, bool> allowElement, Func<Reference, XYZ, bool> allowReference)
        {
            _allowElement = allowElement;
            _allowReference = allowReference;
        }

        public bool AllowElement(Element elem)
        {
            return _allowElement.Invoke(elem);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return _allowReference.Invoke(reference, position);
        }
    }
}

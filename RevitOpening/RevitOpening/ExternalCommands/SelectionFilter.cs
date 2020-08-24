using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitOpening.ExternalCommands
{
    public class SelectionFilter : ISelectionFilter
    {
        private readonly Func<Element, bool> _allowElement;
        private readonly Func<Reference, XYZ, bool> _allowReference;

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
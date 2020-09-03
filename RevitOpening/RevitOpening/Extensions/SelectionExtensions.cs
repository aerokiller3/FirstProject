using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using RevitOpening.RevitExternal;
using System.Collections.Generic;
using System.Linq;

namespace RevitOpening.Extensions
{
    public static class SelectionExtensions
    {
        public static List<Element> GetSelectedTasks(this Selection selection, Document document)
        {
            try
            {
                return selection.PickObjects(ObjectType.Element, new SelectionFilter(el => el.IsTask(),
                        (x, _) => true))
                    .Select(document.GetElement)
                    .ToList();
            }
            catch
            {
                return null;
            }
        }
    }
}

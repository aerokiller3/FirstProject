namespace RevitOpening.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;
    using RevitExternal;

    internal static class SelectionExtensions
    {
        public static IEnumerable<Element> GetSelectedTasks(this Selection selection, Document document)
        {
            try
            {
                return selection.PickObjects(ObjectType.Element, new SelectionFilter(el => el.IsTask(),
                                     (x, _) => true))
                                .Select(document.GetElement);
            }
            catch
            {
                return null;
            }
        }
    }
}
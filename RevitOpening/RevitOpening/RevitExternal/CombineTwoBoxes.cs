using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitOpening.Extensions;
using RevitOpening.Logic;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class CombineTwoBoxes : IExternalCommand
    {
        public Document _document;
        public IEnumerable<Document> _documents;


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents.Cast<Document>();

            var select = commandData.Application.ActiveUIDocument.Selection;
            var selected = select.PickObjects(ObjectType.Element, new SelectionFilter(x => x.IsTask(),
                    (x, _) => true))
                .Select(x => _document.GetElement(x))
                .ToArray();

            using (var t = new Transaction(_document, "Unite"))
            {
                t.Start();
                BoxCombiner.CombineTwoBoxes(_documents, _document, selected[0], selected[1]);
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening.EventHandlers
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Logic;
    using Revit.Async.ExternalEvents;

    internal sealed class UpdateTaskInfoEventHandler : SyncGenericExternalEventHandler<object, object>
    {
        public override string GetName()
        {
            return nameof(UpdateTaskInfoEventHandler);
        }

        protected override object Handle(UIApplication app, object obj)
        {
            var currentDocument = app.ActiveUIDocument.Document;
            var documents = app.Application.Documents
                               .Cast<Document>()
                               .ToList();
            Transactions.UpdateTasksInfo(currentDocument, documents, Extensions.Settings.Offset, Extensions.Settings.Diameter);
            return null;
        }
    }
}

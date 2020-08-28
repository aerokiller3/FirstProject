using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using RevitOpening.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace RevitOpening.EventHandlers
{
    public class BoxShowerEventHandler : SyncGenericExternalEventHandler<List<ElementId>, List<OpeningData>>
    {
        public override string GetName()
        {
            return nameof(BoxShowerEventHandler);
        }

        protected override List<OpeningData> Handle(UIApplication app, List<ElementId> selectItems)
        {
            var activeUi = app.ActiveUIDocument;
            activeUi.Selection.SetElementIds(selectItems);

            var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.SelectionBox);
            var appUiType = app.GetType();
            var revitCommandsField = appUiType
                .GetField("sm_revitCommands", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(app);

            var revitCommandsCount = (int)revitCommandsField.GetType()
                .GetProperty("Count")?
                .GetValue(revitCommandsField);

            using (var t = new Transaction(app.ActiveUIDocument.Document, "Test"))
            {
                t.Start();
                while (true)
                {
                    if (revitCommandsCount > 0 || !app.CanPostCommand(commandId))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        return null;
                    }

                    app.PostCommand(commandId);
                    break;
                }

                t.Commit();
            }

            return null;
        }
    }
}

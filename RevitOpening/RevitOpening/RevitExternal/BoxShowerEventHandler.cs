using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using RevitOpening.Models;

namespace RevitOpening.RevitExternal
{
    public class BoxShowerEventHandler : SyncGenericExternalEventHandler<List<ElementId>,List<OpeningData>>
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
            var appUI = app.GetType();
            var field = appUI
                .GetField("sm_revitCommands", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(app);

            var countCommands = (int)field.GetType().GetProperty("Count")?.GetValue(field);

            using (var t = new Transaction(app.ActiveUIDocument.Document, "Test"))
            {
                t.Start();
                while (true)
                {
                    if (countCommands > 0 || !app.CanPostCommand(commandId))
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

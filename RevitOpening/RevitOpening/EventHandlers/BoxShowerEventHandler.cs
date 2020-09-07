namespace RevitOpening.EventHandlers
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Revit.Async.ExternalEvents;

    internal sealed class BoxShowerEventHandler : SyncGenericExternalEventHandler<ICollection<ElementId>, object>
    {
        public override string GetName()
        {
            return nameof(BoxShowerEventHandler);
        }

        protected override object Handle(UIApplication app, ICollection<ElementId> selectItems)
        {
            app.ActiveUIDocument.Selection.SetElementIds(selectItems);
            var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.SelectionBox);
            app.PostCommand(commandId);
            return null;
        }
    }
}
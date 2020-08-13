using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Logic;
using RevitOpening.UI;
using RevitOpening.ViewModels;

namespace RevitOpening
{
    [Transaction(TransactionMode.Manual)]
    public class Program : IExternalCommand
    {
        private Document _document;
        private AltecJsonSchema _schema;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
            _schema = new AltecJsonSchema();
            var localMessage = message;
            var main = new MainControl();
            (main.DataContext as MainVM).Init(commandData, localMessage, elements, _schema);
            var window = new Window
            {
                Title = "Altec Openings",
                Content = main,
                Width = 1000,
                Height = 450,
                MinHeight = 450,
                MinWidth = 1000
            };
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
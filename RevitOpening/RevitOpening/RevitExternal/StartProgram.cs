using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.UI;
using RevitOpening.ViewModels;
using System.Windows;

namespace RevitOpening.RevitExternal
{
    [Transaction(TransactionMode.Manual)]
    public class StartProgram : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var main = new MainControl();
            var window = new Window
            {
                Title = "Альтек Отверстия",
                Content = main,
                Width = 1000,
                MinWidth = 1000,
                Height = 450,
                MinHeight = 450,
            };

            (main.DataContext as MainVM)?.Init(commandData);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
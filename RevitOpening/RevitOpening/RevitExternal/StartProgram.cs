namespace RevitOpening.RevitExternal
{
    using System.Windows;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using UI;
    using ViewModels;

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

            ((MainVM) main.DataContext).Init(commandData);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
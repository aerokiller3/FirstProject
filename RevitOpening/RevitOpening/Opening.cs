using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitOpening
{
    public class Opening : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            var ribbonPanel = application.CreateRibbonPanel("Altec Openings");

            var thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            var buttonData = new PushButtonData("Openings",
                "Openings", thisAssemblyPath, "RevitOpening.Program");

            var pushButton = ribbonPanel.AddItem(buttonData) as PushButton;
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var endIndex = currentDirectory.LastIndexOf('\\');
            var curDir = currentDirectory.Substring(0, endIndex);
            var uriImage = new Uri($"{curDir}\\opening.jpg");
            var largeImage = new BitmapImage(uriImage);
            pushButton.LargeImage = largeImage;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
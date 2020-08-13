using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using static System.Windows.Interop.Imaging;

namespace RevitOpening
{
    public class Opening : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            var ribbonPanel = application.CreateRibbonPanel("Altec Openings");
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var buttonData = new PushButtonData("Openings",
                "Openings", currentDirectory, "RevitOpening.Program");
            var pushButton = ribbonPanel.AddItem(buttonData) as PushButton;
            var image = Properties.Resources.opening;
            pushButton.LargeImage = CreateBitmapSourceFromHBitmap(image.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
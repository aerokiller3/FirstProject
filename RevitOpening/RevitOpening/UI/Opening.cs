using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RevitOpening.Properties;
using static System.Windows.Interop.Imaging;

namespace RevitOpening.UI
{
    public class Opening : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            var ribbonPanel = application.CreateRibbonPanel("Altec Tasks");
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var mainButtonData = new PushButtonData("Tasks",
                "Tasks", currentDirectory, "RevitOpening.Program");
            var pushButton = ribbonPanel.AddItem(mainButtonData) as PushButton;
            var image = Resources.opening;
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
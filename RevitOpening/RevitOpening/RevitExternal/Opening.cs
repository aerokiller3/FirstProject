using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RevitOpening.Properties;
using static System.Windows.Interop.Imaging;

namespace RevitOpening.RevitExternal
{
    public class Opening : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string moduleName = "Альтек Отверстия";
            application.CreateRibbonTab(moduleName);
            var ribbonPanel = application.CreateRibbonPanel(moduleName, moduleName);
            var currentDirectory = Assembly.GetExecutingAssembly().Location;
            var mainButtonData = new PushButtonData("Create Tasks",
                "Create Tasks", currentDirectory, "RevitOpening.ExternalCommands.StartProgram");
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
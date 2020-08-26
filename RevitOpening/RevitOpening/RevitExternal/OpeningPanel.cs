using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RevitOpening.Properties;
using RevitOpening.UI;
using RevitOpening.ViewModels;
using static System.Windows.Interop.Imaging;

namespace RevitOpening.RevitExternal
{
    public class OpeningPanel : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string altecSystems = "Altec Systems";
            const string moduleName = "Отверстия";
            RibbonPanel panel;
            try
            {
                application.CreateRibbonTab(altecSystems);
            }
            catch
            {
                // ignored
            }

            try
            {
                panel = application.CreateRibbonPanel(altecSystems, moduleName);
            }
            catch
            {
                panel = application.GetRibbonPanels(altecSystems).First(x => x.Name == moduleName);
            }

            var currentDirectory = Assembly.GetExecutingAssembly().Location;

            var startButtonData = new PushButtonData("CreateTasks",
                "Открыть модуль", currentDirectory, "RevitOpening.RevitExternal.StartProgram");
            var moduleStartButton = panel.AddItem(startButtonData) as PushButton;
            var startImage = Resources.opening;
            moduleStartButton.LargeImage = CreateBitmapSourceFromHBitmap(startImage.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
            var combineButtonData = new PushButtonData("CombineTasks",
                "Объединить два задания", currentDirectory, "RevitOpening.RevitExternal.CombineTwoBoxes");
            var combineButton = panel.AddItem(combineButtonData) as PushButton;
            var combineImage = Resources.Unite;
            combineButton.LargeImage = CreateBitmapSourceFromHBitmap(combineImage.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
            var createOpeningData = new PushButtonData("CreateOpening",
                "Создать отверстие", currentDirectory, "RevitOpening.RevitExternal.ChangeSelectedTasksToOpenings");
            var createOpeningButton = panel.AddItem(createOpeningData) as PushButton;
            var createOpeningImage = Resources.createOp;
            createOpeningButton.LargeImage = CreateBitmapSourceFromHBitmap(createOpeningImage.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));

            return Result.Succeeded;
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
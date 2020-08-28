using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Properties;
using RevitOpening.UI;
using RevitOpening.ViewModels;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using static System.Windows.Interop.Imaging;

namespace RevitOpening.RevitExternal
{
    public class OpeningPanel : IExternalApplication
    {
        public const string DockablePanelGuid = "{C2D5D9FF-FCD4-4387-B6CE-B5D4DEDF2637}";
        private TasksDockablePanel _tasksDockableWindow;

        public Result OnStartup(UIControlledApplication application)
        {
            var panel = CreateRibbonPanel(application);
            var currentDirectory = Assembly.GetExecutingAssembly().Location;

            RegisterDockableWindow(application);
            AddButtonOnPanel(panel, currentDirectory, "StartProgram", "Открыть модуль",
                Resources.StartProgram);
            AddButtonOnPanel(panel, currentDirectory, "CombineTwoBoxes", "Объединить два задания",
                Resources.CombineBoxes);
            AddButtonOnPanel(panel, currentDirectory, "ChangeSelectedTasksToOpenings", "Создать отверстие",
                Resources.CreateOpenings);
            AddButtonOnPanel(panel, currentDirectory, "UpdateDockablePanel", "Обновить списпок",
                Resources.List);

            return Result.Succeeded;
        }

        private RibbonPanel CreateRibbonPanel(UIControlledApplication application)
        {
            const string altecSystems = "Altec Systems";
            const string moduleName = "Отверстия";
            try
            {
                application.CreateRibbonTab(altecSystems);
            }
            catch
            {
            }

            try
            {
                return application.CreateRibbonPanel(altecSystems, moduleName);
            }
            catch
            {
                return application.GetRibbonPanels(altecSystems).First(x => x.Name == moduleName);
            }
        }

        private void RegisterDockableWindow(UIControlledApplication uiApplication)
        {
            var data = new DockablePaneProviderData();

            _tasksDockableWindow = new TasksDockablePanel();
            data.FrameworkElement = _tasksDockableWindow;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ElementView,
            };

            var paneId = new DockablePaneId(new Guid(DockablePanelGuid));

            uiApplication.RegisterDockablePane(paneId, "Список заданий на отверстия", _tasksDockableWindow);
            uiApplication.DockableFrameVisibilityChanged += UpdateDockablePanel;
        }

        private void UpdateDockablePanel(object sender, EventArgs e)
        {
            var app = sender as UIApplication;
            (_tasksDockableWindow.DataContext as TaskDockablePanelVM)
                .UpdateList(app.Application.Documents.Cast<Document>());
        }


        private void AddButtonOnPanel(RibbonPanel panel, string directory, string buttonName, string buttonText,
            Bitmap image, string availabilityClass = null)
        {
            var startButtonData = new PushButtonData(buttonName,
                buttonText, directory, $"RevitOpening.RevitExternal.{buttonName}")
            {
                LargeImage = CreateBitmapSourceFromHBitmap(image.GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32)),
                AvailabilityClassName = availabilityClass
            };

            panel.AddItem(startButtonData);
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitOpening.Annotations;
using RevitOpening.EventHandlers;
using RevitOpening.Extensions;
using RevitOpening.Models;
using RevitOpening.RevitExternal;
using RevitOpening.UI;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace RevitOpening.ViewModels
{
    public class TaskDockablePanelVM : INotifyPropertyChanged
    {
        private IRevitTask CurrentRevitTask { get; set; }
        public List<OpeningData> TasksAndOpenings { get; set; }
        public TasksDockablePanel Window { get; set; }

        public void UpdateList(IEnumerable<Document> documents)
        {
            var tasks = documents.GetAllTasks()
                .Select(t => t.GetParentsData().BoxData)
                .ToList();
            var openings = documents.GetAllOpenings()
                .Select(op => op.GetParentsData().BoxData)
                .ToList();
            TasksAndOpenings = new List<OpeningData>(tasks.Count + openings.Count);
            TasksAndOpenings.AddRange(tasks);
            TasksAndOpenings.AddRange(openings);

            RevitTask.Initialize();
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        public void OnCurrentCellChanged(object sender, EventArgs e)
        {
            CurrentRevitTask = new RevitTask();
            CurrentRevitTask.Register(new BoxShowerEventHandler());
            var selectItems = (Window.TasksGrid)
                .GetSelectedItemsFromGrid<OpeningData>()
                .Select(x => new ElementId(x.Id))
                .ToList();

            ShowBoxCommand(selectItems);
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private void ShowBoxCommand(List<ElementId> selectItems)
        {
            try
            {
                CurrentRevitTask.Raise<BoxShowerEventHandler, List<ElementId>,
                    List<OpeningData>>(selectItems);
            }
            catch (Exception)
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using Autodesk.Revit.DB;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitOpening.Annotations;
using RevitOpening.EventHandlers;
using RevitOpening.Extensions;
using RevitOpening.Models;
using RevitOpening.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RevitOpening.ViewModels
{
    public class TaskDockablePanelVM : INotifyPropertyChanged
    {
        private IRevitTask CurrentRevitTask { get; set; }
        public List<OpeningData> TasksAndOpenings { get; set; }
        public TasksDockablePanel Window { get; set; }

        public void UpdateTaskDockablePanel(IEnumerable<Document> documents)
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
            catch
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

namespace RevitOpening.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Controls;
    using Annotations;
    using Autodesk.Revit.DB;
    using EventHandlers;
    using Extensions;
    using Logic;
    using Models;
    using Revit.Async;
    using Revit.Async.Interfaces;
    using Settings = Extensions.Settings;

    public class TaskDockablePanelVM : INotifyPropertyChanged, IDataGridUpdater
    {
        private Document _currentDocument;

        private List<Document> _documents;
        private IRevitTask CurrentRevitTask { get; set; }
        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }

        public void OnCurrentCellChanged(object sender, EventArgs e)
        {
            CurrentRevitTask = new RevitTask();
            CurrentRevitTask.Register(new BoxShowerEventHandler());
            var selectItems = (sender as DataGrid)
                             .GetSelectedItemsFromGrid<OpeningData>()
                             .Select(x => new ElementId(x.Id))
                             .ToList();

            ShowBoxCommand(selectItems);
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateList(List<Document> documents, Document currentDocument)
        {
            _documents = documents;
            _currentDocument = currentDocument;

            UpdateTasksAndOpenings();
            RevitTask.Initialize();
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private void UpdateTasksAndOpenings()
        {
            try
            {
                UpdateTasks();
                UpdateOpenings();
            }
            catch (ArgumentNullException)
            {
                Transactions.UpdateTasksInfo(_currentDocument, _documents, Settings.Offset, Settings.Diameter);
                UpdateTasks();
                UpdateOpenings();
            }

            TasksAndOpenings = new List<OpeningData>(Tasks.Count + Openings.Count);
            TasksAndOpenings.AddRange(Tasks);
            TasksAndOpenings.AddRange(Openings);
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private void UpdateTasks()
        {
            Tasks = _documents.GetAllTasks()
                              .Select(t => t.GetParentsData().BoxData)
                              .ToList();
        }

        private void UpdateOpenings()
        {
            Openings = _documents.GetAllOpenings()
                                 .Select(op => op.GetParentsData().BoxData)
                                 .ToList();
        }

        private void ShowBoxCommand(List<ElementId> selectItems)
        {
            try
            {
                CurrentRevitTask.Raise<BoxShowerEventHandler, List<ElementId>, object>(selectItems);
            }
            catch (Exception)
            {
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
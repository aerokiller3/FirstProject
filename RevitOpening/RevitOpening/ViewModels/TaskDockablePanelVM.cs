namespace RevitOpening.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using Annotations;
    using Autodesk.Revit.DB;
    using EventHandlers;
    using Extensions;
    using LoggerClient;
    using Logic;
    using Models;
    using Revit.Async;
    using Revit.Async.Interfaces;
    using Settings = Extensions.Settings;

    public class TaskDockablePanelVM : INotifyPropertyChanged, IDataGridUpdater
    {
        private Document _currentDocument;

        private List<Document> _documents;
        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; } = new List<OpeningData>();
        public List<OpeningData> Openings { get; set; } = new List<OpeningData>();

        public void ShowItemFromGrid(object sender, EventArgs e)
        {
            ModuleLogger.SendFunctionUseData(nameof(ShowItemFromGrid), nameof(RevitOpening));
            var selectItems = (sender as DataGrid)
                             .GetSelectedItemsFromGrid<OpeningData>()
                             .Select(x => new ElementId(x.Id))
                             .ToList();
            var items = selectItems
                       .Select(i => _documents.GetElement(i.IntegerValue))
                       .Where(el => el != null)
                       .ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Выбранный элемент удалён.\n" +
                    "Обновите текущий список.");
                return;
            }

            RevitTask.RaiseGlobal<BoxShowerEventHandler, List<ElementId>, object>(selectItems);


            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateList(List<Document> documents, Document currentDocument)
        {
            _documents = documents;
            _currentDocument = currentDocument;
            RevitTask.RegisterGlobal(new BoxShowerEventHandler());
            RevitTask.Initialize();

            UpdateTasksAndOpenings();
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private void UpdateTasksAndOpenings()
        {
            try
            {
                UpdateTasks();
                UpdateOpenings();
            }
            catch
            {
            }

            TasksAndOpenings = new List<OpeningData>(Tasks.Count + Openings.Count);
            TasksAndOpenings.AddRange(Tasks);
            TasksAndOpenings.AddRange(Openings);
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private void UpdateTasks()
        {
            Tasks = _documents.GetAllTasks()
                              .Select(t => t.GetParentsDataFromSchema().BoxData)
                              .ToList();
        }

        private void UpdateOpenings()
        {
            Openings = _documents.GetAllOpenings()
                                 .Select(op => op.GetParentsDataFromSchema().BoxData)
                                 .ToList();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
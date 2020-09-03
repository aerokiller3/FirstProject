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
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using RevitOpening.Logic;

namespace RevitOpening.ViewModels
{
    public class TaskDockablePanelVM : INotifyPropertyChanged
    {
        private IRevitTask CurrentRevitTask { get; set; }
        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }
        public TasksDockablePanel Window { get; set; }

        private const string offsetStr = "1.5";
        private const string diameterStr = "200";
        private IEnumerable<Document> _documents;
        private Document _currentDocument;

        public double Offset
        {
            get => double.Parse(GetParameterFromSettings(nameof(Offset), offsetStr),
                NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public double Diameter
        {
            get => double.Parse(GetParameterFromSettings(nameof(Diameter), diameterStr),
                NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public void UpdateList(IEnumerable<Document> documents, Document currentDocument)
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
                Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);
                UpdateTasks();
                UpdateOpenings();
            }

            TasksAndOpenings = new List<OpeningData>(Tasks.Count + Openings.Count);
            TasksAndOpenings.AddRange(Tasks);
            TasksAndOpenings.AddRange(Openings);
            OnPropertyChanged(nameof(TasksAndOpenings));
        }

        private string GetParameterFromSettings(string parameterName, object defaultValue = null)
        {
            return ConfigurationManager.AppSettings[parameterName] ??
                   (ConfigurationManager.AppSettings[parameterName] = defaultValue?.ToString());
        }

        private void SetParameterToSettings(string parameterName, object value)
        {
            ConfigurationManager.AppSettings[parameterName] = value.ToString();
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

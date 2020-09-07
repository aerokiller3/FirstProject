﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Annotations;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using RevitOpening.Models;
using RevitOpening.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RevitOpening.ViewModels
{
    public class MainVM : INotifyPropertyChanged, IDataGridUpdater
    {
        private RelayCommand _changeSelectedTasksToOpenings;
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _combineIntersectsTasks;
        private RelayCommand _filterTasks;
        private RelayCommand _updateTaskInfo;
        private RelayCommand _createAllTasks;

        private const string offsetStr = "1.5";
        private const string diameterStr = "200";
        private ExternalCommandData _commandData;
        private Document _currentDocument;
        private IEnumerable<Document> _documents;

        public bool IsCombineAll
        {
            get => bool.Parse(GetParameterFromSettings(nameof(IsCombineAll), false));
            set => SetParameterToSettings(nameof(IsCombineAll), value);
        }

        public bool IsAnalysisOnStart
        {
            get => bool.Parse(GetParameterFromSettings(nameof(IsAnalysisOnStart), true));
            set => SetParameterToSettings(nameof(IsAnalysisOnStart), value);
        }

        public double Offset
        {
            get => double.Parse(GetParameterFromSettings(nameof(Offset),offsetStr),
                NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public double Diameter
        {
            get => double.Parse(GetParameterFromSettings(nameof(Diameter),diameterStr),
                NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public string OffsetStr
        {
            get => GetParameterFromSettings(nameof(Offset), offsetStr);
            set
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    SetParameterToSettings(nameof(Offset), value);
            }
        }

        public string DiameterStr
        {
            get => GetParameterFromSettings(nameof(Diameter), diameterStr);
            set
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    SetParameterToSettings(nameof(Diameter), value);
            }
        }

        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }

        private string GetParameterFromSettings(string parameterName, object defaultValue = null)
        {
            return ConfigurationManager.AppSettings[parameterName] ??
                   (ConfigurationManager.AppSettings[parameterName] = defaultValue?.ToString());
        }

        private void SetParameterToSettings(string parameterName, object value)
        {
            ConfigurationManager.AppSettings[parameterName] = value.ToString();
        }

        public RelayCommand CombineIntersectsTasks
        {
            get
            {
                return _combineIntersectsTasks ??
                       (_combineIntersectsTasks = new RelayCommand(obj =>
                       {
                           Transactions.CombineIntersectsTasks(_currentDocument, _documents);
                           Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);
                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public RelayCommand UpdateTaskInfo
        {
            get
            {
                return _updateTaskInfo ??
                       (_updateTaskInfo = new RelayCommand(obj =>
                       {
                           Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);
                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public RelayCommand FilterTasks
        {
            get
            {
                return _filterTasks ??
                       (_filterTasks = new RelayCommand(obj =>
                       {
                           var control = new FilterStatusControl();
                           var dialogWindow = new Window
                           {
                               MinWidth = 500,
                               MaxWidth = 500,
                               MinHeight = 200,
                               MaxHeight = 200,
                               Title = "Выбор фильтра",
                               Content = control
                           };
                           ((FilterStatusVM)control.DataContext).HostWindow = dialogWindow;
                           dialogWindow.ShowDialog();
                           var type = ((FilterStatusVM)control.DataContext).SelectStatus;
                           if (string.IsNullOrEmpty(type))
                           {
                               UpdateTasksAndOpenings();
                           }
                           else
                           {
                               var filtered = Tasks
                                   .Where(t => t.Collisions.Contains(type))
                                   .ToList();
                               filtered.AddRange(Openings.Where(o => o.Collisions.Contains(type)));
                               TasksAndOpenings = filtered;
                               OnPropertyChanged(nameof(TasksAndOpenings));
                           }
                       }));
            }
        }

        public RelayCommand ChangeSelectedTasksToOpenings
        {
            get
            {
                return _changeSelectedTasksToOpenings ??
                       (_changeSelectedTasksToOpenings = new RelayCommand(obj =>
                       {
                           var taskId = GetSelectedElementsFromDocument();
                           if (taskId.Count == 0)
                               return;

                           var tasks = taskId
                               .Select(t => _currentDocument.GetElement(t))
                               .ToList();
                           if (!tasks.IsOnlyTasks())
                           {
                               MessageBox.Show("Выберите только задания");
                               return;
                           }

                           var openings = new List<Element>();

                           Transactions.CreateOpeningInSelectedTask(_currentDocument, openings, tasks);
                           Transactions.Drawing(_currentDocument, openings);

                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public RelayCommand CreateAllTasks
        {
            get
            {
                return _createAllTasks ??
                       (_createAllTasks = new RelayCommand(obj =>
                           {
                               Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);
                               UpdateTasksAndOpenings();
                               Transactions.CreateAllTasks(_currentDocument, _documents, Offset,
                                   Diameter, Tasks, Openings);
                               if (IsCombineAll)
                                   Transactions.CombineIntersectsTasks(_currentDocument, _documents);
                               Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);
                               UpdateTasksAndOpenings();
                           },
                           obj => double.TryParse(OffsetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                                  && double.TryParse(DiameterStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _)));
            }
        }

        public RelayCommand ChangeTasksToOpening
        {
            get
            {
                return _changeTasksToOpenings ??
                       (_changeTasksToOpenings = new RelayCommand(obj =>
                       {
                           var openings = new List<Element>();
                           Transactions.SwapAllTasksToOpenings(_currentDocument, openings);
                           Transactions.Drawing(_currentDocument, openings);

                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void OnCurrentCellChanged(object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            var selectItems = grid.GetSelectedItemsFromGrid<OpeningData>()
                .Select(x => new ElementId(x.Id))
                .ToList();
            if (selectItems.Count != 1)
                return;

            _commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectItems);
            _commandData.Application.ActiveUIDocument.ShowElements(selectItems);
        }

        public void Init(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _currentDocument = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                .Cast<Document>();
            if (bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]))
                Transactions.UpdateTasksInfo(_currentDocument, _documents, Offset, Diameter);

            UpdateTasksAndOpenings();
        }

        private List<ElementId> GetSelectedElementsFromDocument()
        {
            return _commandData.Application.ActiveUIDocument.Selection
                .GetElementIds()
                .ToList();
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
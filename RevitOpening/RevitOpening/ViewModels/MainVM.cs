namespace RevitOpening.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using Annotations;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Extensions;
    using LoggerClient;
    using Logic;
    using Models;
    using UI;
    using Settings = Extensions.Settings;

    internal class MainVM : INotifyPropertyChanged, IDataGridUpdater
    {
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _combineIntersectsTasks;
        private ExternalCommandData _commandData;
        private RelayCommand _createAllTasks;
        private Document _currentDocument;
        private List<Document> _documents;
        private RelayCommand _filterTasks;
        private RelayCommand _updateTaskInfo;
        public List<OpeningData> TasksAndOpenings { get; set; }
        private List<OpeningData> Tasks { get; set; }
        private List<OpeningData> Openings { get; set; }
        private bool _isListUpdated;

        public bool IsCombineAll
        {
            get => Settings.IsCombineAll;
            set => Settings.IsCombineAll = value;
        }

        public bool IsAnalysisOnStart
        {
            get => Settings.IsAnalysisOnStart;
            set => Settings.IsAnalysisOnStart = value;
        }

        public string OffsetStr
        {
            get => Settings.OffsetStr;
            set => Settings.OffsetStr = value;
        }

        public string DiameterStr
        {
            get => Settings.DiameterStr;
            set => Settings.DiameterStr = value;
        }

        public RelayCommand CombineIntersectsTasks
        {
            get
            {
                return _combineIntersectsTasks ??
                    (_combineIntersectsTasks = new RelayCommand(obj =>
                    {
                        ModuleLogger.SendFunctionUseData(nameof(CombineIntersectsTasks), nameof(RevitOpening));
                        if (!_isListUpdated)
                            UpdateTaskInfo.Execute(null);
                        Transactions.CombineIntersectsTasks(_currentDocument, _documents);
                        UpdateTaskInfo.Execute(null);
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
                        if (!_isListUpdated)
                            Transactions.UpdateTasksInfo(_currentDocument, _documents, Settings.Offset, Settings.Diameter);
                        _isListUpdated = true;
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
                        ModuleLogger.SendFunctionUseData(nameof(FilterTasks), nameof(RevitOpening));
                        var control = new FilterStatusControl();
                        var dialogWindow = new Window
                        {
                            Topmost = true,
                            MinWidth = 500,
                            MaxWidth = 500,
                            MinHeight = 200,
                            MaxHeight = 200,
                            Title = "Выбор фильтра",
                            Content = control,
                        };
                        ((FilterStatusVM) control.DataContext).HostWindow = dialogWindow;
                        dialogWindow.ShowDialog();
                        var type = ((FilterStatusVM) control.DataContext).SelectStatus;
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

        public RelayCommand CreateAllTasks
        {
            get
            {
                return _createAllTasks ??
                    (_createAllTasks = new RelayCommand(obj =>
                        {
                            ModuleLogger.SendFunctionUseData(nameof(CreateAllTasks), nameof(RevitOpening));
                            if (!_isListUpdated)
                                UpdateTaskInfo.Execute(null);

                            Transactions.CreateAllTasks(_currentDocument, _documents, Settings.Offset,
                                Settings.Diameter, Tasks, Openings);
                            if (IsCombineAll)
                                Transactions.CombineIntersectsTasks(_currentDocument, _documents);

                            UpdateTaskInfo.Execute(null);
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
                        ModuleLogger.SendFunctionUseData(nameof(ChangeTasksToOpening), nameof(RevitOpening));
                        var openings = new List<Element>();
                        if (_isListUpdated)
                            UpdateTaskInfo.Execute(null);
                        Transactions.SwapAllTasksToOpenings(_documents, _currentDocument, openings);
                        Transactions.Drawing(_currentDocument, openings);
                        UpdateTaskInfo.Execute(null);
                        UpdateTasksAndOpenings();
                    }));
            }
        }


        public void ShowItemFromGrid(object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            var selectItems = grid.GetSelectedItemsFromGrid<OpeningData>()
                                  .Select(x => new ElementId(x.Id))
                                  .ToList();
            if (selectItems.Count != 1)
                return;

            ModuleLogger.SendFunctionUseData(nameof(ShowItemFromGrid), nameof(RevitOpening));
            _commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectItems);
            _commandData.Application.ActiveUIDocument.ShowElements(selectItems);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _currentDocument = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                                    .Cast<Document>()
                                    .ToList();
            Transactions.LoadFamiliesToProject(_currentDocument);
            if (bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]))
                UpdateTaskInfo.Execute(null);
            _isListUpdated = IsAnalysisOnStart;
            UpdateTasksAndOpenings();
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
                UpdateTaskInfo.Execute(null);
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
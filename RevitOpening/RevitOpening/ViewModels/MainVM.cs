using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Annotations;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using RevitOpening.Models;
using RevitOpening.UI;

namespace RevitOpening.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        private RelayCommand _changeSelectedTaskToOpening;
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _combineTwoBoxes;
        private RelayCommand _createAllTasks;
        private RelayCommand _showCurrentTask;
        private RelayCommand _updateTaskInfo;
        private RelayCommand _filterTasks;

        private Document _document;
        private IEnumerable<Document> _documents;
        private ExternalCommandData _commandData;
        private ElementSet _elements;
        private string _message;

        public string OffsetRatio { get; set; } = "1,5";
        public string Diameter { get; set; } = "200";
        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }
        public bool CombineAll { get; set; }


        public void OnCurrentCellChanged(object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            var selectItems = grid.SelectedItems
                .Cast<OpeningData>()
                .Select(el => new ElementId(el.Id.Value))
                .ToList();
            if (selectItems.Count == 0)
                return;

            _commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectItems);
            _commandData.Application.ActiveUIDocument.ShowElements(selectItems);
        }

        public RelayCommand UpdateTaskInfo
        {
            get
            {
                return _updateTaskInfo ??
                       (_updateTaskInfo = new RelayCommand(obj =>
                       {
                           AnalyzeTasks();
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
                               Width = 200,
                               MinWidth = 200,
                               MinHeight = 200,
                               Height = 200,
                               MaxHeight = 300,
                               MaxWidth = 300,
                               Title = "Выбор фильтра",
                               Content = control
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
                               TasksAndOpenings = Tasks
                                   .Where(t => t.Collisions.ListOfCollisions
                                       .Contains(type))
                                   .ToList();
                               OnPropertyChanged(nameof(TasksAndOpenings));
                           }
                       }));
            }
        }

        public RelayCommand CombineTwoBoxes
        {
            get
            {
                return _combineTwoBoxes ??
                       (_combineTwoBoxes = new RelayCommand(obj =>
                       {
                           var boxCombiner = new BoxCombiner(_document, _documents);
                           var tasksId = GetSelectedElements();
                           if (tasksId.Length != 2)
                           {
                               MessageBox.Show("Выберите 2 задания");
                               return;
                           }

                           var tasks = tasksId
                               .Select(t => _documents.GetElement(t.IntegerValue))
                               .ToArray();
                           if (!IsOnlyTasksSelected(tasks))
                               return;

                           var t1 = tasks[0].GetParentsData();
                           var t2 = tasks[1].GetParentsData();
                           var isValidPair = boxCombiner.ValidateTasksForCombine(t1, t2);
                           if (!isValidPair)
                           {
                               MessageBox.Show("Данные задания невозможно объеденить автоматически");
                               return;
                           }

                           boxCombiner.CreateUnitedTask(tasks[0], tasks[1]);

                           AnalyzeTasks();
                           UpdateTasks();
                       }));
            }
        }

        public RelayCommand ChangeSelectedTaskToOpening
        {
            get
            {
                return _changeSelectedTaskToOpening ??
                       (_changeSelectedTaskToOpening = new RelayCommand(obj =>
                       {
                           var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes(_document, _documents);
                           var taskId = GetSelectedElements().ToList();
                           if (taskId.Count == 0)
                               return;

                           var tasks = taskId
                               .Select(t => _documents.GetElement(t.IntegerValue));
                           if (!IsOnlyTasksSelected(tasks))
                               return;

                           createOpeningInTaskBoxes.SetTasksParameters(OffsetRatio, Diameter);
                           createOpeningInTaskBoxes.SwapTasksToOpenings(tasks);
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
                               var createTask = new CreateTaskBoxes();
                               createTask.SetTasksParameters(OffsetRatio, Diameter, CombineAll, Tasks, Openings);
                               createTask.Execute(_commandData, ref _message, _elements);
                               AnalyzeTasks();
                               UpdateTasksAndOpenings();
                           },
                           obj => double.TryParse(OffsetRatio, out _) && double.TryParse(Diameter, out _)));
            }
        }

        public RelayCommand ShowCurrentTask
        {
            get
            {
                return _showCurrentTask ??
                       (_showCurrentTask = new RelayCommand(obj =>
                       {
                           var grid = obj as DataGrid;
                           var selectItems = grid.SelectedItems
                               .Cast<OpeningData>()
                               .Select(el => new ElementId(el.Id.Value))
                               .ToList();
                           if (selectItems.Count == 0)
                               return;

                           _commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectItems);
                           _commandData.Application.ActiveUIDocument.ShowElements(selectItems);
                       }));
            }
        }

        public RelayCommand ChangeTasksToOpening
        {
            get
            {
                return _changeTasksToOpenings ??
                       (_changeTasksToOpenings = new RelayCommand(obj =>
                       {
                           var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes();
                           createOpeningInTaskBoxes.SetTasksParameters(OffsetRatio, Diameter);
                           createOpeningInTaskBoxes.Execute(_commandData, ref _message, _elements);
                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(ExternalCommandData commandData, string message, ElementSet elements)
        {
            _commandData = commandData;
            _message = message;
            _elements = elements;
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                .Cast<Document>();
            UpdateTasksAndOpenings();
        }

        private void AnalyzeTasks()
        {
            new CollisionAnalyzer(_document, _documents).ExecuteAnalysis();
        }

        private ElementId[] GetSelectedElements()
        {
            return _commandData.Application.ActiveUIDocument.Selection
                .GetElementIds()
                .ToArray();
        }

        private bool IsOnlyTasksSelected(IEnumerable<Element> tasks)
        {
            if (tasks.All(t => Families.AllFamilies
                .FirstOrDefault(f =>
                    f.SymbolName == (t as FamilyInstance).Symbol.FamilyName) != null))
                return true;

            MessageBox.Show("Выберите только задания");
            return false;
        }

        private void UpdateTasksAndOpenings()
        {
            UpdateTasks();
            UpdateOpenings();
            TasksAndOpenings = new List<OpeningData>(Tasks.Count+Openings.Count);
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
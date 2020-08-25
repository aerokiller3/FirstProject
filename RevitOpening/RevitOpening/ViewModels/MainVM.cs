using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Annotations;
using RevitOpening.Extensions;
using RevitOpening.Logic;
using RevitOpening.Models;
using RevitOpening.Properties;
using RevitOpening.RevitExternal;
using RevitOpening.UI;

namespace RevitOpening.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        private RelayCommand _changeSelectedTaskToOpening;
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _combineTwoBoxes;
        private RelayCommand _createAllTasks;
        private RelayCommand _updateTaskInfo;
        private RelayCommand _filterTasks;

        private Document _currentDocument;
        private IEnumerable<Document> _documents;
        private ExternalCommandData _commandData;

        public bool IsAnalysisOnStart
        {
            get
            {
                if (ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] == null)
                    ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] = false.ToString();

                return bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]);
            }
            set
            {
                ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] = value.ToString();
            }
        }

        public string OffsetRatio { get; set; } = "1,5";
        public string Diameter { get; set; } = "200";
        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }
        public bool IsCombineAll { get; set; }


        public void OnCurrentCellChanged(object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            var selectItems = GetSelectedItemsFromGrid(grid).ToList();
            if (selectItems.Count != 1)
                return;

            _commandData.Application.ActiveUIDocument.Selection.SetElementIds(selectItems);
            var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.SelectionBox);
            var appUI = _commandData.Application.GetType();
            var field = appUI
                .GetField("sm_revitCommands",BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(_commandData.Application);
            var countCommands = (int)field.GetType().GetProperty("Count")?.GetValue(field);
            using (var t = new Transaction(_currentDocument, "Test"))
            {
                t.Start();
                while (true)
                {
                    if (countCommands > 0 || !_commandData.Application.CanPostCommand(commandId))
                        return;

                    _commandData.Application.PostCommand(commandId);
                    break;
                }

                t.Commit();
            }
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
                           var tasksId = GetSelectedElementsFromDocument();
                           if (tasksId.Length != 2)
                           {
                               MessageBox.Show("Выберите 2 задания");
                               return;
                           }

                           var tasks = tasksId
                               .Select(t => _currentDocument.GetElement(t))
                               .ToArray();
                           if (!tasks.IsOnlyTasks())
                           {
                               MessageBox.Show("Выберите только задания");
                               return;
                           }

                           var t1 = tasks[0].GetParentsData();
                           var t2 = tasks[1].GetParentsData();
                           var isValidPair = BoxCombiner.ValidateTasksForCombine(_documents, t1, t2);
                           if (!isValidPair)
                           {
                               MessageBox.Show("Данные задания невозможно объеденить автоматически");
                               return;
                           }

                           BoxCombiner.CombineTwoBoxes(_documents, _currentDocument,tasks[0], tasks[1]);

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
                           var maxDiameter = double.Parse(Diameter);
                           var offset = double.Parse(OffsetRatio);
                           var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes(_currentDocument, _documents, maxDiameter, offset);
                           var taskId = GetSelectedElementsFromDocument().ToList();
                           if (taskId.Count == 0)
                               return;

                           var tasks = taskId
                               .Select(t =>_currentDocument.GetElement(t))
                               .ToList();
                           if (!tasks.IsOnlyTasks())
                           {
                               MessageBox.Show("Выберите только задания");
                               return;
                           }

                           createOpeningInTaskBoxes.SwapTasksToOpenings(tasks.Cast<FamilyInstance>());
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
                               using (var t = new Transaction(_currentDocument, "Создание заданий"))
                               {
                                   t.Start();
                                   var createTask = new CreateTaskBoxes(OffsetRatio, Diameter,
                                       Tasks, Openings, _currentDocument, _documents);
                                   createTask.Execute();
                                   t.Commit();
                               }


                               if (IsCombineAll)
                                   using (var t = new Transaction(_currentDocument, "Объединение заданий"))
                                   {
                                       t.Start();
                                       BoxCombiner.CombineAllBoxes(_documents, _currentDocument);
                                       t.Commit();
                                   }

                               using (var t = new Transaction(_currentDocument, "Анализ заданий"))
                               {
                                   t.Start();
                                   AnalyzeTasks();
                                   t.Commit();
                               }

                               UpdateTasksAndOpenings();
                           },
                           obj => double.TryParse(OffsetRatio, out _) && double.TryParse(Diameter, out _)));
            }
        }

        public RelayCommand ChangeTasksToOpening
        {
            get
            {
                return _changeTasksToOpenings ??
                       (_changeTasksToOpenings = new RelayCommand(obj =>
                       {
                           var maxDiameter = double.Parse(Diameter);
                           var offset = double.Parse(OffsetRatio);
                           var createOpenings = new CreateOpeningInTaskBoxes(_currentDocument, _documents, maxDiameter, offset);
                           createOpenings.SwapAllTasksToOpenings();
                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _currentDocument = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                .Cast<Document>();
            if (bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]))
                AnalyzeTasks();
            UpdateTasksAndOpenings();
        }

        private void AnalyzeTasks()
        {
            var offset = double.Parse(OffsetRatio);
            var maxDiameter = double.Parse(Diameter);
            CollisionAnalyzer.ExecuteAnalysis(_documents,_currentDocument, offset, maxDiameter);
        }

        private IEnumerable<ElementId> GetSelectedItemsFromGrid(DataGrid grid)
        {
            return grid.SelectedItems
                .Cast<OpeningData>()
                .Select(el => new ElementId(el.Id));
        }

        private ElementId[] GetSelectedElementsFromDocument()
        {
            return _commandData.Application.ActiveUIDocument.Selection
                .GetElementIds()
                .ToArray();
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
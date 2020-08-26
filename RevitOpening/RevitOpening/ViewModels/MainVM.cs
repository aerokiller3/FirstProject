using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        private RelayCommand _changeSelectedTasksToOpenings;
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _combineIntersectsTasks;
        private RelayCommand _filterTasks;
        private RelayCommand _updateTaskInfo;
        private RelayCommand _createAllTasks;

        private double _offset = 1.5;
        private double _diameter = 200;
        private string _offsetStr = "1.5";
        private string _diameterStr = "200";
        private ExternalCommandData _commandData;
        private Document _currentDocument;
        private IEnumerable<Document> _documents;

        public List<OpeningData> TasksAndOpenings { get; set; }
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }

        public bool IsCombineAll
        {
            get
            {
                if (ConfigurationManager.AppSettings[nameof(IsCombineAll)] == null)
                    ConfigurationManager.AppSettings[nameof(IsCombineAll)] = false.ToString();

                return bool.Parse(ConfigurationManager.AppSettings[nameof(IsCombineAll)]);
            }
            set => ConfigurationManager.AppSettings[nameof(IsCombineAll)] = value.ToString();
        }

        public bool IsAnalysisOnStart
        {
            get
            {
                if (ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] == null)
                    ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] = true.ToString();

                return bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]);
            }
            set => ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)] = value.ToString();
        }

        public RelayCommand CombineIntersectsTasks
        {
            get
            {
                return _combineIntersectsTasks ??
                       (_combineIntersectsTasks = new RelayCommand(obj =>
                       {
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
                       }));
            }
        }

        public string Offset
        {
            get => _offsetStr;
            set
            {
                _offsetStr = value;
                double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture,out _offset);
            }
        }

        public string Diameter
        {
            get => _diameterStr;
            set
            {
                _diameterStr = value;
                double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _diameter);
            }
        }

        public RelayCommand UpdateTaskInfo
        {
            get
            {
                return _updateTaskInfo ??
                       (_updateTaskInfo = new RelayCommand(obj =>
                       {
                           using (var t = new Transaction(_currentDocument, "Анализ заданий"))
                           {
                               t.Start();
                               AnalyzeTasks();
                               t.Commit();
                           }

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

        public RelayCommand ChangeSelectedTasksToOpenings
        {
            get
            {
                return _changeSelectedTasksToOpenings ??
                       (_changeSelectedTasksToOpenings = new RelayCommand(obj =>
                       {
                           var maxDiameter = _diameter;
                           var offset = _offset;
                           var createOpeningInTaskBoxes =
                               new CreateOpeningInTaskBoxes(_currentDocument, _documents, maxDiameter, offset);
                           var taskId = GetSelectedElementsFromDocument().ToList();
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
                           using (var t = new Transaction(_currentDocument, "Change selected tasks to opening"))
                           {
                               t.Start();
                               openings.AddRange(
                                   createOpeningInTaskBoxes.SwapTasksToOpenings(tasks.Cast<FamilyInstance>()));
                               t.Commit();
                           }

                           using (var t = new Transaction(_currentDocument, "Drawing"))
                           {
                               t.Start();
                               foreach (var el in openings)
                               {
                                   var v = el.LookupParameter("Отверстие_Дисциплина").AsString();
                                   el.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                                   el.LookupParameter("Отверстие_Дисциплина").Set(v);
                               }

                               t.Commit();
                           }

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
                               using (var t = new Transaction(_currentDocument, "Анализ заданий"))
                               {
                                   t.Start();
                                   AnalyzeTasks();
                                   t.Commit();
                               }

                               UpdateTasksAndOpenings();
                               using (var t = new Transaction(_currentDocument, "Создание заданий"))
                               {
                                   t.Start();
                                   var offset = _offset;
                                   var diameter = _diameter;
                                   var createTask = new CreateTaskBoxes(Tasks, Openings, _currentDocument,
                                       _documents,diameter,offset);
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
                           obj => double.TryParse(Offset, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                                  && double.TryParse(Diameter,NumberStyles.Any, CultureInfo.InvariantCulture,out _)));
            }
        }

        public RelayCommand ChangeTasksToOpening
        {
            get
            {
                return _changeTasksToOpenings ??
                       (_changeTasksToOpenings = new RelayCommand(obj =>
                       {
                           var maxDiameter = _diameter;
                           var offset = _offset;
                           var createOpenings =
                               new CreateOpeningInTaskBoxes(_currentDocument, _documents, maxDiameter, offset);
                           var openings = new List<Element>();
                           using (var t = new Transaction(_currentDocument, "Change tasks to opening"))
                           {
                               t.Start();
                               openings.AddRange(createOpenings.SwapAllTasksToOpenings());
                               t.Commit();
                           }

                           using (var t = new Transaction(_currentDocument, "Drawing"))
                           {
                               t.Start();
                               foreach (var el in openings)
                               {
                                   var v = el.LookupParameter("Отверстие_Дисциплина").AsString();
                                   el.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                                   el.LookupParameter("Отверстие_Дисциплина").Set(v);
                               }

                               t.Commit();
                           }

                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


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
                .GetField("sm_revitCommands", BindingFlags.NonPublic | BindingFlags.Static)
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

            //_commandData.Application.ActiveUIDocument.ShowElements(selectItems);
        }

        public void Init(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _currentDocument = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                .Cast<Document>();
            if (bool.Parse(ConfigurationManager.AppSettings[nameof(IsAnalysisOnStart)]))
            {
                using (var t = new Transaction(_currentDocument,"Анализ заданий"))
                {
                    t.Start();
                    AnalyzeTasks();
                    t.Commit();
                }
            }

            UpdateTasksAndOpenings();
        }

        private void AnalyzeTasks()
        {
            var offset = _offset;
            var maxDiameter = _diameter;
            BoxAnalyzer.ExecuteAnalysis(_documents, _currentDocument, offset, maxDiameter);
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
            try
            {
                UpdateTasks();
                UpdateOpenings();
            }
            catch (ArgumentNullException)
            {
                AnalyzeTasks();
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
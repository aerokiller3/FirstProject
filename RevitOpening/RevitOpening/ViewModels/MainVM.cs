﻿using System;
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
            get => bool.Parse(GetParameterFromSettings(nameof(IsCombineAll), false));
            set => ConfigurationManager.AppSettings[nameof(IsCombineAll)] = value.ToString();
        }

        public bool IsAnalysisOnStart
        {
            get => bool.Parse(GetParameterFromSettings(nameof(IsAnalysisOnStart), true));
            set => SetParameterToSettings(nameof(IsAnalysisOnStart), value);
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

                           Transactions.Analysis(_currentDocument,_documents,_offset,_diameter);
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
                           Transactions.Analysis(_currentDocument, _documents, _offset, _diameter);
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
                               new CreateOpeningInTaskBoxes(_currentDocument);
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
                               Transactions.Analysis(_currentDocument,_documents,_offset,_diameter);
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

                               Transactions.Analysis(_currentDocument, _documents, _offset, _diameter);

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
                               new CreateOpeningInTaskBoxes(_currentDocument);
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
            var selectItems = grid.GetSelectedItemsFromGrid<OpeningData>()
                .Select(x=>new ElementId(x.Id))
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
                Transactions.Analysis(_currentDocument,_documents,_offset,_diameter);

            UpdateTasksAndOpenings();
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
                Transactions.Analysis(_currentDocument, _documents,_offset,_diameter);
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
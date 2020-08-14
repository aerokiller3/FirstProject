using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Annotations;
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
        private RelayCommand _filterTasks;

        private ExternalCommandData _commandData;
        private Document _document;
        private IEnumerable<Document> _documents;
        private ElementSet _elements;
        private string _message;
        private AltecJsonSchema _schema;

        public string Offset { get; set; } = "200";
        public string Diameter { get; set; } = "200";
        public List<OpeningData> Tasks { get; set; }
        public List<OpeningData> Openings { get; set; }
        public bool CombineAll { get; set; }

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
                           (control.DataContext as FilterStatusVM).HostWindow = dialogWindow;
                           dialogWindow.ShowDialog();
                           var type = (control.DataContext as FilterStatusVM).SelectStatus;
                           if (string.IsNullOrEmpty(type))
                           {
                               UpdateTasks();
                           }
                           else
                           {
                               Tasks = Tasks
                                   .Where(t => t.Collisions.ListOfCollisions
                                       .Contains(type))
                                   .ToList();
                               OnPropertyChanged(nameof(Tasks));
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
                           var boxCombiner = new BoxCombiner(_document, _schema);
                           var tasksId = GetSelectedElements();
                           if (tasksId.Length != 2)
                           {
                               MessageBox.Show("Выберите 2 задания");
                               return;
                           }

                           var tasks = tasksId
                               .Select(t => t.GetElement(_documents))
                               .ToArray();
                           if (!IsOnlyTasksSelected(tasks))
                               return;

                           if (tasks[0].GetParentsData(_schema).HostId != tasks[1].GetParentsData(_schema).HostId)
                           {
                               MessageBox.Show("У заданий должен быть общий хост элемент");
                               return;
                           }

                           using (var t = new Transaction(_document))
                           {
                               t.Start("United tasks");
                               boxCombiner.CreateUnitedTask(tasks[0], tasks[1]);
                               t.Commit();
                           }

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
                           var taskId = GetSelectedElements();
                           if (taskId.Length == 0)
                               return;

                           var tasks = taskId
                               .Select(t => t.GetElement(_documents));
                           if (!IsOnlyTasksSelected(tasks))
                               return;

                           createOpeningInTaskBoxes.SetTasksParameters(Offset, Diameter);
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
                               createTask.SetTasksParameters(Offset, Diameter, CombineAll, Tasks, Openings);
                               createTask.Execute(_commandData, ref _message, _elements);
                               AnalyzeTasks();
                               UpdateTasks();
                           },
                           obj => double.TryParse(Offset, out _) && double.TryParse(Diameter, out _)));
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
                           createOpeningInTaskBoxes.SetTasksParameters(Offset, Diameter);
                           createOpeningInTaskBoxes.Execute(_commandData, ref _message, _elements);
                           UpdateTasksAndOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(ExternalCommandData commandData, string message, ElementSet elements, AltecJsonSchema schema)
        {
            _commandData = commandData;
            _message = message;
            _elements = elements;
            _document = commandData.Application.ActiveUIDocument.Document;
            _documents = commandData.Application.Application.Documents
                .Cast<Document>();
            _schema = schema;
            AnalyzeTasks();
            UpdateTasksAndOpenings();
        }

        private void AnalyzeTasks()
        {
            var elements = new List<FamilyInstance>();
            elements.AddRange(_document
                .GetTasksFromDocument(Families.FloorRectTaskFamily)
                .Cast<FamilyInstance>());
            elements.AddRange(_document
                .GetTasksFromDocument(Families.WallRectTaskFamily)
                .Cast<FamilyInstance>());
            elements.AddRange(_document
                .GetTasksFromDocument(Families.WallRoundTaskFamily)
                .Cast<FamilyInstance>());

            new CollisionAnalyzer(_document, elements, _documents).ExecuteAnalysis();
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
        }

        private void UpdateTasks()
        {
            Tasks = new List<OpeningData>();
            UpdateElementsCollection(Tasks, Families.FloorRectTaskFamily);
            UpdateElementsCollection(Tasks, Families.WallRectTaskFamily);
            UpdateElementsCollection(Tasks, Families.WallRoundTaskFamily);
            OnPropertyChanged(nameof(Tasks));
        }

        private void UpdateOpenings()
        {
            Openings = new List<OpeningData>();
            UpdateElementsCollection(Openings, Families.WallRectOpeningFamily);
            UpdateElementsCollection(Openings, Families.FloorRectOpeningFamily);
            UpdateElementsCollection(Openings, Families.WallRoundOpeningFamily);
            OnPropertyChanged(nameof(Openings));
        }

        private void UpdateElementsCollection(List<OpeningData> collection, FamilyParameters familyType)
        {
            var elements = _document.GetTasksFromDocument(familyType);
            collection.AddRange(elements.Select(el => el.GetParentsData(_schema).BoxData));
            OnPropertyChanged(nameof(collection));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
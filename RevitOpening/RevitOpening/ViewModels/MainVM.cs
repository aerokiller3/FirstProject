using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Annotations;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        private RelayCommand _changeTasksToOpenings;
        private RelayCommand _createAllTasks;
        private RelayCommand _showCurrentTask;
        private RelayCommand _changeSelectedTaskToOpening;

        private ExternalCommandData _commandData;
        private ElementSet _elements;
        private string _message;

        public Document Document { get; set; }
        public AltecJsonSchema Schema { get; set; }
        public string Offset { get; set; } = "200";
        public string Diameter { get; set; } = "200";
        public List<OpeningData> Openings { get; set; }
        public bool CombineAll { get; set; }

        public RelayCommand ChangeSelectedTaskToOpening
        {
            get
            {
                return _changeSelectedTaskToOpening ??
                       (_changeSelectedTaskToOpening = new RelayCommand(obj =>
                       {
                           var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes(Document,
                               _commandData.Application.Application.Documents.Cast<Document>());
                           var task = _commandData.Application.ActiveUIDocument.Selection
                                   .GetElementIds()
                                   .FirstOrDefault()
                                   .GetElement(_commandData.Application.Application.Documents
                                       .Cast<Document>())
                               as FamilyInstance;
                           createOpeningInTaskBoxes.SetTasksParametrs(Offset,Diameter);
                           createOpeningInTaskBoxes.SwapTasksToOpenings(new []{task});
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
                               createTask.SetTasksParametrs(Offset, Diameter, CombineAll);
                               createTask.Execute(_commandData, ref _message, _elements);
                               InitOpenings();
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
                           createOpeningInTaskBoxes.SetTasksParametrs(Offset, Diameter);
                           createOpeningInTaskBoxes.Execute(_commandData, ref _message, _elements);
                           InitOpenings();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(ExternalCommandData commandData, string messge, ElementSet elements, AltecJsonSchema schema)
        {
            _commandData = commandData;
            _message = messge;
            _elements = elements;
            Document = commandData.Application.ActiveUIDocument.Document;
            Schema = schema;
            InitOpenings();
        }

        private void InitOpenings()
        {
            var t1 = Document.GetTasksFromDocument(Families.FloorRectTaskFamily);
            var t2 = Document.GetTasksFromDocument(Families.WallRectTaskFamily);
            var t3 = Document.GetTasksFromDocument(Families.WallRoundTaskFamily);
            Openings = new List<OpeningData>();
            Openings.AddRange(t1.Select(el => el.GetParentsData(Schema).BoxData));
            Openings.AddRange(t2.Select(el => el.GetParentsData(Schema).BoxData));
            Openings.AddRange(t3.Select(el => el.GetParentsData(Schema).BoxData));
            OnPropertyChanged("Openings");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
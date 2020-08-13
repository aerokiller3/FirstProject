using System.Collections;
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
        private ExternalCommandData _commandData;
        private ElementSet _elements;
        private string _message;
        private RelayCommand _createAllTasks;

        public RelayCommand CreateAllTasks
        {
            get
            {
                return _createAllTasks ??
                       (_createAllTasks = new RelayCommand(obj =>
                           {
                               var createTask = new CreateTaskBoxes();
                               createTask.SetTasksParametrs(Offset, Diametr);
                               createTask.Execute(_commandData, ref _message, _elements);
                               InitOpenings();
                           },
                           obj => double.TryParse(Offset, out _) && double.TryParse(Diametr, out _)));
            }
        }

        private RelayCommand _showCurrentTask;

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
                           _commandData.Application.ActiveUIDocument.ShowElements(selectItems);
                       }));
            }
        }

        public Document Document { get; set; }
        public AltecJsonSchema Schema { get; set; }
        public string Offset { get; set; } = "200";
        public string Diametr { get; set; } = "200";
        public List<OpeningData> Openings { get; set; }

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
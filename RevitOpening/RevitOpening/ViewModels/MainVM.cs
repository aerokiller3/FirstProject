using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using RevitOpening.Annotations;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        public Document Document { get; set; }
        public AltecJsonSchema Schema { get; set; }

        public List<OpeningData> Openings { get; set; }

        public List<LevelInfo> Levels { get; set; }

        public MainVM()
        {
        }

        public void Init(Document document, AltecJsonSchema schema)
        {
            Document = document;
            Schema = schema;
            var t1 = Document.GetTasksFromDocument(Families.FloorRectTaskFamily);
            var t2 = Document.GetTasksFromDocument(Families.WallRectTaskFamily);
            var t3 = Document.GetTasksFromDocument(Families.WallRoundTaskFamily);
            Openings = new List<OpeningData>();
            Openings.AddRange(t1.Select(el => el.GetParentsData(Schema).BoxData));
            Openings.AddRange(t2.Select(el => el.GetParentsData(Schema).BoxData));
            Openings.AddRange(t3.Select(el => el.GetParentsData(Schema).BoxData));
            OnPropertyChanged("Openings");
            Levels = Document
                .GetAllLevels()
                .Select(l => new LevelInfo(l))
                .ToList();
            OnPropertyChanged("Levels");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

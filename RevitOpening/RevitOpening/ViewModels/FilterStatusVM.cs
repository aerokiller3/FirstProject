using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using RevitOpening.Annotations;
using RevitOpening.Logic;

namespace RevitOpening.ViewModels
{
    public class FilterStatusVM : INotifyPropertyChanged
    {
        private RelayCommand _filter;

        public Window HostWindow { get; set; }

        public List<string> Statuses { get; set; } = new List<string>
        {
            Collisions.FloorTaskIntersectWall,
            Collisions.PipeNotPerpendicularHost,
            Collisions.TaskIntersectTask,
            Collisions.WallTaskIntersectFloor,
            Collisions.TaskIntersectManyWalls,
            Collisions.TaskNotActual,
            Collisions.TaskCouldNotBeProcessed,
        };

        public string SelectStatus { get; private set; }

        public RelayCommand Filter
        {
            get
            {
                return _filter ??
                       (_filter = new RelayCommand(obj =>
                       {
                           var comboBox = obj as ComboBox;
                           SelectStatus = comboBox.SelectionBoxItem as string;
                           HostWindow.Close();
                       }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
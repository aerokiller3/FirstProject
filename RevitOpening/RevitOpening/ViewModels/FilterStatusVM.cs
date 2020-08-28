using RevitOpening.Logic;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RevitOpening.ViewModels
{
    public class FilterStatusVM
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

        public string SelectStatus { get; set; }

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
    }
}
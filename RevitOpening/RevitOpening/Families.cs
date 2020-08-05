using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening
{
    public static class Families
    {
        public static TOFamily WallRoundOpeningFamily;
        public static TOFamily FloorRectOpeningFamily;
        public static TOFamily WallRectOpeningFamily;
        public static TOFamily WallRectTaskFamily;
        public static TOFamily WallRoundTaskFamily;
        public static TOFamily FloorRectTaskFamily;


        static Families()
        {
            WallRoundOpeningFamily =
                new TOFamily("Отверстие_Круглое_Стена", "ТолщинаОсновы", null,null, "Размер_Диаметр");
            FloorRectOpeningFamily =
                new TOFamily("Отверстие_Прямоуг_Перекр", "Размер_Толщина основа", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectOpeningFamily =
                new TOFamily("Отверстие_Прямоуг_Стена", "ТолщинаОсновы", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectTaskFamily =
                new TOFamily("Задание_Стена_Прямоугольник_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
            WallRoundTaskFamily =
                new TOFamily("Задание_Круглая_Стена_БезОсновы", "ТолщинаОсновы", null,null, "Диаметр отверстия");
            FloorRectTaskFamily =
                new TOFamily("Задание_Стена_Перекрытие_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public static class Families
    {
        public static readonly FamilyParameters WallRoundOpeningFamily;
        public static readonly FamilyParameters FloorRectOpeningFamily;
        public static readonly FamilyParameters WallRectOpeningFamily;
        public static readonly FamilyParameters WallRectTaskFamily;
        public static readonly FamilyParameters WallRoundTaskFamily;
        public static readonly FamilyParameters FloorRectTaskFamily;

        public static readonly IEnumerable<FamilyParameters> AllFamilies;

        static Families()
        {
            WallRoundOpeningFamily =
                new FamilyParameters("Отверстие_Круглое_Стена", "ТолщинаОсновы",
                    "Круглое отверстие", null, null, "Размер_Диаметр");
            FloorRectOpeningFamily =
                new FamilyParameters("Отверстие_Прямоуг_Перекр", "Размер_Толщина основа",
                    "Отверстие", "Отверстие_Высота", "Отверстие_Ширина", null);
            WallRectOpeningFamily =
                new FamilyParameters("Отверстие_Прямоуг_Стена", "ТолщинаОсновы",
                    "Отверстие", "Отверстие_Высота", "Отверстие_Ширина", null);
            WallRectTaskFamily =
                new FamilyParameters("Задание_Стена_Прямоугольник_БезОсновы", "Отверстие_Глубина",
                    "Задание_Стена_Прямоугольное", "Отверстие_Высота", "Отверстие_Ширина", null);
            WallRoundTaskFamily =
                new FamilyParameters("Задание_Круглая_Стена_БезОсновы", "ТолщинаОсновы",
                    "Круглое отверстие", null, null, "Диаметр отверстия");
            FloorRectTaskFamily =
                new FamilyParameters("Задание_Перекрытие_БезОсновы", "Отверстие_Глубина",
                    "Задание_Перекрытие", "Отверстие_Высота", "Отверстие_Ширина", null);
            AllFamilies = new List<FamilyParameters>
            {
                WallRoundOpeningFamily, FloorRectOpeningFamily,
                WallRectOpeningFamily, WallRectTaskFamily,
                WallRoundTaskFamily, FloorRectTaskFamily
            };
        }

        public static FamilyParameters GetDataFromSymbolName(string familyName)
        {
            return AllFamilies.FirstOrDefault(f => f.SymbolName == familyName);
        }

        public static FamilyParameters GetDataFromInstanceName(string familyName)
        {
            return AllFamilies.FirstOrDefault(f => f.InstanceName == familyName);
        }
    }
}
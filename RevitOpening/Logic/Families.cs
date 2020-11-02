namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    public static class Families
    {
        // Добавил овальные трубы
        public static readonly FamilyParameters WallRoundOpeningFamily;
        public static readonly FamilyParameters FloorRectOpeningFamily;
        public static readonly FamilyParameters WallRectOpeningFamily;
        public static readonly FamilyParameters WallElipticalOpeningFamily;
        public static readonly FamilyParameters WallRectTaskFamily;
        public static readonly FamilyParameters WallRoundTaskFamily;
        public static readonly FamilyParameters FloorRectTaskFamily;
        public static readonly FamilyParameters WallElipticalTaskFamily;


        public static readonly HashSet<FamilyParameters> AllFamilies;
        public static readonly HashSet<string> AllFamiliesNames;

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
            WallElipticalOpeningFamily =
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
            WallElipticalTaskFamily = 
                new FamilyParameters("Задание_Стена_Прямоугольник_БезОсновы", "Отверстие_Глубина",
                    "Задание_Стена_Прямоугольное", "Отверстие_Высота", "Отверстие_Ширина", null);

            AllFamilies = new HashSet<FamilyParameters>
            {
                WallRoundOpeningFamily, FloorRectOpeningFamily,
                WallRectOpeningFamily, WallElipticalOpeningFamily,
                WallRectTaskFamily, WallRoundTaskFamily,
                FloorRectTaskFamily, WallElipticalTaskFamily,
            };
            AllFamiliesNames = new HashSet<string>
            {
                WallRoundOpeningFamily.SymbolName, FloorRectOpeningFamily.SymbolName,
                WallRectOpeningFamily.SymbolName, WallElipticalOpeningFamily.SymbolName,
                WallRectTaskFamily.SymbolName, WallRoundTaskFamily.SymbolName,
                FloorRectTaskFamily.SymbolName, WallElipticalTaskFamily.SymbolName,
            };
        }

        public static FamilyParameters ChooseOpeningFamily(this FamilyParameters taskFamily)
        {
            FamilyParameters familyData;
            if (taskFamily == WallRoundTaskFamily)
                familyData = WallRoundOpeningFamily;
            else if (taskFamily == WallRectTaskFamily)
                familyData = WallRectOpeningFamily;
            else if (taskFamily == WallElipticalTaskFamily)
                familyData = WallElipticalOpeningFamily;
            else if (taskFamily == FloorRectTaskFamily)
                familyData = FloorRectOpeningFamily;
            else
                throw new Exception("Неизвестный экземпляр семейства");

            return familyData;
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
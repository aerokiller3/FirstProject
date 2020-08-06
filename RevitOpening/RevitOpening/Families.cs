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
        public static FamilyParameters WallRoundOpeningFamily;
        public static FamilyParameters FloorRectOpeningFamily;
        public static FamilyParameters WallRectOpeningFamily;
        public static FamilyParameters WallRectTaskFamily;
        public static FamilyParameters WallRoundTaskFamily;
        public static FamilyParameters FloorRectTaskFamily;

        public static IEnumerable<FamilyParameters> AllFamilies;

        static Families()
        {
            WallRoundOpeningFamily =
                new FamilyParameters("Отверстие_Круглое_Стена", "ТолщинаОсновы", null,null, "Размер_Диаметр");
            FloorRectOpeningFamily =
                new FamilyParameters("Отверстие_Прямоуг_Перекр", "Размер_Толщина основа", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectOpeningFamily =
                new FamilyParameters("Отверстие_Прямоуг_Стена", "ТолщинаОсновы", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectTaskFamily =
                new FamilyParameters("Задание_Стена_Прямоугольник_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
            WallRoundTaskFamily =
                new FamilyParameters("Задание_Круглая_Стена_БезОсновы", "ТолщинаОсновы", null,null, "Диаметр отверстия");
            FloorRectTaskFamily =
                new FamilyParameters("Задание_Перекрытие_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
            AllFamilies = new List<FamilyParameters>
            {
                WallRoundOpeningFamily, FloorRectOpeningFamily,
                WallRectOpeningFamily, WallRectTaskFamily,
                WallRoundTaskFamily, FloorRectTaskFamily
            };
        }

        public static FamilyParameters GetFamilyData(string familyName)
        {
            return AllFamilies.FirstOrDefault(f => f.Name == familyName);
        }

        public static FamilySymbol GetFamilySymbol(Document document, string familyName)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows);

            var familySymbol = collector
                .Cast<FamilySymbol>()
                .FirstOrDefault(x => x.FamilyName == familyName);
            if(familySymbol==null)
                throw new Exception("Невозможно найти семейство");
            collector.Dispose();
            return familySymbol;
        }
    }
}

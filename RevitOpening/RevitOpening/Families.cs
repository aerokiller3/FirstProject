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
        public static FamilyData WallRoundOpeningFamilyData;
        public static FamilyData FloorRectOpeningFamilyData;
        public static FamilyData WallRectOpeningFamilyData;
        public static FamilyData WallRectTaskFamilyData;
        public static FamilyData WallRoundTaskFamilyData;
        public static FamilyData FloorRectTaskFamilyData;

        public static IEnumerable<FamilyData> AllFamilies;

        static Families()
        {
            WallRoundOpeningFamilyData =
                new FamilyData("Отверстие_Круглое_Стена", "ТолщинаОсновы", null,null, "Размер_Диаметр");
            FloorRectOpeningFamilyData =
                new FamilyData("Отверстие_Прямоуг_Перекр", "Размер_Толщина основа", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectOpeningFamilyData =
                new FamilyData("Отверстие_Прямоуг_Стена", "ТолщинаОсновы", "Отверстие_Высота", "Отверстие_Ширина");
            WallRectTaskFamilyData =
                new FamilyData("Задание_Стена_Прямоугольник_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
            WallRoundTaskFamilyData =
                new FamilyData("Задание_Круглая_Стена_БезОсновы", "ТолщинаОсновы", null,null, "Диаметр отверстия");
            FloorRectTaskFamilyData =
                new FamilyData("Задание_Перекрытие_БезОсновы", "Отверстие_Глубина", "Отверстие_Высота", "Отверстие_Ширина");
            AllFamilies = new List<FamilyData>
            {
                WallRoundOpeningFamilyData, FloorRectOpeningFamilyData,
                WallRectOpeningFamilyData, WallRectTaskFamilyData,
                WallRoundTaskFamilyData, FloorRectTaskFamilyData
            };
        }

        public static FamilyData GetFamilyData(string familyName)
        {
            return AllFamilies.FirstOrDefault(f => f.Name == familyName);
        }

        public static FamilySymbol GetFamilySymbol(Document document, string familyName)
        {
            var collector = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows);

            var familySymbol = collector
                .ToElements()
                .Cast<FamilySymbol>()
                .FirstOrDefault(x => x.FamilyName == familyName);
            if(familySymbol==null)
                throw new Exception("Невозможно найти семейство");
            collector.Dispose();
            return familySymbol;
        }
    }
}

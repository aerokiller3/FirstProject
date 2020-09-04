using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using System.Collections.Generic;

namespace RevitOpening.Logic
{
    public class CreateOpeningInTaskBoxes
    {
        private readonly Document _document;

        public CreateOpeningInTaskBoxes(Document document)
        {
            _document = document;
        }

        public List<Element> SwapAllTasksToOpenings()
        {
            FamilyLoader.LoadAllFamiliesToProject(_document);

            var wallRectTasks = _document.GetTasksByName(Families.WallRectTaskFamily);
            var wallRoundTasks = _document.GetTasksByName(Families.WallRoundTaskFamily);
            var floorRectTasks = _document.GetTasksByName(Families.FloorRectTaskFamily);

            var (correctWallRectTasks, incorrectWallRectTasks) = GetCheckedBoxes(wallRectTasks);
            var (correctWallRoundTasks, incorrectWallRoundTasks) = GetCheckedBoxes(wallRoundTasks);
            var (correctFloorRectTasks, incorrectFloorRectTasks) = GetCheckedBoxes(floorRectTasks);

            var elementList = new List<Element>();
            elementList.AddRange(SwapTasksToOpenings(correctWallRectTasks));
            elementList.AddRange(SwapTasksToOpenings(correctWallRoundTasks));
            elementList.AddRange(SwapTasksToOpenings(correctFloorRectTasks));
            return elementList;
        }

        public List<Element> SwapTasksToOpenings(IEnumerable<FamilyInstance> tasks)
        {
            var elementList = new List<Element>();
            foreach (var task in tasks)
            {
                var familyData = Families.GetDataFromSymbolName(task.Symbol.FamilyName).ChooseOpeningFamily();
                var parentsData = task.GetParentsData();
                parentsData.BoxData.FamilyName = familyData.SymbolName;
                _document.Delete(task.Id);
                elementList.Add(BoxCreator.CreateTaskBox(parentsData, _document));
            }

            return elementList;
        }

        private (IEnumerable<FamilyInstance>, IEnumerable<FamilyInstance>) GetCheckedBoxes(IEnumerable<FamilyInstance> wallRectTasks)
        {
            var correctTasks = new List<FamilyInstance>();
            var incorrectTasks = new List<FamilyInstance>();
            foreach (var task in wallRectTasks)
            {
                //
                var data = task.GetParentsData();
                if (CheckAgreed(task) &&
                    (data.BoxData.Collisions.Count == 0 || data.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed)))
                    correctTasks.Add(task);
                else
                    incorrectTasks.Add(task);

                //Изменить на спец. атрибут
                //task.LookupParameter("Несогласованно").Set(0);
            }


            return (correctTasks, incorrectTasks);
        }

        private bool CheckAgreed(FamilyInstance box)
        {
            var agreedParameter = box.LookupParameter("Несогласованно");
            //Проверку спец. атрибута
            var intAgreedParameter = agreedParameter.AsInteger();
            return intAgreedParameter == 0;
        }
    }
}
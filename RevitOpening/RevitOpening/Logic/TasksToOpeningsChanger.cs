namespace RevitOpening.Logic
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Extensions;
    using Models;

    internal static class TasksToOpeningsChanger
    {
        public static List<Element> SwapAllTasksToOpenings(Document currentDocument)
        {
            var wallRectTasks = currentDocument.GetTasksByName(Families.WallRectTaskFamily);
            var wallRoundTasks = currentDocument.GetTasksByName(Families.WallRoundTaskFamily);
            var floorRectTasks = currentDocument.GetTasksByName(Families.FloorRectTaskFamily);

            (var correctWallRectTasks, var incorrectWallRectTasks) = GetCheckedBoxes(wallRectTasks);
            (var correctWallRoundTasks, var incorrectWallRoundTasks) = GetCheckedBoxes(wallRoundTasks);
            (var correctFloorRectTasks, var incorrectFloorRectTasks) = GetCheckedBoxes(floorRectTasks);

            var elementList = new List<Element>();
            elementList.AddRange(SwapTasksToOpenings(currentDocument, correctWallRectTasks));
            elementList.AddRange(SwapTasksToOpenings(currentDocument, correctWallRoundTasks));
            elementList.AddRange(SwapTasksToOpenings(currentDocument, correctFloorRectTasks));
            return elementList;
        }

        public static List<Element> SwapTasksToOpenings(Document currentDocument, IEnumerable<FamilyInstance> tasks)
        {
            var elementList = new List<Element>();
            foreach (var task in tasks)
            {
                var familyData = Families.GetDataFromSymbolName(task.Symbol.FamilyName).ChooseOpeningFamily();
                var parentsData = task.GetParentsData();
                parentsData.BoxData.FamilyName = familyData.SymbolName;
                currentDocument.Delete(task.Id);
                elementList.Add(BoxCreator.CreateTaskBox(parentsData, currentDocument));
            }

            return elementList;
        }

        private static (IEnumerable<FamilyInstance>, IEnumerable<FamilyInstance>) GetCheckedBoxes(
            IEnumerable<FamilyInstance> wallRectTasks)
        {
            var correctTasks = new List<FamilyInstance>();
            var incorrectTasks = new List<FamilyInstance>();
            foreach (var task in wallRectTasks)
            {
                var data = task.GetParentsData();
                if (CheckAgreed(task, data) &&
                    (data.BoxData.Collisions.Count == 0 ||
                        data.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed)))
                    correctTasks.Add(task);
                else
                    incorrectTasks.Add(task);

                //Изменить на спец. атрибут
                //task.LookupParameter("Несогласованно").Set(0);
            }


            return (correctTasks, incorrectTasks);
        }

        private static bool CheckAgreed(FamilyInstance box, OpeningParentsData data)
        {
            var agreedParameter = box.LookupParameter("Несогласованно").AsInteger();
            //Проверку спец. атрибута

            return agreedParameter == 0 &&
                !data.BoxData.Collisions.Contains(Collisions.TaskCouldNotBeProcessed);
        }
    }
}
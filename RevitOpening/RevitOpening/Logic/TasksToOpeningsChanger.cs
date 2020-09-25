namespace RevitOpening.Logic
{
    using System.Collections.Generic;
    using Autodesk.Revit.DB;
    using Extensions;
    using Models;

    internal static class TasksToOpeningsChanger
    {
        public static List<Element> SwapAllTasksToOpenings(ICollection<Document> documents, Document currentDocument)
        {
            var wallRectTasks = documents.GetTasksByName(Families.WallRectTaskFamily);
            var wallRoundTasks = documents.GetTasksByName(Families.WallRoundTaskFamily);
            var floorRectTasks = documents.GetTasksByName(Families.FloorRectTaskFamily);

            var correctWallRectTasks = GetCheckedBoxes(wallRectTasks);
            var correctWallRoundTasks = GetCheckedBoxes(wallRoundTasks);
            var correctFloorRectTasks = GetCheckedBoxes(floorRectTasks);

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
                var familyData = Families.GetDataFromSymbolName(task.Symbol.FamilyName)
                                         .ChooseOpeningFamily();
                var parentsData = task.GetParentsDataFromSchema();
                parentsData.BoxData.FamilyName = familyData.SymbolName;

                var createdEl = BoxCreator.CreateTaskBox(parentsData, currentDocument);

                if (createdEl == null)
                    continue;

                currentDocument.Delete(task.Id);
                elementList.Add(createdEl);
            }

            return elementList;
        }

        private static IEnumerable<FamilyInstance> GetCheckedBoxes(
            IEnumerable<FamilyInstance> wallRectTasks)
        {
            foreach (var task in wallRectTasks)
            {
                var data = task.GetParentsDataFromSchema();
                if (CheckAgreed(task, data) &&
                    (data.BoxData.Collisions.Count == 0 ||
                        data.BoxData.Collisions.IsTaskCouldNotBeProcessed))
                    yield return task;

                //Изменить на спец. атрибут
                //task.LookupParameter("Несогласованно").Set(0);
            }
        }

        private static bool CheckAgreed(FamilyInstance box, OpeningParentsData data)
        {
            var agreedParameter = box.LookupParameter("Несогласованно").AsInteger();
            //Проверку спец. атрибута

            return agreedParameter == 0 &&
                !data.BoxData.Collisions.IsTaskCouldNotBeProcessed;
        }
    }
}
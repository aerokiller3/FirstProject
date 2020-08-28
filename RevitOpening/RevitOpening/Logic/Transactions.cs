using Autodesk.Revit.DB;
using RevitOpening.Extensions;
using RevitOpening.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitOpening.Logic
{
    public static class Transactions
    {
        private static void DoTransaction(Document document, string transactionName, Action action)
        {
            using (var t = new Transaction(document, transactionName))
            {
                t.Start();
                action.Invoke();
                t.Commit();
            }
        }

        public static void UpdateTasksInfo(Document document, IEnumerable<Document> documents,
            double offset, double diameter)
        {
            DoTransaction(document, "Обновление информации о заданиях", () =>
            {
                BoxAnalyzer.ExecuteAnalysis(documents, offset, diameter);
            });
        }

        public static void Drawing(Document document, List<Element> openings)
        {
            DoTransaction(document, "Отрисовка", () =>
            {
                foreach (var opening in openings)
                {
                    var v = opening.LookupParameter("Отверстие_Дисциплина").AsString();
                    opening.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                    opening.LookupParameter("Отверстие_Дисциплина").Set(v);
                }
            });
        }

        public static void CombineIntersectsTasks(Document document, IEnumerable<Document> documents)
        {
            DoTransaction(document, "Объединение заданий", () =>
            {
                BoxCombiner.CombineAllBoxes(documents, document);
            });
        }

        public static void CombineSelectedTasks(Document document, IEnumerable<Document> documents, Element el1,
            Element el2, out FamilyInstance newTask)
        {
            FamilyInstance task = null;
            DoTransaction(document, "Объединение заданий", () =>
            {
                task = BoxCombiner.CombineTwoBoxes(documents, document, el1, el2);
            });
            newTask = task;
        }

        public static void CreateOpeningInSelectedTask(Document document, List<Element> openings, List<Element> tasks)
        {
            DoTransaction(document, "Замена заданий на отверстия", () =>
            {
                var createOpeningInTaskBoxes = new CreateOpeningInTaskBoxes(document);
                openings.AddRange(createOpeningInTaskBoxes.SwapTasksToOpenings(tasks.Cast<FamilyInstance>()));
            });
        }

        public static void CreateAllTasks(Document document, IEnumerable<Document> documents,
            double offset, double diameter, List<OpeningData> tasks, List<OpeningData> openings)
        {
            DoTransaction(document, "Создание заданий", () =>
            {
                var createTask = new CreateTaskBoxes(tasks, openings, document,
                    documents, diameter, offset);
                createTask.Execute();
            });
        }

        public static void SwapAllTasksToOpenings(Document document, List<Element> openings)
        {
            DoTransaction(document, "Замена заданий на отверстия", () =>
            {
                var createOpenings = new CreateOpeningInTaskBoxes(document);
                openings.AddRange(createOpenings.SwapAllTasksToOpenings());
            });
        }

        public static void UpdateTaskInfo(Document document, IEnumerable<Document> documents, FamilyInstance newTask)
        {
            DoTransaction(document, "Обновление информации о задании", () =>
            {
                var data = newTask.GetParentsData();
                var walls = documents.GetAllElementsOfClass<Wall>();
                var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
                var tasks = documents.GetAllTasks();
                newTask.AnalyzeElement(data, walls, floors, tasks, documents, 0, 0);
            });
        }
    }
}

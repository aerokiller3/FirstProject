namespace RevitOpening.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Extensions;
    using Models;
    using Settings = Extensions.Settings;

    internal static class Transactions
    {
        public static void DoTransaction(Document document, string transactionName, Action action)
        {
            using (var t = new Transaction(document, transactionName))
            {
                t.Start();
                action.Invoke();
                t.Commit();
            }
        }

        public static void UpdateTasksInfo(Document document, List<Document> documents,
            double offset, double diameter)
        {
            DoTransaction(document, "Обновление информации о заданиях",
                () => { BoxAnalyzer.ExecuteAnalysis(documents,document, offset, diameter); });
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

        public static void CombineIntersectsTasks(Document document, ICollection<Document> documents)
        {
            DoTransaction(document, "Объединение заданий",
                () => BoxCombiner.CombineAllBoxes(documents, document));
        }

        public static FamilyInstance CombineSelectedTasks(Document document, ICollection<Document> documents,
            Element el1, Element el2)
        {
            FamilyInstance task = null;
            DoTransaction(document, "Объединение заданий",
                () => task = BoxCombiner.CombineTwoBoxes(documents, document, el1, el2));
            return task;
        }

        public static void CreateOpeningInSelectedTask(Document document, List<Element> openings, List<Element> tasks)
        {
            DoTransaction(document, "Замена заданий на отверстия", () =>
                openings.AddRange(TasksToOpeningsChanger.SwapTasksToOpenings(document, tasks.Cast<FamilyInstance>())));
        }

        public static void CreateAllTasks(Document document, ICollection<Document> documents,
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
                openings.AddRange(TasksToOpeningsChanger.SwapAllTasksToOpenings(document)));
        }

        public static void UpdateTaskInfo(Document document, ICollection<Document> documents, Element newTask,
            double offset, double diameter)
        {
            DoTransaction(document, "Обновление информации о задании", () =>
            {
                var walls = documents.GetAllElementsOfClass<Wall>();
                var floors = documents.GetAllElementsOfClass<CeilingAndFloor>();
                var tasks = documents.GetAllTasks();
                var mepCurves = documents.GetAllElementsOfClass<MEPCurve>();
                var data = newTask.GetOrInitData(walls, floors, offset, diameter, mepCurves, document);
                BoxAnalyzer.AnalyzeElement(newTask, data, walls, floors, tasks, documents, offset,
                    diameter, mepCurves, document);
            });
        }
    }
}
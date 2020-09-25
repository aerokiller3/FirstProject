namespace RevitOpening.Logic
{
    using System.Collections.Generic;
    using System.Text;

    public class Collisions
    {
        public const string TaskIntersectManyWalls = "Задание пересекает больше одной стены";
        public const string WallTaskIntersectFloor = "Задание для стены пересекает перекрытие";
        public const string FloorTaskIntersectWall = "Задание для перекрытия пересекает стену";
        public const string TaskIntersectTask = "Задание пересекается с другим заданием";
        public const string PipeNotPerpendicularHost = "Труба не перпендекулярна стене";
        public const string TaskCouldNotBeProcessed = "Задание невозможно обработать автоматически";

        public const string TaskNotActual = "Расположение трубы или стены имзенилось с момента построения задания\n" +
            "или информация о заданиях давно не обновлялась";

        public bool IsTaskCouldNotBeProcessed { get; set; } = false;

        public HashSet<string> ListOfCollisions = new HashSet<string>();

        public int Count => ListOfCollisions.Count;

        public void Add(string collision)
        {
            ListOfCollisions.Add(collision);
        }

        public void MarkUnSupported()
        {
            ListOfCollisions.Add(TaskCouldNotBeProcessed);
            IsTaskCouldNotBeProcessed = true;
        }

        public bool Contains(string collision)
        {
            return ListOfCollisions.Contains(collision);
        }

        public override string ToString()
        {
            var str = new StringBuilder(ListOfCollisions.Count);
            foreach (var collision in ListOfCollisions)
                str.AppendLine($"{collision}");
            return str.ToString();
        }
    }
}
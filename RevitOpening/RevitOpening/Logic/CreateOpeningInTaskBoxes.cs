using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitOpening.Extensions;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class CreateOpeningInTaskBoxes
    {
        private readonly Document _document;
        private readonly IEnumerable<Document> _documents;
        private readonly double _maxDiameter;
        private readonly double _offset;

        public CreateOpeningInTaskBoxes(Document document, IEnumerable<Document> documents, double maxDiameter, double offset)
        {
            _document = document;
            _documents = documents;
            _maxDiameter = maxDiameter;
            _offset = offset;
        }

        public void SwapAllTasksToOpenings()
        {
            FamilyLoader.LoadAllFamiliesToProject(_document);

            var wallRectTasks = _document.GetTasks(Families.WallRectTaskFamily);
            var wallRoundTasks = _document.GetTasks(Families.WallRoundTaskFamily);
            var floorRectTasks = _document.GetTasks(Families.FloorRectTaskFamily);

            var (correctWallRectTasks, incorrectWallRectTasks) = GetCheckedBoxes(wallRectTasks);
            var (correctWallRoundTasks, incorrectWallRoundTasks) = GetCheckedBoxes(wallRoundTasks);
            var (correctFloorRectTasks, incorrectFloorRectTasks) = GetCheckedBoxes(floorRectTasks);

            SwapTasksToOpenings(correctWallRectTasks);
            SwapTasksToOpenings(correctWallRoundTasks);
            SwapTasksToOpenings(correctFloorRectTasks);
        }

        public void SwapTasksToOpenings(IEnumerable<FamilyInstance> tasks)
        {
            var elementList = new List<Element>();
            foreach (var task in tasks)
            {
                var familyData = Families.GetDataFromInstanceName(task.Name).ChooseOpeningFamily();
                var parentsData = task.GetParentsData();
                parentsData.BoxData.FamilyName = familyData.SymbolName;
                _document.Delete(task.Id);
                elementList.Add(BoxCreator.CreateTaskBox(parentsData, _document));
            }

            using (var t = new SubTransaction(_document))
            {
                t.Start();
                foreach (var el in elementList)
                {
                    var v = el.LookupParameter("Отверстие_Дисциплина").AsString();
                    el.LookupParameter("Отверстие_Дисциплина").Set(v + "1");
                    el.LookupParameter("Отверстие_Дисциплина").Set(v);
                }

                t.Commit();
            }
        }

        private (IEnumerable<FamilyInstance>, IEnumerable<FamilyInstance>) GetCheckedBoxes(IEnumerable<FamilyInstance> wallRectTasks)
        {
            var correctTasks = new List<FamilyInstance>();
            var incorrectTasks = new List<FamilyInstance>();
            foreach (var task in wallRectTasks)
            {
                //
                var data = task.GetParentsData();
                if (CheckAgreed(task) &&data.IsActualTask(task,_documents, _document,_offset, _maxDiameter))
                {
                    correctTasks.Add(task);
                }
                else
                {
                    incorrectTasks.Add(task);
                    //Изменить на спец. атрибут
                    //task.LookupParameter("Несогласованно").Set(0);
                }
            }


            return (correctTasks, incorrectTasks);
        }

        private bool CheckAgreed(FamilyInstance box)
        {
            var agreedParameter = box.LookupParameter("Несогласованно");
            //Проверку спец. атрибута
            var intAgreedParameter = agreedParameter?.AsInteger();
            return intAgreedParameter == 0;
        }
    }
}
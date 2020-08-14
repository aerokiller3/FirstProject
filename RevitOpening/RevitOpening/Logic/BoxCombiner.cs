using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitOpening.Models;

namespace RevitOpening.Logic
{
    public class BoxCombiner
    {
        private readonly Document _document;
        private readonly AltecJsonSchema _schema;

        public BoxCombiner(Document document, AltecJsonSchema schema)
        {
            _schema = schema;
            _document = document;
        }

        public void CombineAllBoxes()
        {
            var isElementsUnited1 = true;
            var isElementsUnited2 = true;
            while (isElementsUnited1 && isElementsUnited2)
            {
                isElementsUnited1 = CombineOneTypeBoxes(Families.WallRectTaskFamily);
                isElementsUnited2 = CombineOneTypeBoxes(Families.FloorRectTaskFamily);
            }
        }

        public FamilyInstance CreateUnitedTask(Element el1, Element el2)
        {
            var data = el1.GetParentsData(_schema);
            var opening = CalculateOpening(el1, el2, data);
            data.BoxData = opening;
            _document.Delete(el1.Id);
            _document.Delete(el2.Id);
            return BoxCreator.CreateTaskBox(data, _document, _schema);
        }

        private bool CombineOneTypeBoxes(FamilyParameters familyData)
        {
            var isElementsUnited = false;
            using (var t = new Transaction(_document))
            {
                t.Start("United");
                var tasks = _document.GetTasksFromDocument(familyData);
                isElementsUnited |= FindTaskIntersections(tasks);
                t.Commit();
            }

            return isElementsUnited;
        }

        private bool FindTaskIntersections(IEnumerable<Element> tasks)
        {
            var elements = tasks.ToList();
            var isElementsUnited = false;
            for (var i = 0; i < elements.Count; i++)
            {
                var filtered = new ElementIntersectsElementFilter(elements[i]);
                for (var j = i + 1; j < elements.Count; j++)
                    if (filtered.PassesFilter(elements[j]))
                    {
                        elements.Add(CreateUnitedTask(elements[i], elements[j]));
                        elements.RemoveAt(j);
                        elements.RemoveAt(i);
                        isElementsUnited = true;
                        i -= 1;
                        break;
                    }
            }

            return isElementsUnited;
        }

        private OpeningData CalculateOpening(Element el1, Element el2, OpeningParentsData data)
        {
            var box1 = el1.get_BoundingBox(_document.ActiveView);
            var box2 = el2.get_BoundingBox(_document.ActiveView);
            var s1 = (el1.Location as LocationPoint).Point;
            var s2 = (el2.Location as LocationPoint).Point;
            var middle = (s1 + s2) / 2;
            var width = Math.Max(box1.Max.X, box2.Max.X) - Math.Min(box1.Min.X, box2.Min.X);
            var height = Math.Max(box1.Max.Y, box2.Max.Y) - Math.Min(box1.Min.Y, box2.Min.Y);
            return new OpeningData(null,
                width, height, data.BoxData.Depth,
                data.BoxData.Direction,
                new MyXYZ(middle),
                data.BoxData.WallGeometry,
                data.BoxData.PipeGeometry,
                data.BoxData.FamilyName,
                data.BoxData.Level);
        }
    }
}
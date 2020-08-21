using Autodesk.Revit.DB;
using Newtonsoft.Json;
using RevitOpening.Logic;
using RevitOpening.Models;

namespace RevitOpening.Extensions
{
    public static class ElementExtensions
    {
        public static OpeningParentsData GetParentsData(this Element element)
        {
            var json = AltecJsonSchema.GetJson(element);
            return JsonConvert.DeserializeObject<OpeningParentsData>(json);
        }

        public static void SetParentsData(this Element element, OpeningParentsData parentsData)
        {
            var json = JsonConvert.SerializeObject(parentsData);
            AltecJsonSchema.SetJson(element, json);
        }
    }
}
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace RevitOpening.Logic
{
    public static class AltecJsonSchema
    {
        private const string VendorId = "E864A704-827E-4D2A-804C-5F0D3359CF35";
        private static readonly Guid JsonSchemaGuid = new Guid("FDE91B8C-D078-4297-997C-FB5575409341");
        private static readonly Schema Schema;

        static AltecJsonSchema()
        {
            Schema = CheckOrInit();
        }

        public static bool SetJson(Element element, string json)
        {
            var entity = element.GetEntity(Schema);
            if (!entity.IsValid())
                entity = new Entity(Schema);
            entity.Set("json", json);
            element.SetEntity(entity);
            return true;
        }

        public static string GetJson(Element element)
        {
            var entity = element.GetEntity(Schema);
            return entity.IsValid() ? entity.Get<string>("json") : null;
        }

        private static Schema CheckOrInit()
        {
            return Schema.Lookup(JsonSchemaGuid) ?? Initialize();
        }

        private static Schema Initialize()
        {
            var e = new SchemaBuilder(JsonSchemaGuid);
            e.SetSchemaName("altec_json_schema");
            e.SetReadAccessLevel(AccessLevel.Public);
            e.SetWriteAccessLevel(AccessLevel.Public);
            e.SetVendorId(VendorId);
            e.SetDocumentation("Schema to store info for Altec, pls no touch if not from Altec.");
            e.AddSimpleField("json", typeof(string));
            return e.Finish();
        }
    }
}
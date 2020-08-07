using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace RevitOpening
{
    public class AltecJsonSchema
    {
        private const string VendorId = "E864A704-827E-4D2A-804C-5F0D3359CF35";
        private Guid _jsonSchemaGuid = new Guid("FDE91B8C-D078-4297-997C-FB5575409341");
        private readonly Schema _schema;
        public AltecJsonSchema()
        {
            _schema = CheckOrInit();
        }

        public bool SetJson(Element element, string json)
        {
            Entity entity = element.GetEntity(_schema);
            if (!entity.IsValid())
            {
                entity = new Entity(_schema);
            }
            entity.Set("json", json);
            element.SetEntity(entity);
            return true;
        }

        public string GetJson(Element element)
        {
            Entity entity = element.GetEntity(_schema);
            return entity.IsValid() ? entity.Get<string>("json") : null;
        }

        public Schema CheckOrInit()
        {
            return Schema.Lookup(_jsonSchemaGuid) ?? Initialize();
        }

        private Schema Initialize()
        {
            var e = new SchemaBuilder(_jsonSchemaGuid);
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

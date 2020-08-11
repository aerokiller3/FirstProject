using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitOpening.Models
{
    public class LevelInfo
    {
        public Level Level { get; set; }

        public LevelInfo(Level level)
        {
            Level = level;
        }

        public override string ToString()
        {
            return Level.Name;
        }
    }
}

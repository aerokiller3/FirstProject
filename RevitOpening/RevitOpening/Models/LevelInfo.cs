using Autodesk.Revit.DB;

namespace RevitOpening.Models
{
    public class LevelInfo
    {
        public LevelInfo(Level level)
        {
            Level = level;
        }

        public Level Level { get; set; }

        public override string ToString()
        {
            return Level.Name;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening.Logic
{
    public enum Сollisions
    {
        TaskIntersectManyWalls,
        TaskIntersectWallAndFloor,
        TaskNotPerpendicularWall,
        TaskWithoutHost,
        TaskIntersectTask,
        WallTaskIntersectFloor,
        FloorTaskIntersectWall,
    }
}

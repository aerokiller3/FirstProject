﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitOpening
{
    public class OpeningParentsData
    {
        public OpeningParentsData(int hostId, int pipeId, Type hostType, Type pipeType, OpeningParametrs boxData)
        {
            HostId = hostId;
            PipeId = pipeId;
            HostType = hostType;
            PipeType = pipeType;
            BoxData = boxData;
        }

        public int HostId { get; set; }

        public int PipeId { get; set; }

        public Type HostType { get; set; }

        public Type PipeType { get; set; }

        public MyXYZ LocationPoint { get; set; }

        public OpeningParametrs BoxData { get; set; }
    }
}

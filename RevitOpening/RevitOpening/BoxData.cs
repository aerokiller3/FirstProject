using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitOpening
{
    public class BoxData
    {
        public MyXYZ Location { get; set; }

        public MyXYZ FacingOrentation { get; set; }

        public MyXYZ HandOrentation { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Depth { get; set; }
    }
}

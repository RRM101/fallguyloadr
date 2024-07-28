using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fallguyloadr.JSON
{
    public class Replay
    {
        public string Version { get; set; }

        public int Seed { get; set; }

        public string RoundID { get; set; }

        public bool UsingV11Physics { get; set; }

        public float[][] Positions { get; set; }

        public float[][] Rotations { get; set; }
    }
}

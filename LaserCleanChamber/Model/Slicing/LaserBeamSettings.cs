using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Slicing
{
    public class LaserBeamSettings
    {
        public double Width { get; set; }

        public LaserBeamSettings() { }
        public LaserBeamSettings(double width)
        {
            this.Width = width;
        }

    }
}

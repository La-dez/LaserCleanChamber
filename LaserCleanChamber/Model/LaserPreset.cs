using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model
{
    public class LaserPreset
    {
        public string Name { get; set; } = "";
        public double Power { get; set; }
        public double ScanWidth { get;set; }
        public double ScanSpeed { get;set; }
    }
}

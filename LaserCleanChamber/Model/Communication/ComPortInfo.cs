using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public class ComPortInfo
    {
        public string PortName {  get; set; }
        public string Description {  get; set; }
        public string Manufacturer {  get; set; }
        public string DeviceID {  get; set; }

        public ComPortInfo(string portName, string description, string manufacturer, string deviceID)
        {
            PortName = portName;
            Description = description;
            Manufacturer = manufacturer;
            DeviceID = deviceID;
        }
    }
}

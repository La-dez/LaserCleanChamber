using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public record struct SerialPortConnectionProperties(
        string PortName,
        int Baudrate = 115200,
        Parity Parity = Parity.None,
        int DataBits = 8,
        StopBits StopBits = StopBits.One
        )
    {
        //public string PortName { get; set; } = "";
        //public int Baudrate { get; set; } = 115200;
        //public Parity Parity { get; set; } = Parity.None;
        //public int DataBits { get; set; } = 8;
        //public StopBits StopBits { get; set; } = StopBits.One;
    }
}

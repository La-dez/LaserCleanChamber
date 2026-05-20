using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public class ComHelper
    {
        public static List<ComPortInfo> EnumerateComPorts()
        {
            List<ComPortInfo> ports = new List<ComPortInfo>();

            using (ManagementClass i_Entity = new ManagementClass("Win32_PnPEntity"))
            {
                foreach (ManagementObject i_Inst in i_Entity.GetInstances())
                {
                    Object o_Guid = i_Inst.GetPropertyValue("ClassGuid");
                    if (o_Guid == null || o_Guid.ToString()?.ToUpper() != "{4D36E978-E325-11CE-BFC1-08002BE10318}")
                        continue; // Skip all devices except device class "PORTS"

                    String? s_Caption = i_Inst.GetPropertyValue("Caption").ToString();
                    String? s_Manufact = i_Inst.GetPropertyValue("Manufacturer").ToString();
                    String? s_DeviceID = i_Inst.GetPropertyValue("PnpDeviceID").ToString();
                    String? s_RegPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Enum\\" + s_DeviceID + "\\Device Parameters";
                    if (s_RegPath == null || s_Caption == null || s_Manufact == null || s_DeviceID == null)
                        continue;

                    String? s_PortName = Registry.GetValue(s_RegPath, "PortName", "")?.ToString();
                    if (s_PortName == null)
                        continue;

                    int s32_Pos = s_Caption.IndexOf(" (COM");
                    if (s32_Pos > 0) // remove COM port from description
                        s_Caption = s_Caption.Substring(0, s32_Pos);

                    ComPortInfo port = new ComPortInfo(
                        s_PortName,
                        s_Caption,
                        s_Manufact,
                        s_DeviceID
                    );
                    ports.Add(port);
                }
            }

            return ports;
        }
    }
}

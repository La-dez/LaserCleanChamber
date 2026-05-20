using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LaserCleanChamber.Utility
{
    public class Win32DeviceMgmt
    {
        [Flags]
        public enum DiGetClassFlags : uint
        {
            DIGCF_DEFAULT = 0x00000001,  // only valid with DIGCF_DEVICEINTERFACE
            DIGCF_PRESENT = 0x00000002,
            DIGCF_ALLCLASSES = 0x00000004,
            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }

        /// <summary>
        /// Device registry property codes
        /// </summary>
        public enum SPDRP : uint
        {
            /// <summary>
            /// DeviceDesc (R/W)
            /// </summary>
            SPDRP_DEVICEDESC = 0x00000000,

            /// <summary>
            /// HardwareID (R/W)
            /// </summary>
            SPDRP_HARDWAREID = 0x00000001,

            /// <summary>
            /// CompatibleIDs (R/W)
            /// </summary>
            SPDRP_COMPATIBLEIDS = 0x00000002,

            /// <summary>
            /// unused
            /// </summary>
            SPDRP_UNUSED0 = 0x00000003,

            /// <summary>
            /// Service (R/W)
            /// </summary>
            SPDRP_SERVICE = 0x00000004,

            /// <summary>
            /// unused
            /// </summary>
            SPDRP_UNUSED1 = 0x00000005,

            /// <summary>
            /// unused
            /// </summary>
            SPDRP_UNUSED2 = 0x00000006,

            /// <summary>
            /// Class (R--tied to ClassGUID)
            /// </summary>
            SPDRP_CLASS = 0x00000007,

            /// <summary>
            /// ClassGUID (R/W)
            /// </summary>
            SPDRP_CLASSGUID = 0x00000008,

            /// <summary>
            /// Driver (R/W)
            /// </summary>
            SPDRP_DRIVER = 0x00000009,

            /// <summary>
            /// ConfigFlags (R/W)
            /// </summary>
            SPDRP_CONFIGFLAGS = 0x0000000A,

            /// <summary>
            /// Mfg (R/W)
            /// </summary>
            SPDRP_MFG = 0x0000000B,

            /// <summary>
            /// FriendlyName (R/W)
            /// </summary>
            SPDRP_FRIENDLYNAME = 0x0000000C,

            /// <summary>
            /// LocationInformation (R/W)
            /// </summary>
            SPDRP_LOCATION_INFORMATION = 0x0000000D,

            /// <summary>
            /// PhysicalDeviceObjectName (R)
            /// </summary>
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E,

            /// <summary>
            /// Capabilities (R)
            /// </summary>
            SPDRP_CAPABILITIES = 0x0000000F,

            /// <summary>
            /// UiNumber (R)
            /// </summary>
            SPDRP_UI_NUMBER = 0x00000010,

            /// <summary>
            /// UpperFilters (R/W)
            /// </summary>
            SPDRP_UPPERFILTERS = 0x00000011,

            /// <summary>
            /// LowerFilters (R/W)
            /// </summary>
            SPDRP_LOWERFILTERS = 0x00000012,

            /// <summary>
            /// BusTypeGUID (R)
            /// </summary>
            SPDRP_BUSTYPEGUID = 0x00000013,

            /// <summary>
            /// LegacyBusType (R)
            /// </summary>
            SPDRP_LEGACYBUSTYPE = 0x00000014,

            /// <summary>
            /// BusNumber (R)
            /// </summary>
            SPDRP_BUSNUMBER = 0x00000015,

            /// <summary>
            /// Enumerator Name (R)
            /// </summary>
            SPDRP_ENUMERATOR_NAME = 0x00000016,

            /// <summary>
            /// Security (R/W, binary form)
            /// </summary>
            SPDRP_SECURITY = 0x00000017,

            /// <summary>
            /// Security (W, SDS form)
            /// </summary>
            SPDRP_SECURITY_SDS = 0x00000018,

            /// <summary>
            /// Device Type (R/W)
            /// </summary>
            SPDRP_DEVTYPE = 0x00000019,

            /// <summary>
            /// Device is exclusive-access (R/W)
            /// </summary>
            SPDRP_EXCLUSIVE = 0x0000001A,

            /// <summary>
            /// Device Characteristics (R/W)
            /// </summary>
            SPDRP_CHARACTERISTICS = 0x0000001B,

            /// <summary>
            /// Device Address (R)
            /// </summary>
            SPDRP_ADDRESS = 0x0000001C,

            /// <summary>
            /// UiNumberDescFormat (R/W)
            /// </summary>
            SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D,

            /// <summary>
            /// Device Power Data (R)
            /// </summary>
            SPDRP_DEVICE_POWER_DATA = 0x0000001E,

            /// <summary>
            /// Removal Policy (R)
            /// </summary>
            SPDRP_REMOVAL_POLICY = 0x0000001F,

            /// <summary>
            /// Hardware Removal Policy (R)
            /// </summary>
            SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020,

            /// <summary>
            /// Removal Policy Override (RW)
            /// </summary>
            SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021,

            /// <summary>
            /// Device Install State (R)
            /// </summary>
            SPDRP_INSTALL_STATE = 0x00000022,

            /// <summary>
            /// Device Location Paths (R)
            /// </summary>
            SPDRP_LOCATION_PATHS = 0x00000023,
        }

        private const uint DICS_FLAG_GLOBAL = 0x00000001;
        private const uint DIREG_DEV = 0x00000001;
        private const uint KEY_QUERY_VALUE = 0x0001;

        /// <summary>
        /// The SP_DEVINFO_DATA structure defines a device instance that is a member of a device information set.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public UIntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [DllImport("setupapi.dll")]
        private static extern int SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid gClass, uint iEnumerator, uint hParent, DiGetClassFlags nFlags);

        [DllImport("Setupapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiOpenDevRegKey(IntPtr hDeviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint scope,
            uint hwProfile, uint parameterRegistryValueKind, uint samDesired);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        private static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, int lpReserved, out uint lpType,
            byte[] lpData, ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        [DllImport("kernel32.dll")]
        private static extern int GetLastError();

        const int BUFFER_SIZE = 1024;

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiClassGuidsFromName(string ClassName,
            ref Guid ClassGuidArray1stItem, uint ClassGuidArraySize,
            out uint RequiredSize);

        [DllImport("setupapi.dll")]
        private static extern int SetupDiClassNameFromGuid(ref Guid ClassGuid,
            StringBuilder className, int ClassNameSize, ref int RequiredSize);

        /// <summary>
        /// The SetupDiGetDeviceRegistryProperty function retrieves the specified device property.
        /// This handle is typically returned by the SetupDiGetClassDevs or SetupDiGetClassDevsEx function.
        /// </summary>
        /// <param Name="DeviceInfoSet">Handle to the device information set that contains the interface and its underlying device.</param>
        /// <param Name="DeviceInfoData">Pointer to an SP_DEVINFO_DATA structure that defines the device instance.</param>
        /// <param Name="Property">Device property to be retrieved. SEE MSDN</param>
        /// <param Name="PropertyRegDataType">Pointer to a variable that receives the registry data Type. This parameter can be NULL.</param>
        /// <param Name="PropertyBuffer">Pointer to a buffer that receives the requested device property.</param>
        /// <param Name="PropertyBufferSize">Size of the buffer, in bytes.</param>
        /// <param Name="RequiredSize">Pointer to a variable that receives the required buffer size, in bytes. This parameter can be NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            SPDRP Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiGetDevicePropertyW(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA DeviceInfoData,
            [In] ref DEVPROPKEY propertyKey,
            [Out] out uint propertyType,
            byte[] propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize,
            uint flags);

        const int utf16terminatorSize_bytes = 2;

        public struct DeviceInfo
        {
            public string name;
            public string maufacturer;
            public string friendlyName;
            public string description;
            public string bus_description;
            public string instance_id;
            //public Dictionary<string, string> deviceProperties;
        }

        /*static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc;
        static DEVPROPKEY DEVPKEY_Device_InstanceId;
        static DEVPROPKEY DEVPKEY_Device_FriendlyName;
        static DEVPROPKEY DEVPKEY_Device_DeviceDesc;*/
        static DEVPROPKEY DEVPKEY_Device_DeviceDesc = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 2);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_HardwareIds = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 3);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_CompatibleIds = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 4);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_Service = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 6);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_Class = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 9);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_ClassGuid = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 10);    // DEVPROP_TYPE_GUID
        static DEVPROPKEY DEVPKEY_Device_Driver = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 11);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_ConfigFlags = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 12);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_Manufacturer = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 13);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_FriendlyName = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 14);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_LocationInfo = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 15);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_PDOName = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 16);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_Capabilities = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 17);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_UINumber = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 18);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_UpperFilters = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 19);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_LowerFilters = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 20);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_BusTypeGuid = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 21);    // DEVPROP_TYPE_GUID
        static DEVPROPKEY DEVPKEY_Device_LegacyBusType = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 22);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_BusNumber = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 23);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_EnumeratorName = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 24);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_Security = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 25);    // DEVPROP_TYPE_SECURITY_DESCRIPTOR
        static DEVPROPKEY DEVPKEY_Device_SecuritySDS = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 26);    // DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING
        static DEVPROPKEY DEVPKEY_Device_DevType = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 27);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_Exclusive = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 28);    // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_Characteristics = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 29);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_Address = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 30);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_UINumberDescFormat = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 31);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_PowerData = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 32);    // DEVPROP_TYPE_BINARY
        static DEVPROPKEY DEVPKEY_Device_RemovalPolicy = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 33);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_RemovalPolicyDefault = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 34);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_RemovalPolicyOverride = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 35);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_InstallState = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 36);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_LocationPaths = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 37);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_BaseContainerId = GetDEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 38);    // DEVPROP_TYPE_GUID
                                                                                                                                                             //
                                                                                                                                                             // Device and Device Interface property
                                                                                                                                                             // Common DEVPKEY used to retrieve the device instance id associated with devices and device interfaces.
                                                                                                                                                             //
        static DEVPROPKEY DEVPKEY_Device_InstanceId = GetDEVPROPKEY(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57, 256);   // DEVPROP_TYPE_STRING
                                                                                                                                                        //
                                                                                                                                                        // Device properties
                                                                                                                                                        // These DEVPKEYs correspond to a device's status and problem code.
                                                                                                                                                        //
        static DEVPROPKEY DEVPKEY_Device_DevNodeStatus = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 2);     // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_ProblemCode = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 3);     // DEVPROP_TYPE_UINT32
                                                                                                                                                         //
                                                                                                                                                         // Device properties
                                                                                                                                                         // These DEVPKEYs correspond to a device's relations.
                                                                                                                                                         //
        static DEVPROPKEY DEVPKEY_Device_EjectionRelations = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 4);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_RemovalRelations = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 5);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_PowerRelations = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 6);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_BusRelations = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 7);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_Parent = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 8);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_Children = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 9);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_Siblings = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 10);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_TransportRelations = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 11);    // DEVPROP_TYPE_STRING_LIST
                                                                                                                                                                //
                                                                                                                                                                // Device property
                                                                                                                                                                // This DEVPKEY corresponds to a the status code that resulted in a device to be in a problem state.
                                                                                                                                                                //
        static DEVPROPKEY DEVPKEY_Device_ProblemStatus = GetDEVPROPKEY(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 12);     // DEVPROP_TYPE_NTSTATUS
                                                                                                                                                            //
                                                                                                                                                            // Device properties
                                                                                                                                                            // These DEVPKEYs are set for the corresponding types of root-enumerated devices.
                                                                                                                                                            //
        static DEVPROPKEY DEVPKEY_Device_Reported = GetDEVPROPKEY(0x80497100, 0x8c73, 0x48b9, 0xaa, 0xd9, 0xce, 0x38, 0x7e, 0x19, 0xc5, 0x6e, 2);  // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_Legacy = GetDEVPROPKEY(0x80497100, 0x8c73, 0x48b9, 0xaa, 0xd9, 0xce, 0x38, 0x7e, 0x19, 0xc5, 0x6e, 3);  // DEVPROP_TYPE_BOOLEAN
                                                                                                                                                 //
                                                                                                                                                 // Device Container Id
                                                                                                                                                 //
        static DEVPROPKEY DEVPKEY_Device_ContainerId = GetDEVPROPKEY(0x8c7ed206, 0x3f8a, 0x4827, 0xb3, 0xab, 0xae, 0x9e, 0x1f, 0xae, 0xfc, 0x6c, 2);     // DEVPROP_TYPE_GUID
        static DEVPROPKEY DEVPKEY_Device_InLocalMachineContainer = GetDEVPROPKEY(0x8c7ed206, 0x3f8a, 0x4827, 0xb3, 0xab, 0xae, 0x9e, 0x1f, 0xae, 0xfc, 0x6c, 4);     // DEVPROP_TYPE_BOOLEAN
                                                                                                                                                                     //
                                                                                                                                                                     // Device property
                                                                                                                                                                     // This DEVPKEY correspond to a device's model.
                                                                                                                                                                     //
        static DEVPROPKEY DEVPKEY_Device_Model = GetDEVPROPKEY(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57, 39);    // DEVPROP_TYPE_STRING
                                                                                                                                                   //
                                                                                                                                                   // Device Experience related Keys
                                                                                                                                                   //
        static DEVPROPKEY DEVPKEY_Device_ModelId = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 2); // DEVPROP_TYPE_GUID
        static DEVPROPKEY DEVPKEY_Device_FriendlyNameAttributes = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 3); // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_ManufacturerAttributes = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 4); // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_PresenceNotForDevice = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 5); // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_SignalStrength = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 6); // DEVPROP_TYPE_INT32
        static DEVPROPKEY DEVPKEY_Device_IsAssociateableByUserAction = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 7); // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_ShowInUninstallUI = GetDEVPROPKEY(0x80d81ea6, 0x7473, 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b, 8); // DEVPROP_TYPE_BOOLEAN
                                                                                                                                                           //
                                                                                                                                                           // Other Device properties
                                                                                                                                                           //
        static DEVPROPKEY DEVPKEY_Device_Numa_Proximity_Domain = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 1);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_DHP_Rebalance_Policy = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 2);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_Numa_Node = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 3);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 4);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_IsPresent = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 5);    // DEVPROP_TYPE_BOOL
        static DEVPROPKEY DEVPKEY_Device_HasProblem = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 6);    // DEVPROP_TYPE_BOOL
        static DEVPROPKEY DEVPKEY_Device_ConfigurationId = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 7);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_ReportedDeviceIdsHash = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 8);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_PhysicalDeviceLocation = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 9);    // DEVPROP_TYPE_BINARY
        static DEVPROPKEY DEVPKEY_Device_BiosDeviceName = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 10);   // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverProblemDesc = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 11);   // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DebuggerSafe = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 12);   // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_PostInstallInProgress = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 13);   // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_Stack = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 14);   // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_ExtendedConfigurationIds = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 15);   // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_IsRebootRequired = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 16);   // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_FirmwareDate = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 17);   // DEVPROP_TYPE_FILETIME
        static DEVPROPKEY DEVPKEY_Device_FirmwareVersion = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 18);   // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_FirmwareRevision = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 19);   // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DependencyProviders = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 20);   // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_DependencyDependents = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 21);   // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_SoftRestartSupported = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 22);   // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_ExtendedAddress = GetDEVPROPKEY(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2, 23);   // DEVPROP_TYPE_UINT64
        static DEVPROPKEY DEVPKEY_Device_SessionId = GetDEVPROPKEY(0x83da6326, 0x97a6, 0x4088, 0x94, 0x53, 0xa1, 0x92, 0x3f, 0x57, 0x3b, 0x29, 6);     // DEVPROP_TYPE_UINT32
                                                                                                                                                       //
                                                                                                                                                       // Device activity timestamp properties
                                                                                                                                                       //
        static DEVPROPKEY DEVPKEY_Device_InstallDate = GetDEVPROPKEY(0x83da6326, 0x97a6, 0x4088, 0x94, 0x53, 0xa1, 0x92, 0x3f, 0x57, 0x3b, 0x29, 100);   // DEVPROP_TYPE_FILETIME
        static DEVPROPKEY DEVPKEY_Device_FirstInstallDate = GetDEVPROPKEY(0x83da6326, 0x97a6, 0x4088, 0x94, 0x53, 0xa1, 0x92, 0x3f, 0x57, 0x3b, 0x29, 101);   // DEVPROP_TYPE_FILETIME
        static DEVPROPKEY DEVPKEY_Device_LastArrivalDate = GetDEVPROPKEY(0x83da6326, 0x97a6, 0x4088, 0x94, 0x53, 0xa1, 0x92, 0x3f, 0x57, 0x3b, 0x29, 102);   // DEVPROP_TYPE_FILETIME
        static DEVPROPKEY DEVPKEY_Device_LastRemovalDate = GetDEVPROPKEY(0x83da6326, 0x97a6, 0x4088, 0x94, 0x53, 0xa1, 0x92, 0x3f, 0x57, 0x3b, 0x29, 103);   // DEVPROP_TYPE_FILETIME
                                                                                                                                                             //
                                                                                                                                                             // Device driver properties
                                                                                                                                                             //
        static DEVPROPKEY DEVPKEY_Device_DriverDate = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 2);     // DEVPROP_TYPE_FILETIME
        static DEVPROPKEY DEVPKEY_Device_DriverVersion = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 3);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverDesc = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 4);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverInfPath = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 5);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverInfSection = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 6);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverInfSectionExt = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 7);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_MatchingDeviceId = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 8);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverProvider = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 9);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverPropPageProvider = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 10);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverCoInstallers = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 11);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_Device_ResourcePickerTags = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 12);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_ResourcePickerExceptions = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 13);    // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_Device_DriverRank = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 14);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_Device_DriverLogoLevel = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 15);    // DEVPROP_TYPE_UINT32
                                                                                                                                                            //
                                                                                                                                                            // Device properties
                                                                                                                                                            // These DEVPKEYs may be set by the driver package installed for a device.
                                                                                                                                                            //
        static DEVPROPKEY DEVPKEY_Device_NoConnectSound = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 17); // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_GenericDriverInstalled = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 18); // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_AdditionalSoftwareRequested = GetDEVPROPKEY(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6, 19); // DEVPROP_TYPE_BOOLEAN
                                                                                                                                                                     //
                                                                                                                                                                     // Device safe-removal properties
                                                                                                                                                                     //
        static DEVPROPKEY DEVPKEY_Device_SafeRemovalRequired = GetDEVPROPKEY(0xafd97640, 0x86a3, 0x4210, 0xb6, 0x7c, 0x28, 0x9c, 0x41, 0xaa, 0xbe, 0x55, 2); // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_Device_SafeRemovalRequiredOverride = GetDEVPROPKEY(0xafd97640, 0x86a3, 0x4210, 0xb6, 0x7c, 0x28, 0x9c, 0x41, 0xaa, 0xbe, 0x55, 3); // DEVPROP_TYPE_BOOLEAN
                                                                                                                                                                     //
                                                                                                                                                                     // Device properties
                                                                                                                                                                     // These DEVPKEYs may be set by the driver package installed for a device.
                                                                                                                                                                     //
        static DEVPROPKEY DEVPKEY_DrvPkg_Model = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 2);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DrvPkg_VendorWebSite = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 3);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DrvPkg_DetailedDescription = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 4);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DrvPkg_DocumentationLink = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 5);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DrvPkg_Icon = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 6);     // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_DrvPkg_BrandingIcon = GetDEVPROPKEY(0xcf73bb51, 0x3abf, 0x44a2, 0x85, 0xe0, 0x9a, 0x3d, 0xc7, 0xa1, 0x21, 0x32, 7);     // DEVPROP_TYPE_STRING_LIST
                                                                                                                                                          //
                                                                                                                                                          // Device setup class properties
                                                                                                                                                          // These DEVPKEYs correspond to the SetupAPI SPCRP_XXX setup class properties.
                                                                                                                                                          //
        static DEVPROPKEY DEVPKEY_DeviceClass_UpperFilters = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 19);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_DeviceClass_LowerFilters = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 20);    // DEVPROP_TYPE_STRING_LIST
        static DEVPROPKEY DEVPKEY_DeviceClass_Security = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 25);    // DEVPROP_TYPE_SECURITY_DESCRIPTOR
        static DEVPROPKEY DEVPKEY_DeviceClass_SecuritySDS = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 26);    // DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_DevType = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 27);    // DEVPROP_TYPE_UINT32
        static DEVPROPKEY DEVPKEY_DeviceClass_Exclusive = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 28);    // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_DeviceClass_Characteristics = GetDEVPROPKEY(0x4321918b, 0xf69e, 0x470d, 0xa5, 0xde, 0x4d, 0x88, 0xc7, 0x5a, 0xd2, 0x4b, 29);    // DEVPROP_TYPE_UINT32
                                                                                                                                                                  //
                                                                                                                                                                  // Device setup class properties
                                                                                                                                                                  //
        static DEVPROPKEY DEVPKEY_DeviceClass_Name = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 2);      // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_ClassName = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 3);      // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_Icon = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 4);      // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_ClassInstaller = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 5);      // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_PropPageProvider = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 6);      // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_NoInstallClass = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 7);      // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_DeviceClass_NoDisplayClass = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 8);      // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_DeviceClass_SilentInstall = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 9);      // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_DeviceClass_NoUseClass = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 10);     // DEVPROP_TYPE_BOOLEAN
        static DEVPROPKEY DEVPKEY_DeviceClass_DefaultService = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 11);     // DEVPROP_TYPE_STRING
        static DEVPROPKEY DEVPKEY_DeviceClass_IconPath = GetDEVPROPKEY(0x259abffc, 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66, 12);     // DEVPROP_TYPE_STRING_LIST

        static Win32DeviceMgmt()
        {
            /*DEVPKEY_Device_BusReportedDeviceDesc = new DEVPROPKEY();
            DEVPKEY_Device_BusReportedDeviceDesc.fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2);
            DEVPKEY_Device_BusReportedDeviceDesc.pid = 4;

            DEVPKEY_Device_InstanceId = new DEVPROPKEY();
            DEVPKEY_Device_InstanceId.fmtid = new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57);
            DEVPKEY_Device_InstanceId.pid = 256;

            DEVPKEY_Device_FriendlyName = new DEVPROPKEY();
            DEVPKEY_Device_FriendlyName.fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);
            DEVPKEY_Device_FriendlyName.pid = 14;

            DEVPKEY_Device_DeviceDesc = new DEVPROPKEY();
            DEVPKEY_Device_DeviceDesc.fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);
            DEVPKEY_Device_DeviceDesc.pid = 2;*/
        }

        private static Dictionary<string, DEVPROPKEY> CreatePropertyGuidDictionary()
        {
            Dictionary<string, DEVPROPKEY> propsDict = new Dictionary<string, DEVPROPKEY>();
            propsDict.Add("Device_DeviceDesc", DEVPKEY_Device_DeviceDesc);
            propsDict.Add("Device_HardwareIds", DEVPKEY_Device_HardwareIds);
            propsDict.Add("Device_CompatibleIds", DEVPKEY_Device_CompatibleIds);
            propsDict.Add("Device_Service", DEVPKEY_Device_Service);
            propsDict.Add("Device_Class", DEVPKEY_Device_Class);
            propsDict.Add("Device_ClassGuid", DEVPKEY_Device_ClassGuid);
            propsDict.Add("Device_Driver", DEVPKEY_Device_Driver);
            propsDict.Add("Device_ConfigFlags", DEVPKEY_Device_ConfigFlags);
            propsDict.Add("Device_Manufacturer", DEVPKEY_Device_Manufacturer);
            propsDict.Add("Device_FriendlyName", DEVPKEY_Device_FriendlyName);
            propsDict.Add("Device_LocationInfo", DEVPKEY_Device_LocationInfo);
            propsDict.Add("Device_PDOName", DEVPKEY_Device_PDOName);
            propsDict.Add("Device_Capabilities", DEVPKEY_Device_Capabilities);
            propsDict.Add("Device_UINumber", DEVPKEY_Device_UINumber);
            propsDict.Add("Device_UpperFilters", DEVPKEY_Device_UpperFilters);
            propsDict.Add("Device_LowerFilters", DEVPKEY_Device_LowerFilters);
            propsDict.Add("Device_BusTypeGuid", DEVPKEY_Device_BusTypeGuid);
            propsDict.Add("Device_LegacyBusType", DEVPKEY_Device_LegacyBusType);
            propsDict.Add("Device_BusNumber", DEVPKEY_Device_BusNumber);
            propsDict.Add("Device_EnumeratorName", DEVPKEY_Device_EnumeratorName);
            propsDict.Add("Device_Security", DEVPKEY_Device_Security);
            propsDict.Add("Device_SecuritySDS", DEVPKEY_Device_SecuritySDS);
            propsDict.Add("Device_DevType", DEVPKEY_Device_DevType);
            propsDict.Add("Device_Exclusive", DEVPKEY_Device_Exclusive);
            propsDict.Add("Device_Characteristics", DEVPKEY_Device_Characteristics);
            propsDict.Add("Device_Address", DEVPKEY_Device_Address);
            propsDict.Add("Device_UINumberDescFormat", DEVPKEY_Device_UINumberDescFormat);
            propsDict.Add("Device_PowerData", DEVPKEY_Device_PowerData);
            propsDict.Add("Device_RemovalPolicy", DEVPKEY_Device_RemovalPolicy);
            propsDict.Add("Device_RemovalPolicyDefault", DEVPKEY_Device_RemovalPolicyDefault);
            propsDict.Add("Device_RemovalPolicyOverride", DEVPKEY_Device_RemovalPolicyOverride);
            propsDict.Add("Device_InstallState", DEVPKEY_Device_InstallState);
            propsDict.Add("Device_LocationPaths", DEVPKEY_Device_LocationPaths);
            propsDict.Add("Device_BaseContainerId", DEVPKEY_Device_BaseContainerId);
            propsDict.Add("Device_InstanceId", DEVPKEY_Device_InstanceId);
            propsDict.Add("Device_DevNodeStatus", DEVPKEY_Device_DevNodeStatus);
            propsDict.Add("Device_ProblemCode", DEVPKEY_Device_ProblemCode);
            propsDict.Add("Device_EjectionRelations", DEVPKEY_Device_EjectionRelations);
            propsDict.Add("Device_RemovalRelations", DEVPKEY_Device_RemovalRelations);
            propsDict.Add("Device_PowerRelations", DEVPKEY_Device_PowerRelations);
            propsDict.Add("Device_BusRelations", DEVPKEY_Device_BusRelations);
            propsDict.Add("Device_Parent", DEVPKEY_Device_Parent);
            propsDict.Add("Device_Children", DEVPKEY_Device_Children);
            propsDict.Add("Device_Siblings", DEVPKEY_Device_Siblings);
            propsDict.Add("Device_TransportRelations", DEVPKEY_Device_TransportRelations);
            propsDict.Add("Device_ProblemStatus", DEVPKEY_Device_ProblemStatus);
            propsDict.Add("Device_Reported", DEVPKEY_Device_Reported);
            propsDict.Add("Device_Legacy", DEVPKEY_Device_Legacy);
            propsDict.Add("Device_ContainerId", DEVPKEY_Device_ContainerId);
            propsDict.Add("Device_InLocalMachineContainer", DEVPKEY_Device_InLocalMachineContainer);
            propsDict.Add("Device_Model", DEVPKEY_Device_Model);
            propsDict.Add("Device_ModelId", DEVPKEY_Device_ModelId);
            propsDict.Add("Device_FriendlyNameAttributes", DEVPKEY_Device_FriendlyNameAttributes);
            propsDict.Add("Device_ManufacturerAttributes", DEVPKEY_Device_ManufacturerAttributes);
            propsDict.Add("Device_PresenceNotForDevice", DEVPKEY_Device_PresenceNotForDevice);
            propsDict.Add("Device_SignalStrength", DEVPKEY_Device_SignalStrength);
            propsDict.Add("Device_IsAssociateableByUserAction", DEVPKEY_Device_IsAssociateableByUserAction);
            propsDict.Add("Device_ShowInUninstallUI", DEVPKEY_Device_ShowInUninstallUI);
            propsDict.Add("Device_Numa_Proximity_Domain", DEVPKEY_Device_Numa_Proximity_Domain);
            propsDict.Add("Device_DHP_Rebalance_Policy", DEVPKEY_Device_DHP_Rebalance_Policy);
            propsDict.Add("Device_Numa_Node", DEVPKEY_Device_Numa_Node);
            propsDict.Add("Device_BusReportedDeviceDesc", DEVPKEY_Device_BusReportedDeviceDesc);
            propsDict.Add("Device_IsPresent", DEVPKEY_Device_IsPresent);
            propsDict.Add("Device_HasProblem", DEVPKEY_Device_HasProblem);
            propsDict.Add("Device_ConfigurationId", DEVPKEY_Device_ConfigurationId);
            propsDict.Add("Device_ReportedDeviceIdsHash", DEVPKEY_Device_ReportedDeviceIdsHash);
            propsDict.Add("Device_PhysicalDeviceLocation", DEVPKEY_Device_PhysicalDeviceLocation);
            propsDict.Add("Device_BiosDeviceName", DEVPKEY_Device_BiosDeviceName);
            propsDict.Add("Device_DriverProblemDesc", DEVPKEY_Device_DriverProblemDesc);
            propsDict.Add("Device_DebuggerSafe", DEVPKEY_Device_DebuggerSafe);
            propsDict.Add("Device_PostInstallInProgress", DEVPKEY_Device_PostInstallInProgress);
            propsDict.Add("Device_Stack", DEVPKEY_Device_Stack);
            propsDict.Add("Device_ExtendedConfigurationIds", DEVPKEY_Device_ExtendedConfigurationIds);
            propsDict.Add("Device_IsRebootRequired", DEVPKEY_Device_IsRebootRequired);
            propsDict.Add("Device_FirmwareDate", DEVPKEY_Device_FirmwareDate);
            propsDict.Add("Device_FirmwareVersion", DEVPKEY_Device_FirmwareVersion);
            propsDict.Add("Device_FirmwareRevision", DEVPKEY_Device_FirmwareRevision);
            propsDict.Add("Device_DependencyProviders", DEVPKEY_Device_DependencyProviders);
            propsDict.Add("Device_DependencyDependents", DEVPKEY_Device_DependencyDependents);
            propsDict.Add("Device_SoftRestartSupported", DEVPKEY_Device_SoftRestartSupported);
            propsDict.Add("Device_ExtendedAddress", DEVPKEY_Device_ExtendedAddress);
            propsDict.Add("Device_SessionId", DEVPKEY_Device_SessionId);
            propsDict.Add("Device_InstallDate", DEVPKEY_Device_InstallDate);
            propsDict.Add("Device_FirstInstallDate", DEVPKEY_Device_FirstInstallDate);
            propsDict.Add("Device_LastArrivalDate", DEVPKEY_Device_LastArrivalDate);
            propsDict.Add("Device_LastRemovalDate", DEVPKEY_Device_LastRemovalDate);
            propsDict.Add("Device_DriverDate", DEVPKEY_Device_DriverDate);
            propsDict.Add("Device_DriverVersion", DEVPKEY_Device_DriverVersion);
            propsDict.Add("Device_DriverDesc", DEVPKEY_Device_DriverDesc);
            propsDict.Add("Device_DriverInfPath", DEVPKEY_Device_DriverInfPath);
            propsDict.Add("Device_DriverInfSection", DEVPKEY_Device_DriverInfSection);
            propsDict.Add("Device_DriverInfSectionExt", DEVPKEY_Device_DriverInfSectionExt);
            propsDict.Add("Device_MatchingDeviceId", DEVPKEY_Device_MatchingDeviceId);
            propsDict.Add("Device_DriverProvider", DEVPKEY_Device_DriverProvider);
            propsDict.Add("Device_DriverPropPageProvider", DEVPKEY_Device_DriverPropPageProvider);
            propsDict.Add("Device_DriverCoInstallers", DEVPKEY_Device_DriverCoInstallers);
            propsDict.Add("Device_ResourcePickerTags", DEVPKEY_Device_ResourcePickerTags);
            propsDict.Add("Device_ResourcePickerExceptions", DEVPKEY_Device_ResourcePickerExceptions);
            propsDict.Add("Device_DriverRank", DEVPKEY_Device_DriverRank);
            propsDict.Add("Device_DriverLogoLevel", DEVPKEY_Device_DriverLogoLevel);
            propsDict.Add("Device_NoConnectSound", DEVPKEY_Device_NoConnectSound);
            propsDict.Add("Device_GenericDriverInstalled", DEVPKEY_Device_GenericDriverInstalled);
            propsDict.Add("Device_AdditionalSoftwareRequested", DEVPKEY_Device_AdditionalSoftwareRequested);
            propsDict.Add("Device_SafeRemovalRequired", DEVPKEY_Device_SafeRemovalRequired);
            propsDict.Add("Device_SafeRemovalRequiredOverride", DEVPKEY_Device_SafeRemovalRequiredOverride);
            propsDict.Add("DrvPkg_Model", DEVPKEY_DrvPkg_Model);
            propsDict.Add("DrvPkg_VendorWebSite", DEVPKEY_DrvPkg_VendorWebSite);
            propsDict.Add("DrvPkg_DetailedDescription", DEVPKEY_DrvPkg_DetailedDescription);
            propsDict.Add("DrvPkg_DocumentationLink", DEVPKEY_DrvPkg_DocumentationLink);
            propsDict.Add("DrvPkg_Icon", DEVPKEY_DrvPkg_Icon);
            propsDict.Add("DrvPkg_BrandingIcon", DEVPKEY_DrvPkg_BrandingIcon);
            propsDict.Add("DeviceClass_UpperFilters", DEVPKEY_DeviceClass_UpperFilters);
            propsDict.Add("DeviceClass_LowerFilters", DEVPKEY_DeviceClass_LowerFilters);
            propsDict.Add("DeviceClass_Security", DEVPKEY_DeviceClass_Security);
            propsDict.Add("DeviceClass_SecuritySDS", DEVPKEY_DeviceClass_SecuritySDS);
            propsDict.Add("DeviceClass_DevType", DEVPKEY_DeviceClass_DevType);
            propsDict.Add("DeviceClass_Exclusive", DEVPKEY_DeviceClass_Exclusive);
            propsDict.Add("DeviceClass_Characteristics", DEVPKEY_DeviceClass_Characteristics);
            propsDict.Add("DeviceClass_Name", DEVPKEY_DeviceClass_Name);
            propsDict.Add("DeviceClass_ClassName", DEVPKEY_DeviceClass_ClassName);
            propsDict.Add("DeviceClass_Icon", DEVPKEY_DeviceClass_Icon);
            propsDict.Add("DeviceClass_ClassInstaller", DEVPKEY_DeviceClass_ClassInstaller);
            propsDict.Add("DeviceClass_PropPageProvider", DEVPKEY_DeviceClass_PropPageProvider);
            propsDict.Add("DeviceClass_NoInstallClass", DEVPKEY_DeviceClass_NoInstallClass);
            propsDict.Add("DeviceClass_NoDisplayClass", DEVPKEY_DeviceClass_NoDisplayClass);
            propsDict.Add("DeviceClass_SilentInstall", DEVPKEY_DeviceClass_SilentInstall);
            propsDict.Add("DeviceClass_NoUseClass", DEVPKEY_DeviceClass_NoUseClass);
            propsDict.Add("DeviceClass_DefaultService", DEVPKEY_DeviceClass_DefaultService);
            propsDict.Add("DeviceClass_IconPath", DEVPKEY_DeviceClass_IconPath);

            return propsDict;
        }

        private static Dictionary<string, DEVPROPKEY> AllDevPropsDictionary = CreatePropertyGuidDictionary();
        private static Dictionary<string, string> GetAllProperties(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
        {

            Dictionary<string, string> propsDict = new Dictionary<string, string>();
            List<string> keys = new List<string>(AllDevPropsDictionary.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                try
                {
                    propsDict.Add(keys[i], GetDeviceProperty(hDeviceInfoSet, deviceInfoData, AllDevPropsDictionary[keys[i]]));
                    //Console.WriteLine("{0} =\t\t\t {1}", keys[i], propsDict[keys[i]]);
                }
                catch(Exception ex)
                {

                }

            }
            return propsDict;
        }

        private static DEVPROPKEY GetDEVPROPKEY(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k, uint pid)
        {
            DEVPROPKEY devpropkey = new DEVPROPKEY();
            devpropkey.fmtid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
            devpropkey.pid = pid;
            return devpropkey;
        }

        public static List<DeviceInfo> GetAllCOMPorts()
        {
            return GetDeviceInfos("Ports");
        }

        public static List<DeviceInfo> GetAllUSBDevices()
        {
            return GetDeviceInfos("USB");
        }

        public static List<DeviceInfo> GetDeviceInfos(string devicesClass)
        {
            Guid[] guids = GetClassGUIDs(devicesClass);
            List<DeviceInfo> devices = new List<DeviceInfo>();
            for (int index = 0; index < guids.Length; index++)
            {
                IntPtr hDeviceInfoSet = SetupDiGetClassDevs(ref guids[index], 0, 0, DiGetClassFlags.DIGCF_PRESENT);
                if (hDeviceInfoSet == IntPtr.Zero)
                    return new List<DeviceInfo>(); //Failed to get device information set for the COM ports

                try
                {
                    uint iMemberIndex = 0;
                    while (true)
                    {
                        try
                        {
                            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                            deviceInfoData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                            bool success = SetupDiEnumDeviceInfo(hDeviceInfoSet, iMemberIndex, ref deviceInfoData);
                            if (!success)
                            {
                                // No more devices in the device information set
                                break;
                            }

                            DeviceInfo deviceInfo = new DeviceInfo();
                            deviceInfo.name = GetDeviceName(hDeviceInfoSet, deviceInfoData);
                            deviceInfo.friendlyName = GetDeviceProperty(hDeviceInfoSet, deviceInfoData, DEVPKEY_Device_FriendlyName);
                            deviceInfo.description = GetDeviceProperty(hDeviceInfoSet, deviceInfoData, DEVPKEY_Device_DeviceDesc);//GetDeviceDescription(hDeviceInfoSet, deviceInfoData);
                            deviceInfo.bus_description = GetDeviceProperty(hDeviceInfoSet, deviceInfoData, DEVPKEY_Device_BusReportedDeviceDesc);
                            deviceInfo.instance_id = GetDeviceProperty(hDeviceInfoSet, deviceInfoData, DEVPKEY_Device_InstanceId);
                            deviceInfo.maufacturer = GetDeviceProperty(hDeviceInfoSet, deviceInfoData, DEVPKEY_Device_Manufacturer);
                            devices.Add(deviceInfo);

                            //var dict = GetAllProperties(hDeviceInfoSet, deviceInfoData);

                            iMemberIndex++;
                        }
                        catch (Exception ex)
                        {
                            
                        }

                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
                }
            }
            return devices;
        }

        private static string GetDeviceName(IntPtr pDevInfoSet, SP_DEVINFO_DATA deviceInfoData)
        {
            IntPtr hDeviceRegistryKey = SetupDiOpenDevRegKey(pDevInfoSet, ref deviceInfoData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_QUERY_VALUE);

            if (hDeviceRegistryKey == IntPtr.Zero)
                return string.Empty; //Failed to open a registry key for device-specific configuration information

            byte[] ptrBuf = new byte[BUFFER_SIZE];
            uint length = (uint)ptrBuf.Length;
            try
            {
                uint lpRegKeyType;
                int result = RegQueryValueEx(hDeviceRegistryKey, "PortName", 0, out lpRegKeyType, ptrBuf, ref length);

                if (result == 0)
                    return Encoding.Unicode.GetString(ptrBuf, 0, (int)length - utf16terminatorSize_bytes);
            }
            finally
            {
                RegCloseKey(hDeviceRegistryKey);
            }

            return string.Empty; //Can not read registry value PortName for device
        }

        private static string GetDeviceDescription(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
        {
            byte[] ptrBuf = new byte[BUFFER_SIZE];
            uint propRegDataType;
            uint RequiredSize;
            bool success = SetupDiGetDeviceRegistryProperty(hDeviceInfoSet, ref deviceInfoData, SPDRP.SPDRP_DEVICEDESC,
                out propRegDataType, ptrBuf, BUFFER_SIZE, out RequiredSize);

            if (success)
                return Encoding.Unicode.GetString(ptrBuf, 0, (int)RequiredSize - utf16terminatorSize_bytes);

            return string.Empty; //Can not read registry value PortName for device
        }

        private static string GetDeviceBusDescription(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
        {
            byte[] ptrBuf = new byte[BUFFER_SIZE];
            uint propRegDataType;
            uint RequiredSize;
            bool success = SetupDiGetDevicePropertyW(hDeviceInfoSet, ref deviceInfoData, ref DEVPKEY_Device_BusReportedDeviceDesc,
                out propRegDataType, ptrBuf, BUFFER_SIZE, out RequiredSize, 0);

            if (success)
                return Encoding.Unicode.GetString(ptrBuf, 0, (int)RequiredSize - utf16terminatorSize_bytes);

            return string.Empty; //Can not read Bus provided device description device
        }

        private static string GetDeviceProperty(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData, DEVPROPKEY devpropkey)
        {
            byte[] ptrBuf = new byte[BUFFER_SIZE];
            uint propRegDataType;
            uint RequiredSize;
            bool success = SetupDiGetDevicePropertyW(hDeviceInfoSet, ref deviceInfoData, ref devpropkey,
                out propRegDataType, ptrBuf, BUFFER_SIZE, out RequiredSize, 0);

            if (success)
                return Encoding.Unicode.GetString(ptrBuf, 0, (int)RequiredSize - utf16terminatorSize_bytes);

            return string.Empty; //Can not read Bus provided device description device
        }

        private static Guid[] GetClassGUIDs(string className)
        {
            uint requiredSize;
            Guid[] guidArray = new Guid[1];

            bool status = SetupDiClassGuidsFromName(className, ref guidArray[0], 1, out requiredSize);
            if (status)
            {
                if (1 < requiredSize)
                {
                    guidArray = new Guid[requiredSize];
                    SetupDiClassGuidsFromName(className, ref guidArray[0], requiredSize, out requiredSize);
                }
            }

            return guidArray;
        }
    }
}
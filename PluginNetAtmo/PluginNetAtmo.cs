using System;
using System.Runtime.InteropServices;
using Rainmeter;
using NXPorts.Attributes;
using System.Net;

/*
    [measure]
    Measure=Plugin
    Plugin=SystemVersion.dll
    ClientID=""
    ClientSecret=""
    Scope=""
    Code=""
    DeviceModuleID=xx:xx:xx:xx:xx:xx
    Action=GetValue/GetValues
    ValueName=Temperature/CO2/Humidity/Noise/Pressure
*/

namespace PluginNetAtmo
{
    internal class Measure
    {
        private enum Action
        {
            GetValue,
            GetValues
        }

        private enum ValueName
        {
            Temperature,
            CO2,
            Humidity,
            Noise,
            Pressure
        }

        private Action m_Action;
        private ValueName m_ValueName;
        private string m_DeviceModuleID;
        string m_ClientID;
        string m_ClientSecret;
        string m_Scope;
        string m_Code;
        NetAtmo m_Atmo;
        API rainmeterAPI;

        private void Logger(API.LogType log_type, string message)
        {
            API.Log(log_type, "[" + rainmeterAPI.GetMeasureName() + "] " + message);
        }

        internal void Reload(API rm, ref double maxValue)
        {
            rainmeterAPI = rm;
            Logger(API.LogType.Debug, "PluginNetAtmo.dll: Entering function: Reload");

            switch (rm.ReadString("Action", "").ToLowerInvariant())
            {
                case "getvalue":
                    m_Action = Action.GetValue;
                    break;
                case "getvalues":
                    m_Action = Action.GetValues;
                    break;
                case "":
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Action cannot be empty");
                    return;
                default:
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Action=" + rm.ReadString("Action", "").ToLowerInvariant() + " not valid");
                    return;
            }

            switch (rm.ReadString("ValueName", "").ToLowerInvariant())
            {
                case "temperature":
                    m_ValueName = ValueName.Temperature;
                    break;
                case "co2":
                    m_ValueName = ValueName.CO2;
                    break;
                case "humidity":
                    m_ValueName = ValueName.Humidity;
                    break;
                case "noise":
                    m_ValueName = ValueName.Noise;
                    break;
                case "pressure":
                    m_ValueName = ValueName.Pressure;
                    break;
                case "":
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: ValueName cannot be empty");
                    return;
                default:
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: ValueName=" + rm.ReadString("ValueName", "").ToLowerInvariant() + " not valid");
                    return;
            }

            m_ClientID = rm.ReadString("ClientID", "");
            m_ClientSecret = rm.ReadString("ClientSecret", "");
            m_Scope = rm.ReadString("Scope", "");
            m_Code = rm.ReadString("Code", "");
            m_DeviceModuleID = rm.ReadString("DeviceModuleID", "");
            
            if (m_ClientID.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: ClientID cannot be empty");
                return;
            }
            if (m_ClientSecret.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: ClientSecret cannot be empty");
                return;
            }
            if (m_Scope.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: Scope cannot be empty");
                return;
            }
            if (m_Code.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: Code cannot be empty.");
                return;
            }

            string settingsFile = rm.GetSettingsFile();
            Logger(API.LogType.Debug, $"PluginNetAtmo.dll: Using Settings file: {settingsFile}");

            m_Atmo = new NetAtmo(m_ClientID, m_ClientSecret, m_Scope, m_Code, new IniFile(settingsFile), Logger);
            LogDevicesIDs();
        }
        private void LogDevicesIDs()
        {
            var strModuleIDs = "";

            try
            {
                if (m_Atmo == null)
                {
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: LogDevicesIDs. NetAtmo not initialized");
                    return;
                }

                var station = m_Atmo.GetStationsData();

                if (station == null)
                {
                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: LogDevicesIDs. GetStationsData returned null");
                    return;
                }

                foreach (var device in station.body.devices)
                {
                    strModuleIDs += "Station: " + device.station_name;
                    strModuleIDs += ", Device: " + device.module_name + " - " + device._id;

                    foreach (var module in device.modules)
                        strModuleIDs += ", Module: " + module.module_name + " - " + module._id;
                }
            }
            catch (Exception ex)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: Exception in function LogDevicesIDs: " + ex.Message);
            }

            Logger(API.LogType.Notice, "PluginNetAtmo.dll: Found - " + strModuleIDs);
        }
        internal double Update()
        {
            Logger(API.LogType.Debug, "PluginNetAtmo.dll: Entering function: Update");

            try
            {
                switch (m_Action)
                {
                    case Action.GetValue:
                    {
                        if (m_Atmo == null)
                        {
                            Logger(API.LogType.Error,
                                "PluginNetAtmo.dll: Processing function: Update. NetAtmo not initialized");
                            return 0;
                        }

                        if (string.IsNullOrEmpty(m_DeviceModuleID))
                        {
                            Logger(API.LogType.Error,
                                "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                ". DeviceModuleID cannot be empty");
                            return 0;
                        }

                        var station = m_Atmo.GetStationsData();

                        if (station == null)
                        {
                            Logger(API.LogType.Error,
                                "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                ". GetStationsData returned null");
                            return 0;
                        }

                        switch (m_ValueName)
                        {
                            case ValueName.Temperature:
                            {
                                foreach (var device in station.body.devices)
                                {
                                    if (device._id == m_DeviceModuleID)
                                    {
                                        if (device.dashboard_data != null)
                                            return device.dashboard_data.Temperature;

                                        Logger(API.LogType.Error,
                                            "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                    }

                                    foreach (var module in device.modules)
                                        if (module._id == m_DeviceModuleID)
                                        {
                                            if (module.dashboard_data != null)
                                                return module.dashboard_data.Temperature;

                                            Logger(API.LogType.Error,
                                                "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                        }
                                }

                                Logger(API.LogType.Error,
                                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                    ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                return 0;
                            }
                            case ValueName.CO2:
                            {
                                foreach (var device in station.body.devices)
                                {
                                    if (device._id == m_DeviceModuleID)
                                    {
                                        if (device.dashboard_data != null)
                                            return device.dashboard_data.CO2;

                                        Logger(API.LogType.Error,
                                            "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                    }

                                    foreach (var module in device.modules)
                                        if (module._id == m_DeviceModuleID)
                                        {
                                            Logger(API.LogType.Error,
                                                "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                                ". DeviceModuleID=" + m_DeviceModuleID +
                                                " found but it cannot measure " + m_ValueName);
                                            return 0;
                                        }
                                }

                                Logger(API.LogType.Error,
                                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                    ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                return 0;
                            }
                            case ValueName.Humidity:
                            {
                                foreach (var device in station.body.devices)
                                {
                                    if (device._id == m_DeviceModuleID)
                                    {
                                        if (device.dashboard_data != null)
                                            return device.dashboard_data.Humidity;

                                        Logger(API.LogType.Error,
                                            "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                    }

                                    foreach (var module in device.modules)
                                        if (module._id == m_DeviceModuleID)
                                        {
                                            if (module.dashboard_data != null)
                                                return module.dashboard_data.Humidity;

                                            Logger(API.LogType.Error,
                                                "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                        }
                                }

                                Logger(API.LogType.Error,
                                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                    ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                return 0;
                            }
                            case ValueName.Noise:
                            {
                                foreach (var device in station.body.devices)
                                {
                                    if (device._id == m_DeviceModuleID)
                                    {
                                        if (device.dashboard_data != null)
                                            return device.dashboard_data.Noise;

                                        Logger(API.LogType.Error,
                                            "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                    }

                                    foreach (var module in device.modules)
                                        if (module._id == m_DeviceModuleID)
                                        {
                                            Logger(API.LogType.Error,
                                                "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                                ". DeviceModuleID=" + m_DeviceModuleID +
                                                " found but it cannot measure " + m_ValueName);
                                            return 0;
                                        }
                                }

                                Logger(API.LogType.Error,
                                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                    ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                return 0;
                            }
                            case ValueName.Pressure:
                            {
                                foreach (var device in station.body.devices)
                                {
                                    if (device._id == m_DeviceModuleID)
                                    {
                                        if (device.dashboard_data != null)
                                            return device.dashboard_data.Pressure;

                                        Logger(API.LogType.Error,
                                            "PluginNetAtmo.dll: Processing function: Update, NetAtmo reports that device is unreachable - dashboard_data not returned.");
                                    }

                                    foreach (var module in device.modules)
                                        if (module._id == m_DeviceModuleID)
                                        {
                                            Logger(API.LogType.Error,
                                                "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                                ". DeviceModuleID=" + m_DeviceModuleID +
                                                " found but it cannot measure " + m_ValueName);
                                            return 0;
                                        }
                                }

                                Logger(API.LogType.Error,
                                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                                    ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                return 0;
                            }
                        }

                        Logger(API.LogType.Error,
                            "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                            ". Invalid ValueName=" + m_ValueName + " or DeviceModuleID=" + m_DeviceModuleID);
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger(API.LogType.Error,
                    "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action +
                    ". ExceptionMessage=" + ex.Message);
            }

            // Else values are not a numbers. Therefore will be returned in GetString.
            return 0.0;
        }
        internal string GetString()
        {
            //switch (m_Action)
            //{
            //}

            // Else values are numbers. Therefore, null is returned here for them. This is to inform Rainmeter that it can treat those types as numbers.
            return null;
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }
    }
}

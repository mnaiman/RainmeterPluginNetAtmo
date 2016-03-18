using System;
using System.Runtime.InteropServices;
using Rainmeter;

/*
    [measure]
    Measure=Plugin
    Plugin=SystemVersion.dll
    ClientID=""
    ClientSecret=""
    Username=""
    Password=""
    DeviceModuleID=xx:xx:xx:xx:xx:xx
    Action=GetValue/GetValues
    ValueName=Temperature/CO2/Humidity/Noise/Pressure
*/

namespace PluginNetAtmo
{
    internal class Measure
    {
        enum Action
        {
            GetValue,
            GetValues
        }
        enum ValueName
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
        string m_Username;
        string m_Password;
        NetAtmo m_Atmo;
        API rainmeterAPI;

        internal Measure()
        { 
        }

        private void Logger(API.LogType log_type, string message)
        {
            API.Log(log_type, "[" + rainmeterAPI.GetMeasureName() + "] " + message);
        }
       
        internal void Reload(Rainmeter.API rm, ref double maxValue)
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
            m_Username = rm.ReadString("Username", "");
            m_Password = rm.ReadString("Password", "");
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
            if (m_Username.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: Username cannot be empty");
                return;
            }
            if (m_Password.Length == 0)
            {
                Logger(API.LogType.Error, "PluginNetAtmo.dll: Password cannot be empty");
                return;
            }

            m_Atmo = new NetAtmo(m_ClientID, m_ClientSecret, m_Username, m_Password, Logger);
            LogDevicesIDs();
        }
        private void LogDevicesIDs()
        {
            string strModuleIDs = "";

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
            switch (m_Action)
            {
                case Action.GetValue:
                    {
                        if (m_Atmo == null)
                        {
                            Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: LogDevicesIDs. NetAtmo not initialized");
                            return 0;
                        }

                        if (m_DeviceModuleID == null || m_DeviceModuleID.Length == 0)
                        {
                            Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID cannot be empty");
                            return 0;
                        }

                        var station = m_Atmo.GetStationsData();

                        if (station == null)
                        {
                            Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". GetStationsData returned null");
                            return 0;
                        }

                        switch (m_ValueName)
                        {
                            case ValueName.Temperature:
                                {
                                    foreach (var device in station.body.devices)
                                    {
                                        if (device._id == m_DeviceModuleID)
                                            return device.dashboard_data.Temperature;

                                        foreach (var module in device.modules)
                                            if(module._id == m_DeviceModuleID)
                                                return module.dashboard_data.Temperature;
                                    }

                                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                    return 0;
                                }
                            case ValueName.CO2:
                                {
                                    foreach (var device in station.body.devices)
                                    {
                                        if (device._id == m_DeviceModuleID)
                                            return device.dashboard_data.CO2;

                                        foreach (var module in device.modules)
                                            if (module._id == m_DeviceModuleID)
                                            {
                                                Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " found but it cannot measure " + m_ValueName);
                                                return 0;
                                            }
                                    }

                                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                    return 0;
                                }
                            case ValueName.Humidity:
                                {
                                    foreach (var device in station.body.devices)
                                    {
                                        if (device._id == m_DeviceModuleID)
                                            return device.dashboard_data.Humidity;

                                        foreach (var module in device.modules)
                                            if (module._id == m_DeviceModuleID)
                                                return module.dashboard_data.Humidity;
                                    }

                                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                    return 0;
                                }
                            case ValueName.Noise:
                                {
                                    foreach (var device in station.body.devices)
                                    {
                                        if (device._id == m_DeviceModuleID)
                                            return device.dashboard_data.Noise;

                                        foreach (var module in device.modules)
                                            if (module._id == m_DeviceModuleID)
                                            {
                                                Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " found but it cannot measure " + m_ValueName);
                                                return 0;
                                            }
                                    }

                                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                    return 0;
                                }
                            case ValueName.Pressure:
                                {
                                    foreach (var device in station.body.devices)
                                    {
                                        if (device._id == m_DeviceModuleID)
                                            return device.dashboard_data.Pressure;

                                        foreach (var module in device.modules)
                                            if (module._id == m_DeviceModuleID)
                                            {
                                                Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " found but it cannot measure " + m_ValueName);
                                                return 0;
                                            }
                                    }

                                    Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". DeviceModuleID=" + m_DeviceModuleID + " cannot be found");
                                    return 0;
                                }
                        }

                        Logger(API.LogType.Error, "PluginNetAtmo.dll: Processing function: Update, Action=" + m_Action + ". Invalid ValueName=" + m_ValueName + " or DeviceModuleID=" + m_DeviceModuleID);
                        return 0;
                    }
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

        [RGiesecke.DllExport.DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [RGiesecke.DllExport.DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [RGiesecke.DllExport.DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [RGiesecke.DllExport.DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [RGiesecke.DllExport.DllExport]
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

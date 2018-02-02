using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Rainmeter;

namespace PluginNetAtmo
{
    public class AtmoToken
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public List<string> scope { get; set; }
        public int expires_in { get; set; }
        public int expire_in { get; set; }
    }

    public class DashboardDataOutdoor
    {
        public int time_utc { get; set; }
        public double Temperature { get; set; }
        public string temp_trend { get; set; }
        public int Humidity { get; set; }
        public int date_max_temp { get; set; }
        public int date_min_temp { get; set; }
        public double min_temp { get; set; }
        public double max_temp { get; set; }
    }

    public class Module
    {
        public string _id { get; set; }
        public string type { get; set; }
        public int last_message { get; set; }
        public int last_seen { get; set; }
        public DashboardDataOutdoor dashboard_data { get; set; }
        public IList<string> data_type { get; set; }
        public string module_name { get; set; }
        public int last_setup { get; set; }
        public int battery_vp { get; set; }
        public int battery_percent { get; set; }
        public int rf_status { get; set; }
        public int firmware { get; set; }
    }

    public class Place
    {
        public int altitude { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string timezone { get; set; }
        public IList<double> location { get; set; }
    }

    public class DashboardDataIndoor
    {
        public double AbsolutePressure { get; set; }
        public int time_utc { get; set; }
        public int Noise { get; set; }
        public double Temperature { get; set; }
        public int Humidity { get; set; }
        public double Pressure { get; set; }
        public int CO2 { get; set; }
        public int date_max_temp { get; set; }
        public int date_min_temp { get; set; }
        public double min_temp { get; set; }
        public double max_temp { get; set; }
    }

    public class Device
    {
        public string _id { get; set; }
        public string cipher_id { get; set; }
        public int last_status_store { get; set; }
        public List<Module> modules { get; set; }
        public Place place { get; set; }
        public string station_name { get; set; }
        public string type { get; set; }
        public DashboardDataIndoor dashboard_data { get; set; }
        public IList<string> data_type { get; set; }
        public bool co2_calibrating { get; set; }
        public int date_setup { get; set; }
        public int last_setup { get; set; }
        public string module_name { get; set; }
        public int firmware { get; set; }
        public int last_upgrade { get; set; }
        public int wifi_status { get; set; }
        public List<string> friend_users { get; set; }
    }

    public class Administrative
    {
        public string country { get; set; }
        public int feel_like_algo { get; set; }
        public string lang { get; set; }
        public int pressureunit { get; set; }
        public string reg_locale { get; set; }
        public int unit { get; set; }
        public int windunit { get; set; }
    }

    public class User
    {
        public string mail { get; set; }
        public Administrative administrative { get; set; }
    }

    public class Body
    {
        public List<Device> devices { get; set; }
        public User user { get; set; }
    }

    public class Devices
    {
        public Body body { get; set; }
        public string status { get; set; }
        public double time_exec { get; set; }
        public int time_server { get; set; }
    }

    public class AtmoLogin
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public delegate void Logger(API.LogType log_type, string message);

    public class NetAtmo
    {
        private AtmoLogin loginData;
        private AtmoToken tokenData;
        private DateTime lastTokenRefresh;
        private Logger Log;

        public NetAtmo(string client_id, string client_secret, string username, string password, Logger log = null)
        {
            loginData = new AtmoLogin { client_id = client_id, client_secret = client_secret, username = username, password = password };
            Log = log;
        }
        private bool Login()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.netatmo.com/oauth2/token");
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            request.Method = "POST";

            try
            {
                var postData = "grant_type=password&client_id=" + WebUtility.HtmlEncode(loginData.client_id) + "&client_secret=" +
                    WebUtility.HtmlEncode(loginData.client_secret) + "&username=" + WebUtility.HtmlEncode(loginData.username) +
                    "&password=" + WebUtility.HtmlEncode(loginData.password);
                var postArray = Encoding.UTF8.GetBytes(postData);

                using (var reqStream = request.GetRequestStream())
                    reqStream.Write(postArray, 0, postArray.Length);

                using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    var json = sr.ReadToEnd();
                    tokenData = (new JavaScriptSerializer()).Deserialize<AtmoToken>(json);
                }

                lastTokenRefresh = DateTime.Now;
                Log?.Invoke(API.LogType.Notice, "PluginNetAtmo.dll: Login to NetAtmo succeeded");
                Log?.Invoke(API.LogType.Debug, "PluginNetAtmo.dll: Created new AccessToken: " + tokenData.access_token);

                return true;
            }
            catch(Exception ex)
            {
                Log?.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.Login: " + ex.Message);
                return false;
            }
        }

        private bool LoginIfExpiredOrNotLogged()
        {
            if (tokenData == null)
                return Login();

            if (lastTokenRefresh.AddSeconds(tokenData.expire_in - 600) >= DateTime.Now) return true;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.netatmo.com/oauth2/token");
                request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                request.Method = "POST";

                var postData = "grant_type=refresh_token&client_id=" + WebUtility.HtmlEncode(loginData.client_id) + "&client_secret=" +
                               WebUtility.HtmlEncode(loginData.client_secret) + "&refresh_token=" + WebUtility.HtmlEncode(tokenData.refresh_token);
                var postArray = Encoding.UTF8.GetBytes(postData);

                using (var reqStream = request.GetRequestStream())
                    reqStream.Write(postArray, 0, postArray.Length);

                using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    var json = sr.ReadToEnd();
                    tokenData = (new JavaScriptSerializer()).Deserialize<AtmoToken>(json);
                }

                lastTokenRefresh = DateTime.Now;

                Log?.Invoke(API.LogType.Notice, "PluginNetAtmo.dll: Renewal of login token to NetAtmo succeeded");
                Log?.Invoke(API.LogType.Debug, "PluginNetAtmo.dll: Renewed AccessToken: " + tokenData.access_token);

                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.LoginIfExpiredOrNotLogged: " + ex.Message);
                return false;
            }
        }

        public Devices GetStationsData()
        {
            if (!LoginIfExpiredOrNotLogged())
                return null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.netatmo.com/api/getstationsdata?access_token=" + WebUtility.HtmlEncode(tokenData.access_token));

                using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    var json = sr.ReadToEnd();
                    return (new JavaScriptSerializer()).Deserialize<Devices>(json);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.GetStationsData: " + ex.Message);
                return null;
            }
        }
    }
}

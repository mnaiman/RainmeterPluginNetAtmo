using System;
using System.Collections.Generic;
using System.Globalization;
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
        public string scope { get; set; }
        public string code { get; set; }
    }

    enum AccessTokenRequest
    {
        Authorize,
        Refresh
    }

    public delegate void Logger(API.LogType log_type, string message);

    public class NetAtmo
    {
        private AtmoLogin loginData;
        private Logger Log;
        private IniFile settingsIni;
        private string refreshToken;
        private string accessToken;
        private DateTime accessTokenExpireAt;
        private const string redirectUrl = "https://domain_not_exist_for_sure:56789";

        public NetAtmo(string client_id, string client_secret, string scope, string code, IniFile settingsIni, Logger log)
        {
            loginData = new AtmoLogin { client_id = client_id, client_secret = client_secret, scope = scope, code = code };
            Log = log;
            this.settingsIni = settingsIni;
        }

        private bool RequestAccessToken(AccessTokenRequest atr)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.netatmo.com/oauth2/token");
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            request.Method = "POST";


            var postData = "grant_type=refresh_token&client_id=" + WebUtility.HtmlEncode(loginData.client_id) + "&client_secret=" +
                WebUtility.HtmlEncode(loginData.client_secret) + "&refresh_token=" + WebUtility.HtmlEncode(refreshToken); ;

            if (atr == AccessTokenRequest.Authorize)
                postData = "grant_type=authorization_code&client_id=" + WebUtility.HtmlEncode(loginData.client_id) + "&client_secret=" +
                    WebUtility.HtmlEncode(loginData.client_secret) + "&scope=" + WebUtility.HtmlEncode(loginData.scope) +
                    "&code=" + WebUtility.HtmlEncode(loginData.code) + "&redirect_uri=" + WebUtility.HtmlEncode(redirectUrl);
            try
            {
                var postArray = Encoding.UTF8.GetBytes(postData);

                using (var reqStream = request.GetRequestStream())
                    reqStream.Write(postArray, 0, postArray.Length);

                using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    var json = sr.ReadToEnd();
                    var tokenData = (new JavaScriptSerializer()).Deserialize<AtmoToken>(json);
                    accessToken = tokenData.access_token;
                    refreshToken = tokenData.refresh_token;
                    accessTokenExpireAt = DateTime.Now.AddSeconds(tokenData.expire_in - 1800);
                }

                Log.Invoke(API.LogType.Debug, $"PluginNetAtmo.dll: Created new AccessToken: {accessToken} using method {atr.ToString()}");

                settingsIni.Write("PluginNetAtmo", "AccessToken", accessToken);
                settingsIni.Write("PluginNetAtmo", "RefreshToken", refreshToken);
                settingsIni.Write("PluginNetAtmo", "AccessTokenExpireAt", accessTokenExpireAt.ToString("s", DateTimeFormatInfo.InvariantInfo));

                return true;
            }
            catch (Exception ex)
            {
                Log.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.RequestAccessToken: " + ex.Message + request.RequestUri.ToString() + postData);
                Log.Invoke(API.LogType.Error, "In case of failed refresh_token use following authorization URL to generate new Code and update in config");
                Log.Invoke(API.LogType.Error, $"https://api.netatmo.com/oauth2/authorize?client_id={WebUtility.HtmlEncode(loginData.client_id)}&scope={WebUtility.HtmlEncode(loginData.scope)}&state={WebUtility.HtmlEncode(Guid.NewGuid().ToString())}&redirect_uri={WebUtility.HtmlEncode(redirectUrl)}");

                settingsIni.DeleteKey("PluginNetAtmo", "AccessToken");
                settingsIni.DeleteKey("PluginNetAtmo", "RefreshToken");
                settingsIni.DeleteKey("PluginNetAtmo", "AccessTokenExpireAt");
                return false;
            }
        }

        private bool ValidateAndUpdateAccessToken()
        {
            accessToken = settingsIni.Read("PluginNetAtmo", "AccessToken");
            refreshToken = settingsIni.Read("PluginNetAtmo", "RefreshToken");

            if (!DateTime.TryParseExact(settingsIni.Read("PluginNetAtmo", "AccessTokenExpireAt"), "s", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out accessTokenExpireAt))
                accessTokenExpireAt = new DateTime(1970, 1, 1);

            if (string.IsNullOrEmpty(refreshToken))
                return RequestAccessToken(AccessTokenRequest.Authorize);

            if (string.IsNullOrEmpty(accessToken) || accessTokenExpireAt < DateTime.Now)
                return RequestAccessToken(AccessTokenRequest.Refresh);

            return true;
        }

        public Devices GetStationsData()
        {
            try
            {
                return GetStationsDataInternal();
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.Forbidden)
                {
                    accessToken = "";
                    settingsIni.DeleteKey("PluginNetAtmo", "AccessToken");
                    settingsIni.DeleteKey("PluginNetAtmo", "AccessTokenExpireAt");

                    Log.Invoke(API.LogType.Debug, $"PluginNetAtmo.dll: Deleting old AccessToken");

                    try
                    {
                        return GetStationsDataInternal();
                    }
                    catch (Exception ex)
                    {
                        Log.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.GetStationsData: " + ex.Message);
                        return null;
                    }
                }

                Log.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.GetStationsData: " + wex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Log.Invoke(API.LogType.Error, "PluginNetAtmo.dll: Exception in function NetAtmo.GetStationsData: " + ex.Message);
                return null;
            }
        }

        private Devices GetStationsDataInternal()
        {
            if (!ValidateAndUpdateAccessToken())
                return null;

            var request = (HttpWebRequest)WebRequest.Create("https://api.netatmo.com/api/getstationsdata");
            request.Headers["Authorization"] = $"Bearer {accessToken}";

            using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                var json = sr.ReadToEnd();
                return (new JavaScriptSerializer()).Deserialize<Devices>(json);
            }
        }
    }
}

using System;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Java.Net;
using visvitalis.Utils;
using Android.Net;
using System.Net.Http;
using visvitalis.Networking.Responses;
using Newtonsoft.Json;
using System.Collections.Generic;
using Android.Util;
using Android.Preferences;
using System.IO;

namespace visvitalis.Networking
{
    public sealed class ServerConnector : IDisposable
    {
        public delegate void NotifyUserDownloadCompleted();
        public event NotifyUserDownloadCompleted OnNotifyUserDownloadCompleted;

        public ServerConnector() { }

        #region Register device async
        public async Task<ValidMessageResponse> RegisterDeviceAsync(string groupname, string token)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("groupname", groupname),
                    new KeyValuePair<string, string>("token", token)
                });

                var httpResponse = await client.PostAsync("/API/register", content);
                httpResponse.EnsureSuccessStatusCode();

                var response = await httpResponse.Content.ReadAsStringAsync();

                try
                {
                    return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ValidMessageResponse>(response));
                }
                catch
                {
                    return null;
                }
            }
        }
        #endregion

        #region Login async
        public async Task<LoginResponse> LoginAsync(string groupname, string password)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("groupname", groupname),
                    new KeyValuePair<string, string>("password", password)
                });

                var httpResponse = await client.PostAsync("/API/authuser", content);
                httpResponse.EnsureSuccessStatusCode();

                var response = await httpResponse.Content.ReadAsStringAsync();

                try
                {
                    return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<LoginResponse>(response));
                }
                catch (Exception ex)
                {
                    Log.Debug("ServerConnector.LoginAsync", ex.ToString());
                    return null;
                }
            }
        }
        #endregion

        #region Request access_token async
        public async Task<AccessTokenResponse> RequestAccessTokenAsync(LoginResponse loginresponse)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", loginresponse.Groupname),
                        new KeyValuePair<string, string>("password", loginresponse.Password),
                        new KeyValuePair<string, string>("client_id", loginresponse.ClientId),
                        new KeyValuePair<string, string>("client_secret", loginresponse.ClientSecret)
                     });

                    var httpResponse = await client.PostAsync("/API/token", content);
                    httpResponse.EnsureSuccessStatusCode();

                    var response = await httpResponse.Content.ReadAsStringAsync();

                    try
                    {
                        return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<AccessTokenResponse>(response));
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("ServerConnector.RequestAsyncTokenAsync", ex.ToString());
                return null;
            }
        }
        #endregion

        #region Download mask
        public async Task<string> DownloadMaskAsync(Context context, Session session, string groupname, string masknr)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("access_token", session.AccessTokenResponse.AccessToken),
                        new KeyValuePair<string, string>("groupname", groupname),
                        new KeyValuePair<string, string>("masknr", masknr)
                    });

                    var httpResponse = await client.PostAsync("/API/downloadmask", content);
                    
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync();
                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var newAccessTokenResponse = await RefreshToken(session);

                        if (newAccessTokenResponse != null)
                        {
                            session.AccessTokenResponse = newAccessTokenResponse;
                            var manager = PreferenceManager.GetDefaultSharedPreferences(context);
                            var editor = manager.Edit();
                            var sessionJson = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(session));

                            editor.PutString(AppConstants.Session, sessionJson);
                            editor.Commit();

                            return await DownloadMaskAsync(context, session, groupname, masknr);
                        }
                        else
                        {
                            return "[]";
                        }
                    }
                    else
                    {
                        return "[]";
                    }
                }
            }
            catch
            {
                return "[]";
            }
        }
        #endregion

        #region Download new  mask
        public async Task<string> DownloadNewMask(Context context, Session session, string groupname)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("access_token", session.AccessTokenResponse.AccessToken),
                        new KeyValuePair<string, string>("groupname", groupname)
                    });

                    var httpResponse = await client.PostAsync("/API/downloadfavomask", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync();
                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var newAccessTokenResponse = await RefreshToken(session);

                        if (newAccessTokenResponse != null)
                        {
                            session.AccessTokenResponse = newAccessTokenResponse;
                            var manager = PreferenceManager.GetDefaultSharedPreferences(context);
                            var editor = manager.Edit();
                            var sessionJson = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(session));

                            editor.PutString(AppConstants.Session, sessionJson);
                            editor.Commit();

                            return await DownloadNewMask(context, session, groupname);
                        }
                        else
                        {
                            return "[]";
                        }
                    }
                    else
                    {
                        return "[]";
                    }
                }
            }
            catch
            {
                return "[]";
            }
        }
        #endregion

        #region Check for updates
        public async Task<string> CheckForUpdates(Context context, Session session, int version)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);

                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("access_token", session.AccessTokenResponse.AccessToken),
                        new KeyValuePair<string, string>("app_version", version.ToString())
                    });

                    var httpResponse = await client.PostAsync("/API/checkupdate/", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync();
                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var newAccessTokenResponse = await RefreshToken(session);

                        if (newAccessTokenResponse != null)
                        {
                            session.AccessTokenResponse = newAccessTokenResponse;
                            var manager = PreferenceManager.GetDefaultSharedPreferences(context);
                            var editor = manager.Edit();
                            var sessionJson = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(session));

                            editor.PutString(AppConstants.Session, sessionJson);
                            editor.Commit();

                            return await CheckForUpdates(context, session, version);
                        }
                        else
                        {
                            return "{valid: false, message: \"failed.\"}";
                        }
                    }
                    else
                    {
                        return "{valid: false, message: \"failed.\"}";
                    }
                }
            }
            catch
            {
                return "{valid: false, message: \"failed.\"}";
            }
        }
        #endregion

        #region Download updates
        public async Task<bool> DownloadUpdatesAsync(WebClient client, int version)
        {
            try
            {
                var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString();
                var downloadUpdateFolder = Path.Combine(downloadFolder, "vv-updates");
                if (!Directory.Exists(downloadUpdateFolder))
                    Directory.CreateDirectory(downloadUpdateFolder);
                var downloadPath = Path.Combine(downloadUpdateFolder, "visvitalis.apk");
                byte[] appData = null;

                using (client)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        if (System.IO.File.Exists(downloadPath))
                        {
                            System.IO.File.Delete(downloadPath);
                        }
                    });

                    appData = await client.DownloadDataTaskAsync("http://" + AppConstants.ServerIP + "/API/downloadupdate/" + version.ToString());
                }

                using (var file = System.IO.File.Create(downloadPath))
                {
                    await file.WriteAsync(appData, 0, appData.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("D/exception", ex.ToString());
                return false;
            }
        }
        #endregion

        #region Refresh token
        public async Task<AccessTokenResponse> RefreshToken(Session session)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", session.AccessTokenResponse.RefreshToken),
                        new KeyValuePair<string, string>("client_id", session.LoginResponse.ClientId),
                        new KeyValuePair<string, string>("client_secret", session.LoginResponse.ClientSecret)
                    });

                    var httpResponse = await client.PostAsync("/API/token", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync();
                        return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<AccessTokenResponse>(response));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region Upload data to server async
        public async Task<string> UploadDataAsync(Context context, Session session, string jsonContent, string uri = "uploadfinishedmask")
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);

                    var data = new Dictionary<string, string>();
                    data["data"] = jsonContent;

                    var httpResponse = await client.PostAsync("/API/" + uri, new FormUrlEncodedContent(data));

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadAsStringAsync();
                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var newAccessTokenResponse = await RefreshToken(session);

                        if (newAccessTokenResponse != null)
                        {
                            session.AccessTokenResponse = newAccessTokenResponse;
                            var manager = PreferenceManager.GetDefaultSharedPreferences(context);
                            var editor = manager.Edit();
                            var sessionJson = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(session));

                            editor.PutString(AppConstants.Session, sessionJson);
                            editor.Commit();

                            return await UploadDataAsync(context, session, jsonContent);
                        }
                        else
                        {
                            return "[]";
                        }
                    }
                    else
                    {
                        return "[]";
                    }
                }
            }
            catch
            { 
                return "[]";
            }
        }
        #endregion

        #region Check server availabilty
        /// <summary>
        /// Determines whether the server is online or offline
        /// </summar>
        /// <returns>true or false</returns>
        public async Task<bool> IsServerAvailableAsync()
        {
            var available = false;
            
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    URL url = new URL("http://" + AppConstants.ServerIP);

                    HttpURLConnection urlc = (HttpURLConnection)url.OpenConnection();
                    urlc.SetRequestProperty("User-Agent", "Android Application: 1.00");
                    urlc.SetRequestProperty("Connection", "close");
                    urlc.ConnectTimeout = 4000;
                    urlc.Connect();

                    if (urlc.ResponseCode == HttpStatus.Ok)
                    {
                        available = true;
                    }
                }
                catch
                {
                    available = false;
                }
            });
            
            return available;
        }

        public async Task<bool> IsNetworkAvailable(Context Context)
        {
            var available = false;

            await Task.Run(() =>
            {
                try
                {
                    ConnectivityManager cm = (ConnectivityManager)Context.GetSystemService(Context.ConnectivityService);
                    var networkInfo = cm.GetNetworkInfo(ConnectivityType.Wifi);

                    available = networkInfo.IsConnectedOrConnecting;
                }
                catch
                {
                    available = false;
                }

                if (!available)
                {
                    try
                    {
                        ConnectivityManager cm = (ConnectivityManager)Context.GetSystemService(Context.ConnectivityService);
                        var networkInfo = cm.ActiveNetworkInfo;

                        available = (networkInfo != null && networkInfo.IsConnectedOrConnecting);
                    }
                    catch
                    {
                        available = false;
                    }
                }
            });

            return available;
        }
        #endregion

        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
            }
            catch { }
        }

        public void NotifyUser_DownloadCompleted()
        {
            if (OnNotifyUserDownloadCompleted != null)
            {
                OnNotifyUserDownloadCompleted();
            }
        }
    }
}
using System;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Gms.Gcm;
using Android.Preferences;
using visvitalis.Utils;
using Newtonsoft.Json;
using visvitalis.Networking;
using System.IO;
using System.Globalization;
using visvitalis.JSON;
using Android.Util;

namespace visvitalis.NotificationService
{
    [Service(Exported = false), IntentFilter(new[] { "com.google.android.c2dm.intent.RECEIVE" })]
    class AppGCMListenerService : GcmListenerService
    {
        private readonly string FolderPath = Android.OS.Environment.ExternalStorageDirectory.Path;
        private string _year;
        private string _masknr;
        private string _weeknr;
        private string _justAdd;

        public override void OnMessageReceived(string from, Bundle data)
        {
            var type = data.GetString("type");
            _masknr = data.GetString("masknr");
            _weeknr = data.GetString("weeknr");
            _year = data.GetString("year");
            _justAdd = data.GetString("just_add");

            if (type == "download")
            {
                StartDownloadAsync(_masknr, int.Parse(_weeknr));
            }
            else if (type == "update")
            {
                SendBroadcast(new Intent(this, typeof(UpdateReceiver)));
            }
        }

        async void StartDownloadAsync(string masknr, int weeknr)
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            var groupname = preferences.GetString(AppConstants.GroupName, "undefined");
            var session = preferences.GetString(AppConstants.Session, "undefined");

            if (groupname != "undefined" && session != "undefined")
            {
                var sessionObj = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Session>(session));

                using (var client = new ServerConnector())
                {
                    var content = await client.DownloadMaskAsync(this, sessionObj, groupname, masknr);

                    if (content == "[]")
                    {
                        var loginResponse = await client.LoginAsync(groupname, sessionObj.LoginResponse.Password);

                        if (loginResponse != null && (loginResponse != null && loginResponse.Valid))
                        {
                            var accessTokenResponse = await client.RequestAccessTokenAsync(loginResponse);
                            sessionObj.LoginResponse = loginResponse;

                            if (accessTokenResponse != null)
                            {
                                sessionObj.AccessTokenResponse = accessTokenResponse;

                                var editor = preferences.Edit();
                                editor.PutString(AppConstants.Session, await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(sessionObj)));
                                editor.Commit();

                                StartDownloadAsync(masknr, weeknr);
                            }
                        }
                    }
                    else
                    {
                        var rootObj = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                        rootObj.MaskNr = _masknr;
                        rootObj.WeekNr = _weeknr;
                        rootObj.Groupname = sessionObj.LoginResponse.Groupname;

                        var newContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(rootObj));
                        await CreateMaskFileAsync(int.Parse(_masknr), newContent);
                        SendNotification("Maske wurde runtergeladen", "Es wurden die Daten (Nr. " + _masknr + ") runtergeladen. Diese können nun bearbeitet werden.");
                    }
                }
            }
        }

        #region Notification
        void SendNotification(string title, string message)
        {
            Notification.BigTextStyle textStyle = new Notification.BigTextStyle();
            string longTextMessage = message;
            textStyle.BigText(longTextMessage);
            textStyle.SetSummaryText("Jetzt mit der App öffnen!");

            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new Notification.Builder(this)
                .SetSmallIcon(Resource.Drawable.ic_launcher)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetStyle(textStyle)
                .SetAutoCancel(true)
                .SetDefaults(NotificationDefaults.All)
                .SetContentIntent(pendingIntent);

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
        #endregion

        async System.Threading.Tasks.Task<bool> CreateMaskFileAsync(int maskid, string content)
        {
            var result = false;

            await CreateFolderAsync();
            await CreateFutureFileAsync(maskid, content);

            return result;
        }

        async System.Threading.Tasks.Task CreateFutureFileAsync(int maskid, string content)
        {
            var filePath = Path.Combine(FolderPath, AppConstants.DataFolder, "futuremasks", "mask.json");

            using (var fs = File.Create(filePath))
            {
                var buffer = Encoding.UTF8.GetBytes(content);
                await fs.WriteAsync(buffer, 0, buffer.Length);
                await fs.FlushAsync();
                fs.Close();
            }
        }
        
        private async System.Threading.Tasks.Task CreateFolderAsync()
        {
            await System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, "temp");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, "temp", "old.data");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, "futuremasks");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            });
        }
    }
}
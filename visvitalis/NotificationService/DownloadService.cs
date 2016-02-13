using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using visvitalis.Networking;
using Newtonsoft.Json;
using Android.Preferences;
using visvitalis.Utils;
using visvitalis.JSON;
using Android.Util;
using System.IO;

namespace visvitalis.NotificationService
{
    [Service]
    class DownloadService : Service
    {
        private readonly string FolderPath = Android.OS.Environment.ExternalStorageDirectory.Path;
        DownloadServiceBinder binder;

        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Task.Factory.StartNew(() =>
            {
                DoWork();
            });

            return StartCommandResult.Sticky;
        }

        private async void DoWork()
        {
            try
            {
                var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
                var groupname = preferences.GetString(AppConstants.GroupName, "undefined");
                var session = preferences.GetString(AppConstants.Session, "undefined");

                if (groupname != "undefined" && session != "undefined")
                {
                    var sessionObj = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Session>(session));

                    using (var client = new ServerConnector())
                    {
                        var content = await client.DownloadNewMask(this, sessionObj, groupname);

                        if (content == "[]")
                        {
                            var loginResponse = await client.LoginAsync(groupname, sessionObj.LoginResponse.Password);

                            if (loginResponse != null && (loginResponse != null && loginResponse.Valid))
                            {
                                var accessTokenResponse = await client.RequestAsyncTokenAsync(loginResponse);
                                sessionObj.LoginResponse = loginResponse;

                                if (accessTokenResponse != null)
                                {
                                    sessionObj.AccessTokenResponse = accessTokenResponse;

                                    var editor = preferences.Edit();
                                    editor.PutString(AppConstants.Session, await Task.Factory.StartNew(() => JsonConvert.SerializeObject(sessionObj)));
                                    editor.Commit();

                                    DoWork();
                                }
                            }
                        }
                        else
                        {
                            var rootObj = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                            rootObj.WeekNr = "0";
                            rootObj.Groupname = sessionObj.LoginResponse.Groupname;
                            var newContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(rootObj));

                            if (rootObj.IsNew)
                            {
                                await CreateMaskFileAsync(0, newContent);
                                SendNotification("Maske wurde runtergeladen", "Es wurden neue Daten heruntergeladen. Diese können nun bearbeitet werden.");
                            }
                        }
                    }
                }
            }
            catch
            {
                
            }

            StopSelf();
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

        async Task<bool> CreateMaskFileAsync(int maskid, string content)
        {
            var result = false;

            await CreateFutureFileAsync(maskid, content);

            return result;
        }

        async Task CreateFutureFileAsync(int maskid, string content)
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

        public override IBinder OnBind(Intent intent)
        {
            binder = new DownloadServiceBinder(this);
            return binder;
        }
    }

    class DownloadServiceBinder : Binder
    {
        DownloadService service;

        public DownloadServiceBinder(DownloadService service)
        {
            this.service = service;
        }

        public DownloadService GetService()
        {
            return service;
        }
    }
}
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
using Android.Gms.Gcm;
using Android.Preferences;
using visvitalis.Utils;
using Newtonsoft.Json;
using visvitalis.Networking;
using Android.Util;
using System.IO;
using System.Globalization;
using visvitalis.JSON;

namespace visvitalis.NotificationService
{
    [Service(Exported = false), IntentFilter(new[] { "com.google.android.c2dm.intent.RECEIVE" })]
    class AppGCMListenerService : GcmListenerService
    {
        private readonly string FolderPath = Android.OS.Environment.ExternalStorageDirectory.Path;
        private string _year;
        private string _masknr;
        private string _weeknr;

        public override void OnMessageReceived(string from, Bundle data)
        {
            var type = data.GetString("type");
            _masknr = data.GetString("masknr");
            _weeknr = data.GetString("weeknr");
            _year = data.GetString("year");

            if (type == "download")
            {
                StartDownloadAsync(_masknr, int.Parse(_weeknr));
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
                            var accessTokenResponse = await client.RequestAsyncTokenAsync(loginResponse);
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

                        SaveContentInFiles(newContent, weeknr);

                        SendNotification("Maske wurde runtergeladen", "Es wurden die Daten für die Woche " + weeknr + " runtergeladen.");
                    }
                }
            }
        }

        void SendNotification(string title, string message)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new Notification.Builder(this)
                .SetSmallIcon(Resource.Drawable.ic_launcher)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetAutoCancel(true)
                .SetDefaults(NotificationDefaults.All)
                .SetContentIntent(pendingIntent);

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(0, notificationBuilder.Build());
        }

        async void SaveContentInFiles(string content, int weekId)
        {
            await CreateFilesForWeek(content, weekId);
        }

        async System.Threading.Tasks.Task CreateFilesForWeek(string content, int weekId)
        {
            await CreateWeekFolderAsync(weekId);
            var firstDayOfTheWeek = GetFirstDateOfWeek(DateTime.Now.Year, weekId);

            for (int i = 0; i < 7; i++)
            {
                var theDate = firstDayOfTheWeek.AddDays(i);
                var filePath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, weekId.ToString(), theDate.ToString("ddMMyy") + ".json");

                using (var fs = File.Create(filePath))
                {
                    var buffer = Encoding.UTF8.GetBytes(content);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        private async System.Threading.Tasks.Task CreateWeekFolderAsync(int weekid)
        {
            await System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var yearDirectoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year);

                if (!Directory.Exists(yearDirectoryPath))
                {
                    Directory.CreateDirectory(yearDirectoryPath);
                }

                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, weekid.ToString());

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            });
        }

        private DateTime GetFirstDateOfWeek(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.InvariantCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }
    }
}
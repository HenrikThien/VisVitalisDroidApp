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

            if (type == "download" && _justAdd != "true")
            {
                StartDownloadAsync(_masknr, int.Parse(_weeknr), false);
            }
            else
            {
                StartDownloadAsync(_masknr, int.Parse(_weeknr), true);
            }
        }

        async void StartDownloadAsync(string masknr, int weeknr, bool justAdd)
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

                                StartDownloadAsync(masknr, weeknr, justAdd);
                            }
                        }
                    }
                    else
                    {
                        if (!justAdd)
                        {
                            var rootObj = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                            rootObj.MaskNr = _masknr;
                            rootObj.WeekNr = _weeknr;
                            rootObj.Groupname = sessionObj.LoginResponse.Groupname;

                            var newContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(rootObj));

                            SaveContentInFiles(newContent, weeknr);
                            SendNotification("Maske wurde runtergeladen", "Es wurden die Daten für die Woche " + weeknr + " runtergeladen. Diese können nun bearbeitet werden.");
                        }
                        else
                        {
                            var rootObj = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                            rootObj.MaskNr = _masknr;
                            rootObj.WeekNr = _weeknr;
                            rootObj.Groupname = sessionObj.LoginResponse.Groupname;

                            var patients = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(rootObj.PatientMask));
                            AddContentToFiles(patients, weeknr);
                            SendNotification("Datensätze wurden hinzugefügt", "Es wurden neue Datensätze zur Maske hinzugefügt! Diese können nun genutzt werden.");
                        }
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
                .SetStyle(textStyle)
                .SetAutoCancel(true)
                .SetDefaults(NotificationDefaults.All)
                .SetContentIntent(pendingIntent);

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
        #endregion

        async void SaveContentInFiles(string content, int weekId)
        {
            await CreateFilesForWeek(content, weekId, false);
            await SaveMaskForFuture(content, weekId, false);
        }

        async void AddContentToFiles(string content, int weekId)
        {
            await CreateFilesForWeek(content, weekId, true);
            await SaveMaskForFuture(content, weekId, true);
        }

        async System.Threading.Tasks.Task CreateFilesForWeek(string content, int weekId, bool justAdd)
        {
            await CreateWeekFolderAsync(weekId, justAdd);

            var newWeekId = (weekId < 10) ? "0" + weekId.ToString() : weekId.ToString();
            var firstDayOfTheWeek = GetFirstDateOfWeek(DateTime.Now.Year, weekId);

            for (int i = 0; i < 7; i++)
            {
                var theDate = firstDayOfTheWeek.AddDays(i);
                var filePath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, newWeekId, theDate.ToString("ddMMyy") + ".json");

                if (justAdd)
                {
                    RootObject oldContent = null;
                    using (var fs = File.OpenRead(filePath))
                    {
                        using (MemoryStream data = new MemoryStream())
                        {
                            fs.CopyTo(data);
                            data.Seek(0, SeekOrigin.Begin);
                            byte[] buf = new byte[data.Length];
                            await data.ReadAsync(buf, 0, buf.Length);

                            oldContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(Encoding.Default.GetString(data.ToArray())));
                            PatientMask newPatients = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<PatientMask>(content));

                            try
                            {
                                oldContent.PatientMask.PatientOperation.Tours[0].Patients.AddRange(newPatients.PatientOperation.Tours[0].Patients);
                            }
                            catch (Exception ex)
                            {
                                Log.Debug("debug", "Exception[0]: " + ex.ToString());
                            }

                            try
                            {
                                oldContent.PatientMask.PatientOperation.Tours[1].Patients.AddRange(newPatients.PatientOperation.Tours[1].Patients);
                            }
                            catch(Exception ex)
                            {
                                Log.Debug("debug", "Exception[1]: " + ex.ToString());
                            }

                            newPatients = null;
                            await data.FlushAsync();
                        }
                        await fs.FlushAsync();
                    }

                    using (var fs = File.OpenWrite(filePath))
                    {
                        var newContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(oldContent));
                        var buffer = Encoding.UTF8.GetBytes(newContent);
                        await fs.WriteAsync(buffer, 0, buffer.Length);
                        await fs.FlushAsync();
                    }
                }
                else
                {
                    using (var fs = File.Create(filePath))
                    {
                        var buffer = Encoding.UTF8.GetBytes(content);
                        await fs.WriteAsync(buffer, 0, buffer.Length);
                        await fs.FlushAsync();
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task CreateWeekFolderAsync(int weekid, bool justAdd)
        {
            await System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder);

                if (!Directory.Exists(directoryPath) && !justAdd)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var yearDirectoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year);

                if (!Directory.Exists(yearDirectoryPath) && !justAdd)
                {
                    Directory.CreateDirectory(yearDirectoryPath);
                }

                var newWeekId = (weekid < 10) ? "0" + weekid.ToString() : weekid.ToString();
                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _year, newWeekId);

                if (!Directory.Exists(directoryPath) && !justAdd)
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


        public async System.Threading.Tasks.Task SaveMaskForFuture(string content, int weeknr, bool justAdd)
        {
            var filePath = "";

            if (!justAdd)
            {
                await System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, "futuremasks");

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var maskFile = Path.Combine(directoryPath, "mask_" + weeknr + ".json");
                    filePath = maskFile;
                });

                using (var fs = File.Create(filePath))
                {
                    var buffer = Encoding.UTF8.GetBytes(content);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            else
            {
                await System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, "futuremasks");
                    var maskFile = Path.Combine(directoryPath, "mask_" + weeknr + ".json");
                    filePath = maskFile;
                });

                RootObject oldContent = null;
                using (var fs = File.OpenRead(filePath))
                {
                    using (MemoryStream data = new MemoryStream())
                    {
                        fs.CopyTo(data);
                        data.Seek(0, SeekOrigin.Begin);
                        byte[] buf = new byte[data.Length];
                        await data.ReadAsync(buf, 0, buf.Length);

                        oldContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(Encoding.Default.GetString(data.ToArray())));
                        PatientMask newPatients = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.DeserializeObject<PatientMask>(content));

                        try
                        {
                            oldContent.PatientMask.PatientOperation.Tours[0].Patients.AddRange(newPatients.PatientOperation.Tours[0].Patients);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("debug", "Exception[0]: " + ex.ToString());
                        }

                        try
                        {
                            oldContent.PatientMask.PatientOperation.Tours[1].Patients.AddRange(newPatients.PatientOperation.Tours[1].Patients);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("debug", "Exception[1]: " + ex.ToString());
                        }

                        newPatients = null;
                        await data.FlushAsync();
                    }
                    await fs.FlushAsync();
                }

                using (var fs = File.OpenWrite(filePath))
                {
                    var newContent = await System.Threading.Tasks.Task.Factory.StartNew(() => JsonConvert.SerializeObject(oldContent));
                    var buffer = Encoding.UTF8.GetBytes(newContent);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                    await fs.FlushAsync();
                }
            }
        }
    }
}
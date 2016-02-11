using System;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using Android.Preferences;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using visvitalis.Utils;
using visvitalis.Networking;
using Android.Content.PM;
using Newtonsoft.Json;
using visvitalis.JSON;
using Java.Lang;
using System.Net;
using Android.Content;
using System.IO;

namespace visvitalis
{
    [Activity(Label = "Einstellungen", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme")]
    public class SettingsActivity : PreferenceActivity, Preference.IOnPreferenceClickListener
    {
        private int id = 1;
        private NotificationManager mNotifyManager;
        private NotificationCompat.Builder mBuilder;
        private AppCompatDelegate mDelegate;
        private string mDeviceId;
        private int fileId = 0;

        protected override void OnCreate(Bundle bundle)
        {
            GetDelegate().InstallViewFactory();
            GetDelegate().OnCreate(bundle);

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SettingsLayout);

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.toolbar));

            GetDelegate().SupportActionBar.SetHomeButtonEnabled(true);
            GetDelegate().SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            AddPreferencesFromResource(Resource.Xml.settings);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);

            var deviceName = prefs.GetString(AppConstants.GroupName, "%undefined%");
            var preference = FindPreference("deviceAccount");
            preference.Summary = deviceName;

            var deviceToken = prefs.GetString(AppConstants.DeviceToken, "%undefined");
            preference = FindPreference("deviceToken");
            preference.Summary = deviceToken;

            PackageInfo pInfo = PackageManager.GetPackageInfo(PackageName, 0);
            var appVersion = pInfo.VersionCode + " - " + pInfo.VersionName;
            preference = FindPreference("appVersion");
            preference.Summary = appVersion;


            if (deviceToken.Length > 0 && deviceToken.Length >= 31)
            {
                var shortToken = deviceToken.Substring(1, 30) + "...";
                preference = FindPreference("deviceToken");
                mDeviceId = deviceToken;
                preference.Summary = shortToken;
                preference.OnPreferenceClickListener = this;
            }

            var button = new Button(this);
            button.Text = "Auf Updates überprüfen.";
            button.Click += async delegate
            {
                button.Enabled = false;
                button.Text = "Bitte warten...";
                using (var client = new ServerConnector())
                {
                    if (await client.IsNetworkAvailable(this))
                    {
                        if (await client.IsServerAvailableAsync())
                        {
                            var response = await client.CheckForUpdates(this, StaticHolder.SessionHolder, pInfo.VersionCode);
                            
                            try
                            {
                                var msg = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));

                                if (msg.Valid && msg.NewestVersion > pInfo.VersionCode)
                                {
                                    var webclient = new WebClient();
                                    webclient.DownloadProgressChanged += Webclient_DownloadProgressChanged;
                                    webclient.DownloadFileCompleted += Webclient_DownloadFileCompleted;

                                    mNotifyManager = (NotificationManager)GetSystemService(NotificationService);
                                    mBuilder = new NotificationCompat.Builder(this);
                                    mBuilder.SetContentTitle("VisVitalis App: Update download.")
                                        .SetContentText("Das neue Update wird runtergeladen.")
                                        .SetSmallIcon(Android.Resource.Drawable.StatSysDownload);

                                    if (await client.DownloadUpdatesAsync(webclient, version: msg.NewestVersion))
                                    {
                                        fileId = msg.NewestVersion;
                                        Toast.MakeText(this, msg.Message, ToastLength.Short).Show();
                                        button.Text = "Auf Updates überprüfen.";
                                        button.Enabled = true;
                                    }
                                    else
                                    {
                                        Toast.MakeText(this, "Es konnte kein Update heruntergeladen werden.", ToastLength.Short).Show();
                                        button.Text = "Auf Updates überprüfen.";
                                        button.Enabled = true;
                                    }
                                }
                                else if (msg.Valid && msg.NewestVersion == pInfo.VersionCode)
                                {
                                    Toast.MakeText(this, "Die App ist auf dem neusten Stand.", ToastLength.Short).Show();
                                    button.Text = "Auf Updates überprüfen.";
                                    button.Enabled = true;
                                }
                                else
                                {
                                    Toast.MakeText(this, "Fehler bei der Überprüfung.", ToastLength.Short).Show();
                                    button.Text = "Auf Updates überprüfen.";
                                    button.Enabled = true;
                                }
                            }
                            catch
                            {
                                Toast.MakeText(this, "Fehler bei der Überprüfung.", ToastLength.Short).Show();
                                button.Text = "Auf Updates überprüfen.";
                                button.Enabled = true;
                            }
                        }
                        else
                        {
                            Toast.MakeText(this, "Der Server ist derzeit nicht erreichbar.", ToastLength.Short).Show();
                            button.Text = "Auf Updates überprüfen.";
                            button.Enabled = true;
                        }
                    }
                    else
                    {
                        Toast.MakeText(this, "Es muss eine Internetverbindung hergestellt werden.", ToastLength.Short).Show();
                        button.Text = "Auf Updates überprüfen.";
                        button.Enabled = true;
                    }
                }
            };

            ListView.AddFooterView(button);
        }

        private void Webclient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            mBuilder.SetProgress(100, e.ProgressPercentage, false);
            mBuilder.SetAutoCancel(true);
            mBuilder.SetContentText("Status: " + FormatBytes(e.BytesReceived) + "/" + FormatBytes(e.TotalBytesToReceive));
            mNotifyManager.Notify(id, mBuilder.Build());
        }

        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private void Webclient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                mBuilder.SetContentText("Download konnte nicht fertig gestellt werden.")
                  .SetTicker("Der Download wurde nicht fertig gestellt, bitte erneut versuchen.")
                  .SetContentTitle("Fehler beim herunterladen.")
                  .SetProgress(0, 0, false)
                  .SetDefaults((int)NotificationDefaults.All)
                  .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone);

                mNotifyManager.Notify(id, mBuilder.Build());
                return;
            }

            var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString();
            var downloadPath = Path.Combine(downloadFolder, "visvitalis-" + fileId + ".apk");

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(downloadPath)), "application/vnd.android.package-archive");
            intent.SetFlags(ActivityFlags.NewTask);
            intent.PutExtra("FILE_APK_PATH", downloadPath);

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.NoCreate);

            var textStyle = new NotificationCompat.BigTextStyle();
            string longTextMessage = "Der Download wurde fertig gestellt, jetzt installieren.\n" + downloadPath;
            textStyle.BigText(longTextMessage);
            textStyle.SetSummaryText("Jetzt mit der App öffnen!");

            mBuilder.SetContentText("Download wurde fertig gestellt.")
              .SetTicker("Der Download wurde fertig gestellt, jetzt installieren.")
              .SetStyle(textStyle)
              .SetProgress(0, 0, false)
              .SetAutoCancel(true)
              .SetDefaults((int)NotificationDefaults.All)
              .SetContentIntent(pendingIntent)
              .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone);

            mNotifyManager.Notify(id, mBuilder.Build());
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            GetDelegate().OnPostCreate(savedInstanceState);
        }

        public override void SetContentView(int layoutResID)
        {
            GetDelegate().SetContentView(layoutResID);
        }

        protected override void OnPostResume()
        {
            base.OnPostResume();
            GetDelegate().OnPostResume();
        }

        protected override void OnStop()
        {
            base.OnStop();
            GetDelegate().OnStop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GetDelegate().OnDestroy();
        }

        public override bool OnOptionsItemSelected(IMenuItem menu)
        {
            switch (menu.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }

        private void SetSupportActionBar(Toolbar toolbar)
        {
            GetDelegate().SetSupportActionBar(toolbar);
        }

        private AppCompatDelegate GetDelegate()
        {
            if (mDelegate == null)
            {
                mDelegate = AppCompatDelegate.Create(this, null);
            }

            return mDelegate;
        }

        public bool OnPreferenceClick(Preference preference)
        {
            if (preference.HasKey && preference.Key == "deviceToken")
            {
                Toast.MakeText(this, mDeviceId, ToastLength.Short).Show();
            }

            return true;
        }
    }
}
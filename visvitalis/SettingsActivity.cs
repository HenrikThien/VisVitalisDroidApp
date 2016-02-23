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
using visvitalis.NotificationService;

namespace visvitalis
{
    [Activity(Label = "Einstellungen", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme")]
    public class SettingsActivity : PreferenceActivity, Preference.IOnPreferenceClickListener
    {
        private AppCompatDelegate mDelegate;
        private string mDeviceId;

        protected override void OnCreate(Bundle bundle)
        {
            GetDelegate().InstallViewFactory();
            GetDelegate().OnCreate(bundle);

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SettingsLayout);

            var scrollToBottom = (bundle != null) ? bundle.GetBoolean("SCROLL_2_BOTTOM") : false;

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

            var installUpdateBtn = new Button(this);
            installUpdateBtn.Text = "Updates installieren.";
            installUpdateBtn.Enabled = false;

            var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString();
            var downloadUpdateFolder = Path.Combine(downloadFolder, "vv-updates");
            var downloadPath = Path.Combine(downloadUpdateFolder, "visvitalis.apk");

            if (!File.Exists(downloadPath))
            {
                installUpdateBtn.Enabled = false;
                installUpdateBtn.Text = "Es sind keine neuen Updates verfügbar.";
            }
            else if (File.Exists(downloadPath) && !IsNewerVersion(pInfo.VersionCode, pInfo.VersionName, downloadPath))
            {
                installUpdateBtn.Enabled = false;
                installUpdateBtn.Text = "Es sind keine neuen Updates verfügbar.";
            }
            else
            {
                installUpdateBtn.Enabled = true;
                installUpdateBtn.Text = "Die neuen Updates jetzt installieren";
            }

            installUpdateBtn.Click += delegate
            {
                installUpdateBtn.Enabled = false;
                installUpdateBtn.Text = "Bitte warten...";

                var intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(downloadPath)), "application/vnd.android.package-archive");
                intent.SetFlags(ActivityFlags.NewTask);
                intent.PutExtra("FILE_APK_PATH", downloadPath);
                StartActivity(intent);
            };


            var searchUpdateBtn = new Button(this);
            searchUpdateBtn.Text = "Nach neuen Updates suchen.";
            searchUpdateBtn.Enabled = !(installUpdateBtn.Enabled); // wenn kein aktuelles update vorhanden ist, ist suchen erlaubt.

            searchUpdateBtn.Click += async delegate
            {
                ChangeButton(false, "Nach Updates suchen, bitte warten...", searchUpdateBtn);

                using (var connector = new ServerConnector())
                {
                    if (await connector.IsNetworkAvailable(this))
                    {
                        if (await connector.IsServerAvailableAsync())
                        {
                            var response = await connector.CheckForUpdates(this, StaticHolder.SessionHolder, pInfo.VersionCode);

                            try
                            {
                                var msg = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));

                                if (msg.Valid && msg.NewestVersion > pInfo.VersionCode)
                                {
                                    SendBroadcast(new Intent(this, typeof(UpdateReceiver)));
                                    Toast.MakeText(this, "Updates werden nun heruntergeladen!", ToastLength.Long).Show();
                                    ChangeButton(false, "Nach neuen Updates suchen.", searchUpdateBtn);
                                }
                                else
                                {
                                    Toast.MakeText(this, "Zurzeit stehen keine neuen Updates zur Verfügung.", ToastLength.Long).Show();
                                    ChangeButton(false, "Nach neuen Updates suchen.", searchUpdateBtn);
                                }
                            }
                            catch
                            {
                                Toast.MakeText(this, "Zurzeit stehen keine neuen Updates zur Verfügung.", ToastLength.Long).Show();
                                ChangeButton(false, "Nach neuen Updates suchen.", searchUpdateBtn);
                            }
                        }
                        else
                        {
                            Toast.MakeText(this, "Der Server ist derzeit nicht erreichbar.", ToastLength.Long).Show();
                            ChangeButton(true, "Nach neuen Updates suchen.", searchUpdateBtn);
                        }
                    }
                    else
                    {
                        Toast.MakeText(this, "Es muss eine Internetverbindung vorhanden sein.", ToastLength.Long).Show();
                        ChangeButton(true, "Nach neuen Updates suchen.", searchUpdateBtn);
                    }
                }
            };


            ListView.SetHeaderDividersEnabled(true);
            ListView.AddHeaderView(installUpdateBtn);
            ListView.AddHeaderView(searchUpdateBtn);
        }

        private void ChangeButton(bool enable, string text, Button button)
        {
            button.Enabled = enable;
            button.Text = text;
        }

        private bool IsNewerVersion(int cVersion, string cName, string path)
        {
            PackageInfo info = PackageManager.GetPackageArchiveInfo(path, 0);
            return (info.VersionCode > cVersion && info.VersionName != cName);
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
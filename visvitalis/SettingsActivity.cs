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

            var button = new Button(this);
            button.Text = "Updates installieren.";

            var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString();
            var downloadUpdateFolder = Path.Combine(downloadFolder, "vv-updates");
            var downloadPath = Path.Combine(downloadUpdateFolder, "visvitalis.apk");

            if (!File.Exists(downloadPath))
            {
                button.Enabled = false;
                button.Text = "Updates installieren";
            }

            button.Click += delegate
            {
                button.Enabled = false;
                button.Text = "Bitte warten...";

                var intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(downloadPath)), "application/vnd.android.package-archive");
                intent.SetFlags(ActivityFlags.NewTask);
                intent.PutExtra("FILE_APK_PATH", downloadPath);
                StartActivity(intent);
            };

            ListView.SetHeaderDividersEnabled(true);
            ListView.AddHeaderView(button);
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
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

            if (deviceToken.Length > 0 && deviceToken.Length >= 31)
            {
                var shortToken = deviceToken.Substring(1, 30) + "...";
                preference = FindPreference("deviceToken");
                mDeviceId = deviceToken;
                preference.Summary = shortToken;
                preference.OnPreferenceClickListener = this;
            }
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
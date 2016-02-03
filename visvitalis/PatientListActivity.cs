using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using visvitalis.Utils;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using visvitalis.JSON;
using Android.Text;
using Android.Text.Style;
using visvitalis.Networking;
using System.Collections.Generic;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V4.View;
using visvitalis.Fragments;
using com.refractored;
using Android.Util;
using Android.Support.V4.App;

namespace visvitalis
{
    [Activity(Label = "Liste der Patienten", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class PatientListActivity : AppCompatActivity, ViewPager.IOnPageChangeListener
    {
        private ProgressDialog _progressDialog;
        private DateTime _fileDateTime;
        private string _fileWorkerToken;
        private string _fileDate;
        private bool _oldFile;

        private PagerSlidingTabStrip mSlidingTabLayout;
        private ViewPager mViewPager;
        private CompatPagerAdapter mPagerAdapter;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            OverridePendingTransition(Resource.Animation.from_left, Resource.Animation.hold);
            SetContentView(Resource.Layout.PatientListView);

            var jsonContent = Intent.GetStringExtra(AppConstants.JsonMask);
            var date = Intent.GetStringExtra(AppConstants.FileDate);
            _fileWorkerToken = Intent.GetStringExtra(AppConstants.FileWorkerToken);
            _oldFile = Intent.GetBooleanExtra(AppConstants.LoadOldFile, false);

            if (string.IsNullOrEmpty(jsonContent))
            {
                FinishActivity(0);
            }

            _fileDate = date;
            DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);
            Init(date, jsonContent);
        }

        protected override void OnPause()
        {
            OverridePendingTransition(Resource.Animation.hold, Resource.Animation.to_left);
            base.OnPause();
        }

        void Init(string date, string content)
        {
            mPagerAdapter = new CompatPagerAdapter(this, _oldFile, _fileWorkerToken, "", date, SupportFragmentManager);
            mViewPager = FindViewById<ViewPager>(Resource.Id.pager);
            mSlidingTabLayout = FindViewById<PagerSlidingTabStrip>(Resource.Id.tabs);

            mViewPager.Adapter = mPagerAdapter;
            mViewPager.OffscreenPageLimit = 1;
            mViewPager.AddOnPageChangeListener(this);

            mSlidingTabLayout.SetViewPager(mViewPager);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetIcon(null);

            SupportActionBar.Title = "Einsätze - " + _fileWorkerToken;
            SupportActionBar.Subtitle = "Datum: " + _fileDateTime.ToString("dd - MM - yyyy");
        }
        
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Clear();
            MenuInflater.Inflate(Resource.Menu.vvmenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem menu)
        {
            switch (menu.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.finish:
                    AskToUpload();
                    return true;
                case Resource.Id.neuerEinsatz:
                    AskToCreateNewEntry();
                    return true;
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }

        public void UploadDataAsync()
        {
            
        }

        void CreateAlert(string title, string message)
        {
            var alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton("Ok", (sender, args) => { alert.Dispose(); });
            alert.Show();
        }

        void AskToUpload()
        {
            var alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("Daten zum Server senden?");
            alert.SetMessage("Sind Sie sicher, dass die Daten zum Server geschickt werden sollen?\nWurden alle Daten ausgefüllt?");
            alert.SetNegativeButton("Abbrechen", (sender, args) => { alert.Dispose(); });
            alert.SetPositiveButton("Absenden", (sender, args) => { UploadDataAsync(); });
            alert.Show();
        }

        void AskToCreateNewEntry()
        {
            var alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("Neuen Einsatz erstellen?");
            alert.SetMessage("Möchten Sie einen neuen Einsatz erstellen?\nDieser wird nur für den heutigen Tag erstellt.");
            alert.SetNegativeButton("Abbrechen", (sender, args) => { alert.Dispose(); });
            alert.SetPositiveButton("Weiter", (sender, args) => { CreateNewEntry(); });
            alert.Show();
        }

        void CreateNewEntry()
        {
            var intent = new Intent();
            intent.SetClass(this, typeof(CreateNewEntryActivity));
            StartActivity(intent);
        }

        public void OnPageScrollStateChanged(int state)
        {
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
        }

        public void OnPageSelected(int position)
        {
            var fragment = (mViewPager.Adapter as CompatPagerAdapter).GetFragment(position);

            if (fragment != null)
            {
                fragment.OnResume();
            }
        }
    }
}
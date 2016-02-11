using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using visvitalis.Utils;
using System.Globalization;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V4.View;
using visvitalis.Fragments;
using com.refractored;

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

            mPagerAdapter.ViewPager = mViewPager;

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
                default:
                    return base.OnOptionsItemSelected(menu);
            }
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
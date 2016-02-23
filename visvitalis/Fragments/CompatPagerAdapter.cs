using System;

using Android.App;
using Android.Content;
using Android.Support.V4.App;
using Android.Views;
using Java.Lang;
using visvitalis.Recycler;
using System.Collections.Generic;
using Android.Util;
using Android.Support.V4.View;

namespace visvitalis.Fragments
{
    public class CompatPagerAdapter : FragmentPagerAdapter
    {
        public ViewPager ViewPager { get; set; }
        private readonly string[] Titles = { "Morgens", "Abends", "Neu" };

        private readonly Dictionary<int, string> _fragmentTags;
        private readonly string _date;
        private readonly string _text;
        private readonly Android.Support.V4.App.FragmentManager _fragmentManager;
        private readonly Context _appContext;
        private readonly string _workerToken;
        private readonly bool _loadOldFile;

        public CompatPagerAdapter(Context appContext, bool loadOldFile, string workerToken, string text, string date, Android.Support.V4.App.FragmentManager fgManager) : base(fgManager)
        {
            _workerToken = workerToken;
            _appContext = appContext;
            _text = text;
            _date = date;
            _fragmentManager = fgManager;
            _loadOldFile = loadOldFile;
            _fragmentTags = new Dictionary<int, string>();
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            return new Java.Lang.String(Titles[position]);
        }

        public override int Count
        {
            get
            {
                return Titles.Length;
            }
        }

        public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
        {
            Java.Lang.Object obj = base.InstantiateItem(container, position);

            if (obj is Android.Support.V4.App.Fragment)
            {
                var frg = (Android.Support.V4.App.Fragment)obj;
                string tag = frg.Tag;
                if (!_fragmentTags.ContainsKey(position))
                    _fragmentTags.Add(position, tag);
            }

            return obj;
        }

        public Android.Support.V4.App.Fragment GetFragment(int position)
        {
            if (_fragmentTags.ContainsKey(position))
            {
                string tag = _fragmentTags[position];
                if (tag == null)
                    return null;
                return _fragmentManager.FindFragmentByTag(tag);
            }
            return null;
        }

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            Android.Support.V4.App.Fragment fragment = null;
            string pageTitle = GetPageTitle(position).ToLower();

            if (position == 0)
            {
                fragment = RecyclerPatientViewFragment.CreateNewInstance(_loadOldFile, "morgens", _date, _workerToken);
            }
            else if (position == 1)
            {
                fragment = RecyclerPatientViewFragment.CreateNewInstance(_loadOldFile, "abends", _date, _workerToken);
            }
            else if (position == 2)
            {
                var frg = NewEntryFragment.CreateInstance(_date);
                frg.OnTabNeedSwitchEvent += Frg_OnTabNeedSwitchEvent;
                return frg;
            }

            return fragment;
        }

        private void Frg_OnTabNeedSwitchEvent(int position)
        {
            if (ViewPager != null)
            {
                ViewPager.SetCurrentItem(position, false);
            }
        }
    }
}

using System;

using Android.App;
using Android.Content;
using Android.Support.V4.App;
using Android.Views;
using Java.Lang;
using visvitalis.Recycler;
using System.Collections.Generic;

namespace visvitalis.Fragments
{
    public class CompatPagerAdapter : FragmentPagerAdapter
    {
        private readonly string[] Titles = { "Morgens", "Abends" };
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
                _fragmentTags.Add(position, tag);
            }

            return obj;
        }

        public Android.Support.V4.App.Fragment GetFragment(int position)
        {
            string tag = _fragmentTags[position];
            if (tag == null)
                return null;
            return _fragmentManager.FindFragmentByTag(tag);
        }

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            RecyclerPatientViewFragment fragment = null;
            string pageTitle = GetPageTitle(position).ToLower();

            fragment = RecyclerPatientViewFragment.CreateNewInstance(_loadOldFile, "morgens", _date, _workerToken);

             if (pageTitle == "abends")
                fragment = RecyclerPatientViewFragment.CreateNewInstance(_loadOldFile, "abends", _date, _workerToken);

            return fragment;
        }
    }
}

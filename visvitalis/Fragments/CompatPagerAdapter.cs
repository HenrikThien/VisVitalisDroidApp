using System;

using Android.App;
using Android.Content;
using Android.Support.V4.App;
using visvitalis.Recycler;

namespace visvitalis.Fragments
{
    public class CompatPagerAdapter : FragmentPagerAdapter
    {
        private readonly string[] Titles = { "Morgens", "Abends" };

        private DateTime _fileDateTime;
        private readonly string _date;
        private string _content;
        private readonly string _text;
        private Android.Support.V4.App.Fragment _view;
        private readonly Android.Support.V4.App.FragmentManager _fragmentManager;
        private Android.Support.V4.App.FragmentTransaction _fragmentTransaction;
        private ProgressDialog _progressDialog;
        private readonly Context _appContext;
        private readonly string _workerToken;

        public CompatPagerAdapter(Context appContext, string workerToken, string text, string date, Android.Support.V4.App.FragmentManager fgManager) : base(fgManager)
        {
            _workerToken = workerToken;
            _appContext = appContext;
            _text = text;
            _date = date;
            _fragmentManager = fgManager;
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

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            RecyclerPatientViewFragment fragment = null;
            string pageTitle = GetPageTitle(position).ToLower();

            fragment = RecyclerPatientViewFragment.CreateNewInstance("morgens", _date, _workerToken);

             if (pageTitle == "abends")
                fragment = RecyclerPatientViewFragment.CreateNewInstance("abends", _date, _workerToken);

            return fragment;
        }
    }
}

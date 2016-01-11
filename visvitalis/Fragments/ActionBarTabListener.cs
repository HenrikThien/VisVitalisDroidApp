using System;
using Android.App;
using Android.Content;
using System.Globalization;
using visvitalis.Utils;
using System.Threading.Tasks;
using Android.Util;

namespace visvitalis.Fragments
{
    public class TabListener : Java.Lang.Object, ActionBar.ITabListener
    {
        private DateTime _fileDateTime;
        private readonly string _date;
        private string _content;
        private readonly string _text;
        private Fragment _view;
        private readonly FragmentManager _fragmentManager;
        private FragmentTransaction _fragmentTransaction;
        private ProgressDialog _progressDialog;
        private readonly Context _appContext;
        private readonly string _workerToken;

        public TabListener(Context appContext, string workerToken, string text, string date, FragmentManager fgManager)
        {
            _workerToken = workerToken;
            _appContext = appContext;
            _text = text;
            _date = date;
            _fragmentManager = fgManager;
        }

        async Task Init()
        {
            ShowLoadingDialog();

            _fragmentTransaction = _fragmentManager.BeginTransaction();

            if (DateTime.TryParseExact(_date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime))
            {
                await ReadFileContent();

                _view = TabFragment.CreateNewInstance("morgens", _date, _content, _workerToken);

                if (_text.ToLower() == "abends")
                    _view = TabFragment.CreateNewInstance("abends", _date, _content, _workerToken);
            }

            _progressDialog.Dismiss();
        }

        async Task ReadFileContent()
        {
            using (var fileManager = new FileManager(_date, _fileDateTime))
            {
                _content = await fileManager.LoadFileAsync();
            }
        }

        public void OnTabReselected(ActionBar.Tab tab, FragmentTransaction ft)
        {
            // todo: save position?
        }

        public async void OnTabSelected(ActionBar.Tab tab, FragmentTransaction ft)
        {
            await Init();

            _fragmentTransaction.Replace(Resource.Id.fragmentContainer, _view);
            _fragmentTransaction.AddToBackStack(null);
            _fragmentTransaction.Commit();
        }

        void ShowLoadingDialog()
        {
            _progressDialog = new ProgressDialog(_appContext);
            _progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            _progressDialog.SetCancelable(false);
            _progressDialog.SetTitle("Laden...");
            _progressDialog.SetMessage("Laden, bitte warten...");
            _progressDialog.SetCanceledOnTouchOutside(false);
            _progressDialog.Show();
        }

        public void OnTabUnselected(ActionBar.Tab tab, FragmentTransaction ft)
        {
            if (ft != null)
            {
                ft.Remove(_view);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_fragmentTransaction != null)
                _fragmentTransaction.Dispose();
            base.Dispose(disposing);
        }
    }
}
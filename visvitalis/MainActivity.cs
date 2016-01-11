using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Globalization;
using visvitalis.Utils;
using Android.Util;

namespace visvitalis
{
    [Activity(Label = "Mitarbeiter bestätigen", Icon = "@drawable/ic_launcher", Theme = "@style/Theme.Main")]
    public class MainActivity : Activity
    {
        private ProgressDialog _progressDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);

            var loadBtn = FindViewById<Button>(Resource.Id.button1);
            loadBtn.Click += LoadBtn_Click;

            var dateBox = FindViewById<EditText>(Resource.Id.editText2);
            dateBox.LongClick += DateBox_LongClick;
        }

        private void DateBox_LongClick(object sender, View.LongClickEventArgs e)
        {
            var dateBox = FindViewById<EditText>(Resource.Id.editText2);
            dateBox.Text = DateTime.Now.ToString("ddMMyy");
        }

        private async void LoadBtn_Click(object sender, EventArgs e)
        {
            var workerToken = FindViewById<EditText>(Resource.Id.editText1).Text;
            var fileDate = FindViewById<EditText>(Resource.Id.editText2).Text;

            if (!string.IsNullOrEmpty(workerToken) && !string.IsNullOrEmpty(fileDate))
            {
                DateTime result;

                if (DateTime.TryParseExact(fileDate, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    CreateLoadingDialog();

                    using (var fileManager = new FileManager(fileDate, result))
                    {
                        var content = await fileManager.LoadFileAsync();
                        _progressDialog.Dismiss();

                        if (content == "[]")
                        {
                            Toast.MakeText(this, "Es existiert keine Datei für das Datum!", ToastLength.Long).Show();
                            var intent = new Intent();
                            intent.SetClass(this, typeof(CreateMaskActivity));
                            StartActivity(intent);
                            return;
                        }

                        var nextIntent = new Intent();
                        nextIntent.SetClass(this, typeof(PatientListActivity));
                        nextIntent.PutExtra(AppConstants.JsonMask, content);
                        nextIntent.PutExtra(AppConstants.FileDate, fileDate);
                        nextIntent.PutExtra(AppConstants.FileWorkerToken, workerToken);
                        StartActivity(nextIntent);
                    }
                }
                else
                {
                    CreateAlert("Fehler", "Das eingegebene Datum ist nicht gültig!");
                }
            }
            else
            {
                CreateAlert("Fehler", "Es müssen beide Felder ausgefüllt werden.");
            }
        }

        void CreateAlert(string title, string message)
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton("Ok", (sender, args) => { alert.Dispose(); });
            alert.Show();
        }

        void CreateLoadingDialog()
        {
            _progressDialog = new ProgressDialog(this);
            _progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            _progressDialog.SetCancelable(false);
            _progressDialog.SetTitle("Laden...");
            _progressDialog.SetMessage("Laden, bitte warten...");
            _progressDialog.SetCanceledOnTouchOutside(false);
            _progressDialog.Show();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Clear();
            MenuInflater.Inflate(Resource.Menu.main_layout, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem menu)
        {
            switch (menu.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                //case Resource.Id.showChat:
                //    {
                //        var intent = new Intent();
                //        intent.SetClass(this, typeof(ChatActivity));
                //        StartActivity(intent);
                //        return true;
                //    }
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }
    }
}
using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Globalization;
using visvitalis.Utils;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using System.Threading.Tasks;
using visvitalis.NotificationService;
using Android.Util;
using Newtonsoft.Json;
using visvitalis.JSON;
using visvitalis.Networking;
using System.IO;

namespace visvitalis
{
    [Activity(Label = "Mitarbeiter bestätigen", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        private ProgressDialog _progressDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var loadBtn = FindViewById<Button>(Resource.Id.button1);
            loadBtn.Click += LoadBtn_Click;

            var dateBox = FindViewById<EditText>(Resource.Id.editText2);
            dateBox.LongClick += DateBox_LongClick;

            SupportActionBar.SetIcon(null);
        }

        protected override void OnPause()
        {
            var intent = new Intent(this, typeof(RegistrationIntentService));
            StartService(intent);
            base.OnPause();
        }

        protected override void OnResume()
        {
            var intent = new Intent(this, typeof(RegistrationIntentService));
            StartService(intent);

            base.OnResume();
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
            var oldFile = false;

            if (!string.IsNullOrEmpty(workerToken) && !string.IsNullOrEmpty(fileDate))
            {
                DateTime result;

                if (DateTime.TryParseExact(fileDate, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    CreateLoadingDialog();

                    using (var fileManager = new FileManager(fileDate, result))
                    {
                        var content = await fileManager.SearchOldFileContent(DateTime.Now.Year.ToString());

                        if (content == "[]")
                        {
                            if (result.Date == DateTime.Now.Date)
                            {
                                content = await fileManager.LoadFileAsync();

                                if (content == "[]")
                                {
                                    content = await fileManager.CreateFileAsync();
                                }
                            }
                            else
                            {
                                Toast.MakeText(this, "Es existiert keine Datei für das Datum.", ToastLength.Short).Show();
                                _progressDialog.Dismiss();
                                return;
                            }
                        }
                        else
                        {
                            oldFile = true;
                        }

                        if (content == "[]")
                        {
                            Toast.MakeText(this, "Es sind noch keine Daten auf diesem Gerät vorhanden!", ToastLength.Long).Show();
                            _progressDialog.Dismiss();
                            return;
                        }

                        _progressDialog.Dismiss();
                        
                        var nextIntent = new Intent();
                        nextIntent.SetClass(this, typeof(PatientListActivity));
                        nextIntent.PutExtra(AppConstants.JsonMask, content);
                        nextIntent.PutExtra(AppConstants.FileDate, fileDate);
                        nextIntent.PutExtra(AppConstants.FileWorkerToken, workerToken);
                        nextIntent.PutExtra(AppConstants.LoadOldFile, oldFile);
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
            var alert = new Android.App.AlertDialog.Builder(this);
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
                case Resource.Id.showSettings:
                    {
                        var intent = new Intent();
                        intent.SetClass(this, typeof(SettingsActivity));
                        StartActivity(intent);
                        return true;
                    }
                case Resource.Id.finish:
                    {
                        AskToUpload();
                        return true;
                    }
                case Resource.Id.logout:
                    {
                        AskToLogout();
                        return true;
                    }
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }

        void AskToLogout()
        {
            var alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle("Von der App abmelden?");
            alert.SetMessage("Sind Sie sicher das Sie sich nun abmelden möchten?\nZum wieder Anmelden wird eine Internetverbindung benötigt.");
            alert.SetNegativeButton("Abbrechen", (sender, args) => { alert.Dispose(); });
            alert.SetPositiveButton("Abmelden", (sender, args) => { InitLogout(); });
            alert.Show();
        }

        private async void InitLogout()
        {
            CreateLoadingDialog();

            var activity = await Logout();
            _progressDialog.Dismiss();

            StartActivity(activity);
            Finish();
        }
        private async Task<Intent> Logout()
        {
            Intent activity = new Intent(this, typeof(SplashScreen));
            activity.SetFlags(ActivityFlags.NoHistory | ActivityFlags.ClearTask);

            StaticHolder.SessionHolder = null;
            await StaticHolder.DestorySession(this);

            return activity;
        }

        public async void UploadDataAsync()
        {
            _progressDialog = new ProgressDialog(this);
            _progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            _progressDialog.SetCancelable(false);
            _progressDialog.SetTitle("Hochladen...");
            _progressDialog.SetMessage("Daten werden hochgeladen, bitte warten...");
            _progressDialog.SetCanceledOnTouchOutside(false);
            _progressDialog.Show();

            try
            {
                using (var fileManager = new FileManager("", DateTime.Now))
                {
                    var listContent = await fileManager.GetFileContentFromTempAsync(DateTime.Now.Year.ToString());

                    if (listContent.Count == 0)
                    {
                        _progressDialog.Dismiss();
                        _progressDialog = null;
                        CreateAlert("Fehler", "Es wurden keine Dateien zum Hochladen gefunden!");
                        return;
                    }

                    var rootObj = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(listContent[0]));
                    listContent.RemoveAt(0);

                    var startDatum = rootObj.PatientMask[0].PatientOperation.MaskDate;
                    var endDatum = rootObj.PatientMask[0].PatientOperation.MaskDate;

                    foreach (var content in listContent)
                    {
                        var deserializedContent = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content));
                        rootObj.PatientMask.AddRange(deserializedContent.PatientMask);
                        endDatum = deserializedContent.PatientMask[0].PatientOperation.MaskDate;
                    }

                    rootObj.DatumStart = startDatum;
                    rootObj.DatumEnd = endDatum;

                    var newContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(rootObj));

                    using (var client = new ServerConnector())
                    {
                        if (await client.IsNetworkAvailable(this))
                        {
                            if (await client.IsServerAvailableAsync())
                            {
                                try
                                {
                                    var response = await client.UploadDataAsync(this, StaticHolder.SessionHolder, newContent);
                                    var newResponse = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));
                                    Toast.MakeText(this, newResponse.Message, ToastLength.Long).Show();

                                    await fileManager.MoveOldFilesAsync(DateTime.Now.Year.ToString());
                                    StartService(new Intent(this, typeof(DownloadService)));
                                }
                                catch
                                {
                                    CreateAlert("Fehler", "Fehler beim Hochladen der Daten.");
                                    _progressDialog.Dismiss();
                                    _progressDialog = null;
                                }
                            }
                            else
                            {
                                CreateAlert("Datenserver", "Der Server scheint derzeit nicht erreichbar zu sein. Versuchen Sie es später erneut.");
                                _progressDialog.Dismiss();
                                _progressDialog = null;
                            }
                        }
                        else
                        {
                            CreateAlert("Internetverbindung", "Es ist keine Internetverbindung verfügbar.");
                            _progressDialog.Dismiss();
                            _progressDialog = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("exception", ex.ToString());
                _progressDialog.Dismiss();
                _progressDialog = null;
            }

            _progressDialog.Dismiss();
            _progressDialog = null;
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
    }
}
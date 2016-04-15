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
using System.Collections.Generic;

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
                            content = await fileManager.LoadFileAsync();

                            if (content == "[]" && result.Date == DateTime.Now.Date)
                            {
                                content = await fileManager.CreateFileAsync();
                            }
                        }
                        else
                        {
                            oldFile = true;
                        }

                        if (content == "[]")
                        {
                            Toast.MakeText(this, "Die Datei konnte nicht gefunden werden!", ToastLength.Long).Show();
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
            var activity = new Intent(this, typeof(SplashScreen));
            activity.SetFlags(ActivityFlags.NoHistory | ActivityFlags.ClearTask);

            StaticHolder.SessionHolder = null;
            await StaticHolder.DestorySession(this);

            return activity;
        }

        public async void UploadDataAsyncNew()
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

                    var patients = new FinishedPatientMask() { Patients = new List<FinishedPatient>() };

                    // files which needs to be updated (string-path, string-content)
                    var filesToUpdate = new Dictionary<string, string>();
                    // files which need to be moved to old.files directory
                    var filesToMove = new List<string>();

                    // loop through all files in temp folder
                    foreach (var content in listContent)
                    {
                        // patient counter
                        var patientCounter = 0;

                        // list of patients which are already sended
                        var listOfSended = new Dictionary<Patient, bool>();

                        // deserialize the mask
                        var mask = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(content.Value));

                        // loop through the whole file.
                        foreach (var operation in mask.PatientMask)
                        {
                            // if date is != current date, move file to old.data, it can't be edited anyway.
                            // make sure, the parsed date is correct.
                            DateTime result;
                            if (DateTime.TryParseExact(operation.PatientOperation.MaskDate, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                            {
                                if (DateTime.Now.Date != result.Date)
                                {
                                    if (!filesToMove.Contains(content.Key))
                                    {
                                        filesToMove.Add(content.Key);
                                    }
                                }
                            }

                            foreach (var tour in operation.PatientOperation.Tours)
                            {
                                foreach (var patient in tour.Patients)
                                {
                                    // count the patients
                                    patientCounter++;

                                    // checks if the patient needs to be uploaded
                                    if (!string.IsNullOrEmpty(patient.Arrival) &&
                                        !string.IsNullOrEmpty(patient.Departure) &&
                                        !string.IsNullOrEmpty(patient.WorkerToken) &&
                                        patient.ServerState != "sended")
                                    {
                                        // set state to "sended"
                                        patient.ServerState = "sended";

                                        // add to sended list
                                        if (!listOfSended.ContainsKey(patient))
                                        {
                                            listOfSended.Add(patient, true);
                                        }

                                        // add to export list
                                        patients.Patients.Add(new FinishedPatient()
                                        {
                                            Date = mask.PatientMask[0].PatientOperation.MaskDate,
                                            Patient = patient,
                                            Groupname = (string.IsNullOrEmpty(mask.Groupname)) ? "Keine Gruppe" : mask.Groupname
                                        });
                                    }
                                    else if (patient.ServerState == "sended")
                                    {
                                        // add to sended list
                                        if (!listOfSended.ContainsKey(patient))
                                        {
                                            listOfSended.Add(patient, true);
                                        }
                                    }
                                }
                            }
                        }

                        // if listOfSended is the same as patientCounter, add file to files which needs to be moved
                        if (listOfSended.Count == patientCounter)
                        {
                            if (!filesToMove.Contains(content.Key))
                            {
                                filesToMove.Add(content.Key);
                            }
                        }

                        // save "sended" states in file
                        var stateContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(mask));

                        if (!filesToUpdate.ContainsKey(content.Key))
                        {
                            filesToUpdate.Add(content.Key, stateContent);
                        }
                    }


                    if (patients.Patients.Count == 0)
                    {
                        _progressDialog.Dismiss();
                        _progressDialog = null;
                        CreateAlert("Fehler", "Es wurden keine Daten zum Hochladen gefunden!");
                        return;
                    }

                    // json content to upload
                    var uploadContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(patients));

                    using (var client = new ServerConnector())
                    {
                        if (await client.IsNetworkAvailable(this))
                        {
                            if (await client.IsServerAvailableAsync())
                            {
                                try
                                {
                                    var response = await client.UploadDataAsync(this, StaticHolder.SessionHolder, uploadContent, "uploadfinishedmasknew");

                                    var newResponse = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));
                                    Toast.MakeText(this, newResponse.Message, ToastLength.Long).Show();

                                    // update status if upload = 200 OK
                                    if (newResponse.Valid)
                                    {
                                        // save state of files
                                        foreach (var file in filesToUpdate)
                                        {
                                            await fileManager.SaveJsonContentForFileAsync(file.Key, file.Value);
                                        }
                                    }

                                    // move files to old.data directory
                                    await fileManager.MoveOldFilesAsync(DateTime.Now.Year.ToString(), filesToMove);

                                    // start the service to download the favorite mask
                                    StartService(new Intent(this, typeof(DownloadService)));
                                }
                                catch
                                {
                                    Toast.MakeText(this, "Daten konnten nicht hochgeladen werden. Eine Datei ist Fehlerhaft.", ToastLength.Long).Show();
                                    //CreateAlert("Fehler", "Fehler beim Hochladen der Daten.");
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
                Log.Debug("l/ex", ex.ToString());
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
            alert.SetPositiveButton("Absenden", (sender, args) => { UploadDataAsyncNew(); });
            alert.Show();
        }
    }
}
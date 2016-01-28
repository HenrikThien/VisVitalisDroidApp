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

namespace visvitalis
{
    [Activity(Label = "Liste der Patienten", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class PatientListActivity : AppCompatActivity
    {
        private ProgressDialog _progressDialog;
        private DateTime _fileDateTime;
        private string _fileWorkerToken;
        private string _fileDate;

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
            mPagerAdapter = new CompatPagerAdapter(this, _fileWorkerToken, "", date, SupportFragmentManager);
            mViewPager = FindViewById<ViewPager>(Resource.Id.pager);
            mSlidingTabLayout = FindViewById<PagerSlidingTabStrip>(Resource.Id.tabs);

            mViewPager.Adapter = mPagerAdapter;

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

        public async void UploadDataAsync()
        {
            _progressDialog = new ProgressDialog(this);
            _progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            _progressDialog.SetCancelable(false);
            _progressDialog.SetTitle("Hochladen...");
            _progressDialog.SetMessage("Daten werden hochgeladen, bitte warten...");
            _progressDialog.SetCanceledOnTouchOutside(false);
            _progressDialog.Show();

            if (_fileDateTime.Date != DateTime.Now.Date)
            {
                CreateAlert("Fehler", "Die Maske kann nur abgesendet werden wenn das Datum stimmt.");
                _progressDialog.Dismiss();
                _progressDialog = null;
                return;
            }

            var jsonContent = "";

            using (var fileManager = new FileManager(_fileDateTime.ToString("ddMMyy"), _fileDateTime))
            {
                jsonContent = await fileManager.LoadFileAsync();
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                CreateAlert("Fehler", "Es konnte keine Maske zum Hochladen gefunden werden.");
                _progressDialog.Dismiss();
                _progressDialog = null;
                return;
            }

            var maskObj = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(jsonContent));

            maskObj.PatientMask.PatientOperation.MaskDate = _fileDateTime.ToString("ddMMyy");
            maskObj.PatientMask.PatientOperation.WorkerToken = "xxx";

            var newTourList = new Dictionary<string, List<Patient>>();

            using (var client = new ServerConnector())
            {
                if (await client.IsNetworkAvailable(this))
                {
                    if (await client.IsServerAvailableAsync())
                    {
                        foreach (var time in maskObj.PatientMask.PatientOperation.Tours)
                        {
                            newTourList.Add(time.Id, new List<Patient>());

                            foreach (var patient in time.Patients)
                            {
                                if (!string.IsNullOrEmpty(patient.WorkerToken) && patient.WorkerToken.Equals(_fileWorkerToken))
                                {
                                    if (patient.ServerState != "send")
                                    {
                                        patient.ServerState = "send";
                                        newTourList[time.Id].Add(patient);
                                    }
                                }
                            }
                        }


                        int count = 0;
                        foreach (var item in newTourList)
                            foreach (var patient in item.Value)
                                count++;

                        if (count > 0)
                        {
                            var timeToSave = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(maskObj));

                            using (var fileMngr = new FileManager(_fileDate, _fileDateTime))
                            {
                                await fileMngr.SaveJsonContentAsync(timeToSave);
                            }
                        }

                        try
                        {
                            maskObj.PatientMask.PatientOperation.Tours[0].Patients = newTourList["morgens"];
                        }
                        catch { }
                        try
                        {
                            maskObj.PatientMask.PatientOperation.Tours[1].Patients = newTourList["abends"];
                        }
                        catch { }

                        if (count == 0)
                        {
                            Toast.MakeText(this, "Es wurden keine Daten hochgeladen.", ToastLength.Short).Show();
                            _progressDialog.Dismiss();
                            _progressDialog = null;
                            return;
                        }

                        var newJsonContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(maskObj));

                        var response = await client.UploadDataAsync(this, StaticHolder.SessionHolder, newJsonContent);

                        try
                        {
                            var newResponse = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));
                            Toast.MakeText(this, newResponse.Message, ToastLength.Long).Show();
                        }
                        catch
                        {
                        }

                        _progressDialog.Dismiss();
                        _progressDialog = null;
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
    }
}
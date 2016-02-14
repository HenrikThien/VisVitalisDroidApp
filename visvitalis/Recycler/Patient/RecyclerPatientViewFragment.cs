using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using visvitalis.JSON;
using visvitalis.Utils;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Android.Support.V7.Widget;
using Android.Graphics;
using System.Threading;
using Android.Preferences;

namespace visvitalis.Recycler
{
    internal class RecyclerPatientViewFragment : Android.Support.V4.App.Fragment
    {
        private const string StartTime = "00:01";
        private const string EndTime = "00:02";
        private const int Km = 0;
        private const string Performance = "xx";

        private RecyclerView mRecyclerView;
        private RecyclerView.LayoutManager mLayoutManager;
        private RecyclerPatientViewAdapter mAdapter;
        private RecyclerPatientItem mRecyclerItem;

        private ProgressDialog _progressDialog;
        private RootObject _patientMask;
        private FileManager _fileManager;
        private DateTime _fileDateTime;
        private List<Patient> _patientList = new List<Patient>();
        private LayoutInflater _inflater;
        private string _workerToken;
        private bool _loadOldFile;

        public static RecyclerPatientViewFragment CreateNewInstance(bool loadOldFile, string time, string date, string workerToken)
        {
            var fragment = new RecyclerPatientViewFragment();
            var bundle = new Bundle();
            bundle.PutString("JSON_DATE", date);
            bundle.PutString("JSON_TIME", time);
            bundle.PutString("JSON_WORKER_TOKEN", workerToken);
            bundle.PutBoolean("JSON_OLD_FILE", loadOldFile);
            fragment.Arguments = bundle;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var jsonDate = Arguments.GetString("JSON_DATE");
            var jsonTime = Arguments.GetString("JSON_TIME");
            var jsonWorkerToken = Arguments.GetString("JSON_WORKER_TOKEN");

            _loadOldFile = Arguments.GetBoolean("JSON_OLD_FILE");
            _workerToken = jsonWorkerToken;

            var tabView = inflater.Inflate(Resource.Layout.TabRecycler, container, false);
            _inflater = inflater;
            mRecyclerView = tabView.FindViewById<RecyclerView>(Resource.Id.recyclerView);
            return tabView;
        }

        public override void OnResume()
        {
            base.OnResume();
            var jsonDate = Arguments.GetString("JSON_DATE");
            var jsonTime = Arguments.GetString("JSON_TIME");
            var jsonWorkerToken = Arguments.GetString("JSON_WORKER_TOKEN");

            _loadOldFile = Arguments.GetBoolean("JSON_OLD_FILE");
            _workerToken = jsonWorkerToken;

            var patients = GetPatientList(jsonDate, jsonTime);
            _patientList = patients;

            mRecyclerItem = new RecyclerPatientItem(patients);

            // load settings
            var prefs = PreferenceManager.GetDefaultSharedPreferences(Activity);
            var number = int.Parse(prefs.GetString("prefSyncFrequency", "1"));
            mLayoutManager = new GridLayoutManager(Activity, number);

            mRecyclerView.SetLayoutManager(mLayoutManager);
            mAdapter = new RecyclerPatientViewAdapter(mRecyclerItem);
            mAdapter.ItemClick += OnItemClick;
            mAdapter.ItemLongClick += OnLongItemClick;

            mRecyclerView.SetAdapter(mAdapter);
        }

        List<Patient> GetPatientList(string date, string time)
        {
            string jsonMask = "";

            DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);

            _fileManager = new FileManager(date, _fileDateTime);
            jsonMask = _fileManager.LoadFile(_loadOldFile);

            if (jsonMask != "[]")
            {
                _patientMask = JsonConvert.DeserializeObject<RootObject>(jsonMask);
                _patientMask.PatientMask[0].PatientOperation.MaskDate = date;

                return _patientMask.PatientMask[0].GetEinsaetzeByTime(time);
            }

            return new List<Patient>();
        }

        void OnItemClick(object sender, int position)
        {
            var patient = (_patientList[position]);

            var alertView = _inflater.Inflate(Resource.Layout.save_time_alert_dialog, null, false);

            var alert = new AlertDialog.Builder(Activity);
            alert.SetView(alertView);
            alert.SetTitle("Speichern");
            alert.SetCancelable(false);
            alert.SetNegativeButton("Abbrechen", (EventHandler<DialogClickEventArgs>)null);

            var dialog = alert.Create();
            dialog.Show();

            var title = dialog.FindViewById<TextView>(Resource.Id.alertTitle);
            title.Text = "Zeiten abspeichern.";
            var desc = dialog.FindViewById<TextView>(Resource.Id.alertDescription);
            desc.Text = "Zeiten abspeichern für den Patienten";

            var negativeBtn = dialog.GetButton((int)DialogButtonType.Negative);
            var kilometerText = dialog.FindViewById<EditText>(Resource.Id.kmEditText);
            var kilometerBtn = dialog.FindViewById<Button>(Resource.Id.button3);
            var ankunftBtn = dialog.FindViewById<Button>(Resource.Id.button1);
            var abfahrtBtn = dialog.FindViewById<Button>(Resource.Id.button2);
            var leistungFailedBtn = dialog.FindViewById<Button>(Resource.Id.button4);

            if (_loadOldFile || _fileDateTime.Date != DateTime.Now.Date || (!string.IsNullOrEmpty(patient.WorkerToken) && patient.WorkerToken != _workerToken))
            {
                ankunftBtn.Enabled = false;
                ankunftBtn.SetBackgroundColor(Color.Black);
                ankunftBtn.SetTextColor(Color.White);
                kilometerBtn.Enabled = false;
                kilometerBtn.SetBackgroundColor(Color.Black);
                kilometerBtn.SetTextColor(Color.White);
                abfahrtBtn.Enabled = false;
                abfahrtBtn.SetBackgroundColor(Color.Black);
                abfahrtBtn.SetTextColor(Color.White);
                leistungFailedBtn.Enabled = false;
                leistungFailedBtn.SetBackgroundColor(Color.Black);
                leistungFailedBtn.SetTextColor(Color.White);
            }

            var setLeistungFailedDisabled = false;

            if (!string.IsNullOrEmpty(patient.Arrival))
            {
                ankunftBtn.Text = "Ankunftszeit: " + patient.Arrival;
                ankunftBtn.Enabled = false;
                ankunftBtn.SetBackgroundColor(Color.Black);
                ankunftBtn.SetTextColor(Color.White);
            }

            if (!string.IsNullOrEmpty(patient.Departure))
            {
                abfahrtBtn.Text = "Abfahrtszeit: " + patient.Departure;
                abfahrtBtn.Enabled = false;
                abfahrtBtn.SetBackgroundColor(Color.Black);
                abfahrtBtn.SetTextColor(Color.White);
            }

            if (!string.IsNullOrEmpty(patient.Arrival) && !string.IsNullOrEmpty(patient.Departure))
                setLeistungFailedDisabled = true;

            if (setLeistungFailedDisabled)
            {
                leistungFailedBtn.Enabled = false;
                leistungFailedBtn.SetBackgroundColor(Color.Black);
                leistungFailedBtn.SetTextColor(Color.White);
            }

            if (!string.IsNullOrEmpty(patient.Km))
            {
                kilometerText.Text = patient.Km;
                kilometerText.Enabled = false;
                kilometerBtn.Enabled = false;
                kilometerBtn.SetBackgroundColor(Color.Black);
                kilometerBtn.SetTextColor(Color.White);
            }

            negativeBtn.Click += delegate { dialog.Dismiss(); };

            kilometerBtn.Click += async delegate
            {
                Toast.MakeText(Activity, "Kilometer gespeichert.", ToastLength.Short).Show();

                if (string.IsNullOrEmpty(patient.Km))
                {
                    patient.Km = kilometerText.Text;
                    var objInList = (_patientList[position]);
                    objInList.Km = kilometerText.Text;

                    if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                    {
                        patient.WorkerToken = _workerToken;
                        objInList.WorkerToken = _workerToken;
                    }

                    kilometerBtn.Text = "Gespeichert! Schließe...Bitte warten";
                    kilometerBtn.Enabled = false;
                    kilometerBtn.SetBackgroundColor(Color.Black);
                    kilometerBtn.SetTextColor(Color.White);
                }

                await Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    dialog.Dismiss();
                });

                var jsonContent = JsonConvert.SerializeObject(_patientMask);

                if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                {
                    Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                }
                else
                {
                    mAdapter.NotifyDataSetChanged();
                }
            };


            leistungFailedBtn.Click += async delegate
            {
                Toast.MakeText(Activity, "Leistung wurde nicht erbracht.", ToastLength.Short).Show();

                patient.Arrival = StartTime;
                patient.Performances.Clear();
                patient.Performances.Add(Performance);

                var objInList = (_patientList[position]);
                objInList.Arrival = patient.Arrival;
                objInList.Performances.Clear();
                objInList.Performances.Add(Performance);

                ankunftBtn.Enabled = false;
                ankunftBtn.SetBackgroundColor(Color.Black);
                ankunftBtn.SetTextColor(Color.White);

                patient.Departure = EndTime;
                objInList = (_patientList[position]);
                objInList.Departure = patient.Departure;

                abfahrtBtn.Enabled = false;
                abfahrtBtn.SetBackgroundColor(Color.Black);
                abfahrtBtn.SetTextColor(Color.White);

                patient.Km = Km.ToString();
                objInList = (_patientList[position]);
                objInList.Km = Km.ToString();

                kilometerBtn.Enabled = false;
                kilometerBtn.SetBackgroundColor(Color.Black);
                kilometerBtn.SetTextColor(Color.White);

                leistungFailedBtn.Enabled = false;

                if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                {
                    patient.WorkerToken = _workerToken;
                    objInList.WorkerToken = _workerToken;
                }

                await Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    dialog.Dismiss();
                });

                var jsonContent = JsonConvert.SerializeObject(_patientMask);

                if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                {
                    Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                }
                else
                {
                    mAdapter.NotifyDataSetChanged();
                }
            };

            ankunftBtn.Click += async delegate
            {
                Toast.MakeText(Activity, "Ankunftszeit gespeichert", ToastLength.Short).Show();

                if (string.IsNullOrEmpty(patient.Arrival))
                {
                    patient.Arrival = DateTime.Now.ToString("HH:mm");
                    var objInList = (_patientList[position]);
                    objInList.Arrival = patient.Arrival;

                    if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                    {
                        patient.WorkerToken = _workerToken;
                        objInList.WorkerToken = _workerToken;
                    }

                    ankunftBtn.Text = "Gespeichert! Schließe...Bitte warten";
                    ankunftBtn.Enabled = false;
                    ankunftBtn.SetBackgroundColor(Color.Black);
                    ankunftBtn.SetTextColor(Color.White);
                }

                await Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    dialog.Dismiss();
                });

                var jsonContent = JsonConvert.SerializeObject(_patientMask);

                if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                {
                    Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                }
                else
                {
                    mAdapter.NotifyDataSetChanged();
                }
            };

            abfahrtBtn.Click += async delegate
            {
                if (string.IsNullOrEmpty(patient.Arrival))
                {
                    Toast.MakeText(Activity, "Es muss erst eine Ankunftszeit eingegeben werden.", ToastLength.Long).Show();
                    return;
                }

                Toast.MakeText(Activity, "Abfahrtszeit gespeichert", ToastLength.Short).Show();

                if (string.IsNullOrEmpty(patient.Departure))
                {
                    patient.Departure = DateTime.Now.ToString("HH:mm");
                    var objInList = (_patientList[position]);
                    objInList.Departure = patient.Departure;

                    if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                    {
                        patient.WorkerToken = _workerToken;
                        objInList.WorkerToken = _workerToken;
                    }

                    abfahrtBtn.Text = "Gespeichert! Schließe...Bitte warten";
                    abfahrtBtn.Enabled = false;
                    abfahrtBtn.SetBackgroundColor(Color.Black);
                    abfahrtBtn.SetTextColor(Color.White);
                }

                await Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    dialog.Dismiss();
                });

                var jsonContent = JsonConvert.SerializeObject(_patientMask);

                if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                {
                    Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                }
                else
                {
                    mAdapter.NotifyDataSetChanged();
                }
            };
        }

        void OnLongItemClick(object sender, int position)
        {
            var patient = (_patientList[position]);
            if (patient == null)
                return;

            var alertView = _inflater.Inflate(Resource.Layout.change_patient_performance_alert_dialog, null, false);

            var alert = new AlertDialog.Builder(Activity);
            alert.SetView(alertView);
            alert.SetTitle("Speichern");
            alert.SetCancelable(false);
            alert.SetNegativeButton("Abbrechen", (EventHandler<DialogClickEventArgs>)null);

            var dialog = alert.Create();
            dialog.Show();

            var title = dialog.FindViewById<TextView>(Resource.Id.alertTitle);
            title.Text = "Leistungen abspeichern, bitte mit Leerzeichen trennen.";
            var desc = dialog.FindViewById<TextView>(Resource.Id.alertDescription);
            desc.Text = "Leistungen abspeichern für Patient " + patient.PatientName + "";

            var performanceText = dialog.FindViewById<EditText>(Resource.Id.performanceText);
            var performanceBtn = dialog.FindViewById<Button>(Resource.Id.performanceSaveBtn);

            performanceText.Text = patient.LeistungAsString();

            if (patient.Arrival == "00:01:00" || patient.Departure == "00:02:00" || (!string.IsNullOrEmpty(patient.WorkerToken) && patient.WorkerToken != _workerToken))
            {
                performanceText.Enabled = false;
                performanceBtn.Enabled = false;
                performanceBtn.SetBackgroundColor(Color.Black);
                performanceBtn.SetTextColor(Color.White);
            }

            if (_loadOldFile || _fileDateTime.Date != DateTime.Now.Date || (!string.IsNullOrEmpty(patient.WorkerToken) && patient.WorkerToken != _workerToken))
            {
                performanceText.Enabled = false;
                performanceBtn.Enabled = false;
                performanceBtn.SetBackgroundColor(Color.Black);
                performanceBtn.SetTextColor(Color.White);
            }

            performanceBtn.Click += async delegate
            {
                var leistungAsList = performanceText.Text.Split(' ').ToList();

                performanceText.Enabled = false;
                performanceBtn.Enabled = false;
                performanceBtn.SetBackgroundColor(Color.Black);
                performanceBtn.SetTextColor(Color.White);

                patient.Performances = leistungAsList;
                var objInList = (_patientList[position]);
                objInList.Performances = leistungAsList;

                if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                {
                    patient.WorkerToken = _workerToken;
                    objInList.WorkerToken = _workerToken;
                }

                await Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(250);
                    dialog.Dismiss();
                });

                var jsonContent = JsonConvert.SerializeObject(_patientMask);

                if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                {
                    Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                }
                else
                {
                    mAdapter.NotifyDataSetChanged();
                }
            };
        }
    }
}
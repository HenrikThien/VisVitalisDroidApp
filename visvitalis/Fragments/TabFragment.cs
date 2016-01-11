using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using visvitalis.JSON;
using Android.Graphics;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using visvitalis.Utils;
using System.Globalization;

namespace visvitalis.Fragments
{
    public class TabFragment : Fragment
    {
        private RootObject _patientMask;
        private FileManager _fileManager;
        private DateTime _fileDateTime;

        public static TabFragment CreateNewInstance(string time, string date, string jsonMask, string workerToken)
        {
            var fragment = new TabFragment();
            var bundle = new Bundle();
            bundle.PutString("JSON_MASK", jsonMask);
            bundle.PutString("JSON_DATE", date);
            bundle.PutString("JSON_TIME", time);
            bundle.PutString("JSON_WORKER_TOKEN", workerToken);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var jsonMask = Arguments.GetString("JSON_MASK");
            var jsonDate = Arguments.GetString("JSON_DATE");
            var jsonTime = Arguments.GetString("JSON_TIME");
            var jsonWorkerToken = Arguments.GetString("JSON_WORKER_TOKEN");

            var view = inflater.Inflate(Resource.Layout.Tab, container, false);

            Init(jsonMask, jsonDate, jsonTime, jsonWorkerToken, view, inflater);

            return view;
        }

        async void Init(string jsonMask, string date, string time, string token, View view, LayoutInflater inflater)
        {
            await Task.Factory.StartNew(() =>
            {
                _patientMask = JsonConvert.DeserializeObject<RootObject>(jsonMask);
                DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);
                _fileManager = new FileManager(date, _fileDateTime);
            }).ContinueWith(delegate
            {
                FillFragment(time, token, view, inflater);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        ///     Fills the fragment.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="view">The view.</param>
        /// <param name="inflater">The inflater.</param>
        private void FillFragment(string time, string token, View view, LayoutInflater inflater)
        {
            // get all by name
            var values = _patientMask.PatientMask.GetEinsaetzeByTime(time);
            var listView = view.FindViewById<ListView>(Resource.Id.listView1);
            var adapter = new CustomAdapter(values.ToList(), inflater);

            listView.Adapter = adapter;

            listView.ItemLongClick += (sender, args) =>
            {
                var patient = (values[args.Position]);
                if (patient == null)
                    return;

                var alertView = inflater.Inflate(Resource.Layout.change_patient_performance_alert_dialog, null, false);

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

                if (patient.Arrival == "00:01:00" || patient.Departure == "00:02:00")
                {
                    performanceText.Enabled = false;
                    performanceBtn.Enabled = false;
                    performanceBtn.SetBackgroundColor(Color.Black);
                }

                performanceBtn.Click += async delegate
                {
                    var leistungAsList = performanceText.Text.Split(' ').ToList();

                    performanceText.Enabled = false;
                    performanceBtn.Enabled = false;
                    performanceBtn.SetBackgroundColor(Color.Black);

                    patient.Performances = leistungAsList;
                    var objInList = (values[args.Position]);
                    objInList.Performances = leistungAsList;

                    if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                    {
                        patient.WorkerToken = token;
                        objInList.WorkerToken = token;
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
                        adapter.NotifyDataSetChanged();
                    }
                };
            };

            listView.ItemClick += (sender, args) =>
            { 
                var patient = (values[args.Position]);

                var alertView = inflater.Inflate(Resource.Layout.save_time_alert_dialog, null, false);

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
                desc.Text = "Zeiten abspeichern für Patient " + patient.PatientName + "";

                var negativeBtn = dialog.GetButton((int)DialogButtonType.Negative);
                var kilometerText = dialog.FindViewById<EditText>(Resource.Id.kmEditText);
                var kilometerBtn = dialog.FindViewById<Button>(Resource.Id.button3);
                var ankunftBtn = dialog.FindViewById<Button>(Resource.Id.button1);
                var abfahrtBtn = dialog.FindViewById<Button>(Resource.Id.button2);
                var leistungFailedBtn = dialog.FindViewById<Button>(Resource.Id.button4);

                if (_fileDateTime.Date != DateTime.Now.Date)
                {
                    ankunftBtn.Enabled = false;
                    ankunftBtn.SetBackgroundColor(Color.Black);
                    kilometerBtn.Enabled = false;
                    kilometerBtn.SetBackgroundColor(Color.Black);
                    abfahrtBtn.Enabled = false;
                    abfahrtBtn.SetBackgroundColor(Color.Black);
                    leistungFailedBtn.Enabled = false;
                    leistungFailedBtn.SetBackgroundColor(Color.Black);
                }

                var setLeistungFailedDisabled = false;

                if (!string.IsNullOrEmpty(patient.Arrival))
                {
                    ankunftBtn.Text = "Ankunftszeit: " + patient.Arrival;
                    ankunftBtn.Enabled = false;
                    ankunftBtn.SetBackgroundColor(Color.Black);
                }

                if (!string.IsNullOrEmpty(patient.Departure))
                {
                    abfahrtBtn.Text = "Abfahrtszeit: " + patient.Departure;
                    abfahrtBtn.Enabled = false;
                    abfahrtBtn.SetBackgroundColor(Color.Black);
                }


                if (!string.IsNullOrEmpty(patient.Arrival) && !string.IsNullOrEmpty(patient.Departure))
                    setLeistungFailedDisabled = true;

                if (setLeistungFailedDisabled)
                {
                    leistungFailedBtn.Enabled = false;
                    leistungFailedBtn.SetBackgroundColor(Color.Black);
                }

                if (!string.IsNullOrEmpty(patient.Km))
                {
                    kilometerText.Text = patient.Km;
                    kilometerText.Enabled = false;
                    kilometerBtn.Enabled = false;
                    kilometerBtn.SetBackgroundColor(Color.Black);
                }

                negativeBtn.Click += delegate { dialog.Dismiss(); };

                kilometerBtn.Click += async delegate
                {
                    Toast.MakeText(Activity, "Kilometer gespeichert.", ToastLength.Short).Show();

                    if (string.IsNullOrEmpty(patient.Km))
                    {
                        patient.Km = kilometerText.Text;
                        var objInList = (values[args.Position]);
                        objInList.Km = kilometerText.Text;

                        if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                        {
                            patient.WorkerToken = token;
                            objInList.WorkerToken = token;
                        }

                        kilometerBtn.Text = "Gespeichert! Schließe...Bitte warten";
                        kilometerBtn.Enabled = false;
                        kilometerBtn.SetBackgroundColor(Color.Black);
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
                        adapter.NotifyDataSetChanged();
                    }
                };


                leistungFailedBtn.Click += async delegate
                {
                    Toast.MakeText(Activity, "Leistung wurde nicht erbracht.", ToastLength.Short).Show();

                    const string startTime = "00:01:00";
                    const string endTime = "00:02:00";
                    const string performance = "xxx";

                    patient.Arrival = startTime;
                    patient.Performances.Clear();
                    patient.Performances.Add(performance);

                    var objInList = (values[args.Position]);
                    objInList.Arrival = patient.Arrival;
                    objInList.Performances.Clear();
                    objInList.Performances.Add(performance);

                    ankunftBtn.Enabled = false;
                    ankunftBtn.SetBackgroundColor(Color.Black);

                    patient.Departure = endTime;
                    objInList = (values[args.Position]);
                    objInList.Departure = patient.Departure;
                    abfahrtBtn.Enabled = false;
                    abfahrtBtn.SetBackgroundColor(Color.Black);

                    leistungFailedBtn.Enabled = false;

                    if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                    {
                        patient.WorkerToken = token;
                        objInList.WorkerToken = token;
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
                        adapter.NotifyDataSetChanged();
                    }
                };

                ankunftBtn.Click += async delegate
                {
                    Toast.MakeText(Activity, "Ankunftszeit gespeichert", ToastLength.Short).Show();

                    if (string.IsNullOrEmpty(patient.Arrival))
                    {
                        patient.Arrival = DateTime.Now.ToString("HH:mm:ss");
                        var objInList = (values[args.Position]);
                        objInList.Arrival = patient.Arrival;

                        if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                        {
                            patient.WorkerToken = token;
                            objInList.WorkerToken = token;
                        }

                        ankunftBtn.Text = "Gespeichert! Schließe...Bitte warten";
                        ankunftBtn.Enabled = false;
                        ankunftBtn.SetBackgroundColor(Color.Black);
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
                        adapter.NotifyDataSetChanged();
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
                        patient.Departure = DateTime.Now.ToString("HH:mm:ss");
                        var objInList = (values[args.Position]);
                        objInList.Departure = patient.Departure;

                        if (string.IsNullOrEmpty(patient.WorkerToken) && string.IsNullOrEmpty(objInList.WorkerToken))
                        {
                            patient.WorkerToken = token;
                            objInList.WorkerToken = token;
                        }

                        abfahrtBtn.Text = "Gespeichert! Schließe...Bitte warten";
                        abfahrtBtn.Enabled = false;
                        abfahrtBtn.SetBackgroundColor(Color.Black);
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
                        adapter.NotifyDataSetChanged();
                    }
                };
            };
        }
    }
}
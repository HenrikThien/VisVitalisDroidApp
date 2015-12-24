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

        public static TabFragment CreateNewInstance(string time, string date, string jsonMask)
        {
            var fragment = new TabFragment();
            var bundle = new Bundle();
            bundle.PutString("JSON_MASK", jsonMask);
            bundle.PutString("JSON_DATE", date);
            bundle.PutString("JSON_TIME", time);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var jsonMask = Arguments.GetString("JSON_MASK");
            var jsonDate = Arguments.GetString("JSON_DATE");
            var jsonTime = Arguments.GetString("JSON_TIME");

            var view = inflater.Inflate(Resource.Layout.Tab, container, false);

            Init(jsonMask, jsonDate, jsonTime, view, inflater);

            return view;
        }

        async void Init(string jsonMask, string date, string time, View view, LayoutInflater inflater)
        {
            await Task.Factory.StartNew(() =>
            {
                _patientMask = JsonConvert.DeserializeObject<RootObject>(jsonMask);
                DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);
                _fileManager = new FileManager(date, _fileDateTime);
            }).ContinueWith(delegate
            {
                FillFragment(time, view, inflater);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        ///     Fills the fragment.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="view">The view.</param>
        /// <param name="inflater">The inflater.</param>
        private void FillFragment(string time, View view, LayoutInflater inflater)
        {
            // get all by name
            var values = _patientMask.PatientMask.GetEinsaetzeByTime(time);
            var listView = view.FindViewById<ListView>(Resource.Id.listView1);
            var adapter = new CustomAdapter(values.ToList(), inflater);

            Log.Debug("debug", "Start fill the fragment...: " + values.Count);

            listView.Adapter = adapter;

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

                ankunftBtn.Click += async delegate
                {
                    Toast.MakeText(Activity, "Ankunftszeit gespeichert", ToastLength.Short).Show();

                    if (string.IsNullOrEmpty(patient.Arrival))
                    {
                        patient.Arrival = DateTime.Now.ToString("HH:mm:ss");
                        var objInList = (values[args.Position]);
                        objInList.Arrival = patient.Arrival;

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
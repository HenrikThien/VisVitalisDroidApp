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
using visvitalis.JSON;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Android.Util;

namespace visvitalis.Fragments
{
    public class NewEntryFragment : Android.Support.V4.App.Fragment
    {
        public delegate void TabNeedSwitch(int position);
        public event TabNeedSwitch OnTabNeedSwitchEvent;

        private FileManager _fileManager;
        private RootObject _patientMask;
        private DateTime _fileDateTime;

        public static NewEntryFragment CreateInstance(string date)
        {
            var fragment = new NewEntryFragment();
            var bundle = new Bundle();
            bundle.PutString("JSON_DATE", date);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var tabView = inflater.Inflate(Resource.Layout.CreateNewEntryLayout, container, false);

            var button = tabView.FindViewById<Button>(Resource.Id.button1);

            var date = Arguments.GetString("JSON_DATE");

            DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);

            if (_fileDateTime.Date != DateTime.Now.Date)
            {
                button.Enabled = false;
            }

            _fileManager = new FileManager(date, _fileDateTime);
            var jsonMask = _fileManager.LoadFile(false);

            _patientMask = JsonConvert.DeserializeObject<RootObject>(jsonMask);

            button.Click += async delegate
            {
                if (tabView.FindViewById<EditText>(Resource.Id.editText1).Text.Length > 0 &&
                    tabView.FindViewById<EditText>(Resource.Id.editText2).Text.Length > 0 &&
                    tabView.FindViewById<EditText>(Resource.Id.editText3).Text.Length > 0 &&
                    tabView.FindViewById<EditText>(Resource.Id.editText4).Text.Length > 0)
                {
                    var patientId = tabView.FindViewById<EditText>(Resource.Id.editText1);
                    var patientName = tabView.FindViewById<EditText>(Resource.Id.editText2);
                    var patientPerformances = tabView.FindViewById<EditText>(Resource.Id.editText3);
                    var patientMission = tabView.FindViewById<EditText>(Resource.Id.editText4);

                    try
                    {
                        var patient = new Patient()
                        {
                            Nr = int.Parse(patientId.Text),
                            PatientName = patientName.Text,
                            Performances = (patientPerformances.Text.Contains(',')) ? patientPerformances.Text.Split(',').ToList() : (patientPerformances.Text + ",").Split(',').ToList(),
                            Mission = GetPatientMission(patientMission.Text)
                        };

                        var listV = _patientMask.PatientMask[0].PatientOperation.Tours[0];
                        var listA = _patientMask.PatientMask[0].PatientOperation.Tours[1];

                        if (patientMission.Text.ToLower().StartsWith("v"))
                        {
                            var lastPatient = listV.Patients[listV.Patients.Count - 1];
                            patient.Order = lastPatient.Order + 1;
                            listV.Patients.Add(patient);
                        }
                        else
                        {
                            var lastPatient = listA.Patients[listA.Patients.Count - 1];
                            patient.Order = lastPatient.Order + 1;
                            listA.Patients.Add(patient);
                        }

                        var jsonContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(_patientMask));

                        if (!await _fileManager.SaveJsonContentAsync(jsonContent))
                        {
                            Toast.MakeText(Activity, "Fehler beim Speichern der Datei...", ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(Activity, "Der Einsatz wurde erfolgreich erstellt.", ToastLength.Short).Show();

                            patientId.Text = "";
                            patientName.Text = "";
                            patientPerformances.Text = "";
                            var tempMission = patientMission.Text;
                            patientMission.Text = "";

                            UpdateUserView((tempMission.ToLower().StartsWith("v")) ? 0 : 1);
                        }
                    }
                    catch
                    {
                        CreateAlert("Fehler", "Fehler beim Erstellen des Einsatzes.\nBitte erneut versuchen!");
                        return;
                    }
                }
                else
                {
                    CreateAlert("Fehler", "Die Felder müssen korrekt ausgefüllt werden.");
                    return;
                }
            };

            return tabView;
        }

        void UpdateUserView(int position)
        {
            if (OnTabNeedSwitchEvent == null)
                return;
            OnTabNeedSwitchEvent(position);
        }
        
        string GetPatientMission(string mission)
        {
            mission = mission.ToLower();
            bool startsWith = (mission.StartsWith("v") || mission.StartsWith("a"));

            if (startsWith && mission.Length > 1)
            {
                mission = mission.Remove(0, 1);
            }
            else if (!startsWith && mission.Length == 1)
            {
                mission = mission.ToUpper();
            }

            return mission;
        }

        void CreateAlert(string title, string message)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton("Ok", (sender, args) => { alert.Dispose(); });
            alert.Show();
        }
    }
}
using System;
using System.Linq;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Globalization;
using visvitalis.Utils;
using visvitalis.JSON;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using Android.Util;

namespace visvitalis.Fragments
{
    public class NewEntryFragment : Android.Support.V4.App.Fragment
    {
        public delegate void TabNeedSwitch(int position, bool loadOldFile);
        public event TabNeedSwitch OnTabNeedSwitchEvent;

        private FileManager _fileManager;
        private RootObject _patientMask;
        private DateTime _fileDateTime;
        private View _tabView;

        public static NewEntryFragment CreateInstance(string date, bool oldFile)
        {
            var fragment = new NewEntryFragment();
            var bundle = new Bundle();
            bundle.PutString("JSON_DATE", date);
            bundle.PutBoolean("JSON_OLD_FILE", oldFile);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            _tabView = inflater.Inflate(Resource.Layout.CreateNewEntryLayout, container, false);
            return _tabView;
        }

        public override void OnResume()
        {
            base.OnResume();

            try
            {
                var button = _tabView.FindViewById<Button>(Resource.Id.button1);

                var date = Arguments.GetString("JSON_DATE");
                var loadOldFile = Arguments.GetBoolean("JSON_OLD_FILE");

                DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);

                if (_fileDateTime.Date != DateTime.Now.Date)
                {
                    button.Enabled = false;
                }

                _fileManager = new FileManager(date, _fileDateTime);
                var jsonMask = _fileManager.LoadFile(loadOldFile);

                _patientMask = JsonConvert.DeserializeObject<RootObject>(jsonMask);

                if (!button.HasOnClickListeners)
                {
                    button.Click += async delegate
                    {
                        button.Enabled = false;

                        if (loadOldFile)
                        {
                            _fileManager.MoveFileBack();
                            jsonMask = _fileManager.LoadFile(false);
                        }

                        if (_tabView.FindViewById<EditText>(Resource.Id.editText1).Text.Length > 0 &&
                            _tabView.FindViewById<EditText>(Resource.Id.editText2).Text.Length > 0 &&
                            _tabView.FindViewById<EditText>(Resource.Id.editText3).Text.Length > 0 &&
                            _tabView.FindViewById<EditText>(Resource.Id.editText4).Text.Length > 0)
                        {
                            var patientId = _tabView.FindViewById<EditText>(Resource.Id.editText1);
                            var patientName = _tabView.FindViewById<EditText>(Resource.Id.editText2);
                            var patientPerformances = _tabView.FindViewById<EditText>(Resource.Id.editText3);
                            var patientMission = _tabView.FindViewById<EditText>(Resource.Id.editText4);

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
                                    if (listV.Patients.Count > 0)
                                    {
                                        var lastPatient = listV.Patients[listV.Patients.Count - 1];
                                        patient.Order = lastPatient.Order + 1;
                                        listV.Patients.Add(patient);
                                    }
                                    else
                                    {
                                        patient.Order = 0;
                                        listV.Patients.Add(patient);
                                    }
                                }
                                else
                                {
                                    if (listA.Patients.Count > 0)
                                    {
                                        var lastPatient = listA.Patients[listA.Patients.Count - 1];
                                        patient.Order = lastPatient.Order + 1;
                                        listA.Patients.Add(patient);
                                    }
                                    else
                                    {
                                        patient.Order = 0;
                                        listA.Patients.Add(patient);
                                    }
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

                                    // trololo
                                    await Task.Factory.StartNew(() => Thread.Sleep(400));

                                    button.Enabled = true;
                                    UpdateUserView((tempMission.ToLower().StartsWith("v")) ? 0 : 1, false);
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
                            button.Enabled = true; // fixed, dann muss die activity nicht neu geladen werden.
                            return;
                        }
                    };
                }
            }
            catch
            {
                Activity.Finish();
            }
        }

        void UpdateUserView(int position, bool loadOldFile)
        {
            if (OnTabNeedSwitchEvent == null)
                return;
            OnTabNeedSwitchEvent(position, false);
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
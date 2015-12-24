using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using visvitalis.Utils;
using System.Globalization;
using visvitalis.Fragments;
using Android.Support.V4.App;
using System.Threading.Tasks;
using Newtonsoft.Json;
using visvitalis.JSON;
using Android.Text;
using Android.Text.Style;
using visvitalis.Networking;
using Android.Preferences;

namespace visvitalis
{
    [Activity(Label = "Liste der Patienten", Icon = "@drawable/ic_launcher", Theme = "@style/Theme.Main")]
    public class PatientListActivity : FragmentActivity
    {
        private ProgressDialog _progressDialog;
        private DateTime _fileDateTime;
        private string _fileWorkerToken;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.PatientListView);

            var jsonContent = Intent.GetStringExtra(AppConstants.JsonMask);
            var date = Intent.GetStringExtra(AppConstants.FileDate);
            _fileWorkerToken = Intent.GetStringExtra(AppConstants.FileWorkerToken);

            if (string.IsNullOrEmpty(jsonContent))
            {
                FinishActivity(0);
            }

            DateTime.TryParseExact(date, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _fileDateTime);

            Init(date, jsonContent);
        }

        void Init(string date, string content)
        {
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.SetIcon(Resource.Drawable.ic_launcher);

            var st = new SpannableString("Einsätze - " + _fileDateTime.ToString("dd - MM - yyyy") + " - " + _fileWorkerToken);
            st.SetSpan(new TypefaceSpan("fonts/Champ.ttf"), 0, st.Length(), SpanTypes.ExclusiveExclusive);

            ActionBar.TitleFormatted = st;

            AddTab("Morgens", date);
            AddTab("Abends", date);
        }

        void AddTab(string text, string date)
        {
            var tab = ActionBar.NewTab();
            tab.SetText(text);
            tab.SetTabListener(new TabListener(this, text, date, FragmentManager));
            ActionBar.AddTab(tab);
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
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }

        public async void UploadDataAsync()
        {
            var jsonContent = "";

            using (var fileManager = new FileManager(_fileDateTime.ToString("ddMMyy"), _fileDateTime))
            {
                jsonContent = await fileManager.LoadFileAsync();
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                CreateAlert("Fehler", "Es konnte keine Maske zum Hochladen gefunden werden.");
                return;
            }

            var maskObj = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<RootObject>(jsonContent));

            maskObj.PatientMask.PatientOperation.MaskDate = _fileDateTime.ToString("ddMMyy");
            maskObj.PatientMask.PatientOperation.WorkerToken = _fileWorkerToken;

            var newJsonContent = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(maskObj));

            if (await MaskAlreadySent(_fileDateTime.ToString("ddMMyy")))
            {
                CreateAlert("Fehler", "Diese Maske wurde bereits hochgeladen!");
                return;
            }

            using (var fileManager = new FileManager(maskObj.PatientMask.PatientOperation.MaskDate, _fileDateTime))
            {
                await fileManager.SaveJsonContentAsync(newJsonContent);
            }

            using (var client = new ServerConnector())
            {
                if (await client.IsNetworkAvailable(this))
                {
                    if (await client.IsServerAvailableAsync())
                    {
                        var response = await client.UploadDataAsync(this, StaticHolder.SessionHolder, newJsonContent);

                        try
                        {
                            var newResponse = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));
                            Toast.MakeText(this, newResponse.Message, ToastLength.Long).Show();
                            await SaveMaskStateAsync(_fileDateTime.ToString("ddMMyy"));
                        }
                        catch
                        {
                            //Log.Debug("debug", ex.ToString());
                        }
                    }
                    else
                    {
                        CreateAlert("Datenserver", "Der Server scheint derzeit nicht erreichbar zu sein. Versuchen Sie es später erneut.");
                    }
                }
                else
                {
                    CreateAlert("Internetverbindung", "Es ist keine Internetverbindung verfügbar.");
                }
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

        void AskToUpload()
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle("Daten zum Server senden?");
            alert.SetMessage("Sind Sie sicher, dass die Daten zum Server geschickt werden sollen?\nWurden alle Daten ausgefüllt?");
            alert.SetNegativeButton("Abbrechen", (sender, args) => { alert.Dispose(); });
            alert.SetPositiveButton("Absenden", (sender, args) => { UploadDataAsync(); });
            alert.Show();
        }

        async Task<bool> MaskAlreadySent(string date)
        {
            var returnState = false;
            await Task.Factory.StartNew(() =>
            {
                var manager = PreferenceManager.GetDefaultSharedPreferences(this);
                var state = manager.GetBoolean("mask_" + date, false);
                returnState = state;
            });
            return returnState;
        }

        async Task SaveMaskStateAsync(string date)
        {
            await Task.Factory.StartNew(() =>
            {
                var manager = PreferenceManager.GetDefaultSharedPreferences(this);
                var state = manager.GetBoolean("mask_" + date, false);
                
                if (!state)
                {
                    var editor = manager.Edit();
                    editor.PutBoolean("mask_" + date, true);
                    editor.Commit();
                }
            });
        }
    }
}
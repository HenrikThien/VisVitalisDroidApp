using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using System.Threading.Tasks;
using visvitalis.Networking;
using visvitalis.NotificationService;
using Android.Preferences;
using visvitalis.Utils;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace visvitalis
{
    [Activity(Label="Mitarbeiter Login", Icon = "@drawable/ic_launcher", Theme = "@style/MyTheme", NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class LoginActivity : AppCompatActivity
    {
        private ProgressDialog _progressDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
			SetContentView (Resource.Layout.LoginScreen);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Mitarbeiter Login";

            var loginBtn = FindViewById<Button>(Resource.Id.button1);
            CheckPlayService(loginBtn);

            loginBtn.Click += LoginBtn_Click;
        }

        async void CheckPlayService(Button loginBtn)
        {
            if (!await IsPlayServiceAvailable())
            {
                loginBtn.Enabled = false;
            }
        }

        async void LoginBtn_Click(object sender, EventArgs e)
        {
            ShowLoadingDialog();
            await LoginTaskAsync();
            _progressDialog.Dismiss();
        }

        async Task LoginTaskAsync()
        {
            var groupname = FindViewById<EditText>(Resource.Id.editText1).Text;
            var password = FindViewById<EditText>(Resource.Id.editText2).Text;

            if (!string.IsNullOrEmpty(groupname) && !string.IsNullOrEmpty(password))
            {
                using (var connector = new ServerConnector())
                {
                    if (await connector.IsNetworkAvailable(this))
                    {
                        if (await connector.IsServerAvailableAsync())
                        {
                            var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
                            var editor = preferences.Edit();
                            editor.PutString(AppConstants.GroupName, groupname.ToLower());
                            editor.Commit();

                            var loginResponse = await connector.LoginAsync(groupname, password);

                            if (loginResponse != null)
                            {
                                if (loginResponse.Valid)
                                {
                                    var accessTokenResponse = await connector.RequestAccessTokenAsync(loginResponse);

                                    var intent = new Intent(this, typeof(RegistrationIntentService));
                                    StartService(intent);

                                    StaticHolder.SessionHolder.AccessTokenResponse = accessTokenResponse;
                                    StaticHolder.SessionHolder.LoginResponse = loginResponse;

                                    await StaticHolder.SaveSessionAsync(this);

                                    var mainintent = new Intent();
                                    mainintent.SetClass(this, typeof(MainActivity));
                                    StartActivity(mainintent);
                                }
                                else
                                {
                                    CreateAlert("Fehler", "Es ist ein Fehler beim Login aufgetreten, versuchen Sie es sp�ter erneut!");
                                    return;
                                }
                            }
                            else
                            {
                                CreateAlert("Fehler", "Es ist ein Fehler beim Login aufgetreten, versuchen Sie es sp�ter erneut!");
                                return;
                            }
                        }
                        else
                        {
                            CreateAlert("Fehler", "Der Server konnte nicht kontaktiert werden!");
                            return;
                        }
                    }
                    else
                    {
                        CreateAlert("Fehler", "Sie m�ssen eine Internetverbindung herstellen!");
                        return;
                    }
                }
            }
            else
            {
                CreateAlert("Fehler", "Es m�ssen beide Felder ausgef�llt werden!");
                return;
            }
        }

        void ShowLoadingDialog()
        {
            _progressDialog = new ProgressDialog(this);
            _progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            _progressDialog.SetCancelable(false);
            _progressDialog.SetTitle("Laden...");
            _progressDialog.SetMessage("Laden, bitte warten...");
            _progressDialog.SetCanceledOnTouchOutside(false);
            _progressDialog.Show();
        }

        void CreateAlert(string title, string message)
        {
            var alert = new Android.App.AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton("Ok", (sender, args) => { alert.Dispose(); });
            alert.Show();
        }

        async Task<bool> IsPlayServiceAvailable()
        {
            //int resultCode = await Task.Factory.StartNew(() => GooglePlayServicesUtil.IsGooglePlayServicesAvailable(this));

            //if (resultCode != ConnectionResult.Success)
            //{
            //    CreateAlert("Fehler", "Dieses Ger�t wird nicht unterst�tzt. Der Google Playstore fehlt.");
            //    return false;
            //}

            return await Task.Factory.StartNew(() => true);
        }
    }
}
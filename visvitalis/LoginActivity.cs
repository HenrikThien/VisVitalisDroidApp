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
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using System.Threading.Tasks;
using System.Threading;
using visvitalis.Networking;
using Android.Gms.Common;

namespace visvitalis
{
	[Activity(Label="Mitarbeiter Login", Icon = "@drawable/ic_launcher", Theme = "@style/CustomAppTheme")]
    public class LoginActivity : Activity
    {
        private ProgressDialog _progressDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
			SetContentView (Resource.Layout.LoginScreen);

			ActionBar.SetDisplayShowHomeEnabled (true);

			ActionBar.SetIcon (Resources.GetDrawable (Resource.Drawable.ic_launcher));

			var typeface = Typeface.CreateFromAsset (this.Assets, "fonts/Generica.otf");
			var loginTitle = FindViewById<TextView> (Resource.Id.textView1);

			loginTitle.SetTypeface (typeface, TypefaceStyle.Normal);
			loginTitle.TextSize = 35;

			var st = new SpannableString("  " + "Vis Vitalis");
			st.SetSpan (new TypefaceSpan ("fonts/Generica.otf"), 0, st.Length (), SpanTypes.ExclusiveExclusive);

			ActionBar.TitleFormatted = st;

            var loginBtn = FindViewById<Button>(Resource.Id.button1);

            if (!IsPlayServiceAvailable())
            {
                loginBtn.Enabled = false;
            }

            loginBtn.Click += LoginBtn_Click;
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

                        }
                        else
                        {
                            CreateAlert("Fehler", "Der Server konnte nicht kontaktiert werden!");
                        }
                    }
                    else
                    {
                        CreateAlert("Fehler", "Sie müssen eine Internetverbindung herstellen!");
                        return;
                    }
                }
            }
            else
            {
                CreateAlert("Fehler", "Es müssen beide Felder ausgefüllt werden!");
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
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton("Ok", (sender, args) => { alert.Dispose(); });
            alert.Show();
        }

        bool IsPlayServiceAvailable()
        {
            int resultCode = GooglePlayServicesUtil.IsGooglePlayServicesAvailable(this);

            if (resultCode != ConnectionResult.Success)
            {
                if (GooglePlayServicesUtil.IsUserRecoverableError(resultCode))
                {
                    CreateAlert("Fehler", GooglePlayServicesUtil.GetErrorString(resultCode));
                }
                else
                {
                    CreateAlert("Fehler", "Dieses Gerät wird nicht unterstützt. Der Google Playstore fehlt.");
                }

                return false;
            }

            return true;
        }
    }
}
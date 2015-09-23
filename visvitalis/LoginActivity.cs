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

			ActionBar.SetIcon (this.GetDrawable (Resource.Drawable.ic_launcher));


			var typeface = Typeface.CreateFromAsset (this.Assets, "fonts/Generica.otf");
			var loginTitle = FindViewById<TextView> (Resource.Id.textView1);

			loginTitle.SetTypeface (typeface, TypefaceStyle.Normal);
			loginTitle.TextSize = 35;

			var st = new SpannableString("  " + "Vis Vitalis");
			st.SetSpan (new TypefaceSpan ("fonts/Generica.otf"), 0, st.Length (), SpanTypes.ExclusiveExclusive);

			ActionBar.TitleFormatted = st;

            var loginBtn = FindViewById<Button>(Resource.Id.button1);
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
                    if (await connector.IsServerAvailableAsync())
                    {

                    }
                    else
                    {
                        CreateAlert("Fehler", "Der Server konnte nicht kontaktiert werden!");
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
    }
}
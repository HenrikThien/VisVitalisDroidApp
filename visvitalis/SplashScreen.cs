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
using System.Threading.Tasks;
using System.Threading;
using Android.Graphics;
using visvitalis.Utils;
using visvitalis.NotificationService;

namespace visvitalis
{
	[Activity(MainLauncher = true, Icon = "@drawable/ic_launcher", Theme = "@style/Theme.Main", NoHistory = true)]
    public class SplashScreen : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
			base.OnCreate (bundle);
            StaticHolder.SessionHolder = new Networking.Session();
            InitApp();
        }

        async void InitApp()
        {
            await TryLoadSession();
        }

        async Task TryLoadSession()
        {
            var session = await StaticHolder.LoadSessionAsync(this);

            if (session != null)
            {
                StaticHolder.SessionHolder = session;

                var intent = new Intent(this, typeof(RegistrationIntentService));
                StartService(intent);

                OpenMainActivity();
            }
            else
            {
                OpenLoginActivity();
            }
        }

        void OpenLoginActivity()
        {
            var intent = new Intent();
            intent.SetClass(this, typeof(LoginActivity));
            StartActivity(intent);
        }
		void OpenMainActivity()
        {
            var intent = new Intent();
            intent.SetClass(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
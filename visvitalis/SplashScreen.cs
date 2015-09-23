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

namespace visvitalis
{
	[Activity(MainLauncher = true, Icon = "@drawable/ic_launcher", Theme = "@style/SplashScreenTheme", NoHistory = true)]
    public class SplashScreen : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
			base.OnCreate (bundle);
			waitForIt ();
        }

		async void waitForIt()
		{
			await Task.Run (() => {
				Thread.Sleep(1000);
			});

			var intent = new Intent ();
			intent.SetClass (this, typeof(LoginActivity));
			StartActivity (intent);
		}
    }
}
using Android.App;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;
using visvitalis.Utils;
using visvitalis.NotificationService;
using Android.Util;

namespace visvitalis
{
    [Activity(MainLauncher = true, Icon = "@drawable/ic_launcher", Theme = "@style/SplashScreenTheme", NoHistory = true)]
    public class SplashScreen : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
			base.OnCreate (bundle);

            var installReceiver = new AppInstallBroadcastReceiver();
            installReceiver.OnUpdateInstalled += InstallReceiver_OnUpdateInstalled;

            var intentFilter = new IntentFilter();
            intentFilter.AddAction(Intent.ActionPackageChanged);
            intentFilter.AddAction(Intent.ActionPackageReplaced);

            try
            {
                RegisterReceiver(installReceiver, intentFilter);
            }
            catch {
                UnregisterReceiver(installReceiver);
                RegisterReceiver(installReceiver, intentFilter);
            }

            StaticHolder.SessionHolder = new Networking.Session();
            InitApp();
        }

        private void InstallReceiver_OnUpdateInstalled(BroadcastReceiver receiver)
        {
            UnregisterReceiver(receiver);
        }

        protected override void OnPause()
        {
            var intent = new Intent(this, typeof(RegistrationIntentService));
            StartService(intent);
            base.OnPause();
        }

        protected override void OnResume()
        {
            var intent = new Intent(this, typeof(RegistrationIntentService));
            StartService(intent);
            base.OnResume();
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

                OpenActivity(typeof(MainActivity));
            }
            else
            {
                OpenActivity(typeof(LoginActivity));
            }
        }

        void OpenActivity(System.Type type)
        {
            var intent = new Intent();
            intent.SetClass(this, type);
            StartActivity(intent);
        }
    }
}
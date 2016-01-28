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
using Android.App;
using Android.Content;
using Android.Gms.Gcm.Iid;

namespace visvitalis.NotificationService
{
    [Service(Exported = false), IntentFilter(new[] { "com.google.android.gms.iid.InstanceID" })]
    class AppInstanceIDListenerService : InstanceIDListenerService
    {
        public override void OnTokenRefresh()
        {
            var intent = new Intent(this, typeof(RegistrationIntentService));
            StartService(intent);
        }
    }
}
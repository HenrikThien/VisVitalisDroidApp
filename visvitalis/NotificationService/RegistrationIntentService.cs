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
using Android.Util;
using Android.Gms.Gcm;
using Android.Gms.Gcm.Iid;
using Android.Preferences;
using visvitalis.Utils;
using visvitalis.Networking;

namespace visvitalis.NotificationService
{
    [Service(Exported = false)]
    class RegistrationIntentService : IntentService
    {
        const string ServerId = AppConstants.GoogleAPIServerId;
        static object locker = new object();

        public RegistrationIntentService() : base("RegistrationIntentService")
        {
        }

        protected override void OnHandleIntent(Intent intent)
        {
            try
            { 
                lock (locker)
                {
                    var instanceID = InstanceID.GetInstance(this);
                    var token = instanceID.GetToken(ServerId, GoogleCloudMessaging.InstanceIdScope, null);
                    SendRegistrationToAppServer(token);
                    Subscribe(token);
                }
            }
            catch
            {
                return;
            }
        }

        async void SendRegistrationToAppServer(string token)
        {
            await CheckTokenInSharedPreferences(token);
        }

        async System.Threading.Tasks.Task CheckTokenInSharedPreferences(string token)
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            var groupname = preferences.GetString(AppConstants.GroupName, "undefined");
            var oldToken = preferences.GetString(AppConstants.DeviceToken, "undefined");

            if (oldToken == "undefined" || oldToken != token)
            {
                if (groupname != "undefined")
                {
                    using (var client = new ServerConnector())
                    {
                        var response = await client.RegisterDeviceAsync(groupname, token);

                        if (response != null)
                        {
                            var editor = preferences.Edit();
                            editor.PutString(AppConstants.DeviceToken, token);
                            editor.Commit();
                        }
                    }
                }
            }
        }

        void Subscribe(string token)
        {
            var pubSub = GcmPubSub.GetInstance(this);
            pubSub.Subscribe(token, "/topics/global", null);
        }
    }
}
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
using visvitalis.Networking;
using System.Threading.Tasks;
using Android.Preferences;
using Newtonsoft.Json;
using Android.Util;

namespace visvitalis.Utils
{
    static class StaticHolder
    {
        public static Session SessionHolder { get; set; }

        public static async Task DestorySession(Context context)
        {
            await Task.Factory.StartNew(() =>
            {
                var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
                var editor = preferences.Edit();

                editor.PutString(AppConstants.Session, "undefined");
                editor.PutString(AppConstants.GroupName, "undefined");
                editor.Commit();
            });
        }

        public static async Task SaveSessionAsync(Context context)
        {
            var sessionValue = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(SessionHolder));
            await Task.Factory.StartNew(() =>
            {
                var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
                var editor = preferences.Edit();

                editor.PutString(AppConstants.Session, sessionValue);
                editor.Commit();
            });
        }

        public static async Task<Session> LoadSessionAsync(Context context)
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            var session = preferences.GetString(AppConstants.Session, "undefined");

            if (session != "undefined")
            {
                return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Session>(session));
            }
            else
            {
                return null;
            }
        }
    }
}
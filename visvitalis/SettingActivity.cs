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
using Android.Preferences;

namespace visvitalis
{
    [Activity(Label = "Einstellungen", Icon = "@drawable/ic_launcher", Theme = "@style/Theme.Main")]
    public class SettingActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SettingsLayout);

            ActionBar.SetDisplayHomeAsUpEnabled(true);
        }

        private async Task LoadSettingsAsync<T>(string key, T defValue)
        {
            await Task.Factory.StartNew(() =>
            {
                //
            });
        }

        private async Task SaveSettingsAsync<T>(string key, T val)
        {
            await Task.Factory.StartNew(() =>
            {
                var preferencesManager = PreferenceManager.GetDefaultSharedPreferences(this);

                if (typeof(T) == typeof(string))
                {
                    var editor = preferencesManager.Edit();
                    editor.PutString(key, Convert.ToString(val));
                    editor.Commit();
                }
                else if (typeof(T) == typeof(int))
                {
                    var editor = preferencesManager.Edit();
                    editor.PutInt(key, Convert.ToInt32(val));
                    editor.Commit();
                }
                else if (typeof(T) == typeof(bool))
                {
                    var editor = preferencesManager.Edit();
                    editor.PutBoolean(key, Convert.ToBoolean(val));
                    editor.Commit();
                }
            });

            Toast.MakeText(this, "Einstellungen wurden gespeichert.", ToastLength.Short).Show();
        }

        public override bool OnOptionsItemSelected(IMenuItem menu)
        {
            switch (menu.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                default:
                    return base.OnOptionsItemSelected(menu);
            }
        }
    }
}
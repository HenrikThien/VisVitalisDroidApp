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
using Java.IO;
using System.Threading.Tasks;

namespace visvitalis.Utils
{
    [BroadcastReceiver]
    class AppInstallBroadcastReceiver : BroadcastReceiver
    {
        public delegate void UpdateInstalled(BroadcastReceiver receiver);
        public event UpdateInstalled OnUpdateInstalled;

        public override void OnReceive(Context context, Intent intent)
        {
            var path = intent.GetStringExtra("FILE_APK_PATH");
            var file = new File(path);

            Init(file);
        }

        private async void Init(File file)
        {
            await RemoveAsync(file);
            AppInstalled(this);
        }

        private async Task RemoveAsync(File file)
        {
            await Task.Factory.StartNew(() =>
            {
                if (file.Exists())
                {
                    file.Delete();
                }
            });
        }

        public void AppInstalled(BroadcastReceiver receiver)
        {
            if (OnUpdateInstalled != null)
            {
                OnUpdateInstalled(receiver);
            }
        }
    }
}
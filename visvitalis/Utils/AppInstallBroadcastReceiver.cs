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
using System.IO;

namespace visvitalis.Utils
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionPackageChanged, Intent.ActionPackageReplaced, Intent.ActionInstallPackage })]
    class AppInstallBroadcastReceiver : BroadcastReceiver
    {
        public delegate void UpdateInstalled(BroadcastReceiver receiver);
        public event UpdateInstalled OnUpdateInstalled;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent != null && intent.Data != null && context.PackageName.Equals(intent.Data.SchemeSpecificPart))
            {
                var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString();
                var downloadUpdateFolder = Path.Combine(downloadFolder, "vv-updates");
                var downloadPath = Path.Combine(downloadUpdateFolder, "visvitalis.apk");
                var file = new Java.IO.File(downloadPath);

                Init(file);
            }
        }

        private async void Init(Java.IO.File file)
        {
            await RemoveAsync(file);
            AppInstalled(this);
        }

        private async Task RemoveAsync(Java.IO.File file)
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
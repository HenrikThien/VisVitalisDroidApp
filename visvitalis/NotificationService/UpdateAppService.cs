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
using Android.Support.V7.App;
using visvitalis.Networking;
using System.Threading.Tasks;
using visvitalis.Utils;
using Android.Content.PM;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using visvitalis.JSON;
using Android.Support.V4.Content;
using Android.Util;

namespace visvitalis.NotificationService
{
    [BroadcastReceiver]
    [IntentFilter(new[] { "visvitalis.notificationservice.update" }, Priority = (int)IntentFilterPriority.HighPriority)]
    class UpdateReceiver : WakefulBroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var service = new Intent(context, typeof(UpdateAppService));
            service.PutExtras(intent);
            StartWakefulService(context, service);
        }
    }

    [Service(Exported = true)]
    [IntentFilter(new[] { "visvitalis.notificationservice.update" })]
    class UpdateAppService : IntentService
    {
        private int id = 1;
        private NotificationManager mNotifyManager;
        private NotificationCompat.Builder mBuilder;
        private PackageInfo pInfo;
        private int fileId;

        protected override void OnHandleIntent(Intent intent)
        {
            mNotifyManager = (NotificationManager)GetSystemService(NotificationService);
            pInfo = PackageManager.GetPackageInfo(PackageName, 0);
            Init();
        }

        private async void Init()
        {
            await TryDownloadUpdate();
        }

        private async Task TryDownloadUpdate()
        {
            using (var client = new ServerConnector())
            {
                if (await client.IsNetworkAvailable(this))
                {
                    if (await client.IsServerAvailableAsync())
                    {
                        var response = await client.CheckForUpdates(this, StaticHolder.SessionHolder, pInfo.VersionCode);

                        try
                        {
                            var msg = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ResponseMessage>(response));

                            if (msg.Valid && msg.NewestVersion > pInfo.VersionCode)
                            {
                                var webclient = new WebClient();
                                webclient.DownloadProgressChanged += Webclient_DownloadProgressChanged;
                                client.OnNotifyUserDownloadCompleted += Webclient_DownloadFileCompleted;

                                mNotifyManager = (NotificationManager)GetSystemService(NotificationService);
                                mBuilder = new NotificationCompat.Builder(this);
                                mBuilder.SetContentTitle("VisVitalis App: Update download.")
                                    .SetContentText("Das neue Update wird runtergeladen.")
                                    .SetOngoing(true)
                                    .SetSmallIcon(Android.Resource.Drawable.StatSysDownload);

                                if (await client.DownloadUpdatesAsync(webclient, msg.NewestVersion))
                                {
                                    fileId = msg.NewestVersion;
                                    client.NotifyUser_DownloadCompleted();
                                }
                                else
                                {
                                    mBuilder = new NotificationCompat.Builder(this);
                                    mBuilder.SetContentTitle("VisVitalis App: Update nicht möglich.");
                                    mBuilder.SetContentText("Das herunterladen des Updates ist derzeit nicht möglich.")
                                        .SetSmallIcon(Resource.Drawable.ic_launcher);
                                    mNotifyManager.Notify(id, mBuilder.Build());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            mBuilder = new NotificationCompat.Builder(this);
                            mBuilder.SetContentTitle("VisVitalis App: Update nicht möglich.")
                            .SetSmallIcon(Resource.Drawable.ic_launcher);

                            var textStyle = new NotificationCompat.BigTextStyle();
                            string longTextMessage = ex.ToString();
                            textStyle.BigText(longTextMessage);
                            textStyle.SetSummaryText("Jetzt mit der App öffnen!");
                            mBuilder.SetStyle(textStyle);
                            mNotifyManager.Notify(id, mBuilder.Build());
                        }
                    }
                }
            }
        }

        private void Webclient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            mBuilder.SetProgress(100, e.ProgressPercentage, false);
            mBuilder.SetContentText("Status: " + FormatBytes(e.BytesReceived) + "/" + FormatBytes(e.TotalBytesToReceive));
            mNotifyManager.Notify(id, mBuilder.Build());
        }

        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private void Webclient_DownloadFileCompleted()
        {
            var intent = new Intent(this, typeof(SettingsActivity));
            intent.PutExtra("SCROLL_2_BOTTOM", true);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.OneShot);

            var textStyle = new NotificationCompat.BigTextStyle();
            string longTextMessage = "Der Download wurde fertig gestellt, jetzt installieren.";
            textStyle.BigText(longTextMessage);
            textStyle.SetSummaryText("Jetzt mit der App öffnen!");

            mBuilder.SetContentText("Download wurde fertig gestellt.")
              .SetTicker("Der Download wurde fertig gestellt, jetzt installieren.")
              .SetStyle(textStyle)
              .SetProgress(0, 0, false)
              .SetDefaults((int)NotificationDefaults.All)
              .SetContentIntent(pendingIntent)
              .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone);

            mNotifyManager.Notify(id, mBuilder.Build());

            StopSelf();
        }
    }
}
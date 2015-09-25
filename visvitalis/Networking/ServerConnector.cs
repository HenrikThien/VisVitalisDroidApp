using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Java.Net;
using visvitalis.Utils;
using Android.Net.Wifi;
using Android.Net;

namespace visvitalis.Networking
{
    public sealed class ServerConnector : IDisposable
    {
        public ServerConnector()
        {

        }

        public async Task LoginAsync(string groupname, string password)
        {
            //using (var client = new HttpClient())
            //{

            //}
        }

        /// <summary>
        ///     Determines whether the server is online or offline
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsServerAvailableAsync()
        {
            var available = false;

            try
            {
                var addr = new InetSocketAddress(AppConstants.ServerIP, 80);
                var sock = new Socket();

                await sock.ConnectAsync(addr, 3000);
                available = true;
            }
            catch
            {
                available = false;
            }

            return available;
        }

        public async Task<bool> IsNetworkAvailable(Context Context)
        {
            var available = false;

            await Task.Run(() =>
            {
                try
                {
                    ConnectivityManager cm = (ConnectivityManager)Context.GetSystemService(Context.ConnectivityService);
                    var networkInfo = cm.GetNetworkInfo(ConnectivityType.Wifi);

                    available = networkInfo.IsConnectedOrConnecting;
                }
                catch
                {
                    available = false;
                }

                if (!available)
                {
                    try
                    {
                        ConnectivityManager cm = (ConnectivityManager)Context.GetSystemService(Context.ConnectivityService);
                        var networkInfo = cm.ActiveNetworkInfo;

                        available = (networkInfo != null && networkInfo.IsConnectedOrConnecting);
                    }
                    catch
                    {
                        available = false;
                    }
                }
            });

            return available;
        }

        public void Dispose()
        {
            
        }
    }
}
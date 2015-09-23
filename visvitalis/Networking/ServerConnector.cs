using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Java.Net;
using visvitalis.Utils;

namespace visvitalis.Networking
{
    public sealed class ServerConnector : IDisposable
    {
        public ServerConnector()
        {

        }

        public async Task LoginAsync(string groupname, string password)
        {
            throw new NotImplementedException();
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

        public void Dispose()
        {
            
        }
    }
}
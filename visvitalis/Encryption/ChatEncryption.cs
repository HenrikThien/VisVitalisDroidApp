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
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Http;
using visvitalis.Utils;

namespace visvitalis.Encryption
{
    sealed class ChatEncryption
    {
        private const string DEVICE_PRIVATE_KEY = @"-----BEGIN RSA PRIVATE KEY-----MIICXAIBAAKBgQCvSB+1aPCuiGGXmQ/ykqHbNoPRoKHuEEv6tYJdtri3u9nZFNaLy3Ose63hQnvyKN8LyBLH8wZf8Z0X7IVbVpD7EKpcrFD9YN2j24nGrazv4XEHD/A8gl3UPKVPdAACEvSNcdvv8m42Jx3ekAnCjn3HrSpBoX9jizp9t2qvKGPOvwIDAQABAoGAByV5pXvR1EvbLsMe01UHJFjkpvdVos8nSeF8nzWD8nnGOAORe8GfxbiFLln3k7f24BQYL+7Io8DGFuOdzEuLPZ/nlYBzeStc99VngkEvwaw81yXnoGAWHSv+vWbeg8MEsy8ps2Jgr20xmPyR5AW329xy2Azei6T6+WHfmJUyjwECQQDoRa1IY1GD8e4Z5j4B532knGteDh/vIqPXlG1JofyN9JO/VcPqbzGSny6RU+eAxCK+j4ycwyvfEWmwiXpZoVLXAkEAwTAMIScASSvJdQ4a/+PUuqxtcZ4PkpwGzbJ5Y3og9yjFNR84q6FWB13nWy1eKC3+9mSflbtm2wwb/lrW1uPOWQJAAhzAGqxsjVqh47JoVfQY/Go/v7c5Kx+RheBfrg+/EDttLIxHH9arCL5R2hh9PnqKJll/2d0chQbPgz980VvaOQJBAIq4CI2ppq/j7DXMWiDSpQciFzhVahM5TD1Z4YZHxPIU6X6am6PKJq8Fg8JZ0lmBpamhWWI3/cRebp929Pu6+okCQEn9rFh+xZaI++MRuQRnKyGhbFpadDAU0x6tJXLlWqOntoO7nA4eSn7eKm061a99Nsqj7lyrx1griOABXmuGXt4=-----END RSA PRIVATE KEY-----";

        public bool IsConnected { get; private set; }
        private RsaToPhp Rsa { get; set; }
        private AesToPhp Aes { get; set; }

        private string EncryptedKey { get; set; }
        private string EncryptedIv { get; set; }


        public ChatEncryption()
        {
            IsConnected = false;

            Rsa = new RsaToPhp();
            Aes = new AesToPhp();
        }

        public async Task<string> SendMessage(string message)
        {
            IsConnected = await EstablishConnection();

            if (IsConnected)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://" + AppConstants.ServerIP);

                    string encryptedMsg = Aes.Encrypt(message);
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("message", encryptedMsg),
                        new KeyValuePair<string, string>("key", EncryptedKey),
                        new KeyValuePair<string, string>("iv", EncryptedIv)
                    });

                    var httpResponse = await client.PostAsync("/Chat/message", content);
                    httpResponse.EnsureSuccessStatusCode();

                    var response = await httpResponse.Content.ReadAsStringAsync();
                    return response;
                }
            }
            else
            {
                return "Es konnte keine Verbindung zum Server hergestellt werden!";
            }
        }

        private async Task<bool> EstablishConnection()
        {
            string remotePublicKey = await LoadRemotePublicKeyAsync();
            Rsa.LoadCertificateFromString(remotePublicKey);

            Aes.GenerateRandomKeys();

            EncryptedKey = EncryptionUtility.ToUrlSafeBase64(Rsa.Encrypt(Aes.EncryptionKey));
            EncryptedIv = EncryptionUtility.ToUrlSafeBase64(Rsa.Encrypt(Aes.EncryptionIV));

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + AppConstants.ServerIP);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("key", EncryptedKey),
                    new KeyValuePair<string, string>("iv", EncryptedIv)
                });

                var httpResponse = await client.PostAsync("/Chat/keyiv", content);
                httpResponse.EnsureSuccessStatusCode();

                var response = await httpResponse.Content.ReadAsStringAsync();

                if (Aes.Decrypt(response) == "AES OK")
                {
                    return true;
                }

                return false;
            }
        }

        private async Task<string> LoadRemotePublicKeyAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new System.Uri("http://" + AppConstants.ServerIP);
                var httpResponse = await client.GetAsync("/Chat/publickey");
                httpResponse.EnsureSuccessStatusCode();

                var response = await httpResponse.Content.ReadAsStringAsync();

                try
                {
                    return response.ToString();
                }
                catch
                {
                    return "undefined";
                }
            }
        }
    }
}
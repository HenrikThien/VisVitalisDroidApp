using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IO;

namespace visvitalis.Encryption
{
    public class RsaToPhp
    {
        private X509Certificate2 cert;
        private bool initialized;

        /// <summary>
        /// Create a new PHP compatible RSA encryptor from a certificate.
        /// </summary>
        /// <param name="certificateLocation">The file to load as a certificate.</param>
        public RsaToPhp(string certificateLocation)
        {
            LoadCertificateFromFile(certificateLocation);
        }

        /// <summary>
        /// Create a new PHP compatible RSA encryptor. Make sure you load a certificate before trying to encrypt.
        /// </summary>
        public RsaToPhp()
        {
            initialized = false;
        }

        /// <summary>
        /// Create a new PHP compatible RSA encryptor from a certificate file.
        /// </summary>
        /// <param name="certificateLocation">The file to load as a certificate.</param>
        public void LoadCertificateFromFile(string certificateLocation)
        {
            try
            {
                cert = GetCertificateFromFile(certificateLocation);
                initialized = true;
            }
            catch (Exception ex)
            {
                initialized = false;
                throw new CryptographicException("There was an error reading the certificate.", ex);
            }

            // You should keep the private key on the server and only have the public key on the client side.
            if (cert.HasPrivateKey)
                throw new CryptographicException("Use a certificate that does not contain a private key for security purposes.");
        }

        /// <summary>
        /// Create a new PHP compatible RSA encryptor from a certificate string.
        /// </summary>
        /// <param name="certificateText">The base64 encoded text to load as a certificate.</param>
        public void LoadCertificateFromString(string certificateText)
        {
            try
            {
                cert = GetCertificate(certificateText);
                initialized = true;
            }
            catch (Exception ex)
            {
                initialized = false;
                throw new CryptographicException("There was an error reading the certificate.", ex);
            }

            // You should keep the private key on the server and only have the public key on the client side.
            if (cert.HasPrivateKey)
                throw new CryptographicException("Use a certificate that does not contain a private key for security purposes.");
        }

        /// <summary>
        /// Load a public RSA key from a certificate string.
        /// </summary>
        /// <param name="key">The certificate text.</param>
        /// <exception cref="FormatException"></exception>
        private X509Certificate2 GetCertificate(string key)
        {
            try
            {
                if (key.Contains("-----"))
                {
                    // Get just the base64 encoded part of the file then trim off the beginning and ending -----BLAH----- tags
                    key = key.Split(new string[] { "-----" }, StringSplitOptions.RemoveEmptyEntries)[1];
                }

                // Remove "new line" characters
                key.Replace("\n", "");

                // Convert the key to a certificate for encryption
                return new X509Certificate2(Convert.FromBase64String(key));
            }
            catch (Exception ex)
            {
                throw new FormatException("The certificate key was not in the expected format.", ex);
            }
        }

        /// <summary>
        /// Load a public RSA key from a certificate file.
        /// </summary>
        /// <param name="file">The certificate file.</param>
        /// <returns></returns>
        private X509Certificate2 GetCertificateFromFile(string file)
        {
            return GetCertificate(File.ReadAllText(file));
        }

        /// <summary>
        /// Encrypt a messages using the supplied public certificate.
        /// </summary>
        /// <param name="message">The message to encrypt.</param>
        public byte[] Encrypt(byte[] message)
        {
            if (initialized)
            {
                RSACryptoServiceProvider publicprovider = (RSACryptoServiceProvider)cert.PublicKey.Key;
                return publicprovider.Encrypt(message, false);
            }
            else
            {
                throw new Exception("The RSA engine has not been initialized with a certificate yet.");
            }
        }

        /// <summary>
        /// Encrypt a messages using the supplied public certificate and returns the ciphertext as a base64 encoded string.
        /// </summary>
        /// <param name="message">The message to encrypt.</param>
        public string Encrypt(string message)
        {
            if (initialized)
            {
                // Encrypt and convert to a form of base64 with two url-unfriendly characters replaced.
                return EncryptionUtility.ToUrlSafeBase64(Encrypt(ASCIIEncoding.ASCII.GetBytes(message)));
            }
            else
            {
                throw new Exception("The RSA engine has not been initialized with a certificate yet.");
            }
        }
    }
}
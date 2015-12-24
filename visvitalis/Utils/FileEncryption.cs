using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace visvitalis.Utils
{
    class FileEncryption
    {
        public string Password { get; set; }

        private void EncryptFile(string inputFile, string outputFile)
        {
            try
            {
                var UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(Password);

                string cryptFile = outputFile;
                var fsCrypt = new FileStream(cryptFile, FileMode.Create);

                var RMCrypto = new RijndaelManaged();

                var cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateEncryptor(key, key),
                    CryptoStreamMode.Write);

                var fsIn = new FileStream(inputFile, FileMode.Open);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);

                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch
            {
            }
        }

        private void DecryptFile(string inputFile, string outputFile)
        {
            try
            {
                var UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(Password);

                var fsCrypt = new FileStream(inputFile, FileMode.Open);

                var RMCrypto = new RijndaelManaged();

                var cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateDecryptor(key, key),
                    CryptoStreamMode.Read);

                var fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch { }
        }
    }
}
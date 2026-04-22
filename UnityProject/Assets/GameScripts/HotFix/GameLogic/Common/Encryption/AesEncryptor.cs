using System;
using System.Security.Cryptography;
using System.Text;

namespace GameLogic
{
    /// <summary>
    /// AES-ECB / PKCS7 / BlockSize=128 加解密工具。
    /// 移植自 _PortableEncryptionPackage，密钥与生成器端保持一致。
    /// </summary>
    public static class AesEncryptor
    {
        public static string Encrypt(string plainText, string key)
        {
            byte[] keyArray = Encoding.UTF8.GetBytes(key);
            byte[] plainArray = Encoding.UTF8.GetBytes(plainText);

            using var aes = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7,
                BlockSize = 128
            };

            using var encryptor = aes.CreateEncryptor();
            byte[] result = encryptor.TransformFinalBlock(plainArray, 0, plainArray.Length);
            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherBase64, string key)
        {
            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] cipherArray = Convert.FromBase64String(cipherBase64);

                using var aes = new RijndaelManaged
                {
                    Key = keyArray,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7,
                    BlockSize = 128
                };

                using var decryptor = aes.CreateDecryptor();
                byte[] result = decryptor.TransformFinalBlock(cipherArray, 0, cipherArray.Length);
                return Encoding.UTF8.GetString(result);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

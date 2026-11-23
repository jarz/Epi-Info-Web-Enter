using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Epi.Web.Enter.Common.Security
{
    /// <summary>
    /// Encrypts and decrypts connection strings, organization keys, session user ids and the admin key with
    /// deployment-specific key material from appSettings. There is no built-in fallback.
    /// </summary>
    public class Cryptography
    {
        // PublicationOnly doesn't cache configuration exceptions, so a corrected configuration is picked up.
        private static readonly Lazy<KeyMaterial> ConfiguredKeys = new Lazy<KeyMaterial>(
            () => KeyMaterial.FromSettings(ConfigurationManager.AppSettings),
            System.Threading.LazyThreadSafetyMode.PublicationOnly);

        public static KeyMaterial ConfiguredKeyMaterial
        {
            get { return ConfiguredKeys.Value; }
        }

        /// <summary>
        /// Fails fast when key material is missing or publicly known, when an AdminKey is configured but invalid,
        /// or when a configured password salt is unusable or publicly known.
        /// </summary>
        public static void ValidateConfiguration()
        {
            ValidateConfiguration(ConfigurationManager.AppSettings);
        }

        public static void ValidateConfiguration(NameValueCollection settings)
        {
            KeyMaterial keys = KeyMaterial.FromSettings(settings);
            AdminKey.Read(settings, keys);
            PasswordSalt.ValidateConfiguration(settings);
        }

        public static string Encrypt(string plainText)
        {
            return Encrypt(plainText, ConfiguredKeys.Value);
        }

        public static string Decrypt(string cipherText)
        {
            return Decrypt(cipherText, ConfiguredKeys.Value);
        }

        public static string Encrypt(string plainText, KeyMaterial keys)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
            using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keys.DeriveKey(), keys.VectorBytes()))
            {
                return Convert.ToBase64String(encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length));
            }
        }

        public static string Decrypt(string cipherText, KeyMaterial keys)
        {
            if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
            using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keys.DeriveKey(), keys.VectorBytes()))
            {
                return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length));
            }
        }
    }
}

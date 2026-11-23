using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;

namespace Epi.Web.Enter.Common.Security
{
    /// <summary>
    /// Reads the deployment-wide salt that <see cref="PasswordHasher"/> combines with each user name. While a deployment
    /// moves off a publicly known salt, the old value stays in LegacyKeyForUserPasswordSalt and users are re-hashed with
    /// KeyForUserPasswordSalt when they next sign in.
    /// </summary>
    public static class PasswordSalt
    {
        public const string Setting = "KeyForUserPasswordSalt";
        public const string LegacySetting = "LegacyKeyForUserPasswordSalt";

        // Rfc2898DeriveBytes rejects shorter salts.
        public const int MinimumBytes = 8;

        public const int GeneratedLength = 64;

        /// <summary>
        /// Creates a random 64-character salt.
        /// </summary>
        public static string Generate()
        {
            return KeyMaterial.RandomString(GeneratedLength);
        }

        /// <summary>
        /// Returns KeyForUserPasswordSalt; throws when it's missing, too short or publicly known.
        /// </summary>
        public static string ReadCurrent(NameValueCollection settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string value = settings[Setting];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(
                    "Missing required appSetting " + Setting + ". Generate a deployment-specific value with Epi.Web.KeyRotation.");
            }

            RequireLength(Setting, value);
            if (KnownPublicValues.IsKnown(value))
            {
                throw new ConfigurationErrorsException(
                    Setting + " is a publicly known value. Move it to " + LegacySetting + ", set a new value generated with " +
                    "Epi.Web.KeyRotation, and users will be re-hashed when they next sign in.");
            }

            return value;
        }

        /// <summary>
        /// Returns LegacyKeyForUserPasswordSalt, or null when it isn't set. Publicly known values are allowed here.
        /// </summary>
        public static string ReadLegacy(NameValueCollection settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string value = settings[LegacySetting];
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            RequireLength(LegacySetting, value);
            if (string.Equals(value, settings[Setting], StringComparison.Ordinal))
            {
                throw new ConfigurationErrorsException(LegacySetting + " must differ from " + Setting + ".");
            }

            return value;
        }

        /// <summary>
        /// Validates the salt settings when either is present. Sign-in still requires KeyForUserPasswordSalt.
        /// </summary>
        public static void ValidateConfiguration(NameValueCollection settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrWhiteSpace(settings[Setting]) && string.IsNullOrWhiteSpace(settings[LegacySetting]))
            {
                return;
            }

            ReadCurrent(settings);
            ReadLegacy(settings);
        }

        private static void RequireLength(string setting, string value)
        {
            if (Encoding.UTF8.GetByteCount(value) < MinimumBytes)
            {
                throw new ConfigurationErrorsException(setting + " must be at least " + MinimumBytes + " bytes long.");
            }
        }
    }
}

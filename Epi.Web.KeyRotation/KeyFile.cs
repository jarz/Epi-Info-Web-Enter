using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Epi.Web.Enter.Common.Security;

namespace Epi.Web.KeyRotation
{
    /// <summary>
    /// A "name=value" file holding deployment key material. Lines starting with # are comments.
    /// </summary>
    public sealed class KeyFile
    {
        public const string AdminKeyGuidSetting = "AdminKeyGuid";

        private KeyFile(KeyMaterial connectionStringKeys, string passwordSalt, Guid? adminKeyGuid)
        {
            ConnectionStringKeys = connectionStringKeys;
            PasswordSalt = passwordSalt;
            AdminKeyGuid = adminKeyGuid;
        }

        public KeyMaterial ConnectionStringKeys { get; }

        /// <summary>New KeyForUserPasswordSalt; null when the file has none.</summary>
        public string PasswordSalt { get; }

        /// <summary>Plain admin key that administrators enter; Web.config stores it encrypted.</summary>
        public Guid? AdminKeyGuid { get; }

        public static KeyFile Generate()
        {
            return new KeyFile(KeyMaterial.Generate(), Epi.Web.Enter.Common.Security.PasswordSalt.Generate(), Guid.NewGuid());
        }

        /// <summary>
        /// Reads new key material. Publicly known values are rejected.
        /// </summary>
        public static KeyFile ReadNew(string path)
        {
            return ParseNew(ReadLines(path));
        }

        /// <summary>
        /// Reads the key material that existing data is encrypted with. Publicly known values are allowed.
        /// </summary>
        public static KeyMaterial ReadOld(string path)
        {
            return ParseOld(ReadLines(path));
        }

        public static KeyFile ParseNew(IEnumerable<string> lines)
        {
            NameValueCollection settings = Parse(lines);
            KeyMaterial keys = KeyMaterial.FromSettings(settings);
            string passwordSalt = null;
            if (!string.IsNullOrWhiteSpace(settings[Epi.Web.Enter.Common.Security.PasswordSalt.Setting]))
            {
                passwordSalt = Epi.Web.Enter.Common.Security.PasswordSalt.ReadCurrent(settings);
            }

            Guid? adminKeyGuid = null;
            string adminValue = settings[AdminKeyGuidSetting];
            if (!string.IsNullOrWhiteSpace(adminValue))
            {
                Guid parsed;
                if (!Guid.TryParse(adminValue, out parsed) || parsed == Guid.Empty)
                {
                    throw new KeyRotationException(AdminKeyGuidSetting + " must be a non-empty GUID.");
                }

                if (KnownPublicValues.IsKnown(parsed))
                {
                    throw new KeyRotationException(AdminKeyGuidSetting + " is a publicly known value. Generate a new key file.");
                }

                adminKeyGuid = parsed;
            }

            return new KeyFile(keys, passwordSalt, adminKeyGuid);
        }

        public static KeyMaterial ParseOld(IEnumerable<string> lines)
        {
            NameValueCollection settings = Parse(lines);
            var missing = new List<string>();
            foreach (string name in new[] { KeyMaterial.PassphraseSetting, KeyMaterial.SaltSetting, KeyMaterial.VectorSetting })
            {
                if (string.IsNullOrWhiteSpace(settings[name])) missing.Add(name);
            }

            if (missing.Count > 0)
            {
                throw new KeyRotationException("The old key file is missing " + string.Join(", ", missing) + ".");
            }

            try
            {
                return new KeyMaterial(settings[KeyMaterial.PassphraseSetting], settings[KeyMaterial.SaltSetting], settings[KeyMaterial.VectorSetting]);
            }
            catch (ArgumentException)
            {
                throw new KeyRotationException("The old key file has an invalid " + KeyMaterial.VectorSetting + "; it must be exactly 16 ASCII characters.");
            }
        }

        public IList<string> ToLines()
        {
            var lines = new List<string>
            {
                "# Epi Info web key material. Keep this file secret and delete it once the values are stored securely.",
                "# Copy the Key* settings into appSettings of every Web.config that shares the database.",
                KeyMaterial.PassphraseSetting + "=" + ConnectionStringKeys.Passphrase,
                KeyMaterial.SaltSetting + "=" + ConnectionStringKeys.Salt,
                KeyMaterial.VectorSetting + "=" + ConnectionStringKeys.Vector,
            };

            if (AdminKeyGuid.HasValue)
            {
                lines.Add("# Give AdminKeyGuid to administrators; rotate stores it encrypted as the AdminKey setting.");
                lines.Add(AdminKeyGuidSetting + "=" + AdminKeyGuid.Value.ToString("D"));
            }

            if (PasswordSalt != null)
            {
                lines.Add("# rotate replaces a publicly known KeyForUserPasswordSalt with this value and keeps the old one as");
                lines.Add("# LegacyKeyForUserPasswordSalt so users are re-hashed when they next sign in.");
                lines.Add(Epi.Web.Enter.Common.Security.PasswordSalt.Setting + "=" + PasswordSalt);
            }

            return lines;
        }

        /// <summary>
        /// Creates the file (never overwrites) readable only by the current Windows user.
        /// </summary>
        public void WriteNew(string path)
        {
            var security = new FileSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, AccessControlType.Allow));
            using (var stream = new FileStream(path, FileMode.CreateNew, FileSystemRights.Write, FileShare.None, 4096, FileOptions.None, security))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                foreach (string line in ToLines())
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static IEnumerable<string> ReadLines(string path)
        {
            if (!File.Exists(path))
            {
                throw new KeyRotationException("Key file not found: " + path);
            }

            return File.ReadAllLines(path);
        }

        private static NameValueCollection Parse(IEnumerable<string> lines)
        {
            var settings = new NameValueCollection(StringComparer.Ordinal);
            int lineNumber = 0;
            foreach (string raw in lines)
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    throw new KeyRotationException("Key file line " + lineNumber + " isn't in name=value form.");
                }

                string name = line.Substring(0, separator).Trim();
                if (settings[name] != null)
                {
                    throw new KeyRotationException("Key file sets " + name + " more than once.");
                }

                settings[name] = line.Substring(separator + 1).Trim();
            }

            return settings;
        }
    }
}

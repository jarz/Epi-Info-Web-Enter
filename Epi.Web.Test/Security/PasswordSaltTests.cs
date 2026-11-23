using System.Collections.Specialized;
using System.Configuration;
using Epi.Web.Enter.Common.Security;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class PasswordSaltTests
    {
        internal const string TestSalt = "test-only-password-salt-3b9e1f7a";

        private static NameValueCollection WithSalts(string current, string legacy)
        {
            NameValueCollection settings = KeyMaterialTests.Settings(KeyMaterialTests.TestKeys);
            if (current != null) settings[PasswordSalt.Setting] = current;
            if (legacy != null) settings[PasswordSalt.LegacySetting] = legacy;
            return settings;
        }

        [TestCase(null)]
        [TestCase("   ")]
        public void ReadCurrent_requires_the_setting(string value)
        {
            var ex = Assert.Throws<ConfigurationErrorsException>(() => PasswordSalt.ReadCurrent(WithSalts(value, null)));

            Assert.That(ex.Message, Does.Contain(PasswordSalt.Setting));
        }

        [Test]
        public void ReadCurrent_rejects_values_shorter_than_eight_bytes()
        {
            var ex = Assert.Throws<ConfigurationErrorsException>(() => PasswordSalt.ReadCurrent(WithSalts("7-bytes", null)));

            Assert.That(ex.Message, Does.Contain("at least 8 bytes"));
        }

        [Test]
        public void ReadCurrent_rejects_the_former_public_salt_without_echoing_it()
        {
            var ex = Assert.Throws<ConfigurationErrorsException>(() => PasswordSalt.ReadCurrent(WithSalts(FormerPublicValues.PasswordSalt, null)));

            Assert.That(ex.Message, Does.Contain(PasswordSalt.LegacySetting));
            Assert.That(ex.Message, Does.Not.Contain(FormerPublicValues.PasswordSalt));
        }

        [Test]
        public void ReadLegacy_is_optional_and_accepts_the_former_public_salt()
        {
            Assert.That(PasswordSalt.ReadLegacy(WithSalts(TestSalt, null)), Is.Null);
            Assert.That(PasswordSalt.ReadLegacy(WithSalts(TestSalt, FormerPublicValues.PasswordSalt)), Is.EqualTo(FormerPublicValues.PasswordSalt));
        }

        [Test]
        public void ReadLegacy_rejects_the_current_salt()
        {
            var ex = Assert.Throws<ConfigurationErrorsException>(() => PasswordSalt.ReadLegacy(WithSalts(TestSalt, TestSalt)));

            Assert.That(ex.Message, Does.Contain(PasswordSalt.LegacySetting));
            Assert.That(ex.Message, Does.Not.Contain(TestSalt));
        }

        [Test]
        public void ValidateConfiguration_checks_the_salts_only_when_one_is_set()
        {
            Assert.DoesNotThrow(() => Cryptography.ValidateConfiguration(WithSalts(null, null)));
            Assert.DoesNotThrow(() => Cryptography.ValidateConfiguration(WithSalts(TestSalt, FormerPublicValues.PasswordSalt)));
            Assert.Throws<ConfigurationErrorsException>(() => Cryptography.ValidateConfiguration(WithSalts(FormerPublicValues.PasswordSalt, null)));
            Assert.Throws<ConfigurationErrorsException>(() => Cryptography.ValidateConfiguration(WithSalts(null, FormerPublicValues.PasswordSalt)));
        }

        [Test]
        public void Generate_creates_distinct_salts_that_configuration_accepts()
        {
            string first = PasswordSalt.Generate();

            Assert.That(first, Has.Length.EqualTo(PasswordSalt.GeneratedLength));
            Assert.That(first, Is.Not.EqualTo(PasswordSalt.Generate()));
            Assert.That(PasswordSalt.ReadCurrent(WithSalts(first, null)), Is.EqualTo(first));
        }
    }
}

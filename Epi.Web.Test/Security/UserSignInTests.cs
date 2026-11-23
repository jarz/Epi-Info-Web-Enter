using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using Epi.Web.Enter.Common.BusinessObject;
using Epi.Web.Enter.Common.Security;
using Epi.Web.Enter.Interfaces.DataInterface;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class UserSignInTests
    {
        private const string UserName = "Synthetic.User@example.org";
        private const string Password = "synthetic-password-1";

        private FakeUserDao users;

        [SetUp]
        public void CreateUsers()
        {
            users = new FakeUserDao();
        }

        private static string Hash(string saltForSalt, string userName, string password)
        {
            var hasher = new PasswordHasher(saltForSalt);
            return hasher.HashPassword(hasher.CreateSalt(userName), password);
        }

        private static NameValueCollection Settings(string current, string legacy)
        {
            var settings = new NameValueCollection { { PasswordSalt.Setting, current } };
            if (legacy != null) settings[PasswordSalt.LegacySetting] = legacy;
            return settings;
        }

        private UserBO SignIn(NameValueCollection settings, string password)
        {
            return new Epi.Web.BLL.User(users, settings).GetUser(new UserBO { UserName = UserName, PasswordHash = password });
        }

        [Test]
        public void A_user_hashed_with_the_current_salt_signs_in_without_a_rewrite()
        {
            string salt = PasswordSalt.Generate();
            users.Add(UserName, Hash(salt, UserName, Password));

            UserBO signedIn = SignIn(Settings(salt, FormerPublicValues.PasswordSalt), Password);

            Assert.That(signedIn, Is.Not.Null);
            Assert.That(signedIn.UserHighestRole, Is.EqualTo(FakeUserDao.HighestRole));
            Assert.That(users.PasswordUpdates, Is.Empty);
        }

        [Test]
        public void A_user_hashed_with_the_legacy_salt_signs_in_and_is_rehashed_once()
        {
            string salt = PasswordSalt.Generate();
            users.Add(UserName, Hash(FormerPublicValues.PasswordSalt, UserName, Password), resetPassword: true);
            NameValueCollection settings = Settings(salt, FormerPublicValues.PasswordSalt);

            UserBO first = SignIn(settings, Password);
            UserBO second = SignIn(settings, Password);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(users.PasswordUpdates, Has.Count.EqualTo(1));
            Assert.That(users.PasswordUpdates[0].UserName, Is.EqualTo(UserName));
            Assert.That(users.PasswordUpdates[0].ResetPassword, Is.True);
            Assert.That(users.Hashes[UserName], Is.EqualTo(Hash(salt, UserName, Password)));
        }

        [Test]
        public void A_wrong_password_fails_with_either_salt_and_changes_nothing()
        {
            string legacyHash = Hash(FormerPublicValues.PasswordSalt, UserName, Password);
            users.Add(UserName, legacyHash);

            Assert.That(SignIn(Settings(PasswordSalt.Generate(), FormerPublicValues.PasswordSalt), "wrong-password"), Is.Null);
            Assert.That(users.PasswordUpdates, Is.Empty);
            Assert.That(users.Hashes[UserName], Is.EqualTo(legacyHash));
        }

        [Test]
        public void A_legacy_hash_no_longer_signs_in_once_the_legacy_salt_is_removed()
        {
            users.Add(UserName, Hash(FormerPublicValues.PasswordSalt, UserName, Password));

            Assert.That(SignIn(Settings(PasswordSalt.Generate(), null), Password), Is.Null);
            Assert.That(users.PasswordUpdates, Is.Empty);
        }

        [Test]
        public void Sign_in_fails_closed_while_the_current_salt_is_publicly_known()
        {
            users.Add(UserName, Hash(FormerPublicValues.PasswordSalt, UserName, Password));

            Assert.Throws<ConfigurationErrorsException>(() => SignIn(Settings(FormerPublicValues.PasswordSalt, null), Password));
            Assert.That(users.Lookups, Is.EqualTo(0));
        }

        private sealed class FakeUserDao : IUserDao
        {
            public const int HighestRole = 2;

            private readonly Dictionary<string, UserBO> byName = new Dictionary<string, UserBO>(StringComparer.Ordinal);

            public Dictionary<string, string> Hashes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            public List<UserBO> PasswordUpdates { get; } = new List<UserBO>();

            public int Lookups { get; private set; }

            public void Add(string userName, string passwordHash, bool resetPassword = false)
            {
                byName[userName] = new UserBO { UserId = byName.Count + 1, UserName = userName, ResetPassword = resetPassword };
                Hashes[userName] = passwordHash;
            }

            public UserBO GetUser(UserBO User)
            {
                Lookups++;
                UserBO stored;
                if (byName.TryGetValue(User.UserName, out stored) && Hashes[User.UserName] == User.PasswordHash)
                {
                    return new UserBO { UserId = stored.UserId, UserName = stored.UserName, ResetPassword = stored.ResetPassword };
                }

                return null;
            }

            public bool UpdateUserPassword(UserBO User)
            {
                PasswordUpdates.Add(new UserBO { UserName = User.UserName, PasswordHash = User.PasswordHash, ResetPassword = User.ResetPassword });
                Hashes[User.UserName] = User.PasswordHash;
                byName[User.UserName].ResetPassword = User.ResetPassword;
                return true;
            }

            public int GetUserHighestRole(int UserId)
            {
                return HighestRole;
            }

            public bool GetExistingUser(UserBO User) { throw new NotSupportedException(); }

            public bool UpdateUser(UserBO User) { throw new NotSupportedException(); }

            public bool DeleteUser(UserBO User) { throw new NotSupportedException(); }

            public bool InsertUser(UserBO User, OrganizationBO OrgBO) { throw new NotSupportedException(); }

            public bool UpdateUserOrganization(UserBO User, OrganizationBO OrgBO) { throw new NotSupportedException(); }

            public UserBO GetUserByUserId(UserBO User) { throw new NotSupportedException(); }

            public bool UpdateUserInfo(UserBO User, OrganizationBO OrgBO) { throw new NotSupportedException(); }

            public List<UserBO> GetUserByFormId(string FormId) { throw new NotSupportedException(); }

            public UserBO GetCurrentUser(int userId) { throw new NotSupportedException(); }

            public UserBO GetUserByEmail(UserBO User) { throw new NotSupportedException(); }

            public UserBO GetUserByUserName(UserBO User) { throw new NotSupportedException(); }

            public bool IsUserExistsInOrganizaion(UserBO User, OrganizationBO OrgBO) { throw new NotSupportedException(); }

            public List<UserBO> GetUserByOrgId(int OrgId) { throw new NotSupportedException(); }

            public UserBO GetUserByUserIdAndOrgId(UserBO UserBO, OrganizationBO OrgBO) { throw new NotSupportedException(); }

            public List<UserBO> GetAdminsBySelectedOrgs(FormSettingBO FormSettingBO, string p) { throw new NotSupportedException(); }
        }
    }
}

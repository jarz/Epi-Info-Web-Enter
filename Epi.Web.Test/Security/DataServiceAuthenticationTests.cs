using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;
using Epi.Web.Enter.Common.BusinessObject;
using Epi.Web.Enter.Common.ObjectMapping;
using Epi.Web.WCF.SurveyService;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class DataServiceAuthenticationTests
    {
        private const string SharedKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        [Test]
        public void IsAuthorized_accepts_a_matching_key_only_over_secure_transport()
        {
            Assert.That(DataServiceAuthentication.IsAuthorized(SharedKey, SharedKey, true), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("short")]
        [TestCase("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg")]
        public void IsAuthorized_rejects_missing_or_malformed_configured_keys(string configuredKey)
        {
            Assert.That(DataServiceAuthentication.IsAuthorized(configuredKey, SharedKey, true), Is.False);
        }

        [Test]
        public void IsAuthorized_rejects_a_missing_or_mismatched_presented_key()
        {
            Assert.That(DataServiceAuthentication.IsAuthorized(SharedKey, null, true), Is.False);
            Assert.That(DataServiceAuthentication.IsAuthorized(SharedKey, SharedKey.ToUpperInvariant(), true), Is.False);
        }

        [Test]
        public void IsAuthorized_rejects_insecure_transport()
        {
            Assert.That(DataServiceAuthentication.IsAuthorized(SharedKey, SharedKey, false), Is.False);
        }

        [Test]
        public void Wcf_registers_the_service_authorization_attribute()
        {
            var host = new ServiceHost(typeof(EWEDataService), new Uri("http://localhost:8766/"));
            try
            {
                Assert.That(host.Description.Behaviors.Find<DataServiceAuthorizationAttribute>(), Is.Not.Null);
            }
            finally
            {
                host.Abort();
            }
        }

        [Test]
        public void Client_behavior_requires_a_valid_key_and_an_https_endpoint()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new DataServiceClientAuthorizationBehavior("short"));

            var behavior = new DataServiceClientAuthorizationBehavior(SharedKey);
            ServiceEndpoint httpEndpoint = CreateEndpoint("http://localhost/data.svc");
            ServiceEndpoint httpsEndpoint = CreateEndpoint("https://localhost/data.svc");

            Assert.Throws<InvalidOperationException>(() => behavior.Validate(httpEndpoint));
            Assert.DoesNotThrow(() => behavior.Validate(httpsEndpoint));
        }

        [Test]
        public void User_mapper_does_not_copy_the_password_hash_into_the_response_dto()
        {
            var user = new UserBO { UserName = "synthetic-user", PasswordHash = "synthetic-password-hash" };

            Assert.That(Mapper.ToUserDTO(user).PasswordHash, Is.Null);
        }

        private static ServiceEndpoint CreateEndpoint(string address)
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(IEWEDataService));
            return new ServiceEndpoint(contract, new BasicHttpBinding(), new EndpointAddress(address));
        }
    }
}

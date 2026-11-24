using Epi.Web.WCF.SurveyService;

namespace Epi.Web.MVC.DataServiceClient
{
    public sealed class AuthenticatedEWEDataServiceClient : EWEDataServiceClient
    {
        public AuthenticatedEWEDataServiceClient(string endpointConfigurationName, string sharedKey)
            : base(endpointConfigurationName)
        {
            Endpoint.EndpointBehaviors.Add(new DataServiceClientAuthorizationBehavior(sharedKey));
        }
    }
}

using System;
using System.Collections.Generic;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.Enter.Common.Message;
using Epi.Web.MVC.Facade;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Repositories.Core;

namespace Epi.Web.MVC.Test.Fakes
{
    /// <summary>
    /// Hand-written ISurveyFacade double. Members used by the characterized controller paths return configured
    /// synthetic data and record their requests; every other member throws so a test that reaches unexpected
    /// service code (WCF, SQL, check code persistence) fails loudly.
    /// </summary>
    public sealed class FakeSurveyFacade : ISurveyFacade
    {
        public SurveyInfoModel SurveyInfo { get; set; }
        public List<OrganizationDTO> Organizations { get; set; } = new List<OrganizationDTO>();
        public List<FormInfoModel> Forms { get; set; } = new List<FormInfoModel>();
        public FormSettingResponse FormSettings { get; set; }
        public SurveyAnswerResponse FormResponses { get; set; }
        public SurveyAnswerResponse SurveyAnswerResponse { get; set; }
        public UserResponse UserResponse { get; set; } = new UserResponse { User = new List<UserDTO>() };
        public PublishReportResponse Report { get; set; }
        public Exception UpdateResponseStatusFailure { get; set; }

        public List<string> RequestedSurveyIds { get; } = new List<string>();
        public List<OrganizationRequest> OrganizationRequests { get; } = new List<OrganizationRequest>();
        public List<FormsInfoRequest> FormsInfoRequests { get; } = new List<FormsInfoRequest>();
        public List<FormSettingRequest> FormSettingRequests { get; } = new List<FormSettingRequest>();
        public List<SurveyAnswerRequest> FormResponseRequests { get; } = new List<SurveyAnswerRequest>();
        public List<SurveyAnswerRequest> DeleteRequests { get; } = new List<SurveyAnswerRequest>();
        public List<SurveyAnswerRequest> StatusRequests { get; } = new List<SurveyAnswerRequest>();
        public List<UserRequest> UserRequests { get; } = new List<UserRequest>();
        public List<PublishReportRequest> ReportRequests { get; } = new List<PublishReportRequest>();
        public List<Tuple<string, string, int>> SurveyAnswerRequests { get; } = new List<Tuple<string, string, int>>();

        public SurveyInfoModel GetSurveyInfoModel(string surveyId)
        {
            RequestedSurveyIds.Add(surveyId);
            if (SurveyInfo == null)
            {
                throw new InvalidOperationException("No SurveyInfo configured for the fake facade.");
            }
            return SurveyInfo;
        }

        public OrganizationResponse GetOrganizationsByUserId(OrganizationRequest Request)
        {
            OrganizationRequests.Add(Request);
            return new OrganizationResponse { OrganizationList = Organizations };
        }

        public List<FormInfoModel> GetFormsInfoModelList(FormsInfoRequest formReq)
        {
            FormsInfoRequests.Add(formReq);
            return Forms;
        }

        public FormSettingResponse GetFormSettings(FormSettingRequest FormSettingRequest)
        {
            FormSettingRequests.Add(FormSettingRequest);
            return FormSettings ?? throw new InvalidOperationException("No FormSettings configured for the fake facade.");
        }

        public SurveyAnswerResponse GetFormResponseList(SurveyAnswerRequest FormResponseReq)
        {
            FormResponseRequests.Add(FormResponseReq);
            return FormResponses ?? throw new InvalidOperationException("No FormResponses configured for the fake facade.");
        }

        public SurveyAnswerResponse GetSurveyAnswerResponse(string responseId, string FormId = "", int UserId = 0)
        {
            SurveyAnswerRequests.Add(Tuple.Create(responseId, FormId, UserId));
            return SurveyAnswerResponse ?? throw new InvalidOperationException("No SurveyAnswerResponse configured for the fake facade.");
        }

        public SurveyAnswerResponse DeleteResponse(SurveyAnswerRequest SARequest)
        {
            DeleteRequests.Add(SARequest);
            return new SurveyAnswerResponse();
        }

        public void UpdateResponseStatus(SurveyAnswerRequest Request)
        {
            StatusRequests.Add(Request);
            if (UpdateResponseStatusFailure != null)
            {
                throw UpdateResponseStatusFailure;
            }
        }

        public UserResponse GetUserInfo(UserRequest Request)
        {
            UserRequests.Add(Request);
            return UserResponse;
        }

        public PublishReportResponse GetSurveyReport(PublishReportRequest publishReportRequest)
        {
            ReportRequests.Add(publishReportRequest);
            return Report ?? throw new InvalidOperationException("No Report configured for the fake facade.");
        }

        private static NotSupportedException NotUsed(string member)
        {
            return new NotSupportedException("FakeSurveyFacade." + member + " is not expected to be called by these tests.");
        }

        public MvcDynamicForms.Form GetSurveyFormData(string surveyId, int pageNumber, SurveyAnswerDTO surveyAnswerDTO, bool IsMobileDevice, List<SurveyAnswerDTO> _SurveyAnswerDTOList = null, List<FormsHierarchyDTO> FormsHierarchyDTOList = null, bool IsAndroid = false, bool SetAllDropDownValues = true) { throw NotUsed(nameof(GetSurveyFormData)); }
        public SurveyAnswerDTO CreateSurveyAnswer(string surveyId, string responseId, int UserId, bool IsChild = false, string RelateResponseId = "", bool IsEditMode = false, int CurrentOrgId = -1) { throw NotUsed(nameof(CreateSurveyAnswer)); }
        public void UpdateSurveyResponse(SurveyInfoModel surveyInfoModel, string responseId, MvcDynamicForms.Form form, SurveyAnswerDTO surveyAnswerDTO, bool IsSubmited, bool IsSaved, int PageNumber, int UserId) { throw NotUsed(nameof(UpdateSurveyResponse)); }
        public UserAuthenticationResponse ValidateUser(string userName, string password) { throw NotUsed(nameof(ValidateUser)); }
        public UserAuthenticationResponse GetAuthenticatedUser(string userName, bool isSAMS = false) { throw NotUsed(nameof(GetAuthenticatedUser)); }
        public void UpdatePassCode(string responseId, string passcode) { throw NotUsed(nameof(UpdatePassCode)); }
        public UserAuthenticationResponse GetAuthenticationResponse(string responseId) { throw NotUsed(nameof(GetAuthenticationResponse)); }
        public ISurveyAnswerRepository GetSurveyAnswerRepository() { throw NotUsed(nameof(GetSurveyAnswerRepository)); }
        public SurveyAnswerResponse SetChildRecord(SurveyAnswerRequest SurveyAnswerRequest) { throw NotUsed(nameof(SetChildRecord)); }
        public UserAuthenticationResponse GetUserInfo(int UserId) { throw NotUsed("GetUserInfo(int)"); }
        public bool UpdateUser(UserDTO User) { throw NotUsed(nameof(UpdateUser)); }
        public FormSettingResponse SaveSettings(FormSettingRequest FormSettingReq) { throw NotUsed(nameof(SaveSettings)); }
        public SurveyInfoResponse GetChildFormInfo(SurveyInfoRequest SurveyInfoRequest) { throw NotUsed(nameof(GetChildFormInfo)); }
        public FormsHierarchyResponse GetFormsHierarchy(FormsHierarchyRequest FormsHierarchyRequest) { throw NotUsed(nameof(GetFormsHierarchy)); }
        public SurveyAnswerResponse GetSurveyAnswerHierarchy(SurveyAnswerRequest pRequest) { throw NotUsed(nameof(GetSurveyAnswerHierarchy)); }
        public SurveyAnswerResponse GetAncestorResponses(SurveyAnswerRequest pRequest) { throw NotUsed(nameof(GetAncestorResponses)); }
        public SurveyAnswerResponse GetResponsesByRelatedFormId(SurveyAnswerRequest FormResponseReq) { throw NotUsed(nameof(GetResponsesByRelatedFormId)); }
        public void DeleteResponseXml(SurveyAnswerRequest FormResponseReq) { throw NotUsed(nameof(DeleteResponseXml)); }
        public OrganizationResponse GetUserOrganizations(OrganizationRequest Request) { throw NotUsed(nameof(GetUserOrganizations)); }
        public OrganizationResponse GetAdminOrganizations(OrganizationRequest Request) { throw NotUsed(nameof(GetAdminOrganizations)); }
        public OrganizationResponse GetOrganizationInfo(OrganizationRequest Request) { throw NotUsed(nameof(GetOrganizationInfo)); }
        public OrganizationResponse SetOrganization(OrganizationRequest Request) { throw NotUsed(nameof(SetOrganization)); }
        public OrganizationResponse GetOrganizationUsers(OrganizationRequest OrgRequest) { throw NotUsed(nameof(GetOrganizationUsers)); }
        public UserResponse SetUserInfo(UserRequest Request) { throw NotUsed(nameof(SetUserInfo)); }
        public bool HasResponse(string SurveyId, string ResponseId) { throw NotUsed(nameof(HasResponse)); }
        public SourceTablesResponse GetSourceTables(string surveyId) { throw NotUsed(nameof(GetSourceTables)); }
        public bool SetJsonColumn(string Json, string Responseid) { throw NotUsed(nameof(SetJsonColumn)); }
        public string GetSurveyResponseJson(SurveyAnswerDTO surveyAnswerDTO, List<FormsHierarchyDTO> FormsHierarchyDTOList, SurveyControlsResponse List) { throw NotUsed(nameof(GetSurveyResponseJson)); }
        public SurveyControlsResponse GetSurveyControlList(SurveyControlsRequest pRequestMessage) { throw NotUsed(nameof(GetSurveyControlList)); }
        public PublishReportResponse GetSurveyReportList(PublishReportRequest publishReportRequest) { throw NotUsed(nameof(GetSurveyReportList)); }
    }
}

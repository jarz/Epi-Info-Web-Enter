using System;
using System.IO;
using System.Text;
using System.Web.Mvc;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.Enter.Common.Security;
using Epi.Web.MVC.Models;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Fakes
{
    /// <summary>Synthetic Web Enter users, forms and responses; no production data.</summary>
    public static class EnterFixtures
    {
        public const string SurveyId = "2e1d01d4-f50d-4f23-888b-cd4b7fc9884b";
        public const string ResponseId = "a3b4c5d6-0000-4000-8000-000000000001";
        public const string ReportId = "c1d2e3f4-0000-4000-8000-000000000002";
        public const int UserId = 4242;
        public const int OrganizationId = 17;
        public const int UserHighestRole = 2;

        /// <summary>Session["UserId"] holds the user id encrypted with the configured (synthetic) keys.</summary>
        public static string EncryptedUserId()
        {
            return Cryptography.Encrypt(UserId.ToString());
        }

        public static FakeHttpContext AttachSignedInUser(Controller controller, bool isMobileDevice = false)
        {
            var context = ControllerTestContext.Attach(
                controller,
                userAgent: isMobileDevice ? ControllerTestContext.AndroidUserAgent : ControllerTestContext.DesktopUserAgent,
                isMobileDevice: isMobileDevice);
            context.Session["UserId"] = EncryptedUserId();
            context.Session["UserHighestRole"] = UserHighestRole;
            context.Session["UserFirstName"] = "Synthetic";
            context.Session["UserLastName"] = "User";
            return context;
        }

        public static string MetadataXml()
        {
            return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Mock", "MetaDataXML.xml"));
        }

        public static SurveyInfoModel SurveyInfo(bool isDraftMode = false)
        {
            return new SurveyInfoModel
            {
                SurveyId = SurveyId,
                SurveyName = "Synthetic Form",
                ExitText = "Thank you\nfor  taking part",
                XML = "<Template />",
                IsDraftMode = isDraftMode
            };
        }

        public static SurveyInfoDTO SurveyInfoDto(bool isDraftMode = false)
        {
            return new SurveyInfoDTO
            {
                SurveyId = SurveyId,
                SurveyName = "Synthetic Form",
                XML = MetadataXml(),
                ClosingDate = DateTime.Now.AddDays(30),
                IsDraftMode = isDraftMode
            };
        }

        public static string ResponseXml(string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            var xml = new StringBuilder();
            xml.Append("<SurveyResponse SurveyId=\"").Append(SurveyId)
               .Append("\" LastPageVisited=\"1\" HiddenFieldsList=\"\" HighlightedFieldsList=\"\" DisabledFieldsList=\"\" RequiredFieldsList=\"")
               .Append(requiredFieldsList).Append("\" RecordBeforeFlag=\"\">");
            xml.Append("<Page PageNumber=\"1\" PageId=\"1\">");
            foreach (var answer in answers)
            {
                xml.Append("<ResponseDetail QuestionName=\"").Append(answer.Item1).Append("\">")
                   .Append(answer.Item2).Append("</ResponseDetail>");
            }
            xml.Append("</Page></SurveyResponse>");
            return xml.ToString();
        }

        public static SurveyAnswerDTO SurveyAnswer(string responseId = ResponseId, string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            return new SurveyAnswerDTO
            {
                ResponseId = responseId,
                SurveyId = SurveyId,
                Status = 1,
                XML = ResponseXml(requiredFieldsList, answers)
            };
        }
    }
}

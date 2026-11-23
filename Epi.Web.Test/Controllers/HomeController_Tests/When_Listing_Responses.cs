using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.Enter.Common.Message;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.HomeController_Tests
{
    /// <summary>
    /// The Home page response grid (ListResponses), including the session-retained sort and search behavior.
    /// </summary>
    [TestFixture]
    public class When_Listing_Responses
    {
        private const string SqlRecordId = "b0000000-0000-4000-8000-000000000003";

        private FakeSurveyFacade _facade;
        private HomeController _controller;
        private FakeHttpContext _context;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade
            {
                FormSettings = new FormSettingResponse
                {
                    FormSetting = new FormSettingDTO
                    {
                        // Deliberately out of key order: the grid columns are sorted by key.
                        ColumnNameList = new Dictionary<int, string> { { 2, "TextBox2" }, { 1, "TextBox1" } },
                        FormControlNameList = new Dictionary<int, string> { { 0, "TextBox1" }, { 1, "TextBox2" }, { 2, "Multiline" } }
                    },
                    FormInfo = new FormInfoDTO { FormId = EnterFixtures.SurveyId, IsShareable = true, IsShared = false, IsOwner = true, IsSQLProject = false }
                },
                FormResponses = new SurveyAnswerResponse
                {
                    SurveyResponseList = new List<SurveyAnswerDTO>
                    {
                        EnterFixtures.SurveyAnswer(answers: new[] { System.Tuple.Create("TextBox1", "alpha"), System.Tuple.Create("TextBox2", "beta") }),
                        new SurveyAnswerDTO
                        {
                            ResponseId = SqlRecordId,
                            SqlData = new Dictionary<string, string> { { "GlobalRecordId", SqlRecordId }, { "TextBox1", "gamma" }, { "TextBox2", "delta" } }
                        }
                    },
                    FormInfo = new FormInfoDTO { FormId = EnterFixtures.SurveyId, FormName = "Synthetic Form" },
                    NumberOfPages = 3,
                    NumberOfResponses = 41
                },
                UserResponse = new UserResponse { User = new List<UserDTO> { new UserDTO { UserId = EnterFixtures.UserId, Role = 3 } } }
            };
            _controller = new HomeController(_facade);
            _context = EnterFixtures.AttachSignedInUser(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Desktop_Gets_ListResponses_Partial_With_Xml_And_Sql_Rows()
        {
            var result = _controller.ReadResponseInfo(EnterFixtures.SurveyId, 2) as PartialViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("ListResponses"));
            var model = (FormResponseInfoModel)result.Model;
            Assert.That(model.Columns.Select(c => c.Value), Is.EqualTo(new[] { "TextBox1", "TextBox2" }));
            Assert.That(model.ResponsesList, Has.Count.EqualTo(2));
            Assert.That(model.ResponsesList[0].Column0, Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(model.ResponsesList[0].Column1, Is.EqualTo("alpha"));
            Assert.That(model.ResponsesList[0].Column2, Is.EqualTo("beta"));
            Assert.That(model.ResponsesList[1].Column0, Is.EqualTo(SqlRecordId));
            Assert.That(model.ResponsesList[1].Column1, Is.EqualTo("gamma"));
            Assert.That(model.ResponsesList[1].Column2, Is.EqualTo("delta"));
            Assert.That(model.CurrentPage, Is.EqualTo(2));
            Assert.That(model.NumberOfPages, Is.EqualTo(3));
            Assert.That(model.NumberOfResponses, Is.EqualTo(41));
            Assert.That(model.PageSize, Is.EqualTo(20));
            Assert.That(model.FormInfoModel.FormName, Is.EqualTo("Synthetic Form"));
            Assert.That(model.SearchColumns1.Select(i => i.Value), Is.EqualTo(new[] { "TextBox1", "TextBox2", "Multiline" }));
        }

        [Test]
        public void Mobile_Gets_Full_ListResponses_View()
        {
            using (var controller = new HomeController(_facade))
            {
                EnterFixtures.AttachSignedInUser(controller, isMobileDevice: true);

                var result = controller.ReadResponseInfo(EnterFixtures.SurveyId) as ViewResult;

                Assert.That(result, Is.Not.Null);
                Assert.That(result.ViewName, Is.EqualTo("ListResponses"));
            }
        }

        [Test]
        public void Listing_Records_Form_State_In_Session()
        {
            _controller.ReadResponseInfo(EnterFixtures.SurveyId);

            Assert.That(_context.Session["RootFormId"], Is.EqualTo(EnterFixtures.SurveyId));
            Assert.That(_context.Session["IsSqlProject"], Is.EqualTo(false));
            Assert.That(_context.Session["IsOwner"], Is.EqualTo(true));
            Assert.That(_context.Session["UsertRole"], Is.EqualTo(3));
        }

        [Test]
        public void Requests_Carry_User_Page_Org_And_Form_Settings()
        {
            _controller.ReadSortedResponseInfo(EnterFixtures.SurveyId, 2, "ASC", "TextBox1", EnterFixtures.OrganizationId);

            FormSettingRequest settings = _facade.FormSettingRequests.Single();
            Assert.That(settings.FormInfo.FormId, Is.EqualTo(EnterFixtures.SurveyId));
            Assert.That(settings.FormInfo.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(settings.CurrentOrgId, Is.EqualTo(EnterFixtures.OrganizationId));
            Assert.That(settings.FormInfoOnly, Is.True);
            SurveyAnswerRequest responses = _facade.FormResponseRequests.Single();
            Assert.That(responses.Criteria.SurveyId, Is.EqualTo(EnterFixtures.SurveyId));
            Assert.That(responses.Criteria.PageNumber, Is.EqualTo(2));
            Assert.That(responses.Criteria.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(responses.Criteria.UserOrganizationId, Is.EqualTo(EnterFixtures.OrganizationId));
            Assert.That(responses.Criteria.IsShareable, Is.True);
            Assert.That(responses.Criteria.SortOrder, Is.EqualTo("ASC"));
            Assert.That(responses.Criteria.Sortfield, Is.EqualTo("TextBox1"));
            Assert.That(responses.Criteria.SearchCriteria, Is.Empty);
            Assert.That(_facade.UserRequests.Single().Organization.OrganizationId, Is.EqualTo(EnterFixtures.OrganizationId));
            Assert.That(_context.Session["SelectedOrgId"], Is.EqualTo(EnterFixtures.OrganizationId));
            Assert.That(_context.Session["PageNumber"], Is.EqualTo(2));
        }

        [Test]
        public void Saved_Sort_Is_Reused_For_The_Same_Form()
        {
            _context.Session["RootFormId"] = EnterFixtures.SurveyId;
            _context.Session["SortOrder"] = "DESC";
            _context.Session["SortField"] = "TextBox2";

            var model = (FormResponseInfoModel)((PartialViewResult)_controller.ReadSortedResponseInfo(EnterFixtures.SurveyId, 1, "", "", EnterFixtures.OrganizationId)).Model;

            Assert.That(model.sortOrder, Is.EqualTo("DESC"));
            Assert.That(model.sortfield, Is.EqualTo("TextBox2"));
            Assert.That(_facade.FormResponseRequests.Single().Criteria.SortOrder, Is.EqualTo("DESC"));
        }

        [Test]
        public void Saved_Sort_Is_Dropped_When_Switching_Forms()
        {
            _context.Session["RootFormId"] = "another-form";
            _context.Session["SortOrder"] = "DESC";
            _context.Session["SortField"] = "TextBox2";

            _controller.ReadSortedResponseInfo(EnterFixtures.SurveyId, 1, "", "", EnterFixtures.OrganizationId);

            Assert.That(_context.Session["SortOrder"], Is.Null);
            Assert.That(_context.Session["SortField"], Is.Null);
            Assert.That(_facade.FormResponseRequests.Single().Criteria.SortOrder, Is.Null);
        }

        [Test]
        public void Reset_Clears_Saved_Sort_For_The_Same_Form()
        {
            _context.Session["RootFormId"] = EnterFixtures.SurveyId;
            _context.Session["SortOrder"] = "DESC";
            _context.Session["SortField"] = "TextBox2";

            _controller.ReadSortedResponseInfo(EnterFixtures.SurveyId, 1, "", "", EnterFixtures.OrganizationId, reset: true);

            Assert.That(_facade.FormResponseRequests.Single().Criteria.SortOrder, Is.Null);
            Assert.That(_context.Session["SortOrder"], Is.EqualTo(""));
        }

        [Test]
        public void Query_String_Search_Builds_Criteria_And_Is_Saved()
        {
            var query = _context.Request.QueryString;
            query["col1"] = "TextBox1";
            query["val1"] = "alpha";
            query["col2"] = "TextBox2";
            query["val2"] = "beta";
            foreach (var i in new[] { 3, 4, 5 })
            {
                query["col" + i] = "";
                query["val" + i] = "";
            }

            var model = (FormResponseInfoModel)((PartialViewResult)_controller.ReadResponseInfo(EnterFixtures.SurveyId)).Model;

            const string expected = "TextBox1='alpha' AND TextBox2='beta'";
            Assert.That(_facade.FormResponseRequests.Single().Criteria.SearchCriteria, Is.EqualTo(expected));
            Assert.That(_context.Session["SearchCriteria"], Is.EqualTo(expected));
            Assert.That(model.SearchModel.SearchCol1, Is.EqualTo("TextBox1"));
            Assert.That(model.SearchModel.Value1, Is.EqualTo("alpha"));
            Assert.That(model.SearchColumns1.Single(i => i.Selected).Value, Is.EqualTo("TextBox1"));
        }

        [Test]
        public void Saved_Search_Is_Reused_When_No_Search_Is_Posted()
        {
            var savedModel = new SearchBoxModel { SearchCol1 = "TextBox2", Value1 = "beta" };
            _context.Session["SearchCriteria"] = "TextBox2='beta'";
            _context.Session["SearchModel"] = savedModel;

            var model = (FormResponseInfoModel)((PartialViewResult)_controller.ReadResponseInfo(EnterFixtures.SurveyId)).Model;

            Assert.That(_facade.FormResponseRequests.Single().Criteria.SearchCriteria, Is.EqualTo("TextBox2='beta'"));
            Assert.That(model.SearchModel, Is.SameAs(savedModel));
        }

        [Test]
        public void Empty_Form_Id_Returns_An_Empty_Model_Without_Service_Calls()
        {
            FormResponseInfoModel model = _controller.GetFormResponseInfoModel("", 1);

            Assert.That(model.ResponsesList, Is.Null.Or.Empty);
            Assert.That(_facade.FormSettingRequests, Is.Empty);
            Assert.That(_facade.FormResponseRequests, Is.Empty);
        }
    }
}

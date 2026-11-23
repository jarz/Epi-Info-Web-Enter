using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.HomeController_Tests
{
    [TestFixture]
    public class When_Arrive_At_Application
    {
        private FakeSurveyFacade _facade;
        private HomeController _controller;
        private FakeHttpContext _context;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade
            {
                Organizations = new List<OrganizationDTO>
                {
                    new OrganizationDTO { OrganizationId = EnterFixtures.OrganizationId, Organization = "Synthetic Org A", IsEnabled = true },
                    new OrganizationDTO { OrganizationId = 99, Organization = "Synthetic Org B", IsEnabled = true }
                },
                Forms = new List<FormInfoModel>
                {
                    new FormInfoModel { FormId = EnterFixtures.SurveyId, FormName = "Synthetic Form", OrganizationId = EnterFixtures.OrganizationId }
                }
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
        public void Then_Show_Index_Page_With_Forms_And_Organizations()
        {
            var result = _controller.Index(EnterFixtures.SurveyId) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Index"));
            var model = result.Model as FormModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.SelectedForm, Is.EqualTo(EnterFixtures.SurveyId));
            Assert.That(model.UserHighestRole, Is.EqualTo(EnterFixtures.UserHighestRole));
            Assert.That(model.UserFirstName, Is.EqualTo("Synthetic"));
            Assert.That(model.UserLastName, Is.EqualTo("User"));
            Assert.That(model.OrganizationList.Select(o => o.OrganizationId), Is.EqualTo(new[] { EnterFixtures.OrganizationId, 99 }));
            Assert.That(model.FormList.Single().FormId, Is.EqualTo(EnterFixtures.SurveyId));
        }

        [Test]
        public void Then_Requests_Use_The_Decrypted_Session_User_And_First_Organization()
        {
            _controller.Index(EnterFixtures.SurveyId);

            var orgRequest = _facade.OrganizationRequests.Single();
            Assert.That(orgRequest.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(orgRequest.UserRole, Is.EqualTo(EnterFixtures.UserHighestRole));
            var formsRequest = _facade.FormsInfoRequests.Single();
            Assert.That(formsRequest.Criteria.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(formsRequest.Criteria.CurrentOrgId, Is.EqualTo(EnterFixtures.OrganizationId));
        }

        [Test]
        public void Then_Selected_Organization_Defaults_To_The_First_Organization()
        {
            _controller.Index(EnterFixtures.SurveyId);

            Assert.That(_context.Session["SelectedOrgId"], Is.EqualTo(EnterFixtures.OrganizationId));
        }

        [Test]
        public void Then_Explicit_Organization_Is_Selected_But_Forms_Still_Come_From_The_First()
        {
            _controller.Index(EnterFixtures.SurveyId, 99);

            Assert.That(_context.Session["SelectedOrgId"], Is.EqualTo(99));
            Assert.That(_facade.FormsInfoRequests.Single().Criteria.CurrentOrgId, Is.EqualTo(EnterFixtures.OrganizationId));
        }

        [Test]
        public void Then_Mobile_Arrival_Clears_Response_List_State()
        {
            using (var controller = new HomeController(_facade))
            {
                var context = EnterFixtures.AttachSignedInUser(controller, isMobileDevice: true);
                foreach (var key in new[] { "RootFormId", "PageNumber", "SortOrder", "SortField", "SearchCriteria", "SearchModel" })
                {
                    context.Session[key] = "stale";
                }

                var result = controller.Index(EnterFixtures.SurveyId) as ViewResult;

                Assert.That(result.ViewName, Is.EqualTo("Index"));
                foreach (var key in new[] { "RootFormId", "PageNumber", "SortOrder", "SortField", "SearchCriteria", "SearchModel" })
                {
                    Assert.That(context.Session[key], Is.Null, key);
                }
            }
        }

        [Test]
        public void Then_Desktop_Arrival_Keeps_Response_List_State()
        {
            _context.Session["SortField"] = "_DateUpdated";

            _controller.Index(EnterFixtures.SurveyId);

            Assert.That(_context.Session["SortField"], Is.EqualTo("_DateUpdated"));
        }

        [Test]
        public void Then_User_Without_Organizations_Sees_Exception_Page()
        {
            _facade.Organizations = new List<OrganizationDTO>();

            var result = _controller.Index(EnterFixtures.SurveyId) as ViewResult;

            Assert.That(result.ViewName, Is.EqualTo("Exception"));
            Assert.That(result.Model, Is.TypeOf<ExceptionModel>());
            Assert.That(((ExceptionModel)result.Model).Message, Is.Not.Empty);
            Assert.That(_facade.FormsInfoRequests, Is.Empty);
        }

        [Test]
        public void Then_Default_Page_Is_Rendered()
        {
            var result = _controller.Default() as ViewResult;

            Assert.That(result.ViewName, Is.EqualTo("Default"));
        }
    }
}

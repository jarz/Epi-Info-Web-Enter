using System.Web.Mvc;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.FinalController_Tests
{
    /// <summary>
    /// GET Final/Index. The POST action creates a new response through the service and is not covered.
    /// </summary>
    [TestFixture]
    public class When_Arrive_At_Final_Page
    {
        private FakeSurveyFacade _facade;
        private FinalController _controller;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade { SurveyInfo = EnterFixtures.SurveyInfo() };
            _controller = new FinalController(_facade);
            EnterFixtures.AttachSignedInUser(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Then_Show_Final_Index_Page()
        {
            var result = _controller.Index(EnterFixtures.SurveyId, "final") as ViewResult;

            // The stale suite expected a "PostSubmit" view; the current action renders Final/Index.
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Index"));
            Assert.That(result.Model, Is.TypeOf<SurveyInfoModel>());
            Assert.That(_facade.RequestedSurveyIds, Is.EqualTo(new[] { EnterFixtures.SurveyId }));
        }

        [Test]
        public void Then_Exit_Text_Is_Prepared_For_Html()
        {
            var model = (SurveyInfoModel)((ViewResult)_controller.Index(EnterFixtures.SurveyId, "final")).Model;

            Assert.That(model.ExitText, Is.EqualTo("Thank you<br />for &nbsp;taking part"));
        }

        [TestCase(false, "final")]
        [TestCase(true, "draft")]
        public void Then_Watermark_Class_Follows_Draft_Mode(bool isDraftMode, string expectedClass)
        {
            _facade.SurveyInfo = EnterFixtures.SurveyInfo(isDraftMode);

            var model = (SurveyInfoModel)((ViewResult)_controller.Index(EnterFixtures.SurveyId, "final")).Model;

            Assert.That(model.IsDraftModeStyleClass, Is.EqualTo(expectedClass));
        }

        [Test]
        public void Then_Service_Failure_Shows_Exception_View()
        {
            _facade.SurveyInfo = null;

            var result = _controller.Index(EnterFixtures.SurveyId, "final") as ViewResult;

            Assert.That(result.ViewName, Is.EqualTo("Exception"));
            Assert.That(result.Model, Is.Null);
        }
    }
}

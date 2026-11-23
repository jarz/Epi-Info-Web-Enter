using System.Web.Mvc;
using Epi.Web.Controllers;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.ResponseController_Tests
{
    [TestFixture]
    public class When_Opening_Response
    {
        [Test]
        public void Then_Response_Id_Is_Handed_To_The_Survey_Page()
        {
            using (var controller = new ResponseController())
            {
                EnterFixtures.AttachSignedInUser(controller);

                var result = controller.Index(EnterFixtures.SurveyId, EnterFixtures.ResponseId, 3) as RedirectToRouteResult;

                Assert.That(result, Is.Not.Null);
                Assert.That(result.RouteValues["Controller"], Is.EqualTo("Survey"));
                Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
                Assert.That(result.RouteValues["surveyId"], Is.EqualTo(EnterFixtures.SurveyId));
                Assert.That(result.RouteValues["PageNumber"], Is.EqualTo(3));
                Assert.That(controller.TempData["ResponseId"], Is.EqualTo(EnterFixtures.ResponseId));
            }
        }

        [Test]
        public void Then_Missing_Response_Id_Redirects_To_Exception()
        {
            using (var controller = new ResponseController())
            {
                EnterFixtures.AttachSignedInUser(controller);

                var result = controller.Index(EnterFixtures.SurveyId, null) as RedirectToRouteResult;

                Assert.That(result, Is.Not.Null);
                Assert.That(result.RouteValues["action"], Is.EqualTo("Exception"));
                Assert.That(controller.TempData.ContainsKey("ResponseId"), Is.False);
            }
        }
    }
}

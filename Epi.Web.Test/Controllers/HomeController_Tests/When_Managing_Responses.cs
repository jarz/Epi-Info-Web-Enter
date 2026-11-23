using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.Enter.Common.Message;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.HomeController_Tests
{
    /// <summary>
    /// Response-list actions posted by the Home page scripts (delete, unlock, edit concurrency check, sort reset).
    /// HomeController.Notify is not covered because it sends SMTP mail.
    /// </summary>
    [TestFixture]
    public class When_Managing_Responses
    {
        private FakeSurveyFacade _facade;
        private HomeController _controller;
        private FakeHttpContext _context;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade();
            _controller = new HomeController(_facade);
            _context = EnterFixtures.AttachSignedInUser(_controller);
            _context.Session["RootFormId"] = EnterFixtures.SurveyId;
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Delete_Sends_Response_User_And_Root_Form()
        {
            _context.Session["IsSqlProject"] = true;

            var result = _controller.Delete(EnterFixtures.ResponseId) as JsonResult;

            Assert.That(result.Data, Is.EqualTo(string.Empty));
            SurveyAnswerRequest request = _facade.DeleteRequests.Single();
            Assert.That(request.SurveyAnswerList.Single().ResponseId, Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(request.Criteria.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(request.Criteria.IsSqlProject, Is.True);
            Assert.That(request.Criteria.SurveyId, Is.EqualTo(EnterFixtures.SurveyId));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Unlock_Requests_Status_2_And_Remembers_Recovery_Choice(bool recoverLastRecordVersion)
        {
            var result = _controller.Unlock(EnterFixtures.ResponseId, recoverLastRecordVersion) as JsonResult;

            Assert.That(result.Data, Is.EqualTo("Success"));
            SurveyAnswerRequest request = _facade.StatusRequests.Single();
            Assert.That(request.Criteria.StatusId, Is.EqualTo(2));
            Assert.That(request.Criteria.UserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(request.Criteria.SurveyAnswerIdList, Is.EqualTo(new[] { EnterFixtures.ResponseId }));
            Assert.That(request.SurveyAnswerList.Single().ResponseId, Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(_context.Session["RecoverLastRecordVersion"], Is.EqualTo(recoverLastRecordVersion));
        }

        [Test]
        public void Unlock_Failure_Returns_The_Existing_Error_Token()
        {
            _facade.UpdateResponseStatusFailure = new InvalidOperationException("synthetic failure");

            var result = _controller.Unlock(EnterFixtures.ResponseId, false) as JsonResult;

            // The misspelling is the current client contract.
            Assert.That(result.Data, Is.EqualTo("Erorr"));
        }

        [Test]
        public void CheckForConcurrency_Returns_Response_With_Owner_And_Logged_In_User()
        {
            _facade.SurveyAnswerResponse = new SurveyAnswerResponse
            {
                SurveyResponseList = new List<SurveyAnswerDTO> { EnterFixtures.SurveyAnswer() },
                FormInfo = new FormInfoDTO { OwnerId = 7 }
            };

            var result = _controller.CheckForConcurrency(EnterFixtures.ResponseId) as JsonResult;

            var dto = (SurveyAnswerDTO)result.Data;
            Assert.That(dto.ResponseId, Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(dto.FormOwnerId, Is.EqualTo(7));
            Assert.That(dto.LoggedInUserId, Is.EqualTo(EnterFixtures.UserId));
            Assert.That(_context.Session["EditForm"], Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(_facade.SurveyAnswerRequests.Single(), Is.EqualTo(Tuple.Create(EnterFixtures.ResponseId, EnterFixtures.SurveyId, EnterFixtures.UserId)));
        }

        [Test]
        public void ResetSort_Clears_Saved_Sort()
        {
            _context.Session["SortOrder"] = "ASC";
            _context.Session["SortField"] = "_DateUpdated";

            var result = _controller.ResetSort(EnterFixtures.SurveyId) as JsonResult;

            Assert.That(result.Data, Is.EqualTo(true));
            Assert.That(_context.Session["SortOrder"], Is.Null);
            Assert.That(_context.Session["SortField"], Is.Null);
        }

        [Test]
        public void GetDictionary_Indexes_Comma_Separated_Values()
        {
            Dictionary<int, string> values = _controller.GetDictionary("Name,_DateUpdated,Age");

            Assert.That(values, Is.EqualTo(new Dictionary<int, string> { { 0, "Name" }, { 1, "_DateUpdated" }, { 2, "Age" } }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetDictionary_Of_Nothing_Is_Empty(string list)
        {
            Assert.That(_controller.GetDictionary(list), Is.Empty);
        }

        [TestCase("1", true)]
        [TestCase("0", false)]
        [TestCase("2", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void GetBoolValue_Treats_Only_1_As_True(string value, bool expected)
        {
            Assert.That(_controller.GetBoolValue(value), Is.EqualTo(expected));
        }

        [Test]
        public void GetBoolValue_Rejects_Non_Numeric_Values()
        {
            Assert.That(() => _controller.GetBoolValue("true"), Throws.TypeOf<FormatException>());
        }
    }
}

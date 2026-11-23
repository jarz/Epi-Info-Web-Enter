using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epi.Web.Enter.Common.DTO;
using Epi.Web.Enter.Common.Message;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.ReportController_Tests
{
    [TestFixture]
    public class When_Viewing_Report
    {
        private FakeSurveyFacade _facade;
        private ReportController _controller;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade();
            _controller = new ReportController(_facade);
            EnterFixtures.AttachSignedInUser(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Then_Gadget_Html_Is_Concatenated_Into_The_Report()
        {
            var created = new DateTime(2024, 1, 2, 3, 4, 5);
            _facade.Report = new PublishReportResponse
            {
                Reports = new List<ReportInfoDTO>
                {
                    new ReportInfoDTO
                    {
                        ReportId = EnterFixtures.ReportId,
                        CreatedDate = created,
                        Gadgets = new List<GadgetDTO>
                        {
                            new GadgetDTO { GadgetHtml = "<div id=\"g1\"></div>" },
                            new GadgetDTO { GadgetHtml = "<div id=\"g2\"></div>" }
                        }
                    }
                }
            };

            var result = _controller.Index(EnterFixtures.ReportId) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Empty);
            var model = (ReportModel)result.Model;
            Assert.That(model.Reportid, Is.EqualTo(EnterFixtures.ReportId));
            Assert.That(model.DateCreated, Is.EqualTo(created.ToString()));
            Assert.That(model.ReportHtml, Is.EqualTo("<div id=\"g1\"></div><div id=\"g2\"></div>"));
            PublishReportRequest request = _facade.ReportRequests.Single();
            Assert.That(request.ReportInfo.ReportId, Is.EqualTo(EnterFixtures.ReportId));
            Assert.That(request.IncludHTML, Is.False);
        }

        [Test]
        public void Then_Missing_Report_Returns_Json_False()
        {
            _facade.Report = new PublishReportResponse { Reports = new List<ReportInfoDTO>() };

            var result = _controller.Index(EnterFixtures.ReportId) as JsonResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.EqualTo(false));
        }
    }
}

using System;
using System.Linq;
using Epi.Web.MVC.Test.Fakes;
using Epi.Web.MVC.Utility;
using MvcDynamicForms;
using MvcDynamicForms.Fields;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Utility
{
    /// <summary>
    /// Server-side form construction from synthetic Epi Info metadata (replaces the stale
    /// "When_Click_Begin_Survey_Button" test, whose SurveyController.Notify action no longer exists).
    /// </summary>
    [TestFixture]
    public class FormProviderTests
    {
        private static Form DesktopForm(string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            return FormProvider.GetForm(EnterFixtures.SurveyInfoDto(), 1, EnterFixtures.SurveyAnswer(EnterFixtures.ResponseId, requiredFieldsList, answers));
        }

        private static Form MobileForm()
        {
            return MobileFormProvider.GetForm(EnterFixtures.SurveyInfoDto(), 1, EnterFixtures.SurveyAnswer());
        }

        private static InputField Input(Form form, string key)
        {
            return form.InputFields.Single(f => f.Key == key);
        }

        [Test]
        public void Desktop_Form_Builds_The_Seven_Page_One_Fields()
        {
            Form form = DesktopForm();

            Assert.That(form.IsMobile, Is.False);
            Assert.That(form.Fields, Has.Count.EqualTo(7));
            Assert.That(form.Fields.Count(f => f.GetType() == typeof(Literal)), Is.EqualTo(3));
            Assert.That(Input(form, "textbox1").GetType(), Is.EqualTo(typeof(TextBox)));
            Assert.That(Input(form, "textbox2").GetType(), Is.EqualTo(typeof(TextBox)));
            Assert.That(Input(form, "multiline").GetType(), Is.EqualTo(typeof(TextArea)));
            Assert.That(Input(form, "numerictextbox").GetType(), Is.EqualTo(typeof(NumericTextBox)));
        }

        [Test]
        public void Form_Carries_Survey_Response_And_Page_Information()
        {
            Form form = FormProvider.GetForm(EnterFixtures.SurveyInfoDto(isDraftMode: true), 1, EnterFixtures.SurveyAnswer());

            Assert.That(form.ResponseId, Is.EqualTo(EnterFixtures.ResponseId));
            Assert.That(form.SurveyInfo.SurveyId, Is.EqualTo(EnterFixtures.SurveyId));
            Assert.That(form.CurrentPage, Is.EqualTo(1));
            Assert.That(form.NumberOfPages, Is.EqualTo(1));
            Assert.That(form.Width, Is.EqualTo(768));
            Assert.That(form.Height, Is.EqualTo(1028));
            Assert.That(form.IsDraftModeStyleClass, Is.EqualTo("draft"));
        }

        [Test]
        public void Mobile_Form_Uses_Mobile_Field_Types_And_Wrapper()
        {
            Form form = MobileForm();

            Assert.That(form.IsMobile, Is.True);
            Assert.That(form.FormWrapperClass, Is.EqualTo("MvcDynamicMobileForm"));
            Assert.That(form.Fields, Has.Count.EqualTo(7));
            Assert.That(form.Fields.Count(f => f.GetType() == typeof(MobileLiteral)), Is.EqualTo(3));
            Assert.That(Input(form, "textbox1").GetType(), Is.EqualTo(typeof(MobileTextBox)));
            Assert.That(Input(form, "multiline").GetType(), Is.EqualTo(typeof(MobileTextArea)));
            Assert.That(Input(form, "numerictextbox").GetType(), Is.EqualTo(typeof(MobileNumericTextBox)));
        }

        [Test]
        public void Saved_Responses_Populate_Field_Values()
        {
            Form form = DesktopForm("", Tuple.Create("TextBox2", "synthetic answer"));

            Assert.That(Input(form, "textbox2").Response, Is.EqualTo("synthetic answer"));
            Assert.That(Input(form, "textbox1").Response, Is.Null.Or.Empty);
        }

        [Test]
        public void Response_Form_State_Is_Copied_To_The_Form()
        {
            Form form = DesktopForm("textbox1");

            Assert.That(form.RequiredFieldsList, Is.EqualTo("textbox1"));
            Assert.That(form.HiddenFieldsList, Is.Empty);
            Assert.That(form.DisabledFieldsList, Is.Empty);
            Assert.That(form.HighlightedFieldsList, Is.Empty);
        }

        [Test]
        public void Validate_Flags_Empty_Required_Fields_Only()
        {
            Form form = DesktopForm("", Tuple.Create("TextBox2", "synthetic answer"));

            bool isValid = form.Validate("textbox1,textbox2");

            Assert.That(isValid, Is.False);
            Assert.That(Input(form, "textbox1").Required, Is.True);
            Assert.That(Input(form, "textbox1").Error, Is.Not.Null.And.Not.Empty);
            Assert.That(Input(form, "textbox2").Required, Is.True);
            Assert.That(Input(form, "textbox2").Error, Is.Null.Or.Empty);
            Assert.That(Input(form, "multiline").Required, Is.False);
        }

        [Test]
        public void Validate_Passes_When_Nothing_Is_Required()
        {
            Form form = DesktopForm();

            Assert.That(form.Validate(""), Is.True);
            Assert.That(form.InputFields.All(f => !f.Required), Is.True);
        }

        [Test]
        public void Desktop_Html_Uses_Fixed_Size_Wrapper()
        {
            Form form = DesktopForm();

            string html = form.RenderHtml();

            Assert.That(html, Does.Contain("class=\"" + form.FormWrapperClass + "\""));
            Assert.That(html, Does.Contain("width:" + form.Width + "px;height:" + (form.Height + 100) + "px;"));
            Assert.That(html, Does.Not.Contain("data-role=\"fieldcontain\""));
        }

        [Test]
        public void Mobile_Html_Uses_jQuery_Mobile_Attributes()
        {
            string html = MobileForm().RenderHtml();

            Assert.That(html, Does.Contain("class=\"MvcDynamicMobileForm\""));
            Assert.That(html, Does.Contain("data-role=\"fieldcontain\""));
            Assert.That(html, Does.Contain("data-ajax=\"false\""));
            Assert.That(html, Does.Contain("width:auto;height:auto;"));
        }
    }
}

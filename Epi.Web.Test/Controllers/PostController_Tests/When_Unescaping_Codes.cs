using Epi.Web.Controllers;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.PostController_Tests
{
    /// <summary>
    /// PostController.Notify (SMTP) and SignOut (FormsAuthentication) are not covered.
    /// </summary>
    [TestFixture]
    public class When_Unescaping_Codes
    {
        // Only single-escape inputs are characterized: with two or more "\hex" escapes the current
        // implementation passes the match index as a Substring length and duplicates text.
        [TestCase("Line%201%5C0aLine%202", "Line 1\nLine 2")]
        [TestCase("caf%5Ce9", "café")]
        [TestCase("plain%20text", "plain text")]
        public void Url_And_Hex_Escapes_Are_Decoded(string source, string expected)
        {
            Assert.That(PostController.UnescapeCodes(source), Is.EqualTo(expected));
        }
    }
}

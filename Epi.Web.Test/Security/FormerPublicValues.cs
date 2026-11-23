using Epi.Web.Enter.Common.Security;

namespace Epi.Web.MVC.Test.Security
{
    /// <summary>
    /// Key material and password salt from earlier Epi Info Web Enter source. They are public in the source history,
    /// so the applications now reject them; the tests use them to check that and the migration away from them.
    /// </summary>
    internal static class FormerPublicValues
    {
        internal const string PasswordSalt = "1PIJhCF67TY/ciQni/FHYOLH7q+bNajOm3gaHMl34dlYTauD9vBQpfI4uLM+4sz1t8o1LVVO0y6e9PaGPRkLZpr3d+2ubEmrMuNQihMcxXdOATDkulmiYhjH8l55mEQEMozPjw==";

        internal static KeyMaterial ConnectionStringKeys
        {
            get { return new KeyMaterial("80787d6053694493be171dd712e51c61", "476ba16073764022bc7f262c6d67ebef", "0f8f*d5bd&cb4~9f"); }
        }
    }
}
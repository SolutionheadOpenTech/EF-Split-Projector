using System.Configuration;
using System.Reflection;

namespace EF_Split_Projector.Helpers
{
    public class EFSplitProjectorSection : ConfigurationSection
    {
        public static DiagnosticType Diagnostics
        {
            get { return Section.diagnostics; }
            set { Section.diagnostics = value; }
        }

        private static EFSplitProjectorSection Section
        {
            get { return _section ?? (_section = ConfigurationManager.GetSection(MethodBase.GetCurrentMethod().DeclaringType.Name) as EFSplitProjectorSection ?? new EFSplitProjectorSection()); }
        }
        private static EFSplitProjectorSection _section;

        private const string DiagnosticsName = "diagnostics";
        [ConfigurationProperty(DiagnosticsName, IsRequired = false, DefaultValue = DiagnosticType.Disabled)]
        public DiagnosticType diagnostics
        {
            get { return (DiagnosticType)this[DiagnosticsName]; }
            set { this[DiagnosticsName] = value; }
        }

        public enum DiagnosticType
        {
            Disabled,
            Logging,
            LoggingUnbatchedQueries
        }
    }
}
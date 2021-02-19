using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("AttitudeCorrectionResult")]
    class AttitudeCorrectionResult : Structure
    {
        public AttitudeCorrectionResult(Vector torque, Vector translation)
        {
            AddSuffix("TORQUE", new Suffix<Vector>(() => torque, "The torque for this correction."));
            AddSuffix("TRANSLATION", new Suffix<Vector>(() => translation, "The translation force for this correction."));
        }
    }
}

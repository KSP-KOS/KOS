using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("AttitudeCorrectionResult")]
    public class AttitudeCorrectionResult : Structure
    {
        public AttitudeCorrectionResult(Vector torque, Vector translation)
        {
            this.torque = torque;
            this.translation = translation;

            AddSuffix("TORQUE", new Suffix<Vector>(() => torque, "The torque for this correction."));
            AddSuffix("TRANSLATION", new Suffix<Vector>(() => translation, "The translation force for this correction."));
        }

        public Vector torque { get; private set; }
        public Vector translation { get; private set; }

        public override string ToString()
        {
            return String.Format("AttitudeCorrectionResult(torque: {0}, translation: {1})", torque.ToString(), translation.ToString());
        }

        public static AttitudeCorrectionResult operator +(AttitudeCorrectionResult a, AttitudeCorrectionResult b)
        {
            return new AttitudeCorrectionResult(a.torque + b.torque, a.translation + b.translation);
        }
    }
}

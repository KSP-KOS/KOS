using System;
using kOS.Suffixed;

namespace kOS.Communication
{
    [Safe.Utilities.KOSNomenclature("KscTarget", KOSToCSharp = false)]
    public class KscTarget : VesselTarget {
        private static readonly KscTarget kscTarget = new KscTarget();

        public static KscTarget Instance
        {
            get {
                return kscTarget;
            }
        }
        
        private KscTarget()
        {}

        public override Guid GetGuid()
        {
            return Guid.Empty;
        }
    }
}
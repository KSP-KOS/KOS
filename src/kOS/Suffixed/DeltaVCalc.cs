using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS;
using kOS.Suffixed;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("DeltaV")]
    /// <summary>
    /// Wrapper around KSP's stock delta-V calculator's differences between
    /// a stage delta-V and a vessel delta-V.  KSP models them as two separate
    /// classes, but kOS will "smash" the difference between them and call them
    /// one class.
    /// </summary>
    public class DeltaVCalc : Structure
    {
        private readonly SharedObjects shared;

        // This may be a deltaV calculator for the vessel total,
        // or for just one class.  These shouldn't both be populated:
        //
        // It *is*, however, possible for both to be null, because the
        // stock game will often give you a null DeltaVStageInfo for
        // stages which have no chance of dV in them (like a stage that
        // does nothing more than decouple a decoupler, with no engines).
        // If BOTH are null, then assume it was *meant* to be a stage deltaV,
        // but the Stage DeltaV passed in was null so all deltaV values
        // should be zero.
        private VesselDeltaV shipDV;
        private DeltaVStageInfo stageDV;

        /// <summary>
        /// Construct a whole-vessel deltaV calculator
        /// </summary>
        /// <param name="shared"></param>
        /// <param name="dv"></param>
        public DeltaVCalc(SharedObjects shared, VesselDeltaV dv)
        {
            this.shared = shared;
            shipDV = dv;
            RegisterInitializer(InitializeSuffixes);
        }

        /// <summary>
        /// Construct a one-stage deltaV calculator
        /// </summary>
        /// <param name="shared"></param>
        /// <param name="dv"></param>
        public DeltaVCalc(SharedObjects shared, DeltaVStageInfo dv)
        {
            this.shared = shared;
            // It is possible for the KSP API to give us a DeltaVStageInfo of null for
            // stages that currently don't have any dV (if they are just a stage with
            // a decoupler alone, for example).  If this value is null, then we should
            // behave as if the dV values are all zero:
            stageDV = dv;
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CURRENT", new Suffix<ScalarDoubleValue>(GetDVActual));
            AddSuffix("ASL", new Suffix<ScalarDoubleValue>(GetDVatASL));
            AddSuffix("VACUUM", new Suffix<ScalarDoubleValue>(GetDVinVac));
            AddSuffix("DURATION", new Suffix<ScalarDoubleValue>(GetBurnTime));
            AddSuffix("FORCECALC", new NoArgsVoidSuffix(ForceCalc));
        }

        ScalarDoubleValue GetDVActual()
        {
            if (shipDV != null)
                return shipDV.TotalDeltaVActual;
            else if (stageDV != null)
                return stageDV.deltaVActual;
            else
                return 0d;
        }

        ScalarDoubleValue GetDVatASL()
        {
            if (shipDV != null)
                return shipDV.TotalDeltaVASL;
            else if (stageDV != null)
                return stageDV.deltaVatASL;
            else
                return 0d;
        }

        private ScalarDoubleValue GetDVinVac()
        {
            if (shipDV != null)
                return shipDV.TotalDeltaVVac;
            else if (stageDV != null)
                return stageDV.deltaVinVac;
            else
                return 0d;
        }

        private ScalarDoubleValue GetBurnTime()
        {
            if (shipDV != null)
                return shipDV.TotalBurnTime;
            if (stageDV != null)
                return stageDV.stageBurnTime;
            else
                return 0d;
        }

        private void ForceCalc()
        {
            if (shipDV != null)
            {
                shipDV.SetCalcsDirty(true, true);
                return;
            }
            else if (stageDV != null)
            {
                stageDV.vesselDeltaV.SetCalcsDirty(true, true);
                return;
            }
            else
            {
                // do nothing.
            }
        }

        public override string ToString()
        {
            return string.Format("{0} DeltaV {1}m/s for {2}", base.ToString(), GetDVActual(), (shipDV != null ? "a vessel" : "a stage"));
        }
    }
}
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace kOS.AddOns.Principia
{
    [kOS.Safe.Utilities.KOSNomenclature("PRManoeuvre")]
    public class PRManoeuvre : Structure
    {
        private Vessel vesselRef;
        private int index;
        private readonly SharedObjects shared;
        private double time;
        private Vector3d deltaV;
        private double duration;

        public PRManoeuvre(Vessel v, int index, SharedObjects shared) : this(shared)
        {
            vesselRef = v;
            this.index = index;
            Update();
        }

        private PRManoeuvre(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix(new[] {"DELTAV", "BURNVECTOR"}, new Suffix<Vector>(
                () =>
                {
                    Update();
                    return new Vector(deltaV);
                }
                ));

            AddSuffix("ETA", new Suffix<ScalarValue>(
                () =>
                {
                    Update();
                    return time - Planetarium.GetUniversalTime();
                }
            ));

            AddSuffix("DURATION", new Suffix<ScalarValue>(
                () =>
                {
                    Update();
                    return duration;
                }
            ));
        }

        private void Update()
        {
            // If this node is attached, and the values on the attached node have changed, I need to reflect that.
            // Because this is just integer indexed, it's a bit prone to breakage if the flight plan changes.
            // Not really a good way around this with the current Principia flight planning.
            if (index < 0)
                return;

            time = PrincipiaWrapper.FlightPlanGetManoeuvreInitialTime(vesselRef, index) ?? time;
            duration = PrincipiaWrapper.FlightPlanGetManoeuvreDuration(vesselRef, index) ?? duration;
            deltaV = PrincipiaWrapper.FlightPlanGetManoeuvreGuidance(vesselRef, index) ?? deltaV;
        }
    }
}

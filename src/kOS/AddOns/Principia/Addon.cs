using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.AddOns.Principia
{
    [kOSAddon("Principia")]
    [kOS.Safe.Utilities.KOSNomenclature("PrincipiaAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base (shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("GEOPOTREFRADIUS", new Suffix<ScalarValue>(GeopotentialGetReferenceRadius, "Get Geopotential reference radius."));
            AddSuffix("HASMANOEUVRE", new Suffix<BooleanValue>(HasManoeuvre, "Check whether the vessel has any manoeuvres."));
            AddSuffix("NEXTMANOEUVRE", new Suffix<PRManoeuvre>(NextManoeuvre, "Get the next manoeuvre."));
            AddSuffix("ALLMANOEUVRES", new Suffix<ListValue>(AllManoeuvres, "Get a list of all manoeuvres."));
        }

        private ScalarValue GeopotentialGetReferenceRadius()
        {
            if (Available())
            {
                double? result = PrincipiaWrapper.GeopotentialGetReferenceRadius();
                if (result != null)
                    return result;
                throw new KOSException("GeopotRefRadius is not available.");
            }
            throw new KOSUnavailableAddonException("GEOPOTREFRADIUS", "Principia");
        }

        private BooleanValue HasManoeuvre()
        {
            if (Available())
            {
                if ( PrincipiaWrapper.FlightPlanExists(shared.Vessel) )
                {
                    int nodeCount = PrincipiaWrapper.FlightPlanNumberOfManoeuvres(shared.Vessel) ?? 0;
                    return (nodeCount > 0);
                }

                return false;
            }
            throw new KOSUnavailableAddonException("HASMANOEUVRE", "Principia");
        }

        private PRManoeuvre NextManoeuvre()
        {
            if (Available())
            {
                if (PrincipiaWrapper.FlightPlanExists(shared.Vessel))
                {
                    int nodeCount = PrincipiaWrapper.FlightPlanNumberOfManoeuvres(shared.Vessel) ?? 0;
                    if (nodeCount > 0)
                    {
                        return new PRManoeuvre(shared.Vessel, 0, shared);
                    }
                }

                throw new KOSSituationallyInvalidException("No manoeuvres present!");
            }
            throw new KOSUnavailableAddonException("NEXTMANOEUVRE", "Principia");
        }

        private ListValue AllManoeuvres()
        {
            if (Available())
            {
                var list = new ListValue();

                if (PrincipiaWrapper.FlightPlanExists(shared.Vessel))
                {
                    int nodeCount = PrincipiaWrapper.FlightPlanNumberOfManoeuvres(shared.Vessel) ?? 0;
                    for (int i = 0; i < nodeCount; ++i)
                    {
                        list.Add(new PRManoeuvre(shared.Vessel, i, shared));
                    }
                }

                return list;
            }
            throw new KOSUnavailableAddonException("ALLMANOEUVRES", "Principia");
        }

        public override BooleanValue Available()
        {
            return PrincipiaWrapper.Wrapped();
        }

    }
}
using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using PreFlightTests;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Kuniverse")]
    public class KUniverseValue : Structure
    {
        private readonly SharedObjects shared;

        public KUniverseValue(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("CANREVERT", new Suffix<BooleanValue>(CanRevert));
            AddSuffix("CANREVERTTOLAUNCH", new Suffix<BooleanValue>(CanRevertToLaunch));
            AddSuffix("CANREVERTTOEDITOR", new Suffix<BooleanValue>(CanRevvertToEditor));
            AddSuffix("REVERTTOLAUNCH", new NoArgsVoidSuffix(RevertToLaunch));
            AddSuffix("REVERTTOEDITOR", new NoArgsVoidSuffix(RevertToEditor));
            AddSuffix("REVERTTO", new OneArgsSuffix<StringValue>(RevertTo));
            AddSuffix("ORIGINEDITOR", new Suffix<StringValue>(OriginatingEditor));
            AddSuffix("DEFAULTLOADDISTANCE", new Suffix<LoadDistanceValue>(() => new LoadDistanceValue(PhysicsGlobals.Instance.VesselRangesDefault)));
            AddSuffix("ACTIVEVESSEL", new SetSuffix<VesselTarget>(() => new VesselTarget(FlightGlobals.ActiveVessel, shared), SetActiveVessel));
            AddSuffix(new string[] { "FORCESETACTIVEVESSEL", "FORCEACTIVE" }, new OneArgsSuffix<VesselTarget>(ForceSetActiveVessel));
            AddSuffix("HOURSPERDAY", new Suffix<ScalarValue>(GetHoursPerDay));
            AddSuffix("DEBUGLOG", new OneArgsSuffix<StringValue>(DebugLog));
            AddSuffix("GETCRAFT", new TwoArgsSuffix<CraftTemplate, StringValue, StringValue>(GetCraft));
            AddSuffix("LAUNCHCRAFT", new OneArgsSuffix<CraftTemplate>(LaunchShip));
            AddSuffix("LAUNCHCRAFTFROM", new TwoArgsSuffix<CraftTemplate, StringValue>(LaunchShip));
            AddSuffix("CRAFTLIST", new Suffix<ListValue>(CraftTemplate.GetAllTemplates));
        }

        public void RevertToLaunch()
        {
            if (CanRevertToLaunch())
            {
                FlightDriver.RevertToLaunch();
            }
            else throw new KOSCommandInvalidHereException(LineCol.Unknown(), "REVERTTOLAUNCH", "When revert is disabled", "When revert is enabled");
        }

        public void RevertToEditor()
        {
            if (CanRevvertToEditor())
            {
                EditorFacility fac = ShipConstruction.ShipType;
                FlightDriver.RevertToPrelaunch(fac);
            }
            else throw new KOSCommandInvalidHereException(LineCol.Unknown(), "REVERTTOEDITOR", "When revert is disabled", "When revert is enabled");
        }

        public void RevertTo(StringValue editor)
        {
            if (CanRevvertToEditor())
            {
                EditorFacility fac;
                switch (editor.ToUpper())
                {
                    case "VAB":
                        fac = EditorFacility.VAB;
                        break;

                    case "SPH":
                        fac = EditorFacility.SPH;
                        break;

                    default:
                        fac = EditorFacility.None;
                        break;
                }
                FlightDriver.RevertToPrelaunch(fac);
            }
            else throw new KOSCommandInvalidHereException(LineCol.Unknown(), "REVERTTO", "When revert is disabled", "When revert is enabled");
        }

        public BooleanValue CanRevert()
        {
            return FlightDriver.CanRevert;
        }

        public BooleanValue CanRevertToLaunch()
        {
            return FlightDriver.CanRevertToPostInit && HighLogic.CurrentGame.Parameters.Flight.CanRestart;
        }

        public BooleanValue CanRevvertToEditor()
        {
            return FlightDriver.CanRevertToPrelaunch &&
                HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor &&
                ShipConstruction.ShipConfig != null;
        }

        public StringValue OriginatingEditor()
        {
            if (ShipConstruction.ShipConfig != null)
            {
                EditorFacility fac = ShipConstruction.ShipType;
                return fac.ToString().ToUpper();
            }
            return "";
        }

        public void SetActiveVessel(VesselTarget vesselTarget)
        {
            Vessel vessel = vesselTarget.Vessel;
            if (!vessel.isActiveVessel)
            {
                FlightGlobals.SetActiveVessel(vessel);
            }
        }

        public void ForceSetActiveVessel(VesselTarget vesselTarget)
        {
            Vessel vessel = vesselTarget.Vessel;
            if (!vessel.isActiveVessel)
            {
                FlightGlobals.ForceSetActiveVessel(vessel);
            }
        }

        public ScalarValue GetHoursPerDay()
        {
            return GameSettings.KERBIN_TIME ? TimeSpan.HOURS_IN_KERBIN_DAY : TimeSpan.HOURS_IN_EARTH_DAY;
        }

        public void DebugLog(StringValue message)
        {
            SafeHouse.Logger.Log("(KUNIVERSE:DEBUGLOG) " + message);
        }

        public CraftTemplate GetCraft(StringValue name, StringValue editor)
        {
            return CraftTemplate.GetTemplateByName(name, editor);
        }

        private void LaunchShip(CraftTemplate ship)
        {
            LaunchShip(ship, ship.LaunchFacility);
        }

        public void LaunchShip(CraftTemplate ship, StringValue launchSiteName)
        {
            LaunchShip(ship, launchSiteName.ToString());
        }

        private void LaunchShip(CraftTemplate ship, string launchSiteName)
        {
            // From EditorLogic, see launchVessel(), proceedWithVesselLaunch(), and goForLaunch()
            var manifest = VesselCrewManifest.FromConfigNode(ship.InnerTemplate.config);
            manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(ship.InnerTemplate.config, manifest);
            PreFlightCheck preFlightCheck = new PreFlightCheck(
                () =>
                {
                    SafeHouse.Logger.Log("Launch new vessel!");
                    FlightDriver.StartWithNewLaunch(ship.FilePath, EditorLogic.FlagURL, launchSiteName, manifest);
                },
                () =>
                {
                    SafeHouse.Logger.LogError("Could not launch vessel, did not pass preflight...");
                });
            if (launchSiteName.Equals("runway", System.StringComparison.OrdinalIgnoreCase))
            {
                preFlightCheck.AddTest(new CraftWithinPartCountLimit(ship.InnerTemplate, SpaceCenterFacility.SpaceplaneHangar, GameVariables.Instance.GetPartCountLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar), false)));
                preFlightCheck.AddTest(new CraftWithinSizeLimits(ship.InnerTemplate, SpaceCenterFacility.Runway, GameVariables.Instance.GetCraftSizeLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Runway), false)));
                preFlightCheck.AddTest(new CraftWithinMassLimits(ship.InnerTemplate, SpaceCenterFacility.Runway, (double)GameVariables.Instance.GetCraftMassLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.Runway), false)));
            }
            else if (launchSiteName.Equals("launchpad", System.StringComparison.OrdinalIgnoreCase))
            {
                preFlightCheck.AddTest(new CraftWithinPartCountLimit(ship.InnerTemplate, SpaceCenterFacility.VehicleAssemblyBuilding, GameVariables.Instance.GetPartCountLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding), true)));
                preFlightCheck.AddTest(new CraftWithinSizeLimits(ship.InnerTemplate, SpaceCenterFacility.LaunchPad, GameVariables.Instance.GetCraftSizeLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.LaunchPad), true)));
                preFlightCheck.AddTest(new CraftWithinMassLimits(ship.InnerTemplate, SpaceCenterFacility.LaunchPad, (double)GameVariables.Instance.GetCraftMassLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.LaunchPad), true)));
            }
            else
            {
                throw new KOSException("Failed to lauch vessel, unrecognized lauch site: " + launchSiteName + ". Expected \"Runway\" or \"LaunchPad\".");
            }
            preFlightCheck.AddTest(new ExperimentalPartsAvailable(manifest));
            preFlightCheck.AddTest(new CanAffordLaunchTest(ship.InnerTemplate, Funding.Instance));
            preFlightCheck.AddTest(new FacilityOperational(launchSiteName, launchSiteName));
            preFlightCheck.AddTest(new NoControlSources(manifest));
            preFlightCheck.AddTest(new LaunchSiteClear(launchSiteName, HighLogic.CurrentGame));
            preFlightCheck.RunTests();
            SafeHouse.Logger.Log("Craft waiting for preflight checks!");
        }
    }
}
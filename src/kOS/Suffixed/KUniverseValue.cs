using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using PreFlightTests;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature ("Kuniverse")]
    public class KUniverseValue : Structure
    {
        private readonly SharedObjects shared;

        public KUniverseValue (SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes ();
        }

        public void InitializeSuffixes ()
        {
            AddSuffix ("CANREVERT", new Suffix<BooleanValue> (CanRevert));
            AddSuffix ("CANREVERTTOLAUNCH", new Suffix<BooleanValue> (CanRevertToLaunch));
            AddSuffix ("CANREVERTTOEDITOR", new Suffix<BooleanValue> (CanRevvertToEditor));
            AddSuffix ("REVERTTOLAUNCH", new NoArgsVoidSuffix (RevertToLaunch));
            AddSuffix ("REVERTTOEDITOR", new NoArgsVoidSuffix (RevertToEditor));
            AddSuffix ("REVERTTO", new OneArgsSuffix<StringValue> (RevertTo));
            AddSuffix ("CANQUICKSAVE", new Suffix<BooleanValue> (CanQuicksave));
            AddSuffix ("QUICKSAVE", new NoArgsVoidSuffix (QuickSave));
            AddSuffix ("QUICKLOAD", new NoArgsVoidSuffix (QuickLoad));
            AddSuffix ("QUICKSAVETO", new OneArgsSuffix<StringValue> (QuickSaveTo));
            AddSuffix ("QUICKLOADFROM", new OneArgsSuffix<StringValue> (QuickLoadFrom));
            AddSuffix ("QUICKSAVELIST", new Suffix<ListValue> (GetQuicksaveList));
            AddSuffix ("ORIGINEDITOR", new Suffix<StringValue> (OriginatingEditor));
            AddSuffix ("DEFAULTLOADDISTANCE", new Suffix<LoadDistanceValue> (() => new LoadDistanceValue (PhysicsGlobals.Instance.VesselRangesDefault)));
            AddSuffix ("ACTIVEVESSEL", new SetSuffix<VesselTarget> (() => VesselTarget.CreateOrGetExisting (FlightGlobals.ActiveVessel, shared), SetActiveVessel));
            AddSuffix (new string [] { "FORCESETACTIVEVESSEL", "FORCEACTIVE" }, new OneArgsSuffix<VesselTarget> (ForceSetActiveVessel));
            AddSuffix ("HOURSPERDAY", new Suffix<ScalarValue> (GetHoursPerDay));
            AddSuffix ("DEBUGLOG", new OneArgsSuffix<StringValue> (DebugLog));
            AddSuffix ("GETCRAFT", new TwoArgsSuffix<CraftTemplate, StringValue, StringValue> (GetCraft));
            AddSuffix ("LAUNCHCRAFT", new OneArgsSuffix<CraftTemplate> (LaunchShip));
            AddSuffix ("LAUNCHCRAFTFROM", new TwoArgsSuffix<CraftTemplate, StringValue> (LaunchShip));
            AddSuffix ("CRAFTLIST", new Suffix<ListValue> (CraftTemplate.GetAllTemplates));
            AddSuffix ("SWITCHVESSELWATCHERS", new NoArgsSuffix<UniqueSetValue<UserDelegate>> (() => shared.DispatchManager.CurrentDispatcher.GetSwitchVesselNotifyees ()));
            AddSuffix ("TIMEWARP", new Suffix<TimeWarpValue> (() => TimeWarpValue.Instance));
            AddSuffix (new string [] { "REALWORLDTIME", "REALTIME" }, new Suffix<ScalarValue> (GetRealWorldTime));
        }


        public void RevertToLaunch ()
        {
            if (CanRevertToLaunch ()) {
                shared.Cpu.GetCurrentOpcode ().AbortProgram = true;
                FlightDriver.RevertToLaunch ();
            } else throw new KOSCommandInvalidHereException (LineCol.Unknown (), "REVERTTOLAUNCH", "When revert is disabled", "When revert is enabled");
        }

        public void RevertToEditor ()
        {
            if (CanRevvertToEditor ()) {
                EditorFacility fac = ShipConstruction.ShipType;
                shared.Cpu.GetCurrentOpcode ().AbortProgram = true;
                FlightDriver.RevertToPrelaunch (fac);
            } else throw new KOSCommandInvalidHereException (LineCol.Unknown (), "REVERTTOEDITOR", "When revert is disabled", "When revert is enabled");
        }

        public void RevertTo (StringValue editor)
        {
            if (CanRevvertToEditor ()) {
                EditorFacility fac;
                switch (editor.ToUpper ()) {
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
                shared.Cpu.GetCurrentOpcode ().AbortProgram = true;
                FlightDriver.RevertToPrelaunch (fac);
            } else throw new KOSCommandInvalidHereException (LineCol.Unknown (), "REVERTTO", "When revert is disabled", "When revert is enabled");
        }

        public BooleanValue CanRevert ()
        {
            return FlightDriver.CanRevert;
        }

        public BooleanValue CanRevertToLaunch ()
        {
            return FlightDriver.CanRevertToPostInit && HighLogic.CurrentGame.Parameters.Flight.CanRestart;
        }

        public BooleanValue CanRevvertToEditor ()
        {
            return FlightDriver.CanRevertToPrelaunch &&
                HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor &&
                ShipConstruction.ShipConfig != null;
        }

        public StringValue OriginatingEditor ()
        {
            if (ShipConstruction.ShipConfig != null) {
                EditorFacility fac = ShipConstruction.ShipType;
                return fac.ToString ().ToUpper ();
            }
            return "";
        }

        public void SetActiveVessel (VesselTarget vesselTarget)
        {
            Vessel vessel = vesselTarget.Vessel;
            if (!vessel.isActiveVessel) {
                FlightGlobals.SetActiveVessel (vessel);
            }
        }

        public void ForceSetActiveVessel (VesselTarget vesselTarget)
        {
            Vessel vessel = vesselTarget.Vessel;
            if (!vessel.isActiveVessel) {
                FlightGlobals.ForceSetActiveVessel (vessel);
            }
        }

        public ScalarValue GetHoursPerDay ()
        {
            return GameSettings.KERBIN_TIME ? TimeSpan.HOURS_IN_KERBIN_DAY : TimeSpan.HOURS_IN_EARTH_DAY;
        }

        public void DebugLog (StringValue message)
        {
            SafeHouse.Logger.Log ("(KUNIVERSE:DEBUGLOG) " + message);
        }

        public BooleanValue CanQuicksave ()
        {
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickSave && FlightGlobals.ClearToSave () == ClearToSaveStatus.CLEAR) {
                return true;
            }
            return false;
        }

        public void QuickSave ()
        {
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickSave) {
                QuickSaveLoad.QuickSave ();
            } else throw new KOSException ("KSP prevents using quicksave currently.");
        }

        public void QuickSaveTo (StringValue name)
        {
            QuickSaveTo (name.ToString ());
        }

        public void QuickSaveTo (string name)
        {
            if (name.EndsWith (".sfs")) {
                name = name.Substring (0, name.Length - 4);
            }
            var reserved = new List<string> () { "persistent", "quicksave", "kos-backup-quicksave" };
            if (reserved.Contains (name)) {
                throw new KOSException ("Cannot save to " + name + " because it is a reserved name.");
            }
            SaveGame (name);
        }

        private void SaveGame (string name)
        {
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickSave && FlightGlobals.ClearToSave () == ClearToSaveStatus.CLEAR) {
                var game = HighLogic.CurrentGame.Updated ();
                game.startScene = GameScenes.FLIGHT;
                GamePersistence.SaveGame (game, name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
                game.startScene = GameScenes.SPACECENTER;
            } else throw new KOSException ("KSP prevents using quicksave currently.");
        }

        public void QuickLoad ()
        {
            LoadGame ("quicksave");
        }

        public void QuickLoadFrom (StringValue name)
        {
            LoadGame (name.ToString ());
        }

        private void LoadGame (string name)
        {
            if (name.EndsWith (".sfs")) {
                name = name.Substring (0, name.Length - 4);
            }
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad) {
                string filename = name + ".sfs";
                string path = KSPUtil.GetOrCreatePath ("saves/" + HighLogic.SaveFolder);
                if (!File.Exists (Path.Combine (path, filename))) {
                    throw new KOSException ("Error loading the quicksave file, the save file does not exist.");
                }
                shared.Cpu.GetCurrentOpcode ().AbortProgram = true;
                try {
                    SaveGame ("kos-backup-quicksave");
                    var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
                    if (game.flightState != null) {
                        if (game.compatible) {
                            GamePersistence.UpdateScenarioModules (game);
                            if (game.startScene != GameScenes.FLIGHT) {
                                if (KSPUtil.CheckVersion (game.file_version_major, game.file_version_minor, game.file_version_revision, 0, 24, 0) != VersionCompareResult.INCOMPATIBLE_TOO_EARLY) {
                                    GamePersistence.SaveGame (game, name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
                                    HighLogic.LoadScene (GameScenes.SPACECENTER);
                                    return;
                                }
                            }
                            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
                        }
                    }
                } catch (Exception ex) {
                    SafeHouse.Logger.Log (ex.Message);
                    throw new KOSException ("Error loading the quicksave file");
                }
            } else throw new KOSException ("KSP prevents using quickload currently.");
        }

        private ListValue GetQuicksaveList ()
        {
            var ret = new ListValue ();
            string path = KSPUtil.GetOrCreatePath ("saves/" + HighLogic.SaveFolder);
            var files = Directory.GetFiles (path, "*.sfs");
            foreach (var file in files) {
                string name = Path.GetFileNameWithoutExtension (file);
                if (!name.Equals ("persistent")) {
                    ret.Add (new StringValue (name));
                }
            }
            return ret;
        }

        public ScalarDoubleValue GetRealWorldTime ()
        {
            // returns the current unix-timestamp the host operating system is set to
            var timeSpan = (DateTime.UtcNow - new DateTime (1970, 1, 1, 0, 0, 0));
            return new ScalarDoubleValue (timeSpan.TotalSeconds);
        }

        public CraftTemplate GetCraft (StringValue name, StringValue editor)
        {
            return CraftTemplate.GetTemplateByName (name, editor);
        }

        private void LaunchShip (CraftTemplate ship)
        {
            LaunchShip (ship, ship.LaunchFacility);
        }

        public void LaunchShip (CraftTemplate ship, StringValue launchSiteName)
        {
            LaunchShip (ship, launchSiteName.ToString ());
        }

        private void LaunchShip (CraftTemplate ship, string launchSiteName)
        {
            // From EditorLogic, see launchVessel(), proceedWithVesselLaunch(), and goForLaunch()
            var manifest = VesselCrewManifest.FromConfigNode (ship.InnerTemplate.config);
            manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel (ship.InnerTemplate.config, manifest);
            PreFlightCheck preFlightCheck = new PreFlightCheck (
                () => {
                    SafeHouse.Logger.Log ("Launch new vessel!");
                    FlightDriver.StartWithNewLaunch (ship.FilePath, EditorLogic.FlagURL, launchSiteName, manifest);
                },
                () => {
                    SafeHouse.Logger.LogError ("Could not launch vessel, did not pass preflight...");
                });
            if (launchSiteName.Equals ("runway", System.StringComparison.OrdinalIgnoreCase)) {
                preFlightCheck.AddTest (new CraftWithinPartCountLimit (ship.InnerTemplate, SpaceCenterFacility.SpaceplaneHangar, GameVariables.Instance.GetPartCountLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.SpaceplaneHangar), false)));
                preFlightCheck.AddTest (new CraftWithinSizeLimits (ship.InnerTemplate, SpaceCenterFacility.Runway, GameVariables.Instance.GetCraftSizeLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.Runway), false)));
                preFlightCheck.AddTest (new CraftWithinMassLimits (ship.InnerTemplate, SpaceCenterFacility.Runway, (double)GameVariables.Instance.GetCraftMassLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.Runway), false)));
            } else if (launchSiteName.Equals ("launchpad", System.StringComparison.OrdinalIgnoreCase)) {
                preFlightCheck.AddTest (new CraftWithinPartCountLimit (ship.InnerTemplate, SpaceCenterFacility.VehicleAssemblyBuilding, GameVariables.Instance.GetPartCountLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.VehicleAssemblyBuilding), true)));
                preFlightCheck.AddTest (new CraftWithinSizeLimits (ship.InnerTemplate, SpaceCenterFacility.LaunchPad, GameVariables.Instance.GetCraftSizeLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.LaunchPad), true)));
                preFlightCheck.AddTest (new CraftWithinMassLimits (ship.InnerTemplate, SpaceCenterFacility.LaunchPad, (double)GameVariables.Instance.GetCraftMassLimit (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.LaunchPad), true)));
            } else {
                throw new KOSException ("Failed to lauch vessel, unrecognized lauch site: " + launchSiteName + ". Expected \"Runway\" or \"LaunchPad\".");
            }
            preFlightCheck.AddTest (new ExperimentalPartsAvailable (manifest));
            preFlightCheck.AddTest (new CanAffordLaunchTest (ship.InnerTemplate, Funding.Instance));
            preFlightCheck.AddTest (new FacilityOperational (launchSiteName, launchSiteName));
            preFlightCheck.AddTest (new NoControlSources (manifest));
            preFlightCheck.AddTest (new LaunchSiteClear (launchSiteName, launchSiteName, HighLogic.CurrentGame));
            preFlightCheck.RunTests ();
            shared.Cpu.GetCurrentOpcode ().AbortProgram = true;
            SafeHouse.Logger.Log ("Craft waiting for preflight checks!");
        }
    }
}

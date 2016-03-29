using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using System;
using System.IO;
using System.Collections.Generic;

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
            AddSuffix("QUICKSAVE", new NoArgsVoidSuffix(QuickSave));
            AddSuffix("QUICKLOAD", new NoArgsVoidSuffix(QuickLoad));
            AddSuffix("QUICKSAVETO", new OneArgsSuffix<StringValue>(QuickSaveTo));
            AddSuffix("QUICKLOADFROM", new OneArgsSuffix<StringValue>(QuickLoadFrom));
            AddSuffix("QUICKSAVELIST", new NoArgsSuffix<ListValue>(GetQuicksaveList));
            AddSuffix("ORIGINEDITOR", new Suffix<StringValue>(OriginatingEditor));
            AddSuffix("DEFAULTLOADDISTANCE", new Suffix<LoadDistanceValue>(() => new LoadDistanceValue(PhysicsGlobals.Instance.VesselRangesDefault)));
            AddSuffix("ACTIVEVESSEL", new SetSuffix<VesselTarget>(() => new VesselTarget(FlightGlobals.ActiveVessel, shared), SetActiveVessel));
            AddSuffix(new string[] { "FORCESETACTIVEVESSEL", "FORCEACTIVE" }, new OneArgsSuffix<VesselTarget>(ForceSetActiveVessel));
            AddSuffix("HOURSPERDAY", new Suffix<ScalarValue>(GetHoursPerDay));
            AddSuffix("DEBUGLOG", new OneArgsSuffix<StringValue>(DebugLog));
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

        public void QuickSave()
        {
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickSave)
            {
                QuickSaveLoad.QuickSave();
            }
            else throw new KOSException("KSP prevents using quicksave currently.");
        }

        public void QuickSaveTo(StringValue name)
        {
            QuickSaveTo(name.ToString());
        }

        public void QuickSaveTo(string name)
        {
            if (name.EndsWith(".sfs"))
            {
                name = name.Substring(0, name.Length - 4);
            }
            var reserved = new List<string>() { "persistent", "quicksave", "kos-backup-quicksave" };
            if (reserved.Contains(name))
            {
                throw new KOSException("Cannot save to " + name + " because it is a reserved name.");
            }
            SaveGame(name);
        }

        private void SaveGame(string name)
        {
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickSave)
            {
                GamePersistence.SaveGame(name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
            else throw new KOSException("KSP prevents using quicksave currently.");
        }

        public void QuickLoad()
        {
            LoadGame("quicksave");
        }

        public void QuickLoadFrom(StringValue name)
        {
            LoadGame(name.ToString());
        }

        private void LoadGame(string name)
        {
            if (name.EndsWith(".sfs"))
            {
                name = name.Substring(0, name.Length - 4);
            }
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad)
            {
                string filename = name + ".sfs";
                string path = KSPUtil.GetOrCreatePath("saves/" + HighLogic.SaveFolder);
                if (!File.Exists(Path.Combine(path, filename)))
                {
                    throw new KOSException("Error loading the quicksave file, the save file does not exist.");
                }
                try
                {
                    SaveGame("kos-backup-quicksave");
                    var game = GamePersistence.LoadGame(name, HighLogic.SaveFolder, true, false);
                    if (game.flightState != null)
                    {
                        if (game.compatible)
                        {
                            GamePersistence.UpdateScenarioModules(game);
                            if (game.startScene != GameScenes.FLIGHT)
                            {
                                if (KSPUtil.CheckVersion(game.file_version_major, game.file_version_minor, game.file_version_revision, 0, 24, 0) != VersionCompareResult.INCOMPATIBLE_TOO_EARLY)
                                {
                                    GamePersistence.SaveGame(game, name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
                                    HighLogic.LoadScene(GameScenes.SPACECENTER);
                                    return;
                                }
                            }
                            FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeHouse.Logger.Log(ex.Message);
                    throw new KOSException("Error loading the quicksave file");
                }
            }
            else throw new KOSException("KSP prevents using quickload currently.");
        }

        private ListValue GetQuicksaveList()
        {
            var ret = new ListValue();
            string path = KSPUtil.GetOrCreatePath("saves/" + HighLogic.SaveFolder);
            var files = Directory.GetFiles(path, "*.sfs");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!name.Equals("persistent"))
                {
                    ret.Add(new StringValue(name));
                }
            }
            return ret;
        }
    }
}

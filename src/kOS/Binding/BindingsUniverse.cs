using kOS.Execution;
using kOS.Suffixed;
using System;
using System.Collections.Generic;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class BindingTimeWarp : Binding
    {
        private static readonly IList<string> reservedSaveNames = new List<string> { "persistence", "quicksave" };

        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;

            Shared.BindingMgr.AddGetter("QUICKSAVE", cpu =>
            {
                if (!HighLogic.CurrentGame.Parameters.Flight.CanQuickSave) return false;
                QuickSaveLoad.QuickSave();
                return true;
            });
            
            Shared.BindingMgr.AddGetter("QUICKLOAD", cpu =>
                {
                    if (!HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad) return false;
                    try
                    {
                        GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.Log(ex.Message);
                        return false;
                    }
                    return true;
                });

            Shared.BindingMgr.AddSetter("SAVETO", (cpu, val) =>
                {
                    if (reservedSaveNames.Contains(val.ToString().ToLower())) return; 

                    Game game = HighLogic.CurrentGame.Updated();
                    game.startScene = GameScenes.FLIGHT;
                    GamePersistence.SaveGame(game, val.ToString(), HighLogic.SaveFolder, SaveMode.OVERWRITE);
                });

            Shared.BindingMgr.AddSetter("LOADFROM", (cpu, val) =>
                {
                    if (reservedSaveNames.Contains(val.ToString().ToLower())) return;

                    var game = GamePersistence.LoadGame(val.ToString(), HighLogic.SaveFolder, true, false);
                    if (game == null) return;
                    if (game.flightState == null) return;
                    if (!game.compatible) return;
                    FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
                });

            Shared.BindingMgr.AddGetter("LOADDISTANCE", cpu => Vessel.loadDistance);
            Shared.BindingMgr.AddSetter("LOADDISTANCE", delegate(CPU cpu, object val)
                {
                    var distance = Convert.ToSingle(val);
                    Vessel.loadDistance = distance;
                    Vessel.unloadDistance = distance - 250;
                });
            Shared.BindingMgr.AddGetter("WARPMODE", cpu =>
                {
                    switch (TimeWarp.WarpMode)
                    {
                        case TimeWarp.Modes.HIGH:
                            return "RAILS";

                        case TimeWarp.Modes.LOW:
                            return "PHYSICS";

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            Shared.BindingMgr.AddSetter("WARPMODE", (cpu, val) =>
                {
                    TimeWarp.Modes toSet;

                    switch (val.ToString())
                    {
                        case "PHYSICS":
                            toSet = TimeWarp.Modes.LOW;
                            break;

                        case "RAILS":
                            toSet = TimeWarp.Modes.HIGH;
                            break;

                        default:
                            throw new Exception(string.Format("WARPMODE '{0}' is not valid", val));
                    }

                    TimeWarp.fetch.Mode = toSet;
                });
            Shared.BindingMgr.AddGetter("WARP", cpu => TimeWarp.CurrentRateIndex);
            Shared.BindingMgr.AddSetter("WARP", delegate(CPU cpu, object val)
                {
                    int newRate;
                    if (int.TryParse(val.ToString(), out newRate))
                    {
                        TimeWarp.SetRate(newRate, false);
                    }
                });
            Shared.BindingMgr.AddGetter("MAPVIEW", cpu => MapView.MapIsEnabled);
            Shared.BindingMgr.AddSetter("MAPVIEW", delegate(CPU cpu, object val)
                {
                    if (Convert.ToBoolean(val))
                    {
                        MapView.EnterMapView();
                    }
                    else
                    {
                        MapView.ExitMapView();
                    }
                });
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                var cBody = body;
                Shared.BindingMgr.AddGetter(body.name, cpu => new BodyTarget(cBody, Shared));
            }
        }
    }
}
﻿using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class BindingTimeWarp : Binding
    {
        private static readonly IList<string> reservedSaveNames = new List<string> { "persistence", "quicksave" };

        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("QUICKSAVE", () =>
            {
                if (!HighLogic.CurrentGame.Parameters.Flight.CanQuickSave) return false;
                QuickSaveLoad.QuickSave();
                return true;
            });

            shared.BindingMgr.AddGetter("QUICKLOAD", () =>
                {
                    if (!HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad) return false;
                    try
                    {
                        GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
                    }
                    catch (Exception ex)
                    {
                        SafeHouse.Logger.Log(ex.Message);
                        return false;
                    }
                    return true;
                });

            shared.BindingMgr.AddSetter("SAVETO", val =>
                {
                    if (reservedSaveNames.Contains(val.ToString().ToLower())) return;

                    Game game = HighLogic.CurrentGame.Updated();
                    game.startScene = GameScenes.FLIGHT;
                    GamePersistence.SaveGame(game, val.ToString(), HighLogic.SaveFolder, SaveMode.OVERWRITE);
                });

            shared.BindingMgr.AddSetter("LOADFROM", val =>
                {
                    if (reservedSaveNames.Contains(val.ToString().ToLower())) return;

                    var game = GamePersistence.LoadGame(val.ToString(), HighLogic.SaveFolder, true, false);
                    if (game == null) return;
                    if (game.flightState == null) return;
                    if (!game.compatible) return;
                    FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
                });

            shared.BindingMgr.AddGetter("LOADDISTANCE", () => Vessel.loadDistance);
            shared.BindingMgr.AddSetter("LOADDISTANCE", val =>
            {
                var distance = Convert.ToSingle(val);
                Vessel.loadDistance = distance;
                Vessel.unloadDistance = distance - 250;
            });
            shared.BindingMgr.AddGetter("WARPMODE", () =>
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
            shared.BindingMgr.AddSetter("WARPMODE", val =>
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
            shared.BindingMgr.AddGetter("WARP", () => TimeWarp.CurrentRateIndex);
            shared.BindingMgr.AddSetter("WARP", val =>
            {
                int newRate;
                if (int.TryParse(val.ToString(), out newRate))
                {
                    switch (TimeWarp.WarpMode)
                    {
                        case TimeWarp.Modes.HIGH:
                            SetWarpRate(newRate, TimeWarp.fetch.warpRates.Length - 1);
                            break;
                        case TimeWarp.Modes.LOW:
                            SetWarpRate(newRate, TimeWarp.fetch.maxPhysicsRate_index);
                            break;
                        default:
                            throw new Exception(string.Format("WARPMODE '{0}' is unknown to kOS, please contact the devs", val));
                    }
                }
            });
            shared.BindingMgr.AddGetter("MAPVIEW", () => MapView.MapIsEnabled);
            shared.BindingMgr.AddSetter("MAPVIEW", val =>
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
                shared.BindingMgr.AddGetter(body.name, () => new BodyTarget(cBody, shared));
            }

            shared.BindingMgr.AddGetter("VERSION", () => Core.VersionInfo);
        }

        private static void SetWarpRate(int newRate, int maxRate)
        {
            var clampedValue = Mathf.Clamp(maxRate, 0, newRate);
            if (clampedValue != maxRate)
            {
                SafeHouse.Logger.Log(string.Format("Clamped Timewarp rate. Was: {0} Is: {1}", clampedValue, maxRate));
            }
            TimeWarp.SetRate(clampedValue, false);
        }
    }
}
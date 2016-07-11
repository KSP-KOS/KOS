using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Persistence;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class BindingTimeWarp : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("KUNIVERSE", () => new KUniverseValue(shared));

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

                    switch (val.ToString().ToUpper())
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
                            SetWarpRate(newRate, TimeWarp.fetch.physicsWarpRates.Length - 1);
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
            shared.BindingMgr.AddGetter("CONSTANT", () => new ConstantValue());
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                var cBody = body;
                shared.BindingMgr.AddGetter(body.name, () => new BodyTarget(cBody, shared));
            }

            shared.BindingMgr.AddGetter("VERSION", () => Core.VersionInfo);
            shared.BindingMgr.AddGetter("SOLARPRIMEVECTOR", () => new Vector(Planetarium.right));
            shared.BindingMgr.AddGetter("ARCHIVE", () => shared.VolumeMgr.GetVolume(Archive.ArchiveName));
        }

        private static void SetWarpRate(int newRate, int maxRate)
        {
            var clampedValue = Mathf.Clamp(newRate, 0, maxRate);
            if (clampedValue != newRate)
            {
                SafeHouse.Logger.Log(string.Format("Clamped Timewarp rate. Was: {0} Is: {1}", newRate, clampedValue));
            }
            TimeWarp.SetRate(clampedValue, false);
        }
    }
}

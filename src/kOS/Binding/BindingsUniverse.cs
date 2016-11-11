using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Persistence;
using kOS.Communication;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class BindingTimeWarp : Binding
    {
        private HomeConnection homeConnection;
        private ControlConnection controlConnection;

        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("KUNIVERSE", () => new KUniverseValue(shared));
            shared.BindingMgr.AddGetter("HOMECONNECTION", () => homeConnection ?? (homeConnection = new HomeConnection(shared)));
            shared.BindingMgr.AddGetter("CONTROLCONNECTION", () => controlConnection ?? (controlConnection = new ControlConnection(shared)));
            shared.BindingMgr.AddGetter("WARPMODE", () => TimeWarpValue.Instance.GetModeAsString());
            shared.BindingMgr.AddSetter("WARPMODE", val => TimeWarpValue.Instance.SetModeAsString((StringValue)StringValue.FromPrimitive(val.ToString())));
            shared.BindingMgr.AddGetter("WARP", () => TimeWarpValue.Instance.GetWarp());
            shared.BindingMgr.AddSetter("WARP", val => TimeWarpValue.Instance.SetWarp((ScalarIntValue)ScalarIntValue.FromPrimitive(val)));
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

    }
}

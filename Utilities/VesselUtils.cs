using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using kOS.Debug;
using kOS.Value;

namespace kOS.Utilities
{
  public class VesselUtils
  {
    public static List<Part> GetListOfActivatedEngines(Vessel vessel)
    {
      var retList = new List<Part>();

      foreach (var part in vessel.Parts)
      {
        foreach (PartModule module in part.Modules)
        {
            var engineModule = module as ModuleEngines;
            if (engineModule == null) continue;
            var engineMod = engineModule;

            if (engineMod.getIgnitionState)
            {
                retList.Add(part);
            }
        }
      }

      return retList;
    }

    public static bool TryGetResource(Vessel vessel, string resourceName, out double total)
    {
        var resourceIsFound = false;
        total = 0;
        PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.resourceDefinitions.FirstOrDefault(rd => rd.name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        // Ensure the built-in resource types never produce an error, even if the particular vessel is incapable of carrying them
        if (resourceDefinition == null)
            return resourceIsFound;
        resourceName = resourceName.ToUpper();
        foreach (var part in vessel.parts)
        {
            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName.ToUpper() != resourceName) continue;
                resourceIsFound = true;
                total += resource.amount;
            }
        }
    
        return resourceIsFound;
    }

    public static double GetResource(Vessel vessel, string resourceName)
    {
      double total = 0;
      resourceName = resourceName.ToUpper();

      foreach (var part in vessel.parts)
      {
        foreach (PartResource resource in part.Resources)
        {
          if (resource.resourceName.ToUpper() == resourceName)
          {
            total += resource.amount;
          }
        }
      }

      return total;
    }

    public static double GetMaxThrust(Vessel vessel)
    {
      var thrust = 0.0;

        foreach (var p in vessel.parts)
      {
        foreach (PartModule pm in p.Modules)
        {
          if (!pm.isEnabled) continue;
            if (!(pm is ModuleEngines)) continue;
            var e = (pm as ModuleEngines);
            if (!e.EngineIgnited) continue;
            thrust += e.maxThrust;
        }
      }

      return thrust;
    }

    public static Vessel TryGetVesselByName(string name, Vessel origin)
    {
      foreach (Vessel v in FlightGlobals.Vessels)
      {
        if (v != origin && v.vesselName.ToUpper() == name.ToUpper())
        {
          return v;
        }
      }

      return null;
    }

    public static CelestialBody GetBodyByName(string name)
    {
        return FlightGlobals.fetch.bodies.FirstOrDefault<CelestialBody>(body => name.ToUpper() == body.name.ToUpper());
    }

      public static Vessel GetVesselByName(string name, Vessel origin)
    {
        var vessel = TryGetVesselByName(name, origin);

        if (vessel == null)
        {
            throw new KOSException("Vessel '" + name + "' not found");
        }
        return vessel;
    }

    public static void SetTarget(ITargetable val)
    {
      FlightGlobals.fetch.SetVesselTarget(val);
    }

    public static double GetCommRange(Vessel vessel)
    {
      double range = 100000;

      foreach (var part in vessel.parts)
      {
          if (part.partInfo.name != "longAntenna") continue;
          var status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

          if (status == "Fixed" || status == "Locked")
          {
              range += 1000000;
          }
      }

      foreach (var part in vessel.parts)
      {
          if (part.partInfo.name != "mediumDishAntenna") continue;
          var status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

          if (status == "Fixed" || status == "Locked")
          {
              range *= 100;
          }
      }

      foreach (var part in vessel.parts)
      {
          if (part.partInfo.name != "commDish") continue;
          var status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

          if (status == "Fixed" || status == "Locked")
          {
              range *= 200;
          }
      }

      return range;
    }

    public static double GetDistanceToKerbinSurface(Vessel vessel)
    {
        foreach (var body in FlightGlobals.fetch.bodies.Where(body => body.name.ToUpper() == "KERBIN"))
        {
            return Vector3d.Distance(body.position, vessel.GetWorldPos3D()) - 600000; // Kerbin radius = 600,000
        }

        throw new KOSException("Planet Kerbin not found!");
    }

      public static float AngleDelta(float a, float b)
    {
      var delta = b - a;

      while (delta > 180) delta -= 360;
      while (delta < -180) delta += 360;

      return delta;
    }

    public static float GetHeading(Vessel vessel)
    {
      var up = vessel.upAxis;
      var north = GetNorthVector(vessel);
      var headingQ = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * Quaternion.LookRotation(north, up));

      return headingQ.eulerAngles.y;
    }

    public static float GetVelocityHeading(Vessel vessel)
    {
      var up = vessel.upAxis;
      var north = GetNorthVector(vessel);
      var headingQ = Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(vessel.srf_velocity, up)) * Quaternion.LookRotation(north, up));

      return headingQ.eulerAngles.y;
    }

    public static float GetTargetBearing(Vessel vessel, Vessel target)
    {
      return AngleDelta(GetHeading(vessel), GetTargetHeading(vessel, target));
    }

    public static float GetTargetHeading(Vessel vessel, Vessel target)
    {
      var up = vessel.upAxis;
      var north = GetNorthVector(vessel);
      var vector = Vector3d.Exclude(vessel.upAxis, target.GetWorldPos3D() - vessel.GetWorldPos3D()).normalized;
      var headingQ = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(Quaternion.LookRotation(vector, up)) * Quaternion.LookRotation(north, up));

      return headingQ.eulerAngles.y;
    }

    public static Vector3d GetNorthVector(Vessel vessel)
    {
      return Vector3d.Exclude(vessel.upAxis, vessel.mainBody.transform.up);
    }

    public static object TryGetEncounter(Vessel vessel)
    {
      foreach (Orbit patch in vessel.patchedConicSolver.flightPlan)
      {
        if (patch.patchStartTransition == Orbit.PatchTransitionType.ENCOUNTER)
        {
          return new OrbitInfo(patch);
        }
      }

      return "None";
    }

    public static void LandingLegsCtrl(Vessel vessel, bool state)
    {
      // This appears to work on all legs in 0.22
      vessel.rootPart.SendEvent(state ? "LowerLeg" : "RaiseLeg");
    }

    internal static object GetLandingLegStatus(Vessel vessel)
    {
      var atLeastOneLeg = false; // No legs at all? Always return false

      foreach (var p in vessel.parts)
      {
          if (!p.Modules.OfType<ModuleLandingLeg>().Any()) continue;
          atLeastOneLeg = true;

          foreach (var l in p.FindModulesImplementing<ModuleLandingLeg>())
          {
              if (l.savedLegState != (int)(ModuleLandingLeg.LegStates.DEPLOYED))
              {
                  // If just one leg is retracted, still moving, or broken return false.
                  return false;
              }
          }
      }

      return atLeastOneLeg;
    }

    public static object GetChuteStatus(Vessel vessel)
    {
      var atLeastOneChute = false; // No chutes at all? Always return false

      foreach (var p in vessel.parts)
      {
        foreach (var c in p.FindModulesImplementing<ModuleParachute>())
        {
          atLeastOneChute = true;

          if (c.deploymentState == ModuleParachute.deploymentStates.STOWED)
          {
            // If just one chute is not deployed return false
            return false;
          }
        }
      }

      return atLeastOneChute;
    }

    public static void DeployParachutes(Vessel vessel, bool state)
    {
        if (!vessel.mainBody.atmosphere || !state) return;
        foreach (var p in vessel.parts)
        {
            if (!p.Modules.OfType<ModuleParachute>().Any() || !state) continue;
            foreach (var c in p.FindModulesImplementing<ModuleParachute>())
            {
                if (c.deploymentState == ModuleParachute.deploymentStates.STOWED) //&& c.deployAltitude * 3 > vessel.heightFromTerrain)
                {
                    c.DeployAction(null);
                }
            }
        }
    }

      public static object GetSolarPanelStatus(Vessel vessel)
    {
      var atLeastOneSolarPanel = false; // No panels at all? Always return false

      foreach (var p in vessel.parts)
      {
        foreach (var c in p.FindModulesImplementing<ModuleDeployableSolarPanel>())
        {
          atLeastOneSolarPanel = true;

          if (c.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTED)
          {
            // If just one panel is not deployed return false
            return false;
          }
        }
      }

      return atLeastOneSolarPanel;
    }

    public static void SolarPanelCtrl(Vessel vessel, bool state)
    {
      vessel.rootPart.SendEvent(state ? "Extend" : "Retract");
    }


    public static double GetMassDrag(Vessel vessel)
    {
        return vessel.parts.Aggregate<Part, double>(0, (current, p) => current + (p.mass + p.GetResourceMass())*p.maximum_drag);
    }

      public static double RealMaxAtmosphereAltitude(CelestialBody body)
    {
      // This comes from MechJeb CelestialBodyExtensions.cs
      if (!body.atmosphere) return 0;
      //Atmosphere actually cuts out when exp(-altitude / scale height) = 1e-6
      return -body.atmosphereScaleHeight * 1000 * Math.Log(1e-6);
    }

    public static double GetTerminalVelocity(Vessel vessel)
    {
      if(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass()) > RealMaxAtmosphereAltitude(vessel.mainBody)) return double.PositiveInfinity;
      double densityOfAir = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(vessel.findWorldCenterOfMass(), vessel.mainBody));
      return Math.Sqrt(2 * FlightGlobals.getGeeForceAtPosition(vessel.findWorldCenterOfMass()).magnitude * vessel.GetTotalMass() / ( GetMassDrag(vessel) * FlightGlobals.DragMultiplier * densityOfAir ));
    }
    public static float GetVesselLattitude(Vessel vessel)
    {
      var retVal = (float)vessel.latitude;

      if (retVal > 90) return 90;
      if (retVal < -90) return -90;

      return retVal;
    }

    public static float GetVesselLongitude(Vessel vessel)
    {
      var retVal = (float)vessel.longitude;

      while (retVal > 180) retVal -= 360;
      while (retVal < -180) retVal += 360;

      return retVal;
    }
  }
}
using kOS.Binding;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;
using kOS.Utilities;
using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class VesselTarget : Orbitable, IKOSTargetable
    {
        override public Orbit Orbit { get{return Vessel.orbit;} }

        override public string GetName()
        {
            return Vessel.vesselName;
        }

        override public Vector GetPosition()
        {
            return new Vector( Vessel.findWorldCenterOfMass() - CurrentVessel.findWorldCenterOfMass() );
        }

        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Vessel);
        }
        
        /// <summary>
        ///   Calculates the position of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned manuever nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The position as a user-readable Vector in Shared.Vessel-origin raw rotation coordinates.</returns>
        override public Vector GetPositionAtUT(TimeSpan timeStamp)
        {
            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT( desiredUT );
            Vector3d pos = patch.getPositionAtUT(desiredUT);

            // This is an ugly workaround to fix what is probably a bug in KSP's API:
            // If looking at a future orbit patch around a child body of the current body, then
            // the various get{Thingy}AtUT() methods return numbers calculated incorrectly as
            // if the child body was going to remain stationary where it is now, rather than
            // taking into account where it will be later when the intercept happens.
            // This corrects for that case:
            if (Utils.BodyOrbitsBody(patch.referenceBody, Vessel.orbit.referenceBody))
            {
                Vector3d futureSOIPosNow = patch.referenceBody.position;
                Vector3d futureSOIPosLater = patch.referenceBody.getPositionAtUT(desiredUT);
                Vector3d offset = futureSOIPosLater - futureSOIPosNow;
                pos = pos + offset;
            }

            return new Vector( pos - Shared.Vessel.findWorldCenterOfMass() ); // Convert to ship-centered frame.
        }

        /// <summary>
        ///   Calculates the velocities of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned manuever nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The orbit/surface velocity pair as a user-readable Vector in raw rotation coordinates.</returns>
        override public OrbitableVelocity GetVelocitiesAtUT(TimeSpan timeStamp)
        {
            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT( desiredUT );
                        
            Vector3d orbVel = patch.getOrbitalVelocityAtUT(desiredUT);
 
            // This is an ugly workaround to fix what is probably a bug in KSP's API:
            // If looking at a future orbit patch around a child body of the current body, then
            // the various get{Thingy}AtUT() methods return numbers calculated incorrectly as
            // if the child body was going to remain stationary where it is now, rather than
            // taking into account where it will be later when the intercept happens.
            // This corrects for that case:
            if (Utils.BodyOrbitsBody(patch.referenceBody, Vessel.orbit.referenceBody))
            {
                Vector3d futureBodyVel = patch.referenceBody.orbit.getOrbitalVelocityAtUT(desiredUT);
                orbVel = orbVel + futureBodyVel;
            }

            // For some weird reason orbital velocities are returned by the KSP API
            // with Y and Z swapped, so swap them back:
            orbVel = new Vector3d( orbVel.x, orbVel.z, orbVel.y );
            

            CelestialBody parent = patch.referenceBody;
            Vector surfVel;
            if (parent != null)
            {
                Vector3d pos = GetPositionAtUT( timeStamp ).ToVector3D();
                surfVel = new Vector( orbVel - parent.getRFrmVel( pos + Shared.Vessel.findWorldCenterOfMass()) );
            }
            else
                surfVel = new Vector( orbVel.x, orbVel.y, orbVel.z );

            return new OrbitableVelocity( new Vector(orbVel), surfVel );
        }
        
        override public Vector GetUpVector()
        {
            return new Vector( Vessel.upAxis );
        }

        override public Vector GetNorthVector()
        {
            return new Vector( VesselUtils.GetNorthVector(Vessel) );
        }
        
        /// <summary>
        ///   Calcualte which orbit patch contains the timestamp given.
        /// </summary>
        /// <param name="desiredUT">The timestamp to look for</param>
        /// <returns>the orbit patch the vessel is expected to be in at the given time.</returns>
        override public Orbit GetOrbitAtUT(double desiredUT)
        {            
            // After much trial and error this seems to be the only way to do this:

            // Find the lastmost manuever node that occurs prior to timestamp:
            List<ManeuverNode> nodes = Vessel.patchedConicSolver.maneuverNodes;
            Orbit orbitPatch = Vessel.orbit;
            for (int nodeIndex = 0 ; nodeIndex < nodes.Count && nodes[nodeIndex].UT <= desiredUT ; ++nodeIndex)
            {
                orbitPatch = nodes[nodeIndex].nextPatch; // the orbit patch that starts with this node.
            }
            
            // Walk the orbit patch list from there looking for the lastmost orbit patch that
            // contains this timestamp, or if this timestamp is later than the end of the last
            // patch, then just return the last patch (this can happen because a patches' EndUT
            // is one period of time and we might be predicting for a point in time more than one
            // period into the future.)
            while ( !( orbitPatch.StartUT < desiredUT && desiredUT < orbitPatch.EndUT ) )
            {
                // Sadly the way to detect that you are at the end of the orbitPatch list is
                // messy and inconsistent.  Sometimes KSP's API gives you a list that ends
                // with null, and other times it gives you a list that ends with a bogus
                // dummy orbit patch that is not null but contains bogus data and will crash
                // KSP if you try calling many of its methods.  The way to detect if you have
                // such a bogus patch is to check its activePath property:
                if (orbitPatch.nextPatch != null && orbitPatch.nextPatch.activePatch)
                {
                    orbitPatch = orbitPatch.nextPatch;
                }
                else
                {
                    break;
                }
            }
            
            return orbitPatch;
        }

        static VesselTarget()
        {
            // TODO: These need to be refactored into the new suffix system at some point:
            
            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "AVAILABLETHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME", "SHIPNAME",
                    "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE", "SRFRETROGRADE"
                };
        }

        public VesselTarget(Vessel target, SharedObjects shared) :base(shared)
        {
            Vessel = target;
            InitializeSuffixes();
        }

        public VesselTarget(SharedObjects shared) : this(shared.Vessel, shared) { }

        private Vessel CurrentVessel { get { return Shared.Vessel; } }

        public ITargetable Target
        {
            get { return Vessel; }
        }

        // TODO: We will need to replace with the same thing Orbitable:DISTANCE does
        // in order to implement the orbit solver later.
        public double GetDistance()
        {
            return Vector3d.Distance(CurrentVessel.findWorldCenterOfMass(), Vessel.findWorldCenterOfMass());
        }

        public Vessel Vessel { get; private set; }

        public static string[] ShortCuttableShipSuffixes { get; private set; }

        public override string ToString()
        {
            return "SHIP(\"" + Vessel.vesselName + "\")";
        }

        private ListValue GetPartsNamed(string partName)
        {
            string lowerName = partName.ToLower();

            List<global::Part> kspParts = Vessel.parts.FindAll(part => part.name.ToLower() == lowerName);

            ListValue kScriptParts = new ListValue();
            foreach (global::Part kspPart in kspParts)
                kScriptParts.Add( new PartValue(kspPart,Shared));
            return kScriptParts;
        }

        private ListValue GetModulesNamed(string modName)
        {
            string lowerName = modName.ToLower();
            
            ListValue kScriptParts = new ListValue();
            // This is slow - maybe there should be a faster lookup string hash, but
            // KSP's data model seems to have not implemented it:
            foreach (global::Part p in Vessel.parts)
            {
                foreach (PartModule pMod in p.Modules)
                {
                    if (pMod.moduleName.ToLower() == lowerName)
                        kScriptParts.Add(new PartModuleFields(pMod,Shared));
                }
            }
            return kScriptParts;
        }
        
        private ListValue GetPartsInGroup(string groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();
            
            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS")    { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR")   { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS")    { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT")  { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1")    { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2")    { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3")    { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4")    { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5")    { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6")    { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7")    { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8")    { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9")    { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10")   { matchGroup = KSPActionGroup.Custom10; }
            
            ListValue kScriptParts = new ListValue();
                        
            if (matchGroup != KSPActionGroup.None)
                foreach (global::Part p in Vessel.parts)
                {
                    // See if any of the parts' actions are this action group:
                    foreach (BaseAction action in p.Actions)
                        if (action.actionGroup.Equals(matchGroup))
                            kScriptParts.Add(new PartValue(p,Shared));
                    // See if any of the parts' partmodule actions are this action group:
                    foreach (PartModule pm in p.Modules)
                        foreach (BaseAction action in pm.Actions)
                            if (action.actionGroup.Equals(matchGroup))
                                kScriptParts.Add(new PartValue(p,Shared));
                }
            return kScriptParts;
        }
        
        private ListValue GetModulesInGroup(string groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();
            
            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS")    { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR")   { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS")    { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT")  { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1")    { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2")    { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3")    { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4")    { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5")    { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6")    { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7")    { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8")    { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9")    { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10")   { matchGroup = KSPActionGroup.Custom10; }
            
            ListValue kScriptParts = new ListValue();
            
            // This is almost identical to the logic in GetPartsInGroup and it might be a nice idea
            // later to merge them somehow:
            //
            if (matchGroup != KSPActionGroup.None)
                foreach (global::Part p in Vessel.parts)
                    foreach (PartModule pm in p.Modules)
                        foreach (BaseAction action in pm.Actions)
                            if (action.actionGroup.Equals(matchGroup))
                                kScriptParts.Add(new PartModuleFields(pm,Shared));
            return kScriptParts;
        }
        

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "PACKDISTANCE":
                    var distance = (float) value;
                    Vessel.distanceLandedPackThreshold = distance;
                    Vessel.distancePackThreshold = distance;
                    return true;
            }

            return base.SetSuffix(suffixName, value);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PARTSNAMED", new OneArgsSuffix<ListValue,string>(GetPartsNamed));
            AddSuffix("MODULESNAMED", new OneArgsSuffix<ListValue,string>(GetModulesNamed));
            AddSuffix("PARTSINGROUP", new OneArgsSuffix<ListValue,string>(GetPartsInGroup));
            AddSuffix("MODULESINGROUP", new OneArgsSuffix<ListValue,string>(GetModulesInGroup));
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                // TODO: These need to be moved into InitializeSuffixes() at some point:

                case "CONTROL":
                    return FlightControlManager.GetControllerByVessel(Vessel);
                case "BEARING":
                    return VesselUtils.GetTargetBearing(CurrentVessel, Vessel);
                case "HEADING":
                    return VesselUtils.GetTargetHeading(CurrentVessel, Vessel);
                case "AVAILABLETHRUST":
                    return VesselUtils.GetAvailableThrust(Vessel);
                case "MAXTHRUST":
                    return VesselUtils.GetMaxThrust(Vessel);
                case "FACING":
                    return VesselUtils.GetFacing(Vessel);
                case "ANGULARMOMENTUM":
                    return new Direction(Vessel.angularMomentum, true);
                case "ANGULARVEL":
                    return new Direction(Vessel.angularVelocity, true);
                case "MASS":
                    return Vessel.GetTotalMass();
                case "VERTICALSPEED":
                    return Vessel.verticalSpeed;
                case "SURFACESPEED":
                    return Vessel.horizontalSrfSpeed;
                case "AIRSPEED":
                    return
                        (Vessel.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Vessel.findWorldCenterOfMass()))
                            .magnitude; //the velocity of the vessel relative to the air);
                //DEPRICATED VESSELNAME
                case "VESSELNAME":
                    throw new KOSException("VESSELNAME is DEPRICATED, use SHIPNAME.");
                case "SHIPNAME":
                    return Vessel.vesselName;

                // Although there is an implementation of lat/long/alt in Orbitible,
                // it's better to use the methods for vessels that are faster if they're
                // available:
                case "LATITUDE":
                    return VesselUtils.GetVesselLattitude(Vessel);
                case "LONGITUDE":
                    return VesselUtils.GetVesselLongitude(Vessel);
                case "ALTITUDE":
                    return Vessel.altitude;

                case "SENSORS":
                    return new VesselSensors(Vessel);
                case "TERMVELOCITY":
                    return VesselUtils.GetTerminalVelocity(Vessel);
                case "LOADED":
                    return Vessel.loaded;
                case "ROOTPART":
                    return new PartValue(Vessel.rootPart,Shared);
            }

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(Vessel, suffixName, out dblValue))
            {
                return dblValue;
            }

            return base.GetSuffix(suffixName);
        }
    }
}

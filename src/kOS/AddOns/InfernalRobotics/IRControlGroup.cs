using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed;
using System.Collections.Generic;

namespace kOS.AddOns.InfernalRobotics
{
    public class IRControlGroupWrapper : Structure
    {
        private readonly IRWrapper.IControlGroup cg;
        private readonly SharedObjects shared;

        public IRControlGroupWrapper(IRWrapper.IControlGroup init, SharedObjects shared)
        {
            cg = init;
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new SetSuffix<StringValue>(() => cg.Name, value => cg.Name = value));
            AddSuffix("SPEED", new SetSuffix<ScalarDoubleValue>(() => cg.Speed, value => cg.Speed = value));
            AddSuffix("EXPANDED", new SetSuffix<BooleanValue>(() => cg.Expanded, value => cg.Expanded = value));
            AddSuffix("FORWARDKEY", new SetSuffix<StringValue>(() => cg.ForwardKey, value => cg.ForwardKey = value));
            AddSuffix("REVERSEKEY", new SetSuffix<StringValue>(() => cg.ReverseKey, value => cg.ReverseKey = value));

            AddSuffix("SERVOS", new NoArgsSuffix<ListValue> (GetServos));

            AddSuffix("MOVERIGHT", new NoArgsVoidSuffix(MoveRight));
            AddSuffix("MOVELEFT", new NoArgsVoidSuffix(MoveLeft));
            AddSuffix("MOVECENTER", new NoArgsVoidSuffix(MoveCenter));
            AddSuffix("MOVENEXTPRESET", new NoArgsVoidSuffix(MoveNextPreset));
            AddSuffix("MOVEPREVPRESET", new NoArgsVoidSuffix(MovePrevPreset));
            AddSuffix("STOP", new NoArgsVoidSuffix(Stop));

            AddSuffix("VESSEL", new Suffix<VesselTarget>(GetVessel));
        }

        public ListValue GetServos()
        {
            var list = new List <IRServoWrapper> ();

            if(IRWrapper.APIReady)
            {
                foreach(IRWrapper.IServo s in cg.Servos)
                {
                    list.Add(new IRServoWrapper(s, shared));
                }
            }
            
            return ListValue.CreateList(list);
        }

        public VesselTarget GetVessel()
        {
            if (IRWrapper.APIReady) 
            {
                //IF IR version is 0.21.4 or below IR API may return null, but it also means that IR API only returns groups for ActiveVessel
                //so returning the ActiveVessel should work
                return cg.Vessel != null ? new VesselTarget (cg.Vessel, shared) : new VesselTarget(FlightGlobals.ActiveVessel, shared);
            } 
            else
                return new VesselTarget(shared.Vessel, shared); //user should not be able to get here anyway, but to avoid null will return shared.Vessel
        }

        public void MoveRight()
        {
            cg.MoveRight();
        }

        public void MoveLeft()
        {
            cg.MoveLeft();
        }

        public void MoveCenter()
        {
            cg.MoveCenter();
        }

        public void MoveNextPreset()
        {
            cg.MoveNextPreset();
        }

        public void MovePrevPreset()
        {
            cg.MovePrevPreset();
        }

        public void Stop()
        {
            cg.Stop();
        }
    }
}
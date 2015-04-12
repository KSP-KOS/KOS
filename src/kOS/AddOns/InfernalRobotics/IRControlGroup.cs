using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Collections.Generic;
using System.Linq;
using kOS.AddOns.InfernalRobotics;

namespace kOS.AddOns.InfernalRobotics
{
    public class IRControlGroupWrapper : Structure
    {
        private readonly IRWrapper.IRAPI.IRControlGroup cg;
        private readonly SharedObjects shared;

        public IRControlGroupWrapper(IRWrapper.IRAPI.IRControlGroup init, SharedObjects shared)
        {
            cg = init;
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new SetSuffix<string>(() => cg.Name, value => cg.Name = value));
            AddSuffix("SPEED", new SetSuffix<float>(() => cg.Speed, value => cg.Speed = value));
            AddSuffix("EXPANDED", new SetSuffix<bool>(() => cg.Expanded, value => cg.Expanded = value));
            AddSuffix("FORWARDKEY", new SetSuffix<string>(() => cg.ForwardKey, value => cg.ForwardKey = value));
            AddSuffix("REVERSEKEY", new SetSuffix<string>(() => cg.ReverseKey, value => cg.ReverseKey = value));

            AddSuffix("SERVOS", new NoArgsSuffix<ListValue> (GetServos));

            AddSuffix("MOVERIGHT", new NoArgsSuffix(MoveRight));
            AddSuffix("MOVELEFT", new NoArgsSuffix(MoveLeft));
            AddSuffix("MOVECENTER", new NoArgsSuffix(MoveCenter));
            AddSuffix("MOVENEXTPRESET", new NoArgsSuffix(MoveNextPreset));
            AddSuffix("MOVEPREVPRESET", new NoArgsSuffix(MovePrevPreset));
            AddSuffix("STOP", new NoArgsSuffix(Stop));
        }

        public ListValue GetServos()
        {
            var list = new List <IRServoWrapper> ();

            if(IRWrapper.APIReady)
            {
                foreach(IRWrapper.IRAPI.IRServo s in cg.Servos)
                {
                    list.Add(new IRServoWrapper(s, shared));
                }
            }
            
            return ListValue.CreateList(list);
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
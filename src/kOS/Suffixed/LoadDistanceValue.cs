using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    public class LoadDistanceValue : Structure
    {
        private VesselRanges vesselRanges;
        
        public LoadDistanceValue(Vessel vessel) :
            this(vessel.vesselRanges)
        {
        }

        public LoadDistanceValue(VesselRanges ranges)
        {
            vesselRanges = ranges;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ESCAPING", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.escaping)));
            AddSuffix("FLYING", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.flying)));
            AddSuffix("LANDED", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.landed)));
            AddSuffix("ORBIT", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.orbit)));
            AddSuffix("PRELAUNCH", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.prelaunch)));
            AddSuffix("SPLASHED", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.splashed)));
            AddSuffix("SUBORBITAL", new Suffix<SituationLoadDistanceValue>(() => new SituationLoadDistanceValue(vesselRanges.subOrbital)));
        }
        public class SituationLoadDistanceValue : Structure
        {
            private VesselRanges.Situation situationValue;
            
            public SituationLoadDistanceValue(VesselRanges.Situation situation)
            {
                situationValue = situation;
                InitializeSuffixes();
            }

            public void InitializeSuffixes()
            {
                AddSuffix("LOAD", new SetSuffix<float>(() => situationValue.load, value => SetLoad(value)));
                AddSuffix("UNLOAD", new SetSuffix<float>(() => situationValue.unload, value => SetUnload(value)));
                AddSuffix("PACK", new SetSuffix<float>(() => situationValue.pack, value => SetPack(value)));
                AddSuffix("UNPACK", new SetSuffix<float>(() => situationValue.unpack, value => SetUnpack(value)));
            }

            public void SetLoad(float val)
            {
                if (situationValue.unload < val)
                {
                    situationValue.unload = val + situationValue.unload - situationValue.load;
                }
                situationValue.load = val;
            }

            public void SetUnload(float val)
            {
                if (situationValue.load > val)
                {
                    situationValue.load = val + situationValue.load - situationValue.unload;
                }
                situationValue.unload = val;
            }

            public void SetPack(float val)
            {
                if (situationValue.unpack > val)
                {
                    situationValue.unpack = val + situationValue.unpack - situationValue.pack;
                }
                //if (situationValue.unload < val)
                //{
                //    SetUnload(val + situationValue.unload - situationValue.pack);
                //}
                situationValue.pack = val;
            }
            public void SetUnpack(float val)
            {
                if (situationValue.pack < val)
                {
                    situationValue.pack = val + situationValue.pack - situationValue.unpack;
                }
                //if (situationValue.load < val)
                //{
                //    SetLoad(val + situationValue.load - situationValue.unpack);
                //}
                situationValue.unpack = val;
            }
        }
    }
}

using kOS.Safe.Encapsulation;
using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class CrewMember : Structure
    {
        private ProtoCrewMember crewMember;
        private SharedObjects shared;

        public String Name {
            get { return crewMember.name; }
        }

        public String Gender {
            get { return crewMember.gender.ToString(); }
        }

        public int Experience {
            get { return crewMember.experienceLevel; }
        }

        public String Trait {
            get { return crewMember.experienceTrait.Title; }
        }

        public CrewMember(ProtoCrewMember crewMember, SharedObjects shared)
        {
            this.crewMember = crewMember;
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<String>(() => Name));
            AddSuffix("TOURIST", new Suffix<bool>(() => crewMember.type == ProtoCrewMember.KerbalType.Tourist));
            AddSuffix("GENDER", new Suffix<String>(() => Gender));
            AddSuffix("TRAIT", new Suffix<String>(() => Trait));
            AddSuffix("EXPERIENCE", new Suffix<int>(() => Experience));
            AddSuffix("PART", new Suffix<PartValue>(() => PartValueFactory.Construct(crewMember.seat.part, shared)));
        }

        public override string ToString()
        {
            return Name + " " + Gender[0] + ", " + Trait + " " + new String('*', Experience);
        }
    }
}


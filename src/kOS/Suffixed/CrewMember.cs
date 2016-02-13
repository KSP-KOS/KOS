using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class CrewMember : Structure
    {
        private readonly ProtoCrewMember crewMember;
        private readonly SharedObjects shared;

        public string Name {
            get { return crewMember.name; }
        }

        public string Gender {
            get { return crewMember.gender.ToString(); }
        }

        public int Experience {
            get { return crewMember.experienceLevel; }
        }

        public string Trait {
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
            AddSuffix("NAME", new Suffix<StringValue>(() => Name));
            AddSuffix("TOURIST", new Suffix<BooleanValue>(() => crewMember.type == ProtoCrewMember.KerbalType.Tourist));
            AddSuffix("GENDER", new Suffix<StringValue>(() => Gender));
            AddSuffix("TRAIT", new Suffix<StringValue>(() => Trait));
            AddSuffix("EXPERIENCE", new Suffix<ScalarValue>(() => Experience));
            AddSuffix("PART", new Suffix<PartValue>(() => PartValueFactory.Construct(crewMember.seat.part, shared)));
        }

        public override string ToString()
        {
            return Name + " " + Gender[0] + ", " + Trait + " " + new string('*', Experience);
        }
    }
}


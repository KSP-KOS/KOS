using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("CrewMember")]
    public class CrewMember : Structure
    {
        public readonly ProtoCrewMember crewMember;
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
            AddSuffix("STATUS", new Suffix<StringValue>(() => crewMember.rosterStatus.ToString()));
            AddSuffix("EXPERIENCEVALUE", new Suffix<ScalarValue>(() => crewMember.experience));
            AddSuffix("KERBALTYPE", new Suffix<StringValue>(() => crewMember.type.ToString()));
        }

        public override string ToString()
        {
            return Name + " " + Gender[0] + ", " + Trait + " " + new string('*', Experience);
        }
    }
}


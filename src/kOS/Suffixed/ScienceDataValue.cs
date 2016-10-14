using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceData")]
    public class ScienceDataValue : Structure
    {
        private ScienceData scienceData;

        public ScienceDataValue(ScienceData scienceData)
        {
            this.scienceData = scienceData;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DATAAMOUNT", new NoArgsSuffix<ScalarValue>(() => scienceData.dataAmount));
            AddSuffix("SCIENCEVALUE", new NoArgsSuffix<ScalarValue>(() => ScienceValue()));
            AddSuffix("TRANSMITVALUE", new NoArgsSuffix<ScalarValue>(() => TransmitValue()));
            AddSuffix("TITLE", new NoArgsSuffix<StringValue>(() => scienceData.title));
        }

        public float ScienceValue()
        {
            ScienceSubject subjectByID = ResearchAndDevelopment.GetSubjectByID(scienceData.subjectID);

            return ResearchAndDevelopment.GetScienceValue(scienceData.dataAmount, subjectByID, 1) *
                HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
        }

        public float TransmitValue()
        {
            ScienceSubject subjectByID = ResearchAndDevelopment.GetSubjectByID(scienceData.subjectID);

            // TODO: make sure transmitValue became transmitBonus
            return ResearchAndDevelopment.GetScienceValue(scienceData.dataAmount, subjectByID, scienceData.transmitBonus) *
                HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
        }

        public new string ToString()
        {
            return "SCIENCE DATA";
        }
    }
}


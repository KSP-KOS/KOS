using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceData")]
    public class ScienceDataValue : Structure
    {
        private ScienceData scienceData;
        private global::Part hostPart;

        public ScienceDataValue(ScienceData scienceData, global::Part hostPart)
        {
            this.scienceData = scienceData;
            this.hostPart = hostPart;

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
            
            if (subjectByID != null) // fix return values in sandbox mode
            {
                return ResearchAndDevelopment.GetScienceValue(scienceData.dataAmount, subjectByID, 1) *
                    HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
            }
            else
            {
                return 0;
            }
        }

        public float TransmitValue()
        {
            // By using ExperimentResultDialogPage we ensure that the logic calculating the value is exactly the same
            // as that used KSP's dialog.  The page type doesn't include any UI code itself, it just does the math to
            // confirm the values, and stores some callbacks for the UI to call when buttons are pressed.
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                hostPart, scienceData, scienceData.baseTransmitValue, scienceData.transmitBonus, // the parameters with data we care aboue
                false, "", false, // disable transmit warning and reset option, these are used for the UI only
                new ScienceLabSearch(hostPart.vessel, scienceData), // this is used to calculate the transmit bonus, I think...
                null, null, null, null); // null callbacks, no sense in creating objects when we won't actually perform the callback.
            return page.baseTransmitValue * page.TransmitBonus;
        }

        public new string ToString()
        {
            return "SCIENCE DATA";
        }
    }
}


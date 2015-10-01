﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Execution;

namespace kOS.Suffixed
{
    public class KUniverseValue : Structure
    {
        private SharedObjects shared;
        
        public KUniverseValue(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }
        
        public void InitializeSuffixes()
        {
            AddSuffix("CANREVERT", new Suffix<bool>(CanRevert));
            AddSuffix("CANREVERTTOLAUNCH", new Suffix<bool>(CanRevertToLaunch));
            AddSuffix("CANREVERTTOEDITOR", new Suffix<bool>(CanRevvertToEditor));
            AddSuffix("REVERTTOLAUNCH", new NoArgsSuffix(RevertToLaunch));
            AddSuffix("REVERTTOEDITOR", new NoArgsSuffix(RevertToEditor));
            AddSuffix("REVERTTO", new OneArgsSuffix<string>(RevertTo));
            AddSuffix("ORIGINEDITOR", new Suffix<string>(OriginatingEditor));
        }

        public void RevertToLaunch()
        {
            if (CanRevertToLaunch())
            {
                FlightDriver.RevertToLaunch();
                ((CPU)shared.Cpu).GetCurrentOpcode().AbortProgram = true;
            }
            else throw new KOSCommandInvalidHereException("REVERTTOLAUNCH", "When revert is disabled", "When revert is enabled");
        }

        public void RevertToEditor()
        {
            if (CanRevvertToEditor())
            {
                EditorFacility fac = ShipConstruction.ShipType;
                FlightDriver.RevertToPrelaunch(fac);
                ((CPU)shared.Cpu).GetCurrentOpcode().AbortProgram = true;
            }
            else throw new KOSCommandInvalidHereException("REVERTTOEDITOR", "When revert is disabled", "When revert is enabled");
        }

        public void RevertTo(string editor)
        {
            if (CanRevvertToEditor())
            {
                EditorFacility fac;
                switch (editor.ToUpper())
                {
                    case "VAB":
                        fac = EditorFacility.VAB;
                        break;
                    case "SPH":
                        fac = EditorFacility.SPH;
                        break;
                    default:
                        fac = EditorFacility.None;
                        break;
                }
                FlightDriver.RevertToPrelaunch(fac);
                ((CPU)shared.Cpu).GetCurrentOpcode().AbortProgram = true;
            }
            else throw new KOSCommandInvalidHereException("REVERTTO", "When revert is disabled", "When revert is enabled");
        }

        public bool CanRevert()
        {
            return FlightDriver.CanRevert;
        }

        public bool CanRevertToLaunch()
        {
            return FlightDriver.CanRevertToPostInit && HighLogic.CurrentGame.Parameters.Flight.CanRestart;
        }

        public bool CanRevvertToEditor()
        {
            return FlightDriver.CanRevertToPrelaunch &&
                HighLogic.CurrentGame.Parameters.Flight.CanLeaveToEditor &&
                ShipConstruction.ShipConfig != null;
        }

        public string OriginatingEditor()
        {
            if (ShipConstruction.ShipConfig != null)
            {
                EditorFacility fac = ShipConstruction.ShipType;
                return fac.ToString().ToUpper();
            }
            return "";
        }
    }
}

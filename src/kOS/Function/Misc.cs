using kOS.Execution;
using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Text;
using System.Collections.Generic;
using kOS.Suffixed.PartModuleField;
using kOS.Module;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Encapsulation;
using KSP.UI.Screens;
using kOS.Safe;

namespace kOS.Function
{
    [Function("clearscreen")]
    public class FunctionClearScreen : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            shared.Window.ClearScreen();
        }
    }

    [Function("hudtext")]
    public class FunctionHudText : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool      echo      = Convert.ToBoolean(PopValueAssert(shared));
            RgbaColor rgba      = GetRgba(PopValueAssert(shared));
            int       size      = Convert.ToInt32(PopValueAssert(shared));
            int       style     = Convert.ToInt32(PopValueAssert(shared));
            int       delay     = Convert.ToInt32(PopValueAssert(shared));
            string    textToHud = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            string htmlColour = rgba.ToHexNotation();
            switch (style)
            {
                case 1:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_LEFT);
                    break;

                case 2:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case 3:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_RIGHT);
                    break;

                case 4:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.LOWER_CENTER);
                    break;

                default:
                    ScreenMessages.PostScreenMessage("*" + textToHud, 3f, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
            if (echo)
            {
                shared.Screen.Print("HUD: " + textToHud);
            }
        }
    }

    [Function("stage")]
    public class FunctionStage : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            if (StageManager.CanSeparate && shared.Vessel.isActiveVessel)
            {
                StageManager.ActivateNextStage();
                shared.Cpu.YieldProgram(new YieldFinishedNextTick());
            }
            else if (!StageManager.CanSeparate)
            {
                SafeHouse.Logger.Log("FAIL SILENT: Stage is called before it is ready, Use STAGE:READY to check first if staging rapidly");
            }
            else if (!shared.Vessel.isActiveVessel)
            {
                throw new KOSCommandInvalidHereException(LineCol.Unknown(), "STAGE", "a non-active SHIP, KSP does not support this", "Core is on the active vessel");
            }
        }
    }

    [Function("add")]
    public class FunctionAddNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            node.AddToVessel(shared.Vessel);
        }
    }

    [Function("remove")]
    public class FunctionRemoveNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            node.Remove();
        }
    }

    [Function("warpto")]
    public class WarpTo : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // TODO: As of KSP v1.0.2, the maxTimeWarping and minTimeWarping parameters behave as time limiters, not actual warp limiters
            int args = CountRemainingArgs(shared);
            double ut;
            switch (args)
            {
                case 1:
                    ut = GetDouble(PopValueAssert(shared));
                    break;

                default:
                    throw new KOSArgumentMismatchException(new[] { 1 }, args);
            }
            AssertArgBottomAndConsume(shared);
            TimeWarpValue.Instance.WarpTo(ut);
        }
    }

    [Function("processor")]
    public class FunctionProcessor : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object processorTagOrVolume = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            kOSProcessor processor;

            if (processorTagOrVolume is Volume)
            {
                processor = shared.ProcessorMgr.GetProcessor(processorTagOrVolume as Volume);
            }
            else if (processorTagOrVolume is string || processorTagOrVolume is StringValue)
            {
                processor = shared.ProcessorMgr.GetProcessor(processorTagOrVolume.ToString());
            }
            else
            {
                throw new KOSInvalidArgumentException("processor", "processorId", "String or Volume expected");
            }

            if (processor == null)
            {
                throw new KOSInvalidArgumentException("processor", "processorId", "Processor with that volume or name was not found");
            }

            ReturnValue = PartModuleFieldsFactory.Construct(processor, shared);
        }
    }
}

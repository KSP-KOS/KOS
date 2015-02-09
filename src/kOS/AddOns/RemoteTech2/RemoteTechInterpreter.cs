using System;
using System.Collections.Generic;
using kOS.Safe;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Safe.UserIO;
using kOS.Screen;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechInterpreter : Interpreter, IUpdateObserver
    {
        private readonly List<string> commandQueue = new List<string>();
        private readonly List<string> batchQueue = new List<string>();
        private SubBuffer progressBarSubBuffer;
        private bool BatchMode { get; set; }
        private double waitTotal;
        private double waitElapsed;
        private bool deploymentInProgress;
        private bool deployingBatch;
        private string deploymentMessage;
        private bool signalLossWarning;
        
        public RemoteTechInterpreter(SharedObjects shared) : base(shared)
        {
            Shared.UpdateHandler.AddObserver(this);
            CreateProgressBarSubBuffer();
        }

        private void CreateProgressBarSubBuffer()
        {
            progressBarSubBuffer = new SubBuffer();
            progressBarSubBuffer.SetSize(3, ColumnCount);
            progressBarSubBuffer.Fixed = true;
            progressBarSubBuffer.PositionRow = RowCount - progressBarSubBuffer.RowCount;
            AddSubBuffer(progressBarSubBuffer);

            var separator = new string('-', progressBarSubBuffer.ColumnCount);
            separator.ToCharArray().CopyTo(progressBarSubBuffer.Buffer[0], 0);
        }

        protected override void ProcessCommand(string commandText)
        {
            //TODO: Use the parser to identify and extract the BATCH and DEPLOY commands
            //      The current solution is TEMPORAL and fragile, be careful
            switch (commandText.ToLower().Trim())
            {
                case "batch.":
                    ProcessBatchCommand();
                    break;
                case "deploy.":
                    ProcessDeployCommand();
                    break;
                default:
                    if (BatchMode) batchQueue.Add(commandText);
                    else commandQueue.Add(commandText);
                    break;
            }
        }

        private void ProcessBatchCommand()
        {
            if (BatchMode) throw new Exception("Batch mode is already active.");
            BatchMode = true;
            Print("Starting new batch.");
        }

        private void ProcessDeployCommand()
        {
            if (!BatchMode) throw new Exception("Batch mode is not active.");
            if (batchQueue.Count == 0) throw new Exception("There are no commands to deploy.");

            waitTotal = RemoteTechUtility.GetTotalWaitTime(Shared.Vessel);
            if (double.IsPositiveInfinity(waitTotal)) throw new Exception("No connection available.");
                
            Print("Deploying...");
            BatchMode = false;
            deployingBatch = true;
            StartDeployment();
        }

        private void StartDeployment()
        {
            deploymentInProgress = true;
            progressBarSubBuffer.Enabled = (waitTotal > 0.5);
            deploymentMessage = deployingBatch ? "Deploying batch." : "Deploying command.";
            DrawBars("");
        }

        private void StopDeployment()
        {
            deploymentInProgress = false;
            progressBarSubBuffer.Enabled = false;
            waitElapsed = 0;
        }

        public void Update(double deltaTime)
        {
            if (!deploymentInProgress && commandQueue.Count > 0 && !BatchMode)
            {
                waitTotal = RemoteTechUtility.GetTotalWaitTime(Shared.Vessel);
                StartDeployment();
                deltaTime = 0; // so the elapsed time is zero in this update
            }

            if (deploymentInProgress)
            {
                UpdateDeployment(deltaTime);
            }
        }

        private void UpdateDeployment(double deltaTime)
        {
            if (!RemoteTechHook.Instance.HasAnyConnection(Shared.Vessel.id))
            {
                if (!signalLossWarning)
                {
                    DrawStatus("Signal lost.  Waiting to re-acquire signal.");
                    progressBarSubBuffer.Enabled = true;
                    signalLossWarning = true;
                }
            }
            else
            {
                if (signalLossWarning)
                {
                    if (double.IsPositiveInfinity(waitTotal))
                    {
                        waitTotal = RemoteTechUtility.GetTotalWaitTime(Shared.Vessel);
                    }
                    signalLossWarning = false;
                }

                waitElapsed += System.Math.Min(deltaTime, waitTotal - waitElapsed);
                DrawProgressBar(waitElapsed, waitTotal);

                if (waitElapsed == waitTotal)
                {
                    if (deployingBatch)
                    {
                        // deploy all commands
                        foreach (string commandText in batchQueue)
                        {
                            base.ProcessCommand(commandText);
                        }

                        batchQueue.Clear();
                        deployingBatch = false;
                    }
                    else
                    {
                        // deploy first command
                        if (commandQueue.Count > 0)
                        {
                            string commandText = commandQueue[0];
                            commandQueue.RemoveAt(0);
                            base.ProcessCommand(commandText);
                        }
                    }

                    StopDeployment();
                }
            }
        }

        private void DrawProgressBar(double elapsed, double total)
        {
            if (progressBarSubBuffer.Enabled)
            {
                var bars = (int)((ColumnCount) * elapsed / total);
                var time = new DateTime(TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");
                string statusText = deploymentMessage + new string(' ', ColumnCount - time.Length - deploymentMessage.Length) + time;
                var barsText = new string('|', bars);
                DrawStatus(statusText);
                DrawBars(barsText);
            }
        }

        private void DrawStatus(string status)
        {
            status = status.PadRight(progressBarSubBuffer.ColumnCount);
            status.ToCharArray().CopyTo(progressBarSubBuffer.Buffer[1], 0);
        }

        private void DrawBars(string bars)
        {
            bars = bars.PadRight(progressBarSubBuffer.ColumnCount);
            bars.ToCharArray().CopyTo(progressBarSubBuffer.Buffer[2], 0);
        }

        public override void SpecialKey(char key)
        {
            if (key == (char)UnicodeCommand.BREAK && deploymentInProgress)
            {
                if (deployingBatch) batchQueue.Clear();
                else commandQueue.Clear();
                
                StopDeployment();
            }
            else
            {
                base.SpecialKey(key);
            }
        }
    }
}

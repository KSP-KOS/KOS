using kOS.Communication;
using kOS.Safe;
using kOS.Safe.Screen;
using kOS.Safe.UserIO;
using System;
using System.Collections.Generic;

namespace kOS.Screen
{
    public class ConnectivityInterpreter : Interpreter, IUpdateObserver
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

        public ConnectivityInterpreter(SharedObjects shared) : base(shared)
        {
            Shared.UpdateHandler.AddObserver(this);
            CreateProgressBarSubBuffer(this);
            AddResizeNotifier(CreateProgressBarSubBuffer);
        }

        ~ConnectivityInterpreter()
        {
            // Normally this design pattern would fail because the notifier hooks
            // would be references that prevent orphaning and thus we can't remove
            // them in the destructor.
            // 
            // But in this case it probably can work, because this reference is
            // circularly from the object to *itself* (these hooks are inside this
            // instance itself), and thus probably don't prevent orphaning:
            RemoveAllResizeNotifiers();
        }

        /// <summary>Whenever the terminal resizes, resize the progress bar,</summary>
        /// <param name="sb">The method operates on self (this), and the parameter sb would
        /// be unnecessary if it wasn't required by AddResizeNotifyer().</parm>
        private int CreateProgressBarSubBuffer(IScreenBuffer sb)
        {
            if (progressBarSubBuffer == null)
            {
                progressBarSubBuffer = new SubBuffer();
                progressBarSubBuffer.WillTruncate = true;
                AddSubBuffer(progressBarSubBuffer);
            }
            progressBarSubBuffer.SetSize(3, ColumnCount);
            progressBarSubBuffer.Fixed = true;
            progressBarSubBuffer.PositionRow = RowCount - progressBarSubBuffer.RowCount;
            var separator = new string('-', progressBarSubBuffer.ColumnCount);
            progressBarSubBuffer.Buffer[0].ArrayCopyFrom(separator.ToCharArray(), 0, 0);
            return 0;
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

            waitTotal = ConnectivityManager.GetDelayToControl(Shared.Vessel);
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

        public void KOSUpdate(double deltaTime)
        {
            if (!deploymentInProgress && commandQueue.Count > 0 && !BatchMode)
            {
                waitTotal = ConnectivityManager.GetDelayToControl(Shared.Vessel);
                StartDeployment();
                deltaTime = 0; // so the elapsed time is zero in this update
            }
            else if (deploymentInProgress)
            {
                UpdateDeployment(deltaTime);
            }
        }

        private void UpdateDeployment(double deltaTime)
        {
            if (!ConnectivityManager.HasConnectionToControl(Shared.Vessel))
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
                        waitTotal = ConnectivityManager.GetDelayToControl(Shared.Vessel);
                    }
                    signalLossWarning = false;
                }

                waitElapsed += Math.Min(deltaTime, waitTotal - waitElapsed);

                if (waitElapsed >= waitTotal)
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
                else
                {
                    DrawProgressBar(waitElapsed, waitTotal);
                }
            }
        }

        private void DrawProgressBar(double elapsed, double total)
        {
            if (progressBarSubBuffer.Enabled)
            {
                var bars = Math.Max((int)((ColumnCount) * elapsed / total), 0);
                var time = new DateTime(System.TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");
                string statusText = deploymentMessage + new string(' ', ColumnCount - time.Length - deploymentMessage.Length) + time;
                var barsText = new string('|', bars);
                DrawStatus(statusText);
                DrawBars(barsText);
            }
        }

        private void DrawStatus(string status)
        {
            status = status.PadRight(progressBarSubBuffer.ColumnCount);
            progressBarSubBuffer.Buffer[1].ArrayCopyFrom(status.ToCharArray(), 0, 0);
        }

        private void DrawBars(string bars)
        {
            bars = bars.PadRight(progressBarSubBuffer.ColumnCount);
            progressBarSubBuffer.Buffer[2].ArrayCopyFrom(bars.ToCharArray(), 0, 0);
        }

        public override bool SpecialKey(char key)
        {
            if (key == (char)UnicodeCommand.BREAK && deploymentInProgress)
            {
                if (deployingBatch) batchQueue.Clear();
                else commandQueue.Clear();

                StopDeployment();
                return true;
            }
            else
            {
                return base.SpecialKey(key);
            }
        }

        public void Dispose()
        {
            Shared.UpdateHandler.RemoveObserver(this);
        }
    }
}
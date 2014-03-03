using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;

namespace kOS.AddOns.RemoteTech2
{
    // TODO
    // - Reserve the last three/four rows for the progress bar
    // - Disable scroll while deploying or keep the progress bar in a separated buffer
    // - Override the break program command (ctrl+c) to cancel the current deployment
    public class RemoteTechInterpreter : Interpreter, IUpdateObserver
    {
        private List<string> _commandQueue = new List<string>();
        private List<string> _batchQueue = new List<string>();
        private bool _batchMode { get; set; }
        private double _waitTotal = 0;
        private double _waitElapsed = 0;
        private bool _deploymentInProgress = false;
        private bool _deployingBatch = false;
        private bool _signalLossWarning = false;

        
        public RemoteTechInterpreter(SharedObjects shared) : base(shared)
        {
            _shared.UpdateHandler.AddObserver(this);
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
                    if (_batchMode) _batchQueue.Add(commandText);
                    else _commandQueue.Add(commandText);
                    break;
            }
        }

        private void ProcessBatchCommand()
        {
            if (_batchMode) throw new Exception("Batch mode is already active.");
            _batchMode = true;
            Print("Starting new batch.");
        }

        private void ProcessDeployCommand()
        {
            if (!_batchMode) throw new Exception("Batch mode is not active.");
            if (_batchQueue.Count == 0) throw new Exception("There are no commands to deploy.");

            if (RemoteTechHook.Instance != null)
            {
                _waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(_shared.Vessel.id);
                if (double.IsPositiveInfinity(_waitTotal)) throw new Exception("No connection available.");
                
                Print("Deploying...");
                _batchMode = false;
                _deployingBatch = true;
                _deploymentInProgress = true;
            }
        }

        public void Update(double deltaTime)
        {
            if (!_deploymentInProgress && _commandQueue.Count > 0 && !_batchMode)
            {
                _waitTotal = RemoteTechUtility.GetTotalWaitTime(_shared.Vessel);
                _deploymentInProgress = true;
                deltaTime = 0; // so the elapsed time is zero in this update
            }

            if (_deploymentInProgress)
            {
                UpdateDeployment(deltaTime);
            }
        }

        private void UpdateDeployment(double deltaTime)
        {
            if (RemoteTechHook.Instance != null && !RemoteTechHook.Instance.HasAnyConnection(_shared.Vessel.id))
            {
                if (!_signalLossWarning)
                {
                    // TODO: show this next to the progress bar (and erase it when the signal is reacquired)
                    Print("Signal lost.  Waiting to re-acquire signal.");
                    _signalLossWarning = true;
                }
            }
            else
            {
                _signalLossWarning = false;
                _waitElapsed += Math.Min(deltaTime, _waitTotal - _waitElapsed);
                
                string deploymentMessage = _deployingBatch ? "Deploying batch." : "Deploying command.";
                DrawProgressBar(_waitElapsed, _waitTotal, deploymentMessage);

                if (_waitElapsed == _waitTotal)
                {
                    if (_deployingBatch)
                    {
                        // deploy all commands
                        foreach (string commandText in _batchQueue)
                        {
                            base.ProcessCommand(commandText);
                        }

                        _batchQueue.Clear();
                        _deployingBatch = false;
                    }
                    else
                    {
                        // deploy first command
                        if (_commandQueue.Count > 0)
                        {
                            string commandText = _commandQueue[0];
                            _commandQueue.RemoveAt(0);
                            base.ProcessCommand(commandText);
                        }
                    }

                    _deploymentInProgress = false;
                    _waitElapsed = 0;
                    // TODO: erase the progress bar
                }
            }
        }

        private void DrawProgressBar(double elapsed, double total, string text)
        {
            var bars = (int)((ColumnCount) * elapsed / total);
            var time = new DateTime(TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");

            string captionText = text + new string(' ', ColumnCount - time.Length - text.Length) + time;
            string barsText = new string('|', bars).PadRight(ColumnCount);
            PrintAt(captionText, RowCount - 3, 0);
            PrintAt(barsText, RowCount - 2, 0);
        }
    }
}

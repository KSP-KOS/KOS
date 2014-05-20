﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;
using kOS.Utilities;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechInterpreter : Interpreter, IUpdateObserver
    {
        private List<string> _commandQueue = new List<string>();
        private List<string> _batchQueue = new List<string>();
        private SubBuffer _progressBarSubBuffer;
        private bool _batchMode { get; set; }
        private double _waitTotal = 0;
        private double _waitElapsed = 0;
        private bool _deploymentInProgress = false;
        private bool _deployingBatch = false;
        private string _deploymentMessage;
        private bool _signalLossWarning = false;
        
        public RemoteTechInterpreter(SharedObjects shared) : base(shared)
        {
            _shared.UpdateHandler.AddObserver(this);
            CreateProgressBarSubBuffer();
        }

        private void CreateProgressBarSubBuffer()
        {
            _progressBarSubBuffer = new SubBuffer();
            _progressBarSubBuffer.SetSize(3, ColumnCount);
            _progressBarSubBuffer.Fixed = true;
            _progressBarSubBuffer.PositionRow = RowCount - _progressBarSubBuffer.RowCount;
            AddSubBuffer(_progressBarSubBuffer);

            string separator = new string('-', _progressBarSubBuffer.ColumnCount);
            separator.ToCharArray().CopyTo(_progressBarSubBuffer.Buffer[0], 0);
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

            _waitTotal = RemoteTechUtility.GetTotalWaitTime(_shared.Vessel);
            if (double.IsPositiveInfinity(_waitTotal)) throw new Exception("No connection available.");
                
            Print("Deploying...");
            _batchMode = false;
            _deployingBatch = true;
            StartDeployment();
        }

        private void StartDeployment()
        {
            _deploymentInProgress = true;
            _progressBarSubBuffer.Enabled = (_waitTotal > 0.5);
            _deploymentMessage = _deployingBatch ? "Deploying batch." : "Deploying command.";
            DrawBars("");
        }

        private void StopDeployment()
        {
            _deploymentInProgress = false;
            _progressBarSubBuffer.Enabled = false;
            _waitElapsed = 0;
        }

        public void Update(double deltaTime)
        {
            if (!_deploymentInProgress && _commandQueue.Count > 0 && !_batchMode)
            {
                _waitTotal = RemoteTechUtility.GetTotalWaitTime(_shared.Vessel);
                StartDeployment();
                deltaTime = 0; // so the elapsed time is zero in this update
            }

            if (_deploymentInProgress)
            {
                UpdateDeployment(deltaTime);
            }
        }

        private void UpdateDeployment(double deltaTime)
        {
            if (!RemoteTechHook.Instance.HasAnyConnection(_shared.Vessel.id))
            {
                if (!_signalLossWarning)
                {
                    DrawStatus("Signal lost.  Waiting to re-acquire signal.");
                    _progressBarSubBuffer.Enabled = true;
                    _signalLossWarning = true;
                }
            }
            else
            {
                if (_signalLossWarning)
                {
                    if (double.IsPositiveInfinity(_waitTotal))
                    {
                        _waitTotal = RemoteTechUtility.GetTotalWaitTime(_shared.Vessel);
                    }
                    _signalLossWarning = false;
                }

                _waitElapsed += Math.Min(deltaTime, _waitTotal - _waitElapsed);
                DrawProgressBar(_waitElapsed, _waitTotal);

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

                    StopDeployment();
                }
            }
        }

        private void DrawProgressBar(double elapsed, double total)
        {
            if (_progressBarSubBuffer.Enabled)
            {
                var bars = (int)((ColumnCount) * elapsed / total);
                var time = new DateTime(TimeSpan.FromSeconds(total - elapsed + 0.5).Ticks).ToString("H:mm:ss");
                string statusText = _deploymentMessage + new string(' ', ColumnCount - time.Length - _deploymentMessage.Length) + time;
                string barsText = new string('|', bars);
                DrawStatus(statusText);
                DrawBars(barsText);
            }
        }

        private void DrawStatus(string status)
        {
            status = status.PadRight(_progressBarSubBuffer.ColumnCount);
            status.ToCharArray().CopyTo(_progressBarSubBuffer.Buffer[1], 0);
        }

        private void DrawBars(string bars)
        {
            bars = bars.PadRight(_progressBarSubBuffer.ColumnCount);
            bars.ToCharArray().CopyTo(_progressBarSubBuffer.Buffer[2], 0);
        }

        public override void SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK && _deploymentInProgress)
            {
                if (_deployingBatch) _batchQueue.Clear();
                else _commandQueue.Clear();
                
                StopDeployment();
            }
            else
            {
                base.SpecialKey(key);
            }
        }
    }
}

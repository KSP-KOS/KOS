using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Control;
using kOS.Module;

namespace kOS.Module
{
    /// <summary>
    /// Tracks the on-screen messages from the autopilot, and their cooldown timers.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class AutopilotMsgManager : MonoBehaviour
    {
        public static AutopilotMsgManager Instance { get; private set; }

        /// <summary>Timestamp (using Unity3d's Time.time) for when the suppression message cooldown is over and a new message should be emitted.</summary>
        private static float suppressMessageCooldownEnd = 0;
        private static string suppressMessageText = "kOS's config setting is preventing kOS control.";
        private static ScreenMessage suppressMessage;
        /// <summary>If true then the 'suppressing autopilot' message should be getting repeated to the screen right now.</summary>
        private HashSet<kOSVesselModule> vesselsAskingForSuppressMsg;

        /// <summary>Timestamp (using Unity3d's Time.time) for when the SAS message cooldown is over and a new message should be emitted.</summary>
        private static float sasMessageCooldownEnd = 0;
        /// <summary>Tracks a small window of time in which the message won't appear if the problem is fixed before then.</summary>
        private static float sasMessageGracePeriodEnd = 0;
        private static string sasMessageText = "kOS: SAS and lock steering fight. Please turn one of them off.";
        private static ScreenMessage sasMessage;
        /// <summary>If true then the 'SAS conflict' message should be getting repeated to the screen right now.</summary>
        private HashSet<kOSVesselModule> vesselsAskingForSasMsg;

        public void Awake()
        {
            vesselsAskingForSuppressMsg = new HashSet<kOSVesselModule>();
            vesselsAskingForSasMsg = new HashSet<kOSVesselModule>();
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
        }

        public void Start()
        {
        }

        public void TurnOnSuppressMessage(kOSVesselModule requestor)
        {
            vesselsAskingForSuppressMsg.Add(requestor);
        }

        public void TurnOffSuppressMessage(kOSVesselModule requestor)
        {
            vesselsAskingForSuppressMsg.Remove(requestor);
        }

        public void TurnOnSasMessage(kOSVesselModule requestor)
        {
            vesselsAskingForSasMsg.Add(requestor);
        }

        public void TurnOffSasMessage(kOSVesselModule requestor)
        {
            vesselsAskingForSasMsg.Remove(requestor);
        }

        public void Update()
        {
            // Handle the message and timeout of the message
            if (vesselsAskingForSuppressMsg.Count > 0)
            {
                if (Time.time > suppressMessageCooldownEnd)
                {
                    suppressMessageCooldownEnd = Time.time + 5f;
                    suppressMessage = ScreenMessages.PostScreenMessage(
                        string.Format("<color=white><size=20>{0}</size></color>", suppressMessageText),
                        4, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                suppressMessageCooldownEnd = 0f;
                if (suppressMessage != null)
                {
                    // Get it to stop right away even if the timer isn't over:
                    suppressMessage.duration = 0;
                }
            }

            // Handle the message and timeout of the message
            if (vesselsAskingForSasMsg.Count > 0)
            {
                if (Time.time > sasMessageCooldownEnd && Time.time > sasMessageGracePeriodEnd)
                {
                    sasMessageCooldownEnd = Time.time + 5f;
                    sasMessage = ScreenMessages.PostScreenMessage(
                        string.Format("<color=white><size=20>{0}</size></color>", sasMessageText),
                        4, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                sasMessageCooldownEnd = 0f;
                // If ShowSasMessage becomes true, this will be left as
                // whatever 1 second past the last time it was false was:
                sasMessageGracePeriodEnd = Time.time + 1f;
                if (sasMessage != null)
                {
                    // Get it to stop right away even if the timer isn't over:
                    sasMessage.duration = 0;
                }
            }
        }
    }
}

using kOS.Safe.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace kOS.Module
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class kOSSettingsChecker : MonoBehaviour
    {
        private static Queue<MultiOptionDialog> dialogsToSpawn = new Queue<MultiOptionDialog>();
        private static bool dialogShown = false;

        public void Start()
        {
            CheckSettings();
        }

        private void CheckSettings(Game game)
        {
            CheckSettings();
        }

        private void CheckSettings(ConfigNode node)
        {
            CheckSettings();
        }

        private void CheckSettings()
        {
            SafeHouse.Logger.SuperVerbose("kOSSettingsChecker.CheckSettings()");
            HighLogic.CurrentGame.Parameters.CustomParams<kOSCustomParameters>().CheckMigrateSettings();
            Destroy(this);
        }

        // Because rapidly showing dialogs can prevent some from being hidden, we can just queue up
        // any dialogs that we want to show.  This also ensures that the first dialog displayed is
        // guaranteed to be the first one queued.
        public static void QueueDialog(MultiOptionDialog dialog)
        {
            if (dialogShown)
            {
                dialogsToSpawn.Enqueue(dialog);
            }
            else
            {
                ShowDialog(dialog);
            }
        }

        private static void ShowDialog(MultiOptionDialog dialog)
        {
            dialogShown = true;
            var popup = PopupDialog.SpawnPopupDialog(dialog, true, HighLogic.UISkin);
            popup.onDestroy.AddListener(new UnityEngine.Events.UnityAction(OnDialogDestroy));
        }

        private static void OnDialogDestroy()
        {
            dialogShown = false;
            if (dialogsToSpawn.Count > 0)
            {
                var dialog = dialogsToSpawn.Dequeue();
                ShowDialog(dialog);
            }
        }
    }
}
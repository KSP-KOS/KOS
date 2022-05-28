using kOS.Communication;
using kOS.Safe.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace kOS.Module
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class kOSSettingsChecker : MonoBehaviour
    {
        private static Queue<MultiOptionDialogWithAnchor> dialogsToSpawn = new Queue<MultiOptionDialogWithAnchor>();
        private static bool dialogShown = false;

        public void Start()
        {
            DontDestroyOnLoad(this);
            CheckSettings();
        }

        private void CheckSettings()
        {
            SafeHouse.Logger.SuperVerbose("kOSSettingsChecker.CheckSettings()");
            HighLogic.CurrentGame.Parameters.CustomParams<kOSCustomParameters>().CheckMigrateSettings();
            HighLogic.CurrentGame.Parameters.CustomParams<kOSConnectivityParameters>().CheckNewManagers();
        }

        // Because rapidly showing dialogs can prevent some from being shown, we can just queue up
        // any dialogs that we want to show.  This also ensures that the first dialog displayed is
        // guaranteed to be the first one queued.
        public static void QueueDialog(float xAnchor, float yAnchor, MultiOptionDialog dialog)
        {
            if (dialogShown)
            {
                dialogsToSpawn.Enqueue(new MultiOptionDialogWithAnchor() { dialog = dialog, anchor = new Vector2(xAnchor, yAnchor) });
            }
            else
            {
                ShowDialog(new MultiOptionDialogWithAnchor() { dialog = dialog, anchor = new Vector2(xAnchor, yAnchor) });
            }
        }

        private static void ShowDialog(MultiOptionDialogWithAnchor dialog)
        {
            dialogShown = true;
            var popup = PopupDialog.SpawnPopupDialog(dialog.anchor, dialog.anchor, dialog.dialog, true, HighLogic.UISkin);
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
            else
            {
                // Fire the OnGameSettingsApplied event because the dialog windows
                // potentially changed settings.  If another mod or addon is relying
                // on those settings, it should be signaled to refresh the values.
                GameEvents.OnGameSettingsApplied.Fire();
            }
        }

        private struct MultiOptionDialogWithAnchor
        {
            public MultiOptionDialog dialog;

            // Passed into KSP's dialog call directly:
            // KSP's standard dialog box maker allows you to pass in
            // settings for AnchorMax and AnchorMin, which as far as
            // I can tell from experimentation, seems to be coords
            // relative to the size of the dialog box itself.  So
            // that (1.5f) for "X coord" means "150% of the width it
            // took to draw the box".  Also, positive is to the lower-
            // left and negative is to the upper-right, for some
            // reason I don't understand.  Setting Min and Max to the
            // same numbers works, while setting them to different
            // numbers starts doing random things I don't understand.
            // Therefore we will just pass in the same value for mins
            // and maxes when using this.  There is a chance this system
            // actually does make sense, but it's undocumented what these
            // numbers were meant to represent, so it's hard by trial
            // and error to make sense of it:
            public Vector2 anchor;
        }
    }
}

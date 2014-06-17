using System.Collections.Generic;
using UnityEngine;

namespace kOS.Screen
{
    public delegate void DialogAction( KOSTextEditPopup editor ); // a callback to invoke upon a button press.
    
    /// <summary>
    /// A GUI widget that will only appear when the text editor is asking
    /// for a confirmation.  Pass it a list of options to display and a list
    /// of actions (delegate methods) to perform corresponding to them.
    /// Until one of the options is picked, the editor will be frozen.
    /// </summary>
    /// This was MEANT to be a nested class inside KOSTextEditPopup, but Unity can't
    /// handle a MonoBehaviour class nested inside a MonoBehaviour class.  It wants them
    /// all flat and globally accessible
    public class DelegateDialog : MonoBehaviour
    {
        private KOSTextEditPopup   parent;
        private string             message = "";
        private List<string>       options;
        private List<DialogAction> actions;
        private bool               invoked;
        
        
        public void Invoke( KOSTextEditPopup parent, string message, List<string> options, List<DialogAction> actions )
        {
            this.parent = parent;
            this.parent.Freeze(true);
            this.message = message;
            this.options = options;
            this.actions = actions;
            invoked = true;
        }
        
        public void OnGUI()
        {
            if (invoked)
            {
                float guessWidth = GUI.skin.label.CalcSize( new GUIContent(message) ).x;
                GUILayout.Window( 101, new Rect( parent.GetRect().xMin+10,
                                                 parent.GetRect().yMin+10,
                                                 guessWidth,
                                                 0) , DrawConfirm, "Confirm", GUILayout.ExpandWidth(true) );
            }
        }
        
        public void DrawConfirm( int windowID )
        {
            if (invoked)
            {
                GUILayout.Label( message );
                for (int bNum = 0 ; bNum < options.Count ; bNum++)
                {
                    if (GUILayout.Button( options[bNum]))
                    {
                        actions[bNum](parent);
                        invoked = false;
                        parent.Freeze(false);
                        GUI.FocusWindow(parent.WindowID);
                    }
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS;
using kOS.Persistence;

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
        private KOSTextEditPopup   _parent = null;
        private string             _message = "";
        private List<string>       _options;
        private List<DialogAction> _actions;
        private bool               _invoked = false;
        
        
        public void Invoke( KOSTextEditPopup parent, string message, List<string> options, List<DialogAction> actions )
        {
            _parent = parent;
            _parent.Freeze(true);
            _message = message;
            _options = options;
            _actions = actions;
            _invoked = true;
        }
        
        public void OnGUI()
        {
            if (_invoked)
            {
                float guessWidth = GUI.skin.label.CalcSize( new GUIContent(_message) ).x;
                GUILayout.Window( 101, new Rect( _parent.GetRect().xMin+10,
                                                 _parent.GetRect().yMin+10,
                                                 guessWidth,
                                                 0) , DrawConfirm, "Confirm", GUILayout.ExpandWidth(true) );
            }
        }
        
        public void DrawConfirm( int windowID )
        {
            if (_invoked)
            {
                GUILayout.Label( _message );
                for (int bNum = 0 ; bNum < _options.Count ; bNum++)
                {
                    if (GUILayout.Button( _options[bNum]))
                    {
                        _actions[bNum](_parent);
                        _invoked = false;
                        _parent.Freeze(false);
                        GUI.FocusWindow(_parent.windowID);
                    }
                }
            }
        }
    }
}
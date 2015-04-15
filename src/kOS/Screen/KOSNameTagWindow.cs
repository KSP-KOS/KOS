using kOS.Utilities;
using UnityEngine;
using kOS.Module;
using System;

namespace kOS.Screen
{    
    public class KOSNameTagWindow : MonoBehaviour
    {
        private KOSNameTag attachedModule;
        private Rect windowRect;
        private string tagValue;
        // ReSharper disable RedundantDefaultFieldInitializer
        private bool wasFocusedOnce = false; // "explicit", not "redundant".
        private int numberOfRepaints = 0; // "explicit", not "redundant".
        private bool gameEventHooksExist = false; // "explicit", not "redundant".
        private int myWindowId; // must be unique for Unity to not mash two nametag windows togehter.
        
        // ReSharper enable RedundantDefaultFieldInitializer

        public void Invoke(KOSNameTag module, string oldValue)
        {
            Console.WriteLine("eraseme: KOsNameTagWindow (instance="+GetInstanceID()+") .Invoke() called with module = " + module.GetInstanceID() );
            attachedModule = module;
            tagValue = oldValue;
            myWindowId = GetInstanceID(); // Use the Id of this MonoBehaviour to guarantee unique window ID.
            
            Vector3 screenPos = GetViewportPosFor(attachedModule.part.transform.position);

            // screenPos is in coords from 0 to 1, 0 to 1, not screen pixel coords.
            // Transform it to pixel coords:
            float xPixelPoint = screenPos.x * UnityEngine.Screen.width;
            float yPixelPoint = (1-screenPos.y) * UnityEngine.Screen.height;
            const float WINDOW_WIDTH = 200;

            // windowRect = new Rect(xPixelWindow, yPixelPoint, windowWidth, 130);
            windowRect = new Rect(xPixelPoint, yPixelPoint, WINDOW_WIDTH, 130);

            // Please don't delete these.  They're not being used, but that's because we haven't
            // finished prettying up the interface with the tag line and so the coords aren't
            // being made use of yet.  But keep this in the code so I can remember how I did the math:
            // --------------------------------------------------------------------------------------
            // bool drawOnLeft = (screenPos.x > 0.5f);
            // float xPixelWindow = (drawOnLeft ? screenPos.x - 0.3f : screenPos.x + 0.2f) * UnityEngine.Screen.width;
            // float tagLineWidth = (drawOnLeft) ? (xPixelPoint - xPixelWindow) : (xPixelWindow - xPixelPoint - windowWidth);
            // tagLineRect = new Rect(xPixelPoint, yPixelPoint, tagLineWidth, 3);
            // SafeHouse.Logger.Log("tagLineRect = " + tagLineRect );

            SetEnabled(true);

            if (HighLogic.LoadedSceneIsEditor)
                attachedModule.part.SetHighlight(false, false);
            
        }
        
        /// <summary>
        /// Catch the event of the part disappearing, from crashing or
        /// from unloading from distance or scene change, and ensure
        /// the window closes if it was open when that happens:
        /// </summary>
        /// <param name="whichPartWentAway">The callback is called for EVERY part
        /// that ever goes away, so we have to check if it's the right one</param>
        public void GoAwayEventCallback(Part whichPartWentAway)
        {
            if (whichPartWentAway != attachedModule.part)
                return;
            
            Close();
        }

        /// <summary>
        /// If you try to set a Unity.Behaviour.enabled to false when it already IS false,
        /// and Unity hasn't fully finished configuring the MonoBehaviour yet, the Property's
        /// "set" code throws a null ref error. How lame is that?
        /// That's why I wrapped every attempt to set enabled's value with this check, because KSP
        /// tries running my hooks in this class before Unity's ready for them.
        /// </summary>
        private void SetEnabled(bool newVal)
        {
            // ReSharper disable once RedundantCheckBeforeAssignment
            if (newVal != enabled)
                enabled = newVal;

            if (enabled)
            {
                if (! gameEventHooksExist)
                {
                    GameEvents.onPartDestroyed.Add(GoAwayEventCallback);
                    GameEvents.onPartDie.Add(GoAwayEventCallback);
                    gameEventHooksExist = true;
                }
            }
            else
            {
                if (gameEventHooksExist)
                {
                    GameEvents.onPartDestroyed.Remove(GoAwayEventCallback);
                    GameEvents.onPartDie.Remove(GoAwayEventCallback);                
                    gameEventHooksExist = false;                    
                }
            }
        }

        /// <summary>
        /// Get the position in screen coordinates that the 3d world coordinates
        /// project onto, abstracting the two different ways KSP has to access
        /// the camera depending on view mode.
        /// Returned coords are in a system where the screen viewport goes from
        /// (0,0) to (1,1) and the Z coord is how far from the screen it is
        /// (-Z means behind you).
        /// </summary>
        private Vector3 GetViewportPosFor( Vector3 v )
        {
            return Utils.GetCurrentCamera().WorldToViewportPoint(v);
        }
        
        public void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                ++numberOfRepaints;
            
            if (! enabled)
                return;
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.Lock(false, false, false, "KOSNameTagLock");

            GUI.skin = HighLogic.Skin;
            GUILayout.Window(myWindowId, windowRect, DrawWindow,"KOS nametag");
            
            // Ensure that the first time the window is made, it gets keybaord focus,
            // but allow the focus to leave the window after that:
            // The reason for the "number of repaints" check is that OnGUI has to run
            // through several initial passes before all the components are present,
            // and if you call FocusControl on the first few passes, it has no effect.
            if (numberOfRepaints >= 2 && ! wasFocusedOnce)
            {
                GUI.FocusControl("NameTagField");
                wasFocusedOnce = true;
            }
        }

        public void DrawWindow( int windowID )
        {
            if (! enabled)
                return;
            
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return ||
                    e.keyCode == KeyCode.KeypadEnter)
                {
                    e.Use();
                    attachedModule.TypingDone(tagValue);
                    Close();
                }
            }
            GUILayout.Label(attachedModule.part.name);
            GUI.SetNextControlName("NameTagField");
            tagValue = GUILayout.TextField( tagValue, GUILayout.MinWidth(160f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                e.Use();
                Close();
            }
            if (GUILayout.Button("Accept"))
            {
                e.Use();
                attachedModule.TypingDone(tagValue);
                Close();
            }
            GUILayout.EndHorizontal();

            // Before going any further, suppress any remaining unprocessed clicks
            // so they don't end up causing the editor to detach parts:
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag)
                e.Use();
        }
        
        public void Close()
        {
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.Unlock("KOSNameTagLock");
            
            SetEnabled(false);

            if (HighLogic.LoadedSceneIsEditor)
                attachedModule.part.SetHighlight(false, false);
        }
        
        public void OnDestroy()
        {
            SetEnabled(false);
        }
    }
}
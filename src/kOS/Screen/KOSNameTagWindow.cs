using System.Collections.Generic;
using UnityEngine;
using kOS.Module;

namespace kOS.Screen
{    
    public class KOSNameTagWindow : MonoBehaviour
    {
        private KOSNameTag attachedModule;
        private Rect windowRect;
        private string tagValue;

        public void Invoke(KOSNameTag module, string oldValue)
        {
            attachedModule = module;
            tagValue = oldValue;
            
            Vector3 screenPos = GetViewportPosFor(this.attachedModule.part.transform.position);

            // screenPos is in coords from 0 to 1, 0 to 1, not screen pixel coords.
            // Transform it to pixel coords:
            float xPixel = screenPos.x * UnityEngine.Screen.width;
            float yPixel = (1-screenPos.y) * UnityEngine.Screen.height;
            
            windowRect = new Rect(xPixel-60, yPixel-20, 120,40);
            enabled = true;
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
            Camera cam = MapView.MapIsEnabled ? 
                MapView.MapCamera.camera : 
                FlightCamera.fetch.mainCamera;
            return cam.WorldToViewportPoint( v );
        }
        
        public void OnGUI()
        {
            if (! enabled)
                return;
            GUILayout.Window((int) attachedModule.part.uid, windowRect, DrawWindow,"Name this " + attachedModule.part.name, GUILayout.Width(100));
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
                }
            }

            tagValue = GUILayout.TextField( tagValue, GUILayout.MinWidth(150f));
        }
        
        public void Close()
        {
            enabled = false;
        }
    }
}
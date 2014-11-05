using kOS.Utilities;
using UnityEngine;
using kOS.Module;

namespace kOS.Screen
{    
    public class KOSNameTagWindow : MonoBehaviour
    {
        private KOSNameTag attachedModule;
        private Rect windowRect;
        private Rect tagLineRect;
        private string tagValue;

        public void Invoke(KOSNameTag module, string oldValue)
        {
            attachedModule = module;
            tagValue = oldValue;
            
            Vector3 screenPos = GetViewportPosFor(attachedModule.part.transform.position);

            // screenPos is in coords from 0 to 1, 0 to 1, not screen pixel coords.
            // Transform it to pixel coords:
            float xPixelPoint = screenPos.x * UnityEngine.Screen.width;
            float yPixelPoint = (1-screenPos.y) * UnityEngine.Screen.height;
            float windowWidth = 200;

            // windowRect = new Rect(xPixelWindow, yPixelPoint, windowWidth, 130);
            windowRect = new Rect(xPixelPoint, yPixelPoint, windowWidth, 130);

            // Please don't delete these.  They're not being used, but that's because we haven't
            // finished prettying up the interface with the tag line and so the coords aren't
            // being made use of yet.  But keep this in the code so I can remember how I did the math:
            // --------------------------------------------------------------------------------------
            // bool drawOnLeft = (screenPos.x > 0.5f);
            // float xPixelWindow = (drawOnLeft ? screenPos.x - 0.3f : screenPos.x + 0.2f) * UnityEngine.Screen.width;
            // float tagLineWidth = (drawOnLeft) ? (xPixelPoint - xPixelWindow) : (xPixelWindow - xPixelPoint - windowWidth);
            // tagLineRect = new Rect(xPixelPoint, yPixelPoint, tagLineWidth, 3);
            // Debug.Log("tagLineRect = " + tagLineRect );

            enabled = true;

            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.SetHighlightRecursive(false, attachedModule.part);
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
            if (! enabled)
                return;
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.Lock(false, false, false, "KOSNameTagLock");
            GUILayout.Window(0, windowRect, DrawWindow,"KOS nametag");
        }
        
        public void DrawTagLineWindow( int windowID )
        {
            if (! enabled)
                return;
           // The window is just an empty line with no style.
            GUI.skin = HighLogic.Skin;
            GUI.Box( new Rect(0,0,tagLineRect.width-5,2), "" );
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
            GUI.skin = HighLogic.Skin;
            GUILayout.Label(attachedModule.part.name);
            tagValue = GUILayout.TextField( tagValue, GUILayout.MinWidth(160f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            if (GUILayout.Button("Accept"))
            {
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
            enabled = false;
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.SetHighlightRecursive(false, attachedModule.part);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Persistence;

namespace kOS.Screen
{
    /// <summary>
    /// kOSManagedWindow is for any Unity Monobehavior that you'd like to
    /// have contain a GUI.Window, and you need kOS to keep track of the
    /// window stacking for which click is on top of which other click.
    /// Unity's built in systems for this don't work well at all, so
    /// we had to make up our own.
    /// </summary>
    public abstract class KOSManagedWindow : MonoBehaviour
    {
        // The staic values are for the way the windows keep track of each other:
        
        // Give each instance of TermWindow a unique ID block to ensure it can create
        // Unity windows that don't clash:
        protected static int termWindowIDRange = 215300; // I literally just mashed the keyboard to get a unique number.
        protected static int windowsMadeSoFar = 0;
        // Keep track of the stacking order of the Terminal windows relative to each other.
        // surprisingly, the Unity GUi doesn't do this automatically itself:
        protected static List<KOSManagedWindow> depthSort = new List<KOSManagedWindow>();

        protected static int dragTolerance = 2; // mouse pixel movement of this or less counts as a click and not a drag.
        

        protected Rect windowRect;

        protected int uniqueId; // For the window attached to this widget.

        /// subclasses of KOSManagedWindow can use these to see the mouse position
        /// by full screen coords, or relative to their windowRect:
        protected Vector2 mousePosAbsolute;
        protected Vector2 mousePosRelative;

        /// When doing a mousedown, and mouseup to form a click, this is the
        /// position of the most recent mousedown:
        protected Vector2 mouseButtonDownPosAbsolute;
        protected Vector2 mouseButtonDownPosRelative;

        protected bool isOpen = false;

        public KOSManagedWindow()
        {
            // mult by 50 so there's a range for future expansion for other GUI objects inside the window:
            uniqueId = termWindowIDRange + (windowsMadeSoFar * 50);
            ++windowsMadeSoFar;            
        }
        
        /// <summary>
        /// Implement this for how to make your widget get the keyboard focus:
        /// </summary>
        public abstract void GetFocus();

        /// <summary>
        /// Implement this for how to make your widget give up the keyboard focus:
        /// </summary>
        public abstract void LoseFocus();
        
        /// <summary>
        /// Implement this to make the window appear when it wasn't there before.
        /// be sure to also call the base.Open() as well, because it has
        /// important logic in it.
        /// </summary>
        public virtual void Open()
        {
            isOpen = true;
            BringToFront();
            GetFocus();
            
            // I was having trouble before with this.  If it crops up again, I want
            // to re-enable this line:
            // DumpDepthSort();
        }
        
        /// <summary>
        /// Implement this to make the window disappear when it was there before:
        /// be sure to also call the base.Close() as well, because it has
        /// important logic in it.
        /// </summary>
        public virtual void Close()
        {
            isOpen = false;
            depthSort.Remove(this);
            LoseFocus();

            // I was having trouble before with this.  If it crops up again, I want
            // to re-enable this line:
            // DumpDepthSort();
        }
        
        public virtual bool IsOpen()
        {
            return isOpen;
        }

        public void DumpDepthSort() // exists purely for debugging.
        {
            for (int i=0 ; i< depthSort.Count ; ++i)
            {
                KOSManagedWindow w = depthSort[i];
            }
        }
        
        /// <summary>
        /// Pass in the absolute GUI screen location of a mouse click to decide whether
        /// or not this widget gets keyboard focus because of that click.
        /// (Clicking outside the window takes focus away.  Clicking inside
        /// the window gives focus to the window and brings it to the front.)
        /// </summary>
        /// <param name="absMousPos">Absolute position of mouse on whole screen</param>
        /// <returns>True if the window got focused, false if it didn't.</returns>
        public bool FocusClickLocationCheck(Vector2 absMousePos)
        {
            bool wasInside = false;
            if (IsInsideMyExposedPortion(absMousePos))
            {
                BringToFront();
                GetFocus();
                wasInside = true;
            }
            else
            {
                LoseFocus();
            }
            return wasInside;
        }

        /// <summary>
        /// Cause this window to become frontmost, AND it also gets the keyboard focus.
        /// </summary>
        public virtual void BringToFront()
        {
            // Remove me from where I was in the depth list, and put me at the front instead:
            if (depthSort.Exists( delegate(KOSManagedWindow t){ return t==this; }))
            {
                depthSort.Remove(this);
            }
            depthSort.Insert(0,this);
            GUI.BringWindowToFront(uniqueId);
            // Make sure all the other windows unlock (lose focus):
            for (int i = 1 ; i<depthSort.Count ; ++i)
            {
                depthSort[i].LoseFocus();
            }
        }

        // Returns true only if the position is inside this window and NOT inside
        // any terminal windows that are closer to the the front.
        // (This is the sort of basic checking that Unity's GUI *should* do itself
        // but it seems quite bad at it.)
        public bool IsInsideMyExposedPortion(Vector2 posAbsolute)
        {
            if (windowRect.Contains(posAbsolute))
            {
                int myDepthIndex = depthSort.FindIndex(delegate(KOSManagedWindow t){ return t==this; });
                bool insideHigher = false;
                for (int stackedIndex = myDepthIndex - 1 ; (!insideHigher) && stackedIndex >= 0 ; --stackedIndex)
                {
                    insideHigher = depthSort[stackedIndex].windowRect.Contains(posAbsolute);
                }
                if (!insideHigher)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Whne you subclass KOSManagedWindow, make sure that you call this
        /// from inside your Update.  It does not use OnGUI because of the fact
        /// that the OnGUI event handler is broken - it only sends MouseDown 
        /// and MouseUp events when the mouse is OUTSIDE the window, which is
        /// utterly backward, and it's hard to work out how to fix this,
        /// given how badly documented the Unity GUI API is.  If anyone who
        /// actually understands the Unity GUI system wants to fix this,
        /// please feel free to do so.
        /// </summary>
        /// <returns>True if there was a mouseclick within this window.</returns>
        public bool UpdateLogic()
        {
            if (IsOpen())
            {
                // Input.mousePosition, unlike Event.current.MousePosition, puts the origin at the
                // lower-left instead of upper-left of the screen, thus the subtraction in the y coord below:
                mousePosAbsolute = new Vector2( Input.mousePosition.x, UnityEngine.Screen.height - Input.mousePosition.y);
    
                // Mouse coord within the window, rather than within the screen.
                mousePosRelative = new Vector2( mousePosAbsolute.x - windowRect.xMin, mousePosAbsolute.y - windowRect.yMin);
    
                bool clickUp = false;
                if (Input.GetMouseButtonDown(0))
                {
                    mouseButtonDownPosAbsolute = mousePosAbsolute;
                    mouseButtonDownPosRelative = mousePosRelative;
                }
    
                if (Input.GetMouseButtonUp(0))
                {
                    clickUp = true;
                    if (Vector2.Distance(mousePosAbsolute,mouseButtonDownPosAbsolute) <= dragTolerance)
                    {
                        FocusClickLocationCheck(mousePosAbsolute);
                    }
                }
                return IsInsideMyExposedPortion(mousePosAbsolute) && clickUp;
            }
            else
            {
                return false;
            }
        }
    }
}

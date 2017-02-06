using System.Collections.Generic;
using UnityEngine;

namespace kOS.Screen
{
    public delegate void ChangeAction(string pick); // a callback to invoke upon a selection fron the list.
    
    /// <summary>
    /// A GUI widget that lets the user pick a string from a list of strings.
    /// When a string is picked, the dialog is called to tell someone about it.
    /// (Not to be confused with the kerbscript-capable popup dialog.  This is 
    /// for use with kOS's own C# code.)
    /// </summary>
    public class ListPickerDialog : MonoBehaviour
    {
        private List<string> choices;
        private string current;
        private string title;
        private Rect outerWindowRect = new Rect();
        private static Dictionary<int,Vector2> prevScrollPositions; // track prev scroll positions of my instances.
        private bool running;
        private ChangeAction callWhenChanged;
        private string longestString;

        ListPickerDialog()
        {
            running = false;
        }

        /// <summary>
        /// Starts the instance of the <see cref="kOS.Screen.ListPickerDialog"/> class.
        /// Because Monobehaviour doesn't like setting up things in the constructor, you call this
        /// after the constructor:
        /// </summary>
        /// <param name="leftX">position of upper left x coord</param>
        /// <param name="topY">position of upper left y coord</param>
        /// <param name="title">Title text.</param>
        /// <param name="current">Current string value.</param>
        /// <param name="choices">Choices.</param>
        /// <param name="callWhenChanged">The dialog box will call this when a new pick has been made.</param>
        public void Summon(float leftX, float topY, string title, string current, IEnumerable<string> choices, ChangeAction callWhenChanged)
        {
            this.choices = new List<string>(choices);
            this.current = current;
            this.title = title;
            this.callWhenChanged = callWhenChanged;
            outerWindowRect.x = leftX;
            outerWindowRect.y = topY;
            running = true;

            // For calculating the rectangle size, what's the biggest string we'll need to fit?
            longestString = title;
            foreach (string str in choices)
            {
                if (str.Length > longestString.Length)
                    longestString = str;
            }
        }

        void Awake()
        {
            running = false;
        }

        void OnGUI()
        {
            if (!running)
                return;

            Vector2 maxSize = HighLogic.Skin.label.CalcSize(new GUIContent(longestString));
            // Make sure it shifts enough to the left to fit the biggest string:
            outerWindowRect.x = Mathf.Min(outerWindowRect.x, UnityEngine.Screen.width - maxSize.x - 60);

            outerWindowRect = GUILayout.Window(
                title.GetHashCode(),
                outerWindowRect,
                DrawInnards,
                title,
                HighLogic.Skin.window,
                GUILayout.MinWidth(maxSize.x + 50),
                GUILayout.MaxWidth(UnityEngine.Screen.width - outerWindowRect.x - 15),
                GUILayout.MinHeight(300),
                GUILayout.MaxHeight(UnityEngine.Screen.height - outerWindowRect.y - 15)
            );

            // This is meant to be a one-off instance.  Once it's been used to make one pick, it's gone:
            if (!running)
                Destroy(this);
        }

        void DrawInnards(int windowId)
        {
            GUILayout.BeginVertical();
            string newValue = GUIScrollPick(windowId, current, choices);
            if (!newValue.Equals(current))
            {
                current = newValue;
                callWhenChanged(current);
            }
            bool closed = GUILayout.Button("Close", HighLogic.Skin.button);
            if (closed)
            {
                running = false;
            }
            GUILayout.EndVertical();
        }

        private string GUIScrollPick(int windowId, string value, IEnumerable<string>values)
        {
            string returnThis = value; // Echo back the same value unless something else got picked.
            GUILayout.BeginVertical(GUILayout.MaxHeight(200));
            GUILayout.Label(value, HighLogic.Skin.label);

            // prevScrollPositions remembers the previous spot that each instance of this class
            // had been positioned at previously.  By remembering it statically, we can pop open a new
            // instance of the list scrolled to the same position as last time, under the assumption
            // that the new instance will use the same window ID.
            if (prevScrollPositions == null)
                prevScrollPositions = new Dictionary<int, Vector2>();
            if (! prevScrollPositions.ContainsKey(windowId))
                prevScrollPositions.Add(windowId, new Vector2());
            
            prevScrollPositions[windowId] = GUILayout.BeginScrollView(prevScrollPositions[windowId]);

            GUILayout.BeginVertical();
            foreach (string thisValue in values)
            {
                bool toggleBefore = (value.Equals(thisValue));
                bool toggleAfter = GUILayout.Toggle(toggleBefore, thisValue, HighLogic.Skin.toggle);
                if (toggleAfter && !toggleBefore)
                    returnThis = thisValue;
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            return returnThis;
        }

    }
}
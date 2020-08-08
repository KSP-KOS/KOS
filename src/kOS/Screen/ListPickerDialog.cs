using System.Collections.Generic;
using UnityEngine;
using ClickThroughFix; // Needs ClickThroughBlocker DLL to be in the Reference directory.


namespace kOS.Screen
{
    /// <summary>
    /// A GUI widget that lets the user pick a string from a list of strings.
    /// It creates the widget and leaves it showing while the rest of the C# code
    /// continues on.  Then when the user picks one of the strings
    /// the ChangeAction delegate is called to let the C# code know the pick
    /// has happened.
    /// (Not to be confused with the kerbscript-capable popup dialog.  This is 
    /// for use with kOS's own C# code.)
    /// </summary>
    public class ListPickerDialog : MonoBehaviour
    {
        /// <summary>
        /// Your callback that this class will invoke upon a selection from the list.
        /// If you make it return true, you are telling ListPickerDialog that you allow
        /// the selection and it should highlight the selection.  If you make this
        /// return false, then you are forbidding the selection and the list
        /// picker will not highlight the selected thing.
        /// </summary>
        public delegate bool ChangeAction(string pick);

        /// <summary>
        /// Your callback to be invoked when the user closes the list box.
        /// </summary>
        public delegate void CloseAction();

        private List<string> choices;
        private string current;
        private string title;
        private string subTitle;
        private Rect outerWindowRect = new Rect();
        private static Dictionary<int,Vector2> prevScrollPositions; // track prev scroll positions of my instances.
        private bool running;
        private ChangeAction callWhenChanged;
        private CloseAction callWhenClosed;
        private string longestString;
        private float minWidth;
        private GUIStyle listItemStyle;
        private GUIStyle listItemSelectedStyle;
        private GUIStyle listHeaderStyle;

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
        /// <param name="minWidth">force Unity to render the window at least this wide.</param>"> 
        /// <param name="title">Title text.</param>
        /// <param name="current">Current string value.</param>
        /// <param name="choices">Choices.</param>
        /// <param name="callWhenChanged">Your Callback invoked when a new pick has been made.</param>
        /// <param name="callWhenClosed">Your Callback invoked when the user closes the dialog box.</param>
        public void Summon(
            float leftX,
            float topY,
            float minWidth,
            string title,
            string subTitle,
            string current,
            IEnumerable<string> choices,
            ChangeAction callWhenChanged,
            CloseAction callWhenClosed)
        {
            this.choices = new List<string>(choices);
            this.current = current;
            this.title = title;
            this.subTitle = subTitle;
            this.callWhenChanged = callWhenChanged;
            this.callWhenClosed = callWhenClosed;
            MakeStyles();
            outerWindowRect.x = leftX;
            outerWindowRect.y = topY;
            this.minWidth = minWidth;
            running = true;

            // For calculating the rectangle size, what's the biggest string we'll need to fit?
            longestString = (subTitle!=null && subTitle.Length > title.Length) ? subTitle : title;
            foreach (string str in choices)
            {
                if (str.Length > longestString.Length)
                    longestString = str;
            }
        }

        public void Awake()
        {
            running = false;
        }

        public void Close()
        {
            running = false;
        }

        public void OnGUI()
        {
            if (!running)
                return;

            // Make sure it shifts enough to the left to fit the biggest string:
            outerWindowRect.x = Mathf.Min(outerWindowRect.x, UnityEngine.Screen.width - outerWindowRect.width - 60);
            outerWindowRect = ClickThruBlocker.GUILayoutWindow(
                title.GetHashCode(),
                outerWindowRect,
                DrawInnards,
                title,
                HighLogic.Skin.window,
                GUILayout.MinWidth(minWidth + 50),
                GUILayout.MinHeight(300),
                GUILayout.MaxHeight(UnityEngine.Screen.height - outerWindowRect.y - 15)
            );

            // This is meant to be a one-off instance.  Once it's been used to make one pick, it's gone:
            if (!running)
                Destroy(this);
        }

        private void DrawInnards(int windowId)
        {
            GUILayout.BeginVertical();
            string newValue = GUIScrollPick(windowId, current, choices);
            if (!newValue.Equals(current))
            {
                // Notify the caller of the change, and if they accept it, commit it so
                // the widget will highlight the new pick (else the highlight won't move).
                if (callWhenChanged(newValue))
                {
                    current = newValue;
                    running = false; // close on successful pick.
                    callWhenClosed();
                }
            }
            bool closeButtonPressed = GUILayout.Button("Close", HighLogic.Skin.button);
            if (closeButtonPressed)
            {
                running = false;
                callWhenClosed();
            }
            GUILayout.EndVertical();
        }

        private string GUIScrollPick(int windowId, string value, IEnumerable<string>values)
        {
            string returnThis = value; // Echo back the same value unless something else got picked.
            GUILayout.BeginVertical(GUILayout.MaxHeight(220));

            if (!string.IsNullOrEmpty(subTitle))
                GUILayout.Label(subTitle, HighLogic.Skin.label);

            GUILayout.Label(Texture2D.blackTexture, GUILayout.ExpandWidth(true));
            GUILayout.Label(value, listHeaderStyle);

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
                bool toggleAfter = GUILayout.Toggle(toggleBefore, thisValue, (toggleBefore ? listItemSelectedStyle : listItemStyle));
                if (toggleAfter && !toggleBefore)
                    returnThis = thisValue;
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            return returnThis;
        }

        private void MakeStyles()
        {
            listItemStyle = new GUIStyle(HighLogic.Skin.label);
            listItemStyle.name = "ListPickerDialogItem";
            listItemStyle.wordWrap = false;
            listItemStyle.margin.top = 0;
            listItemStyle.margin.bottom = 0;
            listItemStyle.normal.background = null;
            listItemStyle.hover.background = new Texture2D(1, 1);
            listItemStyle.hover.background.SetPixel(0, 0, Color.gray);
            listItemStyle.hover.textColor = Color.black;
            listItemStyle.active.background = listItemStyle.hover.background;
            listItemStyle.active.textColor = Color.white;
            listItemStyle.focused.textColor = Color.white;
            listItemStyle.stretchWidth = true;

            // I tried everything to make Unity actually obey the following
            // style rules and it refuses to do so.  It refused to highlight
            // the selected item properly using the 'on' styles below.
            // It just behaves as if the item cannot ever be "on".  That is
            // why there is just a separate style entirely (listItemSelectedStyle)
            // to perform the same thing: 
            // listItemStyle.onNormal.textColor = Color.white;
            // listItemStyle.onActive.textColor = Color.white;
            // listItemStyle.onHover.textColor = Color.white;
            // listItemStyle.onFocused.textColor = Color.white;

            listItemSelectedStyle = new GUIStyle(listItemStyle);
            listItemSelectedStyle.normal.textColor = Color.white;

            listHeaderStyle = new GUIStyle(HighLogic.Skin.label);
            listHeaderStyle.name = "ListPickerDialogHeader";
            listHeaderStyle.normal.textColor = Color.black;
            listHeaderStyle.normal.background = Texture2D.whiteTexture;
            listHeaderStyle.stretchWidth = true;

        }

    }
}
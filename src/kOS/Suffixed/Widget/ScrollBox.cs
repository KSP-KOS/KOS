using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("ScrollBox")]
    public class ScrollBox : Box
    {
        private bool hscrollalways = false;
        private bool vscrollalways = false;
        private Vector2 position;
        private WidgetStyle hsbStyle;
        private WidgetStyle vsbStyle;

        public ScrollBox(Box parent) : base(parent, LayoutMode.Vertical, parent.FindStyle("scrollView"))
        {
            hsbStyle = parent.FindStyle("horizontalScrollbar");
            vsbStyle = parent.FindStyle("verticalScrollbar");
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HALWAYS", new SetSuffix<BooleanValue>(() => hscrollalways, value => hscrollalways = value));
            AddSuffix("VALWAYS", new SetSuffix<BooleanValue>(() => vscrollalways, value => vscrollalways = value));
            AddSuffix("POSITION", new SetSuffix<Vector>(() => new Vector(position.x,position.y,0), value => { position.x = (float)value.X; position.y = (float)value.Y; }));
        }

        public override void DoGUI()
        {
            if (!Shown) return;
            bool was = GUI.enabled;
            GUI.enabled = true; // always allow scrolling
            position = GUILayout.BeginScrollView(position,hscrollalways,vscrollalways,hsbStyle.ReadOnly,vsbStyle.ReadOnly,ReadOnlyStyle);
            if (Mode == LayoutMode.Horizontal) GUILayout.BeginHorizontal();
            if (!Enabled || !was) GUI.enabled = false;
            DoChildGUIs();
            if (Mode == LayoutMode.Horizontal) GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUI.enabled = was;
        }

        public override string ToString()
        {
            return "SCROLLBOX";
        }
    }
}

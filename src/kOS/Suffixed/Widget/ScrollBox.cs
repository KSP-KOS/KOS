using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ScrollBox")]
    public class ScrollBox : Box
    {
        bool hscrollalways = false;
        bool vscrollalways = false;
        Vector2 position;

        public ScrollBox(Box parent) : base(parent, LayoutMode.Vertical)
        {
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.scrollView;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HALWAYS", new SetSuffix<BooleanValue>(() => hscrollalways, value => hscrollalways = value));
            AddSuffix("VALWAYS", new SetSuffix<BooleanValue>(() => vscrollalways, value => vscrollalways = value));
            AddSuffix("POSITION", new SetSuffix<Vector>(() => new Vector(position.x,position.y,0), value => { position.x = (float)value.X; position.y = (float)value.Y; }));
        }

        public override void DoGUI()
        {
            if (!shown) return;
            var was = GUI.enabled;
            GUI.enabled = true; // always allow scrolling
            position = GUILayout.BeginScrollView(position,hscrollalways,vscrollalways,HighLogic.Skin.horizontalScrollbar,HighLogic.Skin.verticalScrollbar,style);
            if (layout == LayoutMode.Horizontal) GUILayout.BeginHorizontal();
            if (!enabled || !was) GUI.enabled = false;
            DoChildGUIs();
            if (layout == LayoutMode.Horizontal) GUILayout.EndHorizontal();
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

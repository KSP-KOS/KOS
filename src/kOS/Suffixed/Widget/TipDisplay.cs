using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("TipDisplay")]
    public class TipDisplay : Label
    {
        public string ToolTip { get; private set; }

        public TipDisplay(Box parent, string text) : this(parent, text, parent.FindStyle("tipDisplay"))
        {
        }

        public TipDisplay(Box parent, string text, WidgetStyle buttonStyle) : base(parent, text, buttonStyle)
        {
        }

        private void PopulateToolTipText()
        {
            // Walk up the parent list till I find the GUI() object I'm inside of, and get its tip value.
            // Put that in my label text.
            while (parent != null)
            {
                GUIWidgets p = parent as GUIWidgets;
                if (p != null)
                {
                    SetText(p.RecentToolTip);
                    break;
                }
                parent = parent.GetParent();
            }
            if (parent == null)
            { 
                SetText("");
            }
        }

        public override void DoGUI()
        {
            PopulateToolTipText();
            base.DoGUI();
        }

        public override string ToString()
        {
            return "TipDisplay(" + VisibleText() + ")";
        }
    }
}

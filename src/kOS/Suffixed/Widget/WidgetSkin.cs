using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using System.Collections.Generic;
using kOS.Safe.Utilities;
using System.IO;
using System;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Skin")]
    public class WidgetSkin : Structure
    {
        public GUISkin Skin { get; set; }
        private Dictionary<string, WidgetStyle> styles;
        public WidgetSkin(GUISkin original)
        {
            Skin = original;
            styles = new Dictionary<string, WidgetStyle>();
            RegisterInitializer(InitializeSuffixes);
        }

        public WidgetStyle GetStyle(StringValue name) { return GetStyle(name.ToString().ToUpperInvariant()); }
        private WidgetStyle GetStyle(string name)
        {
            WidgetStyle r;
            if (styles.TryGetValue(name, out r)) return r;
            r = new WidgetStyle(Skin.GetStyle(name));
            styles.Add(name, r);
            return r;
        }

        public void SetStyle(string name, WidgetStyle style)
        {
            styles[name.ToUpperInvariant()] = style;
        }

        private void InitializeSuffixes()
        {
            string[] builtIn = {
                "box", "button",
                "horizontalScrollbar", "horizontalScrollbarLeftButton", "horizontalScrollbarRightButton", "horizontalScrollbarThumb",
                "horizontalSlider", "horizontalSliderThumb",
                "verticalScrollbar", "verticalScrollbarLeftButton", "verticalScrollbarRightButton", "verticalScrollbarThumb",
                "verticalSlider", "verticalSliderThumb",
                "label", "scrollView", "textArea", "textField", "toggle", "window" };

            foreach (string s in builtIn) {
                string name = s.ToUpperInvariant();
                AddSuffix(name, new SetSuffix<WidgetStyle>(() => GetStyle(name), value => SetStyle(name, value)));
            }
            foreach (GUIStyle cs in Skin.customStyles) {
                if (cs != null) {
                    string name = cs.name.ToUpperInvariant();
                    AddSuffix(name, new SetSuffix<WidgetStyle>(() => GetStyle(name), value => SetStyle(name, value)));
                }
            }

            AddSuffix("ADD", new OneArgsSuffix<WidgetStyle, StringValue>(AddStyle));
            AddSuffix("GET", new OneArgsSuffix<WidgetStyle, StringValue>(GetStyle));
        }

        public WidgetStyle AddStyle(StringValue name)
        {
            var r = new WidgetStyle(HighLogic.Skin.label);
            styles.Add(name.ToString().ToUpperInvariant(), r);
            return r;
        }


        public override string ToString()
        {
            return "SKIN";
        }
    }
}

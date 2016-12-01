using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using System.Collections.Generic;
using kOS.Safe.Utilities;
using System.IO;
using System;

namespace kOS.Suffixed
{
    static class StringExt
    {
        public static string Ellipsis(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Widget")]
    abstract public class Widget : Structure
    {
        abstract protected GUIStyle BaseStyle();
        protected Box parent;
        private GUIStyle _style;
        public GUIStyle style { get { return _style == null ? BaseStyle() : _style; } }
        public GUIStyle setstyle { get { if (_style == null) { _style = new GUIStyle(BaseStyle()); } return _style; } }

        public bool enabled { get; protected set; }
        public bool shown { get; protected set; }

        public Widget(Box parent)
        {
            this.parent = parent;
            enabled = true;
            shown = true;
            RegisterInitializer(InitializeSuffixes);
        }

        protected GUIWidgets FindGUI()
        {
            var c = this;
            while (c.parent != null) {
                c = c.parent;
            }
            return c as GUIWidgets;
        }

        protected RgbaColor GetStyleRgbaColor()
        {
            var c = style.normal.textColor;
            return new RgbaColor(c.r, c.g, c.b, c.a);
        }
        
        protected void SetStyleRgbaColor(RgbaColor rgba)
        {
            setstyle.normal.textColor = rgba.Color;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HMARGIN", new SetSuffix<ScalarIntValue>(() => style.margin.left, value => { setstyle.margin.left = value; setstyle.margin.right = value; }));
            AddSuffix("HPADDING", new SetSuffix<ScalarIntValue>(() => style.padding.left, value => { setstyle.padding.left = value; setstyle.padding.right = value; }));
            AddSuffix("VMARGIN", new SetSuffix<ScalarIntValue>(() => style.margin.top, value => { setstyle.margin.top = value; setstyle.margin.bottom = value; }));
            AddSuffix("VPADDING", new SetSuffix<ScalarIntValue>(() => style.padding.top, value => { setstyle.padding.top = value; setstyle.padding.bottom = value; }));

            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => style.fixedWidth, value => setstyle.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => style.fixedHeight, value => setstyle.fixedHeight = value));
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => enabled, value => enabled = value));
            AddSuffix("SHOW", new NoArgsVoidSuffix(Show));
            AddSuffix("HIDE", new NoArgsVoidSuffix(Hide));
            AddSuffix("DISPOSE", new NoArgsVoidSuffix(Dispose));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchWidth, value => setstyle.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchHeight, value => setstyle.stretchHeight = value));

            AddSuffix("HBORDER", new SetSuffix<ScalarIntValue>(() => style.border.left, value => { setstyle.border.left = value; setstyle.border.right = value; }));
            AddSuffix("VBORDER", new SetSuffix<ScalarIntValue>(() => style.border.top, value => { setstyle.border.top = value; setstyle.border.bottom = value; }));

            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => setstyle.normal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED", new SetSuffix<StringValue>(() => "", value => setstyle.focused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE", new SetSuffix<StringValue>(() => "", value => setstyle.active.background = GetTexture(value)));
            AddSuffix("BG_HOVER", new SetSuffix<StringValue>(() => "", value => setstyle.hover.background = GetTexture(value)));
            AddSuffix("BG_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onNormal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onFocused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onActive.background = GetTexture(value)));
            AddSuffix("BG_HOVER_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onHover.background = GetTexture(value)));
        }

        virtual public void Show()
        {
            shown = true;
        }

        virtual public void Hide()
        {
            shown = false;
        }

        virtual public void Dispose()
        {
            shown = false;
            if (parent != null) {
                parent.Remove(this);
                var gui = FindGUI();
                if (gui != null)
                    gui.ClearCommunication(this);
            }
        }

        abstract public void DoGUI();

        class TexFileInfo
        {
            public Texture2D texture;
            public FileInfo info;
        }
        static Dictionary<string, TexFileInfo> textureCache;

        public static Texture2D GetTexture(string relativePath)
        {
            if (relativePath == "") return null;
            if (textureCache == null)
                textureCache = new Dictionary<string, TexFileInfo>();
            TexFileInfo t;
            if (textureCache.TryGetValue(relativePath, out t)) {
                if (t.texture != null) {
                    var curInfo = new FileInfo(t.info.FullName);
                    if (curInfo.LastWriteTime == t.info.LastWriteTime)
                        return t.texture;
                }
            } else {
                t = new TexFileInfo();
                textureCache.Add(relativePath, t);
            }
            var path = Path.Combine(SafeHouse.ArchiveFolder, relativePath);
            var r = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            string[] exts = { ".png", "" };
            foreach (var ext in exts) {
                var filename = path + ext;
                if (File.Exists(filename)) {
                    r.LoadImage(File.ReadAllBytes(filename));
                    t.texture = r;
                    t.info = new FileInfo(filename);
                    break;
                }
            }
            return r;
        }

        virtual protected void Communicate(Action a)
        {
            var gui = FindGUI();
            if (gui != null)
                gui.Communicate(this,ToString(),a);
            else
                a();
        }

        public override string ToString()
        {
            return "WIDGET";
        }
    }
}

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
        protected Box parent;

        // To optimize the common case where each subclass just uses the one default style,
        // subclasses implement BaseStyle() to return that style (directly from HighLogic.Skin),
        // then use Style to read the Style and SetStyle to change properties of the style.
        //
        abstract protected GUIStyle BaseStyle();
        private GUIStyle copyOnWriteStyle;
        public GUIStyle Style { get { return copyOnWriteStyle == null ? BaseStyle() : copyOnWriteStyle; } }
        public GUIStyle SetStyle { get { if (copyOnWriteStyle == null) { copyOnWriteStyle = new GUIStyle(BaseStyle()); } return copyOnWriteStyle; } }

        public bool Enabled { get; protected set; }
        public bool Shown { get; protected set; }

        public Widget(Box parent)
        {
            this.parent = parent;
            Enabled = true;
            Shown = true;
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
            var c = Style.normal.textColor;
            return new RgbaColor(c.r, c.g, c.b, c.a);
        }
        
        protected void SetStyleRgbaColor(RgbaColor rgba)
        {
            SetStyle.normal.textColor = rgba.Color;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HMARGIN", new SetSuffix<ScalarIntValue>(() => Style.margin.left, value => { SetStyle.margin.left = value; SetStyle.margin.right = value; }));
            AddSuffix("HPADDING", new SetSuffix<ScalarIntValue>(() => Style.padding.left, value => { SetStyle.padding.left = value; SetStyle.padding.right = value; }));
            AddSuffix("VMARGIN", new SetSuffix<ScalarIntValue>(() => Style.margin.top, value => { SetStyle.margin.top = value; SetStyle.margin.bottom = value; }));
            AddSuffix("VPADDING", new SetSuffix<ScalarIntValue>(() => Style.padding.top, value => { SetStyle.padding.top = value; SetStyle.padding.bottom = value; }));

            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => Style.fixedWidth, value => SetStyle.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => Style.fixedHeight, value => SetStyle.fixedHeight = value));
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Enabled, value => Enabled = value));
            AddSuffix("SHOW", new NoArgsVoidSuffix(Show));
            AddSuffix("HIDE", new NoArgsVoidSuffix(Hide));
            AddSuffix("DISPOSE", new NoArgsVoidSuffix(Dispose));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => Style.stretchWidth, value => SetStyle.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => Style.stretchHeight, value => SetStyle.stretchHeight = value));

            AddSuffix("HBORDER", new SetSuffix<ScalarIntValue>(() => Style.border.left, value => { SetStyle.border.left = value; SetStyle.border.right = value; }));
            AddSuffix("VBORDER", new SetSuffix<ScalarIntValue>(() => Style.border.top, value => { SetStyle.border.top = value; SetStyle.border.bottom = value; }));

            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => SetStyle.normal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED", new SetSuffix<StringValue>(() => "", value => SetStyle.focused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE", new SetSuffix<StringValue>(() => "", value => SetStyle.active.background = GetTexture(value)));
            AddSuffix("BG_HOVER", new SetSuffix<StringValue>(() => "", value => SetStyle.hover.background = GetTexture(value)));
            AddSuffix("BG_ON", new SetSuffix<StringValue>(() => "", value => SetStyle.onNormal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED_ON", new SetSuffix<StringValue>(() => "", value => SetStyle.onFocused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE_ON", new SetSuffix<StringValue>(() => "", value => SetStyle.onActive.background = GetTexture(value)));
            AddSuffix("BG_HOVER_ON", new SetSuffix<StringValue>(() => "", value => SetStyle.onHover.background = GetTexture(value)));
        }

        virtual public void Show()
        {
            Shown = true;
        }

        virtual public void Hide()
        {
            Shown = false;
        }

        virtual public void Dispose()
        {
            Shown = false;
            if (parent != null) {
                parent.Remove(this);
                GUIWidgets gui = FindGUI();
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
            string path = Path.Combine(SafeHouse.ArchiveFolder, relativePath);
            var r = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            string[] exts = { ".png", "" };
            foreach (string ext in exts) {
                string filename = path + ext;
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
            GUIWidgets gui = FindGUI();
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

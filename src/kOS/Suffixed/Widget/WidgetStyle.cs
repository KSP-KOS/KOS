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
    [kOS.Safe.Utilities.KOSNomenclature("Style")]
    public class WidgetStyle : Structure
    {
        private GUIStyle copyOnWriteStyle;
        private bool copied = false;

        public GUIStyle ReadOnly { get { return copyOnWriteStyle; } }
        public GUIStyle Writable { get {
                if (!copied) {
                    copyOnWriteStyle = new GUIStyle(copyOnWriteStyle);
                    copied = true;
                }
                return copyOnWriteStyle;
            }
        }

        public WidgetStyle(GUIStyle original)
        {
            copyOnWriteStyle = original;
            RegisterInitializer(InitializeSuffixes);
        }

        protected RgbaColor TextColor
        {
            get
            {
                var c = ReadOnly.normal.textColor;
                return new RgbaColor(c.r, c.g, c.b, c.a);
            }
            set
            {
                Writable.normal.textColor = value.Color;
            }
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HMARGIN", new SetSuffix<ScalarIntValue>(() => ReadOnly.margin.left, value => { Writable.margin.left = value; Writable.margin.right = value; }));
            AddSuffix("HPADDING", new SetSuffix<ScalarIntValue>(() => ReadOnly.padding.left, value => { Writable.padding.left = value; Writable.padding.right = value; }));
            AddSuffix("VMARGIN", new SetSuffix<ScalarIntValue>(() => ReadOnly.margin.top, value => { Writable.margin.top = value; Writable.margin.bottom = value; }));
            AddSuffix("VPADDING", new SetSuffix<ScalarIntValue>(() => ReadOnly.padding.top, value => { Writable.padding.top = value; Writable.padding.bottom = value; }));

            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => ReadOnly.fixedWidth, value => Writable.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => ReadOnly.fixedHeight, value => Writable.fixedHeight = value));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => ReadOnly.stretchWidth, value => Writable.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => ReadOnly.stretchHeight, value => Writable.stretchHeight = value));

            AddSuffix("HBORDER", new SetSuffix<ScalarIntValue>(() => ReadOnly.border.left, value => { Writable.border.left = value; Writable.border.right = value; }));
            AddSuffix("VBORDER", new SetSuffix<ScalarIntValue>(() => ReadOnly.border.top, value => { Writable.border.top = value; Writable.border.bottom = value; }));

            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => Writable.normal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED", new SetSuffix<StringValue>(() => "", value => Writable.focused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE", new SetSuffix<StringValue>(() => "", value => Writable.active.background = GetTexture(value)));
            AddSuffix("BG_HOVER", new SetSuffix<StringValue>(() => "", value => Writable.hover.background = GetTexture(value)));
            AddSuffix("BG_ON", new SetSuffix<StringValue>(() => "", value => Writable.onNormal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED_ON", new SetSuffix<StringValue>(() => "", value => Writable.onFocused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE_ON", new SetSuffix<StringValue>(() => "", value => Writable.onActive.background = GetTexture(value)));
            AddSuffix("BG_HOVER_ON", new SetSuffix<StringValue>(() => "", value => Writable.onHover.background = GetTexture(value)));

            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => ReadOnly.fontSize, value => Writable.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => ReadOnly.richText, value => Writable.richText = value));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => TextColor, value => TextColor = value));
            AddSuffix("ALIGN", new SetSuffix<StringValue>(GetAlignment, SetAlignment));
        }

        StringValue GetAlignment()
        {
            if (ReadOnly.alignment == TextAnchor.MiddleCenter) return "CENTER";
            if (ReadOnly.alignment == TextAnchor.MiddleRight) return "RIGHT";
            return "LEFT";
        }

        void SetAlignment(StringValue s)
        {
            s = s.ToLower();
            if (s == "center") Writable.alignment = TextAnchor.MiddleCenter;
            else if (s == "right") Writable.alignment = TextAnchor.MiddleRight;
            else if (s == "left") Writable.alignment = TextAnchor.MiddleLeft;
            else throw new KOSInvalidArgumentException("LABEL", "ALIGNMENT", "expected CENTER, LEFT, or RIGHT, found " + s);
        }

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

        public override string ToString()
        {
            return "STYLE("+copyOnWriteStyle.name+")";
        }
    }
}

using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using System.Collections.Generic;
using kOS.Safe.Utilities;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using System.IO;
using System;

namespace kOS.Suffixed.Widget
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

        /// <summary>
        /// Temporarily set to "true" to tell the widget that any state changes
        /// are happening because of a gui activity (rather than because of a script
        /// deliberately changing a value for example).
        /// </summary>
        protected bool guiCaused;

        protected int myId;

        // The WidgetStyle is cheap as it only creates a new GUIStyle if it is
        // actually changed, otherwise it just refers to the one in the GUI:SKIN.
        private WidgetStyle copyOnWriteStyle;
        public GUIStyle ReadOnlyStyle { get { return copyOnWriteStyle.ReadOnly; } }

        public WidgetStyle FindStyle(string name)
        {
            GUIWidgets gui = FindGUI();
            if (gui != null) {
                var s = gui.Skin.GetStyle(name);
                return new WidgetStyle(s.ReadOnly);
            }
            return new WidgetStyle(HighLogic.Skin.GetStyle(name)); // fallback; shouldn't happen
        }

        public bool Enabled { get; protected set; }
        public bool Shown { get; protected set; }

        public Widget(Box parent, WidgetStyle style)
        {
            this.parent = parent;
            this.copyOnWriteStyle = style;
            Enabled = true;
            Shown = true;
            RegisterInitializer(InitializeSuffixes);
        }

        public Box GetParent()
        {
            return parent;
        }

        /// <summary>
        /// Return either the delegate itself or a dummy delegate if it's null,
        /// to protect against sending a null to kerboscript.  To be used with
        /// all the callbacks the GUI system will use.
        /// </summary>
        /// <returns>The callback or NoDelegate.</returns>
        /// <param name="d">delegate to try</param>
        public static UserDelegate CallbackGetter(UserDelegate d)
        {
            return d ?? NoDelegate.Instance;
        }

        /// <summary>
        /// Return the GUI callbackm or if NoDelegate was passed in, then
        /// return null instead.  To be used in the Setters of SetSuffixes
        /// for gui user callbacks so the kerboscript can pass in NoDelegate
        /// and the kOS C# code will interpret that to mean a null.
        /// </summary>
        /// <param name="d">delegate to return</param>
        public static UserDelegate CallbackSetter(UserDelegate d)
        {
            return (d is NoDelegate ? null : d);
        }

        protected GUIWidgets FindGUI()
        {
            var c = this;
            while (c.parent != null) {
                c = c.parent;
            }
            return c as GUIWidgets;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Enabled, value => Enabled = value));
            AddSuffix("VISIBLE", new SetSuffix<BooleanValue>(() => Shown, value => {  if (value) Show(); else Hide(); }));
            AddSuffix("SHOW", new NoArgsVoidSuffix(Show));
            AddSuffix("HIDE", new NoArgsVoidSuffix(Hide));
            AddSuffix("DISPOSE", new NoArgsVoidSuffix(Dispose));
            AddSuffix("STYLE", new SetSuffix<WidgetStyle>(() => copyOnWriteStyle, value => copyOnWriteStyle = value));
            AddSuffix("GUI", new Suffix<GUIWidgets>(() => FindGUI()));
            AddSuffix("PARENT", new Suffix<Widget>(() => parent));
            AddSuffix("HASPARENT", new Suffix<BooleanValue>(() => parent != null));
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
                textureCache = new Dictionary<string, TexFileInfo> ();

            string path = Path.GetFullPath (Path.Combine (SafeHouse.ArchiveFolder, relativePath));
            if (!path.StartsWith (SafeHouse.ArchiveFolder, StringComparison.Ordinal))
                throw new KOSInvalidPathException ("Path refers to parent directory", path);

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

            var r = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            string[] exts = {".png", "" };
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
            guiCaused = true;
            GUIWidgets gui = FindGUI();
            if (gui != null)
                gui.Communicate(this,ToString(),a);
            else
                a();
            guiCaused = false;
        }

        public override string ToString()
        {
            return "WIDGET";
        }
    }
}

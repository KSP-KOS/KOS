using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Utilities;

namespace kOS.Module
{
    /// <summary>
    /// Class to handle all the loading of the unity art assets
    /// in one place instead of having it be part of all the other
    /// Unity UI elements.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class AssetManager : MonoBehaviour
    {
        /// <summary>
        /// There will only be one instance of AssetManager, accessed through here.
        /// You do not need to construct AssetManager explicitly, as one will be made
        /// at load time by the way the KSPAddon attribute is set up.
        /// </summary>
        public static AssetManager Instance {get; set;}

        /// <summary>
        /// All the fonts that were loaded by kOS will be in this dictionary,
        /// with font names as the keys.  It is possible for the same font
        /// to be in the dictionary more than once if it has multiple alias names.
        /// </summary>
        protected Dictionary<string,Font> Fonts {get; set;}

        /// <summary>
        /// The fonts that we will load from the OS itself:
        /// </summary>
        protected List<string> osFontNames;
        /// <summary>
        /// All the font names that are already loaded from the Resources of KSP itself instead of
        /// being taken from the OS.  It's important that we treat these differently from how 
        /// we treat the fonts that come from the OS, so as to avoid clobbering things like the
        /// Unity default Arial font with the OS's own actual Arial font which is usually different:
        /// </summary>
        protected List<string> resourceFontNames;
        /// <summary>
        /// The fonts (both os and resource) which are monospaced.
        /// </summary>
        protected List<string> monoFontNames;
        /// <summary>
        /// The fonts (both os and resource) which are not monospaced.
        /// </summary>
        protected List<string> proportionalFontNames;

        private static readonly string rootPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            

            Fonts = new Dictionary<string, Font>();
            osFontNames = new List<String>();
            resourceFontNames = new List<String>();

            UpdateSystemFontLists();
        }

        /// <summary>
        /// Query the OS to build a list of all font names currently installed.
        /// If any new fonts have been installed into the OS since the last time
        /// this was called, or any old fonts have been uninstalled, calling this
        /// should rebuild the list to reflect those changes.  This returns void,
        /// so to actually see the list, you have to call GetSystemFontNames after calling this.
        /// <para></para>
        /// <para>------------</para>
        /// <para>Be aware that this is an expensive operation that compares two lists and is O(n^2).
        /// (n = number of fonts installed on the OS so it won't be *that* big).  But still,
        /// It shouldn't be performed repetatively.</para>
        /// </summary>
        public void UpdateSystemFontLists()
        {
            resourceFontNames = new List<string>();
            UnityEngine.Object[] resFonts = Resources.FindObjectsOfTypeAll(typeof (Font));
            foreach (UnityEngine.Object obj in resFonts)
            {
                Font f = obj as Font;
                if (f != null)
                {
                    resourceFontNames.Add(f.name);
                    Fonts.Add(f.name, f);
                    Console.WriteLine("eraseme: UpdateSystemFontLists found Resource Font: " + f.name + ", def size=" + f.fontSize);
                }
            }

            foreach (string fontName in Font.GetOSInstalledFontNames())
            {
                // This is a bit inefficient in that it's doing sequential searches on lists
                // to perform the "contains" checks, but this only gets run at startup
                // so it should be okay:
                if (!osFontNames.Contains(fontName))
                    if (!resourceFontNames.Contains(fontName)) // Do not clobber the game's fonts with the OS's fonts
                    {
                        osFontNames.Add(fontName);
                        Console.WriteLine("eraseme: UpdateSystemFontLists found OS Font: " + fontName);
                    }
            }

            // Now perform the walk of all the names to
            // find which are mono spaced and which aren't.  Unity does not 
            // expose the font metadata information so this has to be done
            // the slow way by test printing something in the font and seeing
            // how many pixels it takes up.
            //
            // Depending on how fast your computer is, this can add anywhere
            // from 0.5 to about 2 seconds to the KSP loading screen time per
            // 1000 fonts you've installed on the machine.  Given how slow
            // the loading screen is anyway, hopefully nobody will notice.
            monoFontNames = new List<string>();
            proportionalFontNames = new List<string>();
            foreach (string fontName in osFontNames)
            {
                Console.WriteLine("eraseme: Now testing if " + fontName + " is monospaced.");
                // Ask Unity to use the font at size 13 (have to pick an arbitrary size to instantiate an OS font):
                Font testFont = Font.CreateDynamicFontFromOSFont(fontName, 13); 
                if (IsFontMonospaced(testFont))
                    monoFontNames.Add(fontName);
                else
                    proportionalFontNames.Add(fontName);
            }
            foreach (object obj in resFonts)
            {
                Font testFont = obj as Font;
                if (testFont != null)
                {
                    if (IsFontMonospaced(testFont))
                        monoFontNames.Add(testFont.name);
                    else
                        proportionalFontNames.Add(testFont.name);
                }
            }
        }

        /// <summary>
        /// Returns a Unity Font object corresponding to the system font
        /// name you give it.  This will lazy-load the font, so the first time
        /// you use a font name it may take a bit of time, and then on each
        /// subsequent call using the same name it should be fast.  Can return
        /// null if the font name you give it is no good.
        /// </summary>
        /// <returns>The font</returns>
        /// <param name="name">Name of the font as it appaers in GetSystemFontNames</param>
        /// <param name="size">point size for the desired font.  If this is the first time
        /// this font was used, this will set the font's default size.</param>
        /// <param name="checkMono">If true, then perform a check for monospace and issue a warning and return null
        /// if it's not monospaced.</param>
        public Font GetFontByNameAndSize(string name, int size, bool checkMono)
        {
            // Now that a font is asked for, now we'll lazy-load it.
            Console.WriteLine("eraseme: GetFontByNameAndSize(\""+name+"\", "+size+", "+checkMono);
            string key = name;
            if ( (!Fonts.ContainsKey(key)) || Fonts[key] == null)
            {
                Console.WriteLine("eraseme:     GetFontByNameAndSize - font didn't exist already - loading it from OS");
                Fonts[key] = Font.CreateDynamicFontFromOSFont(name, size);
            }

            Font potentialReturn = Fonts[key];
            if (checkMono && !(IsFontMonospaced(potentialReturn)))
            {
                // With recent changes this message should hypothetically never appear now because the font name
                // list is being culled to just the monospace fonts before the list picker is invoked.  But
                // we're leaving this check here so it will still tell the user what's going on if we screwed
                // up and put a proportional font into the list and they end up picking it.
                string msg = string.Format("{0} is proportional width.\nA monospaced font is required.", name);
                ScreenMessages.PostScreenMessage(
                    string.Format("<color=#ff9900><size=20>{0}</size></color>",msg), 8, ScreenMessageStyle.UPPER_CENTER);
                return null;
            }
            return potentialReturn;
        }

        /// <summary>
        /// Just like GetFontByNameAndSize, except that you give it a list of
        /// multiple names and it will try each in turn until it finds a name
        /// that returns an existing font.  Only if all the names fail to find
        /// a hit will it return null.
        /// </summary>
        /// <returns>The system font by name.</returns>
        /// <param name="names">Names.</param>
        /// <param name="size">Size.</param>
        /// <param name="checkMono">If true, then perform a check for monospace and issue a warning and return null
        /// if it's not monospaced.</param>
        public Font GetFontByNameAndSize(string[] names, int size, bool checkMono)
        {
            foreach (string name in names)
            {
                Font hit = GetFontByNameAndSize(name, size, checkMono);
                if (hit != null)
                    return hit;
            }
            return null;
        }

        /// <summary>
        /// This is whatever system font names existed when the Unity engine
        /// was first started.
        /// </summary>
        /// <returns>A list of the OS font names kOS knows about.</returns>
        public List<string> GetSystemFontNames()
        {
            return osFontNames;
        }

        /// <summary>
        /// This is the subset of GetSystemFontNames() which have been
        /// tested to be monospaced.
        /// </summary>
        /// <returns>The system mono font names.</returns>
        public List<string> GetSystemMonoFontNames()
        {
            return monoFontNames;
        }

        /// <summary>
        /// This will be the subset of GetSystemFontNames() which have been
        /// tested and found NOT to be monospaced.
        /// </summary>
        /// <returns>The system mono font names.</returns>
        public List<string> GetSystemProportionalFontNames()
        {
            return proportionalFontNames;
        }

        /// <summary>A tool we can use to check if a font is monospaced by
        /// comparing the width of certain key letters.</summary>
        private static bool IsFontMonospaced(Font f)
        {
            CharacterInfo chInfo;
            int prevWidth;

            // Unity Lazy-loads the character info for the font.  Until you try
            // to actually render a character, it's CharacterInfo isn't populated
            // yet (all fields for the character's dimensions return a bogus zero value).
            // This next call forces Unity to load the information for the given characters
            // even though they haven't been rendered yet:
            f.RequestCharactersInTexture("XiW _i:");

            f.GetCharacterInfo('X', out chInfo);
            prevWidth = chInfo.advance;

            f.GetCharacterInfo('i', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;

            f.GetCharacterInfo('W', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;

            f.GetCharacterInfo(' ', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;

            f.GetCharacterInfo('_', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;

            f.GetCharacterInfo(':', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;

            // That's probably a good enough test.  If all the above characters
            // have the same width, there's really good chance this is monospaced.

            return true;
        }

        /* ------- Comment out - this is the old way using an asset bundle.
		 * ------- This is left here because it took time to work out how
		 * ------- to do this and I don't want to lose my record of how it was
		 * ------- done (this code isn't committed yet as I type this because it
		 * ------- wasn't fully right yet, thus why I want to leave it here
		 * ------- becuase it's not in the history yet.).
        private bool fontAssetNeedsLoading = true;
        public void LoadFontBundle()
        {

            if (!fontAssetNeedsLoading)
                return;

            WWW fontURL = new WWW("file://"+ rootPath + "GameData/kOS/GFX/fonts.asset");
            AssetBundle fontBundle = fontURL.assetBundle;
            
            Fonts = new Dictionary<string,Font>();
            foreach (Font aFont in fontBundle.LoadAllAssets<Font>())
            {
                fontAssetNeedsLoading = false; // if at least one font loaded, turn off the flag.
                foreach (string name in aFont.fontNames)
                {
                    Fonts[name] = aFont;
                    SafeHouse.Logger.Log (string.Format ("kOS LoadFontBundle: just loaded a font called: {0}.", aFont));
                }
            }
        }    
        --------------------- End of commented-out section ----------------- */
    }
}

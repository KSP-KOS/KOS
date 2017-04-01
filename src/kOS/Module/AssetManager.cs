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
        /// <para>
        /// There will only be one instance of AssetManager, accessed through here.
        /// You do not need to construct AssetManager explicitly, as one will be made
        /// at load time by the way the KSPAddon attribute is set up.
        /// </para><para>
        /// WARNING!  Be sure you call Instance.EnsureFontsLoaded() once before using
        /// any of the other members of Instance.  See the summary for
        /// EnsureFontsLoaded() to see why you have to do this.</para>
        /// </summary>
        public static AssetManager Instance {get; set;}

        /// <summary>
        /// All the fonts that were loaded by kOS will be in this dictionary,
        /// with font names as the keys.  It is possible for the same font
        /// to be in the dictionary more than once if it has multiple alias names.
        /// </summary>
        protected Dictionary<string,Font> Fonts {get; set;}

        protected List<string> FontNames;

        protected bool fontsDone;

        private static readonly string rootPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            

            Fonts = new Dictionary<string, Font>();
            FontNames = new List<String>();

            fontsDone = false;
        }

        /// <summary>
        /// Font loading has to wait until the game seems to be ready for it.
        /// (When this was being done in Awake() or Start(),
        /// it caused Unity to erase all glyph data for the default Arial font,
        /// thus breaking every mod that uses Unity's default GUI.skin.
        /// Moving this to happen later seems to avoid that bug, for unknown reasons.)
        /// Whenever you want to use AssetManager.Instance, make sure to call this
        /// first and it will load the fonts if it's the first time you've done so,
        /// else it will do nothing and have no effect.
        /// </summary>
        public void EnsureFontsLoaded()
        {
            if (fontsDone)
                return;            
            UpdateSystemFontLists();
            fontsDone = true;
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
            List<string> namesThatNoLongerExist = new List<string>(FontNames);
            foreach (string fontName in Font.GetOSInstalledFontNames())
            {
                if (!FontNames.Contains(fontName))
                {
                    // Only add those fonts which pass the monospace test:
                    if (GetSystemFontByNameAndSize(fontName, 13, true, false) != null)
                    {
                        FontNames.Add(fontName);
                    }
                }
                namesThatNoLongerExist.Remove(fontName);
            }
            // Any font name that used to be in the list but wasn't seen this time around
            // must be a font that has been uninstalled from the OS while KSP was running:
            foreach (string goneName in namesThatNoLongerExist)
            {
                FontNames.Remove(goneName);
            }
        }

        // Unity loads each differently sized version of a font as a new
        // dynamic font, so we have to track them separately by size:
        private string MakeKey(string name, int size)
        {
            return string.Format("{0}/{1}", name, size);
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
        /// <param name="size">point size for the desired font.</param>
        /// <param name="checkMono">If true, then perform a check for monospace and issue a warning and return null
        /// if it's not monospaced.</param>
        /// <param name="doErrorMessage">If true, then if the checkMono check (see above) fails, a message will
        /// appear on screen complaining about this as it returns a null, else it will return null silently.</param>
        public Font GetSystemFontByNameAndSize(string name, int size, bool checkMono, bool doErrorMessage = true)
        {
            // Now that a font is asked for, now we'll lazy-load it.

            // Make a string key from the name plus the size:
            string key = MakeKey(name, size);
            if ( (!Fonts.ContainsKey(key)) || Fonts[key] == null)
            {
                Fonts[key] = Font.CreateDynamicFontFromOSFont(name, size);
            }

            Font potentialReturn = Fonts[key];
            if (checkMono && !(IsFontMonospaced(potentialReturn)))
            {
                if (doErrorMessage)
                {
                    string msg = string.Format("{0} is proportional width.\nA monospaced font is required.", name);
                    ScreenMessages.PostScreenMessage(
                        string.Format("<color=#ff9900><size=20>{0}</size></color>", msg), 8, ScreenMessageStyle.UPPER_CENTER);
                }
                return null;
            }
            return potentialReturn;
        }

        /// <summary>
        /// Just like GetSystemFontByNameAndSize, except that you give it a list of
        /// multiple names and it will try each in turn until it finds a name
        /// that returns an existing font.  Only if all the names fail to find
        /// a hit will it return null.
        /// </summary>
        /// <returns>The system font by name.</returns>
        /// <param name="names">Names.</param>
        /// <param name="size">Size.</param>
        /// <param name="checkMono">If true, then perform a check for monospace and issue a warning and return null
        /// if it's not monospaced.</param>
        public Font GetSystemFontByNameAndSize(string[] names, int size, bool checkMono)
        {
            foreach (string name in names)
            {
                Font hit = GetSystemFontByNameAndSize(name, size, checkMono);
                if (hit != null)
                    return hit;
            }
            return null;
        }

        /// <summary>
        /// This will be whatever
        /// system font names existed the last time UpdateSystemFontLists()
        /// was called.  (If you install a new font to the OS and re-run
        /// UpdateSystemFontNames(), that new font name gets added to this
        /// list.)  If you install a new system font and don't call 
        /// UpdateSystemFontNames() again, the new font name won't be
        /// in this list yet.
        /// </summary>
        /// <returns>A list of the OS font names kOS knows about.</returns>
        public List<string> GetSystemFontNames()
        {
            return FontNames;
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
            System.Console.WriteLine("eraseme: X advance is " + prevWidth);

            f.GetCharacterInfo('i', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;
            System.Console.WriteLine("eraseme: i advance is " + prevWidth);

            f.GetCharacterInfo('W', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;
            System.Console.WriteLine("eraseme: W advance is " + prevWidth);

            f.GetCharacterInfo(' ', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;
            System.Console.WriteLine("eraseme: ' ' advance is " + prevWidth);

            f.GetCharacterInfo('_', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;
            System.Console.WriteLine("eraseme: _ advance is " + prevWidth);

            f.GetCharacterInfo(':', out chInfo);
            if (prevWidth != chInfo.advance)
                return false;
            prevWidth = chInfo.advance;
            System.Console.WriteLine("eraseme: : advance is " + prevWidth);

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

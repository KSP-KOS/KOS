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

        protected List<string> FontNames;

        private static readonly string rootPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            

            Fonts = new Dictionary<string, Font>();
            FontNames = new List<String>();

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
            List<string> namesThatNoLongerExist = new List<string>(FontNames);
            foreach (string fontName in Font.GetOSInstalledFontNames())
            {
                if (!FontNames.Contains(fontName))
                    FontNames.Add(fontName);
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
        public Font GetSystemFontByNameAndSize(string name, int size)
        {
            // Now that a font is asked for, now we'll lazy-load it.

            // Make a string key from the name plus the size:
            string key = MakeKey(name, size);
            if ( (!Fonts.ContainsKey(key)) || Fonts[key] == null)
            {
                Fonts[key] = Font.CreateDynamicFontFromOSFont(name, size);
            }
            return Fonts[key];
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
        public Font GetSystemFontByNameAndSize(string[] names, int size)
        {
            foreach (string name in names)
            {
                Font hit = GetSystemFontByNameAndSize(name, size);
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

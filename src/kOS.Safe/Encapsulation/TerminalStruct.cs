using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Terminal")]
    public class TerminalStruct : Structure
    {
        private readonly SafeSharedObjects shared;
        private TerminalInput terminalInput = null;

        // Some sanity values to prevent the terminal display from getting garbled up:
        // They may have to change after experimentation.
        protected const int MINROWS = 3;

        protected const int MAXROWS = 160;
        protected const int MINCOLUMNS = 15;
        protected const int MAXCOLUMNS = 255;
        
        protected const int MINCHARPIXELS = 4;
        protected const int MAXCHARPIXELS = 24;

        // TODO: To implement IsOpen, we'd have to make a kOS.Safe interface wrapper around TermWindow first.
        // That's more than I want to do in this update, I'm leaving it as a TODO for me or someone else:
        //
        // protected bool IsOpen { get { return shared.Window.IsOpen(); } set {if (value) shared.Window.Open(); else shared.Window.Close(); } }

        public TerminalStruct(SafeSharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        protected internal SafeSharedObjects Shared
        {
            get { return shared; }
        }

        private void InitializeSuffixes()
        {
            // TODO: Uncomment the following if IsOpen gets implemented later:
            // AddSuffix("ISOPEN", new SetSuffix<BooleanValue>(() => IsOpen, Isopen = value, "true=open, false=closed.  You can set it to open/close the window."));
            AddSuffix("HEIGHT", new ClampSetSuffix<ScalarValue>(() => Shared.Screen.RowCount,
                                                         value => Shared.Screen.SetSize(value, Shared.Screen.ColumnCount),
                                                         MINROWS,
                                                         MAXROWS,
                                                         "Get or Set the number of rows on the screen.  Value is limited to the range [" + MINROWS + "," + MAXROWS + "]"));
            AddSuffix("WIDTH", new ClampSetSuffix<ScalarValue>(() => Shared.Screen.ColumnCount,
                                                        value => Shared.Screen.SetSize(Shared.Screen.RowCount, value),
                                                        MINCOLUMNS,
                                                        MAXCOLUMNS,
                                                        "Get or Set the number of columns on the screen.  Value is limited to the range [" + MINCOLUMNS + "," + MAXCOLUMNS + "]"));
            AddSuffix("REVERSE", new SetSuffix<BooleanValue>(() => Shared.Screen.ReverseScreen,
                                                     value => Shared.Screen.ReverseScreen = value,
                                                     "Get or set the value of whether or not the terminal is in reversed mode."));
            AddSuffix("VISUALBEEP", new SetSuffix<BooleanValue>(() => Shared.Screen.VisualBeep,
                                                       value => Shared.Screen.VisualBeep = value,
                                                       "Get or set the value of whether or not the terminal shows beeps silently with a visual flash."));
            AddSuffix("BRIGHTNESS", new ClampSetSuffix<ScalarValue>(() => Shared.Screen.Brightness,
                                                                    value => Shared.Screen.Brightness = (float)value,
                                                                    0f,
                                                                    1f,
                                                                    "Screen Brightness, between 0.0 and 1.0"));
            AddSuffix("CHARWIDTH", new ClampSetSuffix<ScalarValue>(() => Shared.Screen.CharacterPixelWidth,
                                                                   value => Shared.Screen.CharacterPixelWidth = (int)value,
                                                                   MINCHARPIXELS,
                                                                   MAXCHARPIXELS,
                                                                   2,
                                                                   "Character width on in-game terminal screen in pixels"));
            AddSuffix("CHARHEIGHT", new ClampSetSuffix<ScalarValue>(() => Shared.Screen.CharacterPixelHeight,
                                                                    value => Shared.Screen.CharacterPixelHeight = (int)value,
                                                                    MINCHARPIXELS,
                                                                    MAXCHARPIXELS,
                                                                    2,
                                                                    "Character height on in-game terminal screen in pixels"));
            AddSuffix("INPUT", new Suffix<TerminalInput>(GetTerminalInputInstance));
        }
        
        public TerminalInput GetTerminalInputInstance()
        {
            if (terminalInput == null)
                terminalInput = new TerminalInput(shared);
            return terminalInput;
        }
        
        public override string ToString()
        {
            return string.Format("{0} Terminal", base.ToString());
        }
    }
}

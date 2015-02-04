using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class TerminalStruct : Structure
    {
        private readonly SharedObjects shared;

        // Some sanity values to prevent the terminal display from getting garbled up:
        // They may have to change after experimentation.
        protected const int MINROWS = 3;

        protected const int MAXROWS = 80;
        protected const int MINCOLUMNS = 15;
        protected const int MAXCOLUMNS = 160;

        // TODO: To implement IsOpen, we'd have to make a kOS.Safe interface wrapper around TermWindow first.
        // That's more than I want to do in this update, I'm leaving it as a TODO for me or someone else:
        //
        // protected bool IsOpen { get { return shared.Window.IsOpen(); } set {if (value) shared.Window.Open(); else shared.Window.Close(); } }

        public TerminalStruct(SharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        protected internal SharedObjects Shared
        {
            get { return shared; }
        }

        private void InitializeSuffixes()
        {
            // TODO: Uncomment the following if IsOpen gets implemented later:
            // AddSuffix("ISOPEN", new SetSuffix<bool>(() => IsOpen, Isopen = value, "true=open, false=closed.  You can set it to open/close the window."));
            AddSuffix("HEIGHT", new ClampSetSuffix<int>(() => Shared.Screen.RowCount,
                                                         value => Shared.Screen.SetSize(value, Shared.Screen.ColumnCount),
                                                         MINROWS,
                                                         MAXROWS,
                                                         "Get or Set the number of rows on the screen.  Value is limited to the range [" + MINROWS + "," + MAXROWS + "]"));
            AddSuffix("WIDTH", new ClampSetSuffix<int>(() => Shared.Screen.ColumnCount,
                                                        value => Shared.Screen.SetSize(Shared.Screen.RowCount, value),
                                                        MINCOLUMNS,
                                                        MAXCOLUMNS,
                                                        "Get or Set the number of columns on the screen.  Value is limited to the range [" + MINCOLUMNS + "," + MAXCOLUMNS + "]"));
        }

        public override string ToString()
        {
            return string.Format("{0} Terminal", base.ToString());
        }
    }
}
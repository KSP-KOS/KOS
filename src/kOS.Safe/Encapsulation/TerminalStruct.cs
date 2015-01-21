using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe;

namespace kOS.Safe.Encapsulation
{
    public class TerminalStruct : Structure
    {
        protected SharedObjects shared;
        
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
        
        private void InitializeSuffixes()
        {
            // TODO: Uncomment the following if IsOpen gets implemented later:
            // AddSuffix("ISOPEN", new SetSuffix<bool>(() => IsOpen, Isopen = value, "true=open, false=closed.  You can set it to open/close the window."));
            AddSuffix("HEIGHT",  new ClampSetSuffix<int>(() => shared.Screen.RowCount,
                                                         value => shared.Screen.SetSize(value, shared.Screen.ColumnCount),
                                                         MINROWS,
                                                         MAXROWS,
                                                         "Get or Set the number of rows on the screen.  Value is limited to the range ["+MINROWS+","+MAXROWS+"]"));
            AddSuffix("WIDTH",  new ClampSetSuffix<int>(() => shared.Screen.ColumnCount,
                                                        value => shared.Screen.SetSize(shared.Screen.RowCount, value),
                                                        MINCOLUMNS,
                                                        MAXCOLUMNS,
                                                        "Get or Set the number of columns on the screen.  Value is limited to the range ["+MINCOLUMNS+","+MAXCOLUMNS+"]"));
        }

        public override string ToString()
        {
            return string.Format("{0} Terminal", base.ToString());
        }
    }

}
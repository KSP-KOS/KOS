using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using kOS.Safe.Utilities;
using UnityEngine;
using kOS.Module;
using kOS.Safe.UserIO;
using Math = System.Math;

namespace kOS.UserIO
{
    /// <summary>
    /// Handles the reading and writing of the not-yet-connected menu for
    /// telnet clients that aren't attached to a CPU at the moment.
    /// It is a Monobehavior rather than a thread, because it needs to talk
    /// to the KSP API and KSP is not threadsafe.  All its work is done
    /// through Update().
    /// </summary>
    public class TelnetWelcomeMenu : MonoBehaviour
    {
        private TelnetSingletonServer telnetServer;
        private List<kOSProcessor> availableCPUs;
        private DateTime lastMenuQueryTime;
        private readonly StringBuilder localMenuBuffer;
        private bool firstTime;
        private volatile bool forceMenuReprint;
        private MenuLevel currentLevel;

        private abstract class MenuLevel
        {
            public virtual MenuLevel parentMenu { get { return new OuterMenu (); } }

            public virtual List<String> helpText { get; } = new List<String> { };

            public virtual List<List<String> > headers { get; } = new List<List<String> > {
                new List<String> { "Menu", "" },
                new List<String> { "Pick", "" },
                new List<String> { "----", "----------------" }
            };
            public virtual String format { get; } = "{0,4} {1}";

            public virtual void PrintMenu (TelnetWelcomeMenu parent)
            {

                List<String> displayChoices = new List<string> { };
                int longestLength = 0;
                foreach (List<String> hdrLine in headers) {
                    String choice = String.Format (format, hdrLine.Cast<object> ().ToArray ());
                    displayChoices.Add (choice);
                    longestLength = Math.Max (choice.Length, longestLength);
                }
                int i = 1;
                foreach (object[] bodyLine in this.MenuItems()) {
                    string choice = String.Format (format, new object[] { "[" + i.ToString () + "]" }.Concat (bodyLine).ToArray ());
                    displayChoices.Add (choice);
                    longestLength = Math.Max (choice.Length, longestLength);
                    i++;
                }
                foreach (string choice in displayChoices) {
                    string choicePaddedToLongest = choice + new String (' ', (longestLength - choice.Length));
                    parent.telnetServer.Write (parent.CenterPadded (choicePaddedToLongest, ' ') + (char)UnicodeCommand.STARTNEXTLINE);
                }
            }

            public virtual List< object[] > MenuItems ()
            { // Deliberately empty, so this is allowed not to be implemented.
                return new List< object[] > ();
            }

            public virtual int ItemCount ()
            {
                return MenuItems ().Count;
            }

            public virtual bool ItemsChanged ()
            {
                return false;
            }

            public virtual void Selected (TelnetWelcomeMenu parent, int k)
            {
                return;
            }
        }

        private class OuterMenu : MenuLevel
        {
            public override MenuLevel parentMenu { get { return this; } }

            public override String format { get; } = "{0,4} {1}";

            static List<object[] > theList = new List<object[] > {
                new object[]{ "CPU List" },
                new object[]{ "Order a Launch (Unimplemented)" },
                new object[]{ "Vessel List" },
                new object[]{ "The Fourth Wall" },
            };

            public override List<object[] > MenuItems ()
            {
                return theList;
            }

            public override bool ItemsChanged ()
            {
                return false;
            }

            public override void Selected (TelnetWelcomeMenu parent, int k)
            {
                switch (k) {
                case 1:
                    parent.currentLevel = new CPUMenu ();
                    break;
                case 2:
                    parent.currentLevel = new LaunchOrderMenu ();
                    break;
                case 3:
                    parent.currentLevel = new VesselSwitchMenu ();
                    break;
                case 4:
                    parent.currentLevel = new FourthWallMenu ();
                    break;
                }
            }
        }

        private class CPUMenu : MenuLevel
        {
            public override List<List<String> > headers { get; } = new List<List<String> > {
                    new List<String>{"Menu", "GUI ", " Other ", "", "", ""},
                    new List<String>{"Pick", "Open", "Telnets", "", "Vessel Name", "(CPU tagname)"},
                    new List<String>{"----", "----", "-------", "", "--------------------------------", "" }
            };
            public override String format { get; } = "{0,4} {1,4} {2,4} {3} {4}";

            public List<kOSProcessor> availableCPUs = new List<kOSProcessor> ();

            public override List<String> helpText { get; } = new List<String> {
                        "Choose a CPU to attach to by typing a " +
                        "selection number and pressing return/enter. " +
                        "Or enter [Q] to quit terminal server, " +
                        "or [*] to go up a menu level.",
                        "(After attaching, you can (D)etach and return " +
                        "to this menu by pressing Control-D as the first " +
                        "character on a new command line.)"
                };

            public override bool ItemsChanged ()
            {
                List<kOSProcessor> newList = kOSProcessor.AllInstances ();
                bool itChanged = false;

                if (newList.Count != availableCPUs.Count)
                    itChanged = true;
                else
                    for (int i = 0; i < newList.Count; ++i)
                        if (newList [i] != availableCPUs [i])
                            itChanged = true;

                availableCPUs = newList;
                return itChanged;
            }

            public override List<object[]> MenuItems ()
            {
                List<object[]> rv = new List<object[]> ();
                foreach (kOSProcessor kModule in availableCPUs) {
                    Part thisPart = kModule.part;
                    KOSNameTag partTag = thisPart.Modules.OfType<KOSNameTag> ().FirstOrDefault ();
                    string partLabel = String.Format ("{0}({1})",
                                                       thisPart.partInfo.title.Split (' ') [0], // just the first word of the name, i.e "CX-4181"
                                                       ((partTag == null) ? "" : partTag.nameTag)
                                                   );
                    Vessel vessel = (thisPart == null) ? null/*can this even happen?*/ : thisPart.vessel;
                    string vesselLabel = (vessel == null) ? "<no vessel>"/*can this even happen?*/ : vessel.GetName ();

                    bool guiOpen = kModule.GetWindow ().IsOpen;
                    int numTelnets = kModule.GetWindow ().NumTelnets ();
                    rv.Add (new object[]{ (guiOpen ? "yes" : "no"), numTelnets, vesselLabel, "(" + partLabel + ")" });
                }
                return rv;
            }

            public override void Selected (TelnetWelcomeMenu parent, int pickNumber)
            {
                parent.telnetServer.ConnectToProcessor (availableCPUs [pickNumber - 1]);
            }
        }

        private class FourthWallMenu : MenuLevel
        {
            Suffixed.KUniverseValue v = new Suffixed.KUniverseValue(null); // We just don't use anything that hits the shared objects.

            public override List<object[] > MenuItems ()
            {
                List<object[] > theList = new List<object[] > {
                    new object[] { "Revert to Launch " + (v.CanRevertToLaunch() ? "" : "(Unavailable)" ) },
                    new object[] { "Revert to Editor " + (v.CanRevvertToEditor() ? "" : "(Unavailable)" ) },
                    new object[] { "Load Saved Game (Unimplemented)" }
                };
                return theList;
            }

            public override void Selected (TelnetWelcomeMenu parent, int pickNumber)
            {
                switch (pickNumber) {
                case 1:
                    v.RevertToLaunch();
                    break;
                case 2:
                    v.RevertToEditor();
                    break;
                case 3:
                    try {
                        HighLogic.SaveFolder = "kossitude";
                        Game game = GamePersistence.LoadGame ("quicksave", HighLogic.SaveFolder, true, false);
                        FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
                    } catch (Exception e) {
                        SafeHouse.Logger.Log (e.Message);
                    }
                    break;
                }
            }
        }

        private class LaunchOrderMenu : MenuLevel
        {
        }

        private class VesselSwitchMenu : MenuLevel
        {

            List<Vessel> availableVessels = new List<Vessel> ();

            public override bool ItemsChanged ()
            {
                var vl = FlightGlobals.Vessels.Where (v => v.DiscoveryInfo.Level == DiscoveryLevels.Owned);
                if (availableVessels.SequenceEqual (vl))
                    return false;
                availableVessels = vl.ToList ();
                return true;
            }

            public override List<object[]> MenuItems ()
            {
                List<object[]> rv = new List<object[]> ();
                foreach (var vessel in availableVessels) {
                    rv.Add (new object[]{ vessel.vesselName });
                }
                return rv;
            }

            public override void Selected (TelnetWelcomeMenu parent, int pickNumber)
            {
                Vessel vessel = availableVessels [pickNumber - 1];
                if (!vessel.isActiveVessel) {
                    FlightGlobals.SetActiveVessel (vessel);
                }
            }
        }

        // Because this will be created as a Unity Gameobject, it has to have a parameterless constructor.
        // Actual setup args will be in the Setup() method below.
        public TelnetWelcomeMenu()
        {
            firstTime = true;
            currentLevel = new CPUMenu();
            localMenuBuffer = new StringBuilder();
            availableCPUs = new List<kOSProcessor>();
        }
        
        public void Setup(TelnetSingletonServer tServer)
        {
            telnetServer = tServer;
            lastMenuQueryTime = DateTime.MinValue; // Force a stale timestamp the first time.
            telnetServer.Write( (char)UnicodeCommand.TITLEBEGIN + "kOS Terminal Server Welcome Menu" + (char)UnicodeCommand.TITLEEND );
            forceMenuReprint = true; // force it to print the menu once the first time regardless of the state of the CPU list.
        }
        
        public void Quit()
        {
            enabled = false;
            telnetServer.StopListening();
        }
        
        public void Detach()
        {
            enabled = false;
            telnetServer = null;
        }
        
        /// <summary>
        /// The telnet server should tell me when the client got resized by calling this.
        /// I will reprint my menu in that case, since it is calculated by terminal width.
        /// </summary>
        public void NotifyResize()
        {
            forceMenuReprint = true;
        }
                           
        public virtual void Update()
        {
            if (telnetServer == null)
            {
                enabled = false; // turn me off.  I'm done.  I should get garbage collected shortly.
                return;
            }
            
            // Regularly check to see if the CPU list has changed while the user was sitting
            // on the menu.  If so, reprint it:
            // Querying the list of CPU's can be a little expensive, so don't do it too often:
            if (DateTime.Now > lastMenuQueryTime + TimeSpan.FromMilliseconds(1000))
            {
                if (forceMenuReprint)
                    telnetServer.Write((char)UnicodeCommand.CLEARSCREEN); // if we HAVE to reprint - do so on a clear screen.
                bool listChanged = ActiveListChanged();
                if (!firstTime && listChanged)
                    telnetServer.Write("--(List has Changed)--" + (char)UnicodeCommand.STARTNEXTLINE);
                firstTime = false;
                if (listChanged || forceMenuReprint )
                    PrintActiveMenu();
            }
            
            while (telnetServer.InputWaiting())
            {
                char ch = telnetServer.ReadChar();
                switch (ch)
                {
                    case (char)UnicodeCommand.STARTNEXTLINE:
                        LineEntered();
                        break;
                    // Implement crude input editing (backspace only - map delete to the same as backspace) in this menu prompt:
                    case (char)UnicodeCommand.DELETELEFT:
                    case (char)UnicodeCommand.DELETERIGHT:
                        if (localMenuBuffer.Length > 0)
                            localMenuBuffer.Remove(localMenuBuffer.Length-1,1);
                        telnetServer.Write((char)0x08+" "+(char)0x08); // backspace space backspace.
                        break;
                    default:
                        localMenuBuffer.Append(ch);
                        telnetServer.Write(ch);
                        break;
                }
            }
        }

        public void PrintActiveMenu() {
            localMenuBuffer.Remove(0,localMenuBuffer.Length); // Any time the menu is reprinted, clear out any previous buffer text.
            telnetServer.ReadAll(); // Consume and throw away any readahead typing that preceeded the printing of this menu.

            forceMenuReprint = false;

            telnetServer.Write("Terminal: type = " +
                               telnetServer.ClientTerminalType +
                               ", size = "
                               + telnetServer.ClientWidth + "x" + telnetServer.ClientHeight +
                               (char)UnicodeCommand.STARTNEXTLINE);
            telnetServer.Write(CenterPadded("",'_')/*line of '-' chars*/ + (char)UnicodeCommand.STARTNEXTLINE);
        currentLevel.PrintMenu(this);

        if (currentLevel.ItemCount() > 0) {
            telnetServer.Write(CenterPadded("",'-')/*line of '-' chars*/ + (char)UnicodeCommand.STARTNEXTLINE);
            foreach (String helpStr in currentLevel.helpText) {
                telnetServer.Write(
                        (char)UnicodeCommand.STARTNEXTLINE +
                        WordBreak(helpStr) +
                        (char)UnicodeCommand.STARTNEXTLINE
                        );
            }
            telnetServer.Write(CenterPadded("", '-')/*line of '-' chars*/ +
                    (char)UnicodeCommand.STARTNEXTLINE +
                    "> ");
        }
            else
                telnetServer.Write(CenterPadded(String.Format("{0,20}", "<NONE>"), ' ') + (char)UnicodeCommand.STARTNEXTLINE);
        }
        
        public void LineEntered()
        {
            if (localMenuBuffer.Length == 0)
                return;
            string cmd = localMenuBuffer.ToString();
            SafeHouse.Logger.SuperVerbose( "TelnetWelcomeMenu.LineEntered(): String from client was: [" + cmd + "]");
            localMenuBuffer.Remove(0,localMenuBuffer.Length); // clear the buffer for next line.
            
            if (String.Equals(cmd.Substring(0,1),"Q",StringComparison.CurrentCultureIgnoreCase))
            {
                Quit();
                return;
            }
            if (String.Equals(cmd.Substring(0,1),"*",StringComparison.CurrentCultureIgnoreCase))
            {
                StarCommand(cmd);
                return;
            }
            int pickNumber;
            if (!int.TryParse(cmd, out pickNumber) )
            {
                telnetServer.Write("Garbled selection. Try again." + (char)UnicodeCommand.STARTNEXTLINE);
                forceMenuReprint = true;
                return;
            }
            if (pickNumber <= 0 || pickNumber > currentLevel.ItemCount())
            {
                telnetServer.Write("No such number (" + pickNumber + ") on the menu." + (char)UnicodeCommand.STARTNEXTLINE);
                forceMenuReprint = true;
                return;
            }

            forceMenuReprint = true;
            currentLevel.Selected(this, pickNumber);
            // Quit(); - uncomment to make it so that the TelnetWelcomeMenu aborts telnet when done - for testing purposes.
        }

        public void StarCommand(String cmd) {
            // Separated out for possible additional levels of tree, etc.
            currentLevel=currentLevel.parentMenu;
            // Might as well let you dial the next level, if you know what you're dialing for.
            localMenuBuffer.Append(cmd.Substring(1));
            forceMenuReprint = true;
            LineEntered();
        }
        
        private bool ActiveListChanged() {
        lastMenuQueryTime = DateTime.Now;
        return currentLevel.ItemsChanged();
        }

        /// <summary>
        /// For writing out a message to the screen, trying to center it according to
        /// what the telnet client claimed its current width is:
        /// </summary>
        /// <param name="msg">message to center</param>
        /// <param name="padChar">character to pad with. ' ' for space, '=' or '-' to draw a line across.</param>
        /// <returns>the padded version of the string</returns>
        private string CenterPadded(string msg, char padChar)
        {
            int width = telnetServer.ClientWidth;
            int halfPadWidth = (width - msg.Length)/2;
            string padString = new String(padChar, halfPadWidth);
            
            return padString + msg + (padChar == ' ' ? "" : padString);
        }
        
        /// <summary>
        /// Break up a potentially long line so it only breaks on spaces and not
        /// in the middle of a word.  Looks at the telnet client's width to decide
        /// how to break.
        /// </summary>
        /// <param name="msg">string to attempt to print.</param>
        /// <returns>new string that has the linebreaks added as \r\n</returns>
        private string WordBreak(string msg)
        {
            char[] msgAsArray = msg.ToCharArray();

            int width = telnetServer.ClientWidth;            
            int lineStartPos = 0;
            int lastSpacePos = -1;
            for (int i = 0 ; i < msgAsArray.Length ; ++i)
            {
                if (msgAsArray[i] == ' ')
                    lastSpacePos = i;
                if ( (i - lineStartPos) >= width)
                {
                    msgAsArray[lastSpacePos] = (char)UnicodeCommand.STARTNEXTLINE;
                    lineStartPos = lastSpacePos+1;
                }
            }
            return new string(msgAsArray);
        }
        
    }
}

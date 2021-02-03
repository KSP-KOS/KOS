using System;
using System.Globalization;
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

        public bool IsAttached { get { return telnetServer != null; } }

        // Because this will be created as a Unity Gameobject, it has to have a parameterless constructor.
        // Actual setup args will be in the Attach() method below.
        public TelnetWelcomeMenu()
        {
            firstTime = true;
            localMenuBuffer = new StringBuilder();
            availableCPUs = new List<kOSProcessor>();
        }
        
        public void Attach(TelnetSingletonServer tServer)
        {
            telnetServer = tServer;
            lastMenuQueryTime = DateTime.MinValue; // Force a stale timestamp the first time.
            telnetServer.Write( (char)UnicodeCommand.TITLEBEGIN + "kOS Terminal Server Welcome Menu" + (char)UnicodeCommand.TITLEEND );
            forceMenuReprint = true; // force it to print the menu once the first time regardless of the state of the CPU list.
            firstTime = true;
        }

        public void Detach()
        {
            telnetServer = null;
        }

        public void Quit()
        {
            telnetServer.StopListening();
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
                // Do not disable the monobehaviour because the only place that knows
                // when to turn it back on again is in threads of TelnetSingletonServer,
                // and Unity hates it when you try to perform enabled=true from another thread.
                // So instead we have to leave this thing running here so it notices when
                // telnetServer gets attached again, and it can re-enable *itself*.
                return;
            }
            
            // Regularly check to see if the CPU list has changed while the user was sitting
            // on the menu.  If so, reprint it:
            // Querying the list of CPU's can be a little expensive, so don't do it too often:
            if (DateTime.Now > lastMenuQueryTime + System.TimeSpan.FromMilliseconds(1000))
            {
                if (forceMenuReprint)
                    telnetServer.Write((char)UnicodeCommand.CLEARSCREEN); // if we HAVE to reprint - do so on a clear screen.
                bool listChanged = CPUListChanged();
                if (!firstTime && listChanged)
                    telnetServer.Write("--(List of CPU's has Changed)--" + (char)UnicodeCommand.STARTNEXTLINE);
                firstTime = false;
                if (listChanged || forceMenuReprint )
                    PrintCPUMenu();
            }
            
            while (telnetServer != null && // telnetServer can become null in the midst of this loop if you detach/attach.
                telnetServer.InputWaiting())
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
            int pickNumber;
            if (!int.TryParse(cmd, NumberStyles.Integer, CultureInfo.InvariantCulture, out pickNumber) )
            {
                telnetServer.Write("Garbled selection. Try again." + (char)UnicodeCommand.STARTNEXTLINE);
                forceMenuReprint = true;
                return;
            }
            if (pickNumber <= 0 || pickNumber > availableCPUs.Count)
            {
                telnetServer.Write("No such number (" + pickNumber + ") on the menu." + (char)UnicodeCommand.STARTNEXTLINE);
                forceMenuReprint = true;
                return;
            }
            telnetServer.ConnectToProcessor(availableCPUs[pickNumber-1]);
            // Quit(); - uncomment to make it so that the TelnetWelcomeMenu aborts telnet when done - for testing purposes.
        }
        
        private bool CPUListChanged()
        {
            List<kOSProcessor> newList = kOSProcessor.AllInstances();
            bool itChanged = false;

            if (newList.Count != availableCPUs.Count)
                itChanged = true;
            else
                for( int i = 0; i < newList.Count ; ++i)
                    if (newList[i] != availableCPUs[i])
                        itChanged = true;

            availableCPUs = newList;
            lastMenuQueryTime = DateTime.Now;
            return itChanged;
        }

        private void PrintCPUMenu()
        {
            localMenuBuffer.Remove(0,localMenuBuffer.Length); // Any time the menu is reprinted, clear out any previous buffer text.
            telnetServer.ReadAll(); // Consume and throw away any readahead typing that preceeded the printing of this menu.

            kOSProcessor.SortAllInstances();
            CPUListChanged();


            forceMenuReprint = false;

            telnetServer.Write("Terminal: type = " +
                               telnetServer.ClientTerminalType +
                               ", size = "
                               + telnetServer.ClientWidth + "x" + telnetServer.ClientHeight +
                               (char)UnicodeCommand.STARTNEXTLINE);
            telnetServer.Write(CenterPadded("",'_')/*line of '-' chars*/ + (char)UnicodeCommand.STARTNEXTLINE);

            const string FORMATTER = "{0,4} {1,4} {2,4} {3} {4} {5}";

            int userPickNum = 1;
            int longestLength = 0;
            List<string> displayChoices = new List<string>
            {
                String.Format(FORMATTER, "Menu", "GUI ", " Other ", "", "", ""),
                String.Format(FORMATTER, "Pick", "Open", "Telnets", "", "Vessel Name", "(CPU tagname)"),
                String.Format(FORMATTER, "----", "----", "-------", "", "--------------------------------", "")
            };
            longestLength = displayChoices[2].Length;
            foreach (kOSProcessor kModule in availableCPUs)
            {
                Part thisPart = kModule.part;
                KOSNameTag partTag = thisPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
                string partLabel = String.Format("{0}({1})",
                                             thisPart.partInfo.title.Split(' ')[0], // just the first word of the name, i.e "CX-4181"
                                             ((partTag == null) ? "" : partTag.nameTag)
                                            );
                Vessel vessel = (thisPart == null) ? null/*can this even happen?*/ : thisPart.vessel;
                string vesselLabel = (vessel == null) ? "<no vessel>"/*can this even happen?*/ : vessel.GetName();
                
                bool guiOpen = kModule.GetWindow().IsOpen;
                int numTelnets = kModule.GetWindow().NumTelnets();
                string choice = String.Format(FORMATTER, "["+userPickNum+"]", (guiOpen ? "yes": "no"), numTelnets, "   ", vesselLabel, "("+partLabel+")");
                displayChoices.Add(choice);
                longestLength = Math.Max(choice.Length, longestLength);
                ++userPickNum;
            }
            foreach (string choice in displayChoices)
            {
                string choicePaddedToLongest = choice + new String(' ',(longestLength - choice.Length));
                telnetServer.Write(CenterPadded(choicePaddedToLongest, ' ') + (char)UnicodeCommand.STARTNEXTLINE);
            }
            
            if (availableCPUs.Count > 0)
                telnetServer.Write(CenterPadded("",'-')/*line of '-' chars*/ + (char)UnicodeCommand.STARTNEXTLINE +
                                   WordBreak("Choose a CPU to attach to by typing a " +
                                             "selection number and pressing return/enter. " +
                                             "Or enter [Q] to quit terminal server.") +
                                   (char)UnicodeCommand.STARTNEXTLINE +
                                   (char)UnicodeCommand.STARTNEXTLINE +
                                   WordBreak("(After attaching, you can (D)etach and return " +
                                             "to this menu by pressing Control-D as the first " +
                                             "character on a new command line.)") +
                                   (char)UnicodeCommand.STARTNEXTLINE +
                                   CenterPadded("", '-')/*line of '-' chars*/ +
                                   (char)UnicodeCommand.STARTNEXTLINE +
                                   "> ");
            else
                telnetServer.Write(CenterPadded(String.Format(FORMATTER,"", "", "", "", "<NONE>", ""), ' ') + (char)UnicodeCommand.STARTNEXTLINE);

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

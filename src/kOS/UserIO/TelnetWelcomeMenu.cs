using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using kOS.Module;

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
        private StringBuilder localMenuBuffer;
        private bool firstTime;
        private bool forceMenuReprint;
        
        // Because this will be created as a Unity Gameobject, it has to have a parameterless constructor.
        // Actual setup args will be in the Setup() method below.
        public TelnetWelcomeMenu()
        {
            firstTime = true;
            localMenuBuffer = new StringBuilder();
            availableCPUs = new List<kOSProcessor>();
        }
        
        public void Setup(TelnetSingletonServer tserver)
        {
            telnetServer = tserver;
            lastMenuQueryTime = System.DateTime.MinValue; // Force a stale timestamp the first time.
            telnetServer.Write( (char)UnicodeCommand.TITLEBEGIN + "kOS Terminal Server Welcome Menu" + (char)UnicodeCommand.TITLEEND );
            forceMenuReprint = true; // force it to print the menu once the first time regardless of the state of the CPU list.
        }
        
        public void Quit()
        {
            telnetServer.StopListening();
            enabled = false;
        }
                           
        public virtual void Update()
        {
            
            // Regularly check to see if the CPU list has changed while the user was sitting
            // on the menu.  If so, reprint it:
            // Querying the list of CPU's can be a little expensive, so don't do it too often:
            if (System.DateTime.Now > lastMenuQueryTime + TimeSpan.FromMilliseconds(1000))
            {
                bool listChanged = CPUListChanged();
                if (!firstTime && listChanged)
                    telnetServer.Write("--(List of CPU's has Changed)--\r\n");
                firstTime = false;
                if (listChanged || forceMenuReprint )
                    PrintCPUMenu();
            }
            
            while (telnetServer.InputWaiting())
            {
                char ch = telnetServer.ReadChar();
                
                switch (ch)
                {
                    case (char)UnicodeCommand.NEWLINERETURN:
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
            kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "TelnetWelcomeMenu.LineEntered(): String from client was: [" + cmd.ToString() + "]");
            localMenuBuffer.Remove(0,localMenuBuffer.Length); // clear the buffer for next line.
            
            if (String.Equals(cmd.Substring(0,1),"Q",StringComparison.CurrentCultureIgnoreCase))
            {
                Quit();
                return;
            }
            int pickNumber;
            if (!int.TryParse(cmd, out pickNumber) )
            {
                telnetServer.Write("Garbled selection. Try again.\r\n");
                forceMenuReprint = true;
                return;
            }
            if (pickNumber <= 0 || pickNumber > availableCPUs.Count)
            {
                telnetServer.Write("No such number (" + pickNumber + ") on the menu.\r\n");
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

            Console.WriteLine("in CPUListChanged(): itChanged = "+itChanged+", newList.Count="+newList.Count+", availableCPUs.Count="+availableCPUs.Count);
            availableCPUs = newList;
            lastMenuQueryTime = System.DateTime.Now;
            return itChanged;
        }

        private void PrintCPUMenu()
        {
            localMenuBuffer.Remove(0,localMenuBuffer.Length); // Any time the menu is reprinted, clear out any previous buffer text.
            telnetServer.ReadAll(); // Consume and throw away any readahead typing that preceeded the printing of this menu.

            forceMenuReprint = false;

            telnetServer.Write("Terminal: type = " + telnetServer.ClientTerminalType + ", size = " + telnetServer.ClientWidth + "x" + telnetServer.ClientHeight + "\r\n");
            telnetServer.Write("    Available CPUS for Connection:\r\n" +
                               "+---------------------------------------+\r\n");
            
            int userPickNum = 1; 
            bool atLeastOne = false;
            foreach (kOSProcessor kModule in availableCPUs)
            {
                atLeastOne = true;
                Part thisPart = kModule.part;
                KOSNameTag partTag = thisPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
                string partLabel = String.Format("{0}({1})",
                                             thisPart.partInfo.title.Split(' ')[0], // just the first word of the name, i.e "CX-4181"
                                             ((partTag == null) ? "" : partTag.nameTag)
                                            );
                Vessel vessel = (thisPart == null) ? null/*can this even happen?*/ : thisPart.vessel;
                string vesselLabel = (vessel == null) ? "<no vessel>"/*can this even happen?*/ : vessel.GetName();
                
                telnetServer.Write(String.Format("[{0}] Vessel({1}), CPU({2})\r\n",userPickNum, vesselLabel, partLabel));
                ++userPickNum;
            }
            if (atLeastOne)
                telnetServer.Write("+---------------------------------------+\r\n" +
                                   "Choose a CPU to attach to by typing a\r\n" +
                                   "selection number and pressing return/enter.\r\n" +
                                   "Or enter [Q] to quit terminal server.\r\n" +
                                   "\r\n" +
                                   "(After attaching, you can (D)etach and return\r\n" +
                                   "to this menu by pressing Control-D as the first\r\n" +
                                   "character on a new command line.)\r\n" +
                                   "+---------------------------------------+\r\n" +
                                   "> ");
            else
                telnetServer.Write("\t<NONE>\r\n");

        }
        
    }
}

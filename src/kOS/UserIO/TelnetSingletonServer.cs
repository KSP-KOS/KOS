using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Module;
using UnityEngine;

namespace kOS.UserIO
{
    /// <summary>
    /// Spawned by TelnetMainServer, this handles the connection to just one
    /// single telnet client, leaving the main server free to go back to listening
    /// to other new connecting clients.
    /// </summary>
    public class TelnetSingletonServer : MonoBehaviour
    {
        // ReSharper disable SuggestUseVarKeywordEvident
        // ReSharper disable RedundantDefaultFieldInitializer
        private volatile TcpClient client;
        private volatile NetworkStream stream;
        
        private List<kOSProcessor> availableCPUs;
        private DateTime lastMenuQueryTime;

        private kOSProcessor connectedCPU = null; // when not connected, it will be on the local menu.
        
        private volatile byte[] rawReadBuffer = new byte[128]; // use small chunks
        private string localMenuBuffer = "";
        
        private Thread inThread;
        // ReSharper enable RedundantDefaultFieldInitializer
        // ReSharper enable SuggestUseVarKeywordEvident

        // Special telnet protocol bytes with magic meaning, taken from interet RFC's:
        // Many of these will go unused at first, but it's important to get them down for future
        // embetterment.  Uncomment them if you start needing to use one.

        private const byte RFC854_SE       = 240;  //  End of subnegotiation parameters.
        private const byte RFC854_NOP      = 241;  //  No operation.
        private const byte RFC854_DATAMARK = 242;  //  The data stream portion of a Synch.
                                                  // This should always be accompanied
                                                  // by a TCP Urgent notification.
        private const byte RFC854_BREAK    = 243;  //  NVT character BRK.
        private const byte RFC854_IP       = 244;  //  The function IP.
        private const byte RFC854_AO       = 245;  //  The function AO.
        private const byte RFC854_AYT      = 246;  //  The function AYT.
        private const byte RFC854_EC       = 247;  //  The function EC.
        private const byte RFC854_EL       = 248;  // The function EL.
        private const byte RFC864_GA       = 249;  //  The GA signal.
        private const byte RFC854_SB       = 250;  //  Indicates that what follows is
                                                   // subnegotiation of the indicated
                                                   // option.
        private const byte RFC854_WILL     = 251;  //  Indicates the desire to begin
                                                   // performing, or confirmation that
                                                   // you are now performing, the
                                                   // indicated option.
        private const byte RFC854_WONT     = 252;  //   Indicates the refusal to perform,
                                                   // or continue performing, the
                                                   // indicated option.
        private const byte RFC854_DO       = 253;  // Indicates the request that the
                                                   // other party perform, or
                                                   // confirmation that you are expecting
                                                   // the other party to perform, the
                                                   // indicated option.
        private const byte RFC854_DONT     = 254; // Indicates the demand that the
                                                  // other party stop performing,
                                                  // or confirmation that you are no
                                                  // longer expecting the other party
                                                  // to perform, the indicated option.
        private const byte RFC854_IAC      = 255; // Interpret as Command - the escape char to start all the above things.
        
        private const byte RFC857_ECHO     = 1;
        
        private const byte RFC858_SUPPRESS_GOAHEAD = 3;
        
        private const byte RFC1184_LINEMODE = 34;
        private const byte RFC1184_MODE     = 1;
        private const byte RFC1184_EDIT     = 1;
        private const byte RFC1184_TRAPSIG  = 2;
        private const byte RFC1184_MODE_ACK = 4;
        private const byte RFC1184_SOFT_TAB = 8;
        private const byte RFC1184_LIT_ECHO = 16;  
        
        public TelnetSingletonServer(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            inThread = new Thread(DoInThread);
            
        }
        
        public void SendText(string str)
        {
            byte[] outBuff = System.Text.Encoding.UTF8.GetBytes(str);
            stream.Write(outBuff, 0, outBuff.Length);
        }
        
        public void StopListening()
        {
            inThread.Abort();
            inThread = null; // dispose old thread.
            stream.Close();
        }

        public void StartListening()
        {
            inThread.Start();
            SendText("Connected to kOS Telnet Server.\r\n");
            
            LineAtATime(false);
            
            // The proper protocol demands that we pay attention to whether or not the client telnet session actually knows how to
            // do those requested settings, and negotiate back to it.  But nobody makes telnet clients anymore that are that bad
            // that they can't do these things so I'll be lazy and just assume they worked without paying attention to the responses sent back.
        }
        
        public void LineAtATime(bool buffered)
        {
            if (buffered)
            {
                // Send some telnet protocol stuff telling the other side how I'd like it to behave:
                stream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC857_ECHO},0, 3); // don't local-echo.
                stream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // send one char at a time without buffering lines.
            }
            else
            {
                // Send some telnet protocol stuff telling the other side how I'd like it to behave:
                stream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC857_ECHO},0, 3); // don't local-echo.
                stream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // send one char at a time without buffering lines.
            }
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

            return itChanged;
        }

        private void PrintCPUMenu()
        {
            SendText("______________________________\r\n");
            SendText("Available CPUS for Connection:\r\n");
            
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
                
                SendText(String.Format("  [{0}] Vessel({1}), CPU({2})\r\n",userPickNum, vesselLabel, partLabel));
                ++userPickNum;
            }
            if (atLeastOne)
                SendText("Type a selection number and press return/enter.\r\n" +
                         "Or enter [Q] to quit.\r\n" +
                         "> ");
            else
                SendText("<NONE>\r\n");
        }
        
        private void DoInThread()
        {
            // All threads in a KSP mod must be careful NOT to access any KSP or Unity methods
            // from inside their execution, because KSP and Unity are not threadsafe

            while (true)
            {
                // Detect if the client closed from its end:
                if (!client.Connected)
                {
                    inThread.Abort();
                    StopListening();
                    break;
                }
                
                if (connectedCPU == null)
                {
                    DoMenu();
                }
                else
                {
                    int numRead = stream.Read(rawReadBuffer, 0, rawReadBuffer.Length); // This is blocking, so this thread will be idle when client isn't typing.
                    string cleanedText = TelnetProtocolScrape(rawReadBuffer, numRead);
                    // TODO - make this better - right now I'm ignoring the inBuffer and just echoing this out
                    // to the out buffer to print it back to the user for a test.  In future this should attach to the CPU:
                    SendText("["+cleanedText+"]");
                }                
            }
        }
        
        private void DoMenu()
        {
                    
            if (System.DateTime.Now > lastMenuQueryTime.AddMilliseconds(500))
            {
                lastMenuQueryTime = System.DateTime.Now;
                if( CPUListChanged() )
                    PrintCPUMenu();
            }
            int numRead = stream.Read(rawReadBuffer, 0, rawReadBuffer.Length); // This is blocking, so this thread will be idle when client isn't typing.
            string cleanedText = TelnetProtocolScrape(rawReadBuffer, numRead);
            SendText(cleanedText);
            localMenuBuffer = localMenuBuffer + cleanedText;
            int firstEOLN = localMenuBuffer.IndexOfAny(new char[] {'\r','\n'});

            // end of line hasn't been pressed
            if (firstEOLN < 0)
                return;

            if(localMenuBuffer.Substring(0,1).Equals("Q",StringComparison.CurrentCultureIgnoreCase))
            {
                StopListening();
                stream.Close();
                localMenuBuffer = "";
                return;
            }
        
            int endOfNumbers = cleanedText.LastIndexOfAny(new char[] {'0','1','2','3','4','5','6','7','8','9'});
            int pick;
            if( int.TryParse(localMenuBuffer.Substring(0,endOfNumbers+1),out pick) )
            {
                SendText("Connecting...");
                connectedCPU = availableCPUs[pick-1];
            }
            else
            {
                SendText("Garbled answer.  Try Again.\r\n");
                availableCPUs = new List<kOSProcessor>(); // resetting the list forces it to recalc and reprint the menu next time.
            }
            localMenuBuffer = "";
        }
        
        private string TelnetProtocolScrape(byte[] inRawBuff, int rawLength)
        {
            // At max the cooked version will be the same size as the raw.  It might be a little less:
            byte[] inCookedBuff = new byte[rawLength];
            int cookedIndex = 0;
            int rawIndex = 0;
            
            while (rawIndex < rawLength)
            {
                switch (inRawBuff[rawIndex])
                {
                    case RFC854_IAC:
                        byte commandByte = inRawBuff[++rawIndex];
                        switch (commandByte)
                        {
                            case RFC854_DO: rawIndex += TelnetConsumeDo(inRawBuff, rawIndex ); break;
                            case RFC854_IAC: break; // pass through to normal behaviour when two IAC's are back to back - that's how a real IAC char is encoded.
                            default:
                                rawIndex += TelnetConsumeOther( commandByte, inRawBuff, rawIndex); 
                                break;
                        }
                        break;
                    default:
                        // The normal case is to just copy the bytes over as-is when no special chars were seen.
                        inCookedBuff[cookedIndex] = inRawBuff[rawIndex];
                        ++rawIndex;
                        ++cookedIndex;
                        break;
                }
            }
            
            // Convert the cooked buffer into a string to passing back:
            return System.Text.Encoding.UTF8.GetString(inCookedBuff,0,cookedIndex);
        }
        
        private int TelnetConsumeDo( byte[] remainingBuff, int index)
        {
            int offset = 0;
            byte option = remainingBuff[index + (++offset)];
            switch (option)
            {
                // If other side wants me to echo, agree
                case RFC857_ECHO:  stream.Write( new byte[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO}, 0, 3);
                    break;
                // If other side wants me to go char-at-a-atime, agree
                case RFC858_SUPPRESS_GOAHEAD:
                    stream.Write( new byte[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD}, 0, 3);
                    break;
                default: break;
            }

            // Everything below here is to help debug:
            StringBuilder sb = new StringBuilder();
            sb.Append("{"+RFC854_DO+"}");
            sb.Append("{"+option+"}");
            kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "telnet protocol submessage from client: " + sb.ToString());

            return offset;
        }

        private int TelnetConsumeOther( byte commandByte, byte[] remainingBuff, int index)
        {
            int offset = 0;
            StringBuilder sb = new StringBuilder();
            switch (commandByte)
            {
                case RFC854_SB:
                    while (offset < remainingBuff.Length && remainingBuff[index+offset] != RFC854_SE)
                        ++offset;

                    // Everything below here is to help debug:
                    sb.Append("{"+commandByte+"}");
                    for( int i = index; i < index+offset ; ++i )
                        sb.Append("{"+remainingBuff[i]+"}");
                    kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "unhandled telnet protocol submessage from client: " + sb.ToString());

                    break;
                default:
                    ++offset;

                    // Everything below here is to help debug:
                    sb.Append("{"+commandByte+"}");
                    sb.Append("{"+remainingBuff[index+offset]+"}");
                    kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "unhandled telnet protocol command from client: " + sb.ToString());

                    break;
            }
            return offset;
        }
    }
}

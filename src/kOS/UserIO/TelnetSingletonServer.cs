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
using System.IO;
using System.IO.Pipes;

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
        private TcpClient client;
        
        /// <summary>
        /// The raw socket stream used to talk directly to the client across the network.
        /// It is bidirectional - handling both the input from and output to the client.
        /// </summary>
        private NetworkStream rawStream;
        
        /// <summary>
        /// The queue that other parts of KOS can use to read characters from the telnet client.
        /// </summary>
        private volatile Queue<char> inQueue;

        /// <summary>
        /// The queue that other parts of kOS can use to write characters to the telent client.
        /// </summary>
        private volatile Queue<char> outQueue;

        public kOSProcessor ConnectedProcessor { get; private set; }
        
        private byte[] rawReadBuffer = new byte[128]; // use small chunks
        private byte[] rawWriteBuffer = new byte[128]; // use small chunks
        
        private Thread inThread;
        private Thread outThread;
        
        private TelnetWelcomeMenu welcomeMenu;

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
            rawStream = client.GetStream();
            inThread = new Thread(DoInThread);
            outThread = new Thread(DoOutThread);
            outQueue = new Queue<char>();
            inQueue = new Queue<char>();
        }
        
        public void ConnectToProcessor(kOSProcessor processor)
        {
            ConnectedProcessor = processor;
            // TODO: Write the part of the mod that listens to this telnet inside termwindow, and at this point
            // tell it to connect to this telnet server.
        }

        /// <summary>
        /// Get the next available char from the client.
        /// Can throw exception if there is no such char.  Check ahead of time to see if
        /// there's at least one character available using InputWaiting().
        /// </summary>
        /// <returns>one character read</returns>
        public char ReadChar()
        {
            return inQueue.Dequeue();
        }
        
        /// <summary>
        /// Get a string of all available charactesr from the client.
        /// If no such chars are available it will not throw an exception.  Instead it just returns a zero length string.
        /// </summary>
        /// <returns>All currently available input characters returned in one string.</returns>
        public string ReadAll()
        {
            if (inQueue.Count == 0)
                return String.Empty;
            StringBuilder sb = new StringBuilder();
            while (inQueue.Count > 0)
                sb.Append(inQueue.Dequeue());
            return sb.ToString();
        }
        
        /// <summary>
        /// Determine if the input queue has pending chars to read.  If it isn't, then attempts to call ReadOneChar will throw an exception.
        /// </summary>true if input is currently Queued</returns>
        public bool InputWaiting()
        {
            return inQueue.Count > 0;
        }
        
        /// <summary>
        /// Write one character out to the telnet client.
        /// <param name="ch">character to write</param>
        /// </summary>
        public void Write(char ch)
        {
            rawStream.Write(Encoding.UTF8.GetBytes(new String(ch,1)),0,1);
        }

        /// <summary>
        /// Write a string out to the telnet client.
        /// <param name="str">string to write</param>
        /// </summary>
        public void Write(string str)
        {
            byte[] buf = Encoding.UTF8.GetBytes(str);
            rawStream.Write(buf,0,buf.Length);
        }
        
        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        /// <param name="str"></param>
        private void SendTextRaw(string str)
        {
            byte[] outBuff = System.Text.Encoding.UTF8.GetBytes(str);
            rawStream.Write(outBuff, 0, outBuff.Length);
        }
        
        public void StopListening()
        {
            inThread.Abort();
            inThread = null; // dispose old thread.
            outThread.Abort();
            outThread = null; // dispose old thread.
            
            rawStream.Close();
        }

        public void StartListening()
        {
            SendTextRaw("Connected to the kOS Terminal Server.\r\n");
            
            inThread.Start();
            outThread.Start();
            
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
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC857_ECHO},0, 3); // don't local-echo.
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // send one char at a time without buffering lines.
            }
            else
            {
                // Send some telnet protocol stuff telling the other side how I'd like it to behave:
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC857_ECHO},0, 3); // don't local-echo.
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // send one char at a time without buffering lines.
            }
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
                
                if (welcomeMenu == null)
                {
                    if (ConnectedProcessor == null)
                        SpawnWelcomeMenu();
                }
                else if (ConnectedProcessor != null) // welcome menu is attached but we now have a processor picked, so detach it.
                {
                    welcomeMenu.Quit();
                    welcomeMenu = null ; // let it get garbage collected.
                }
                
                int numRead = rawStream.Read(rawReadBuffer, 0, rawReadBuffer.Length); // This is blocking, so this thread will be idle when client isn't typing.
                if (numRead > 0 )
                {
                    string sendOut = TelnetProtocolScrape(rawReadBuffer, numRead);
                    foreach (char ch in sendOut)
                    {
                        inQueue.Enqueue(ch);
                    }
                }
            }
        }
        
        private void DoOutThread()
        {
            // All threads in a KSP mod must be careful NOT to access any KSP or Unity methods
            // from inside their execution, because KSP and Unity are not threadsafe

            while (true)
            {
                // Detect if the client closed from its end:
                if (!client.Connected)
                {
                    outThread.Abort();
                    StopListening();
                    break;
                }
                
                StringBuilder sb = new StringBuilder();
                while (outQueue.Count > 0)
                    sb.Append(outQueue.Dequeue());
                if (sb.Length > 0)
                {
                    SendTextRaw(sb.ToString());
                }
            }
        }
        
        private void SpawnWelcomeMenu()
        {
            var gObj = new GameObject( "TelnetWelcomeMenu_" + this.GetInstanceID(), typeof(TelnetWelcomeMenu) );
            DontDestroyOnLoad(gObj);
            welcomeMenu = (TelnetWelcomeMenu)gObj.GetComponent(typeof(TelnetWelcomeMenu));
            welcomeMenu.Setup(this);
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
            
            // Convert the cooked buffer into a shorter buff to pass back:
            return Encoding.UTF8.GetString(inCookedBuff, 0, cookedIndex);
        }
        
        private int TelnetConsumeDo( byte[] remainingBuff, int index)
        {
            int offset = 0;
            byte option = remainingBuff[index + (++offset)];
            switch (option)
            {
                // If other side wants me to echo, agree
                case RFC857_ECHO:  rawStream.Write( new byte[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO}, 0, 3);
                    break;
                // If other side wants me to go char-at-a-atime, agree
                case RFC858_SUPPRESS_GOAHEAD:
                    rawStream.Write( new byte[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD}, 0, 3);
                    break;
                default:
                    offset += TelnetConsumeOther(RFC854_DO, remainingBuff, 0);
                    break;
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

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
        
        // I want to communicate the difference between cases where the fact that the
        // value starts at zero is irrelevant to the algorithm from cases where it's
        // actually part of the algorithm:
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
        
        private byte[] rawReadBuffer = new byte[4096];
        private byte[] rawWriteBuffer = new byte[4096];
        
        private Thread inThread;
        private Thread outThread;
        
        private TelnetWelcomeMenu welcomeMenu;
        
        private TerminalUnicodeMapper terminalMapper;
        
        private bool isLineAtATime;
        private bool allowResize;

        public int ClientWidth {get; private set;}
        public int ClientHeight {get; private set;}

        private string termTypeBackingField; // This is deliberately NOT named as just a lower-case version of ClientTerminalType,
                                             // so as to prevent accidentally typing this name instead of the property.  It's essential
                                             // that all the access even inside this class itself, be done via the property to
                                             // force it to keep the terminalMapper updated to match.
        public string ClientTerminalType
        {
            get{ return termTypeBackingField;}
            private set{ termTypeBackingField = value; terminalMapper = TerminalUnicodeMapper.TerminalMapperFactory(value); }
        }

        // Special telnet protocol bytes with magic meaning, taken from interet RFC's:
        // Many of these will go unused at first, but it's important to get them down for future
        // embetterment.
        //
        // For documentation on how these telnet controls work, and how they're expected
        // to be passed back and forth, do an internet search based on the RFC number of the
        // constant (i.e. to see how the byte code RFC854_IAC is used, go look up "RFC 854" on
        // the internet and look at what it says about a byte code called "IAC".)

        private const byte RFC854_SE       = 240; //  End of subnegotiation parameters.
        private const byte RFC854_NOP      = 241; //  No operation.
        private const byte RFC854_DATAMARK = 242; //  The data stream portion of a Synch.
                                                  // This should always be accompanied
                                                  // by a TCP Urgent notification.
        private const byte RFC854_BREAK    = 243; //  NVT character BRK.
        private const byte RFC854_IP       = 244; //  The function IP.
        private const byte RFC854_AO       = 245; //  The function AO.
        private const byte RFC854_AYT      = 246; //  The function AYT.
        private const byte RFC854_EC       = 247; //  The function EC.
        private const byte RFC854_EL       = 248; // The function EL.
        private const byte RFC864_GA       = 249; //  The GA signal.
        private const byte RFC854_SB       = 250; //  Indicates that what follows is
                                                  // subnegotiation of the indicated
                                                  // option.
        private const byte RFC854_WILL     = 251; //  Indicates the desire to begin
                                                  // performing, or confirmation that
                                                  // you are now performing, the
                                                  // indicated option.
        private const byte RFC854_WONT     = 252; //   Indicates the refusal to perform,
                                                  // or continue performing, the
                                                  // indicated option.
        private const byte RFC854_DO       = 253; // Indicates the request that the
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
        
        private const byte RFC1091_TERMTYPE = 24;
        private const byte RFC1091_IS       = 0;
        private const byte RFC1091_SEND     = 1;

        private const byte RFC1073_NAWS     = 31;
        
        public TelnetSingletonServer(TcpClient client)
        {
            this.client = client;
            rawStream = client.GetStream();
            inThread = new Thread(DoInThread);
            outThread = new Thread(DoOutThread);
            outQueue = new Queue<char>();
            inQueue = new Queue<char>();
            ClientWidth = 80; // common default for a lot of terminal progs.  Will allow resize via RFC1073.
            ClientHeight = 24; // common default for a lot of terminal progs.  Will allow resize via RFC1073.
            ClientTerminalType = "UNKNOWN"; // will be set by telnet client as described by RFC 1091
        }

        /// <summary>
        /// Connect this TelnetSingletonServer to a CPU to start acting as its terminal.
        /// </summary>
        /// <param name="processor">The kOSProcessor PartModule to attach to</param>
        public void ConnectToProcessor(kOSProcessor processor)
        {
            ConnectedProcessor = processor;
            ConnectedProcessor.GetScreen().SetSize(ClientHeight, ClientWidth);
            ConnectedProcessor.GetWindow().AttachTelnet(this);
            ConnectedProcessor.GetWindow().SendTitleToTelnet(this);
        }

        public void DisconnectFromProcessor()
        {
            if (ConnectedProcessor != null)
            {
                string detachMessage = "Detaching from " + ConnectedProcessor.GetWindow().TitleText;
                SendTextRaw( (char)UnicodeCommand.TITLEBEGIN + detachMessage + (char)UnicodeCommand.TITLEEND );
                SendTextRaw("\r\n{" + detachMessage + "}\r\n");
                ConnectedProcessor.GetWindow().DetachTelnet(this);
                ConnectedProcessor = null;
            }
        }

        /// <summary>
        /// Get the next available char from the client.
        /// Can throw exception if there is no such char.  Check ahead of time to see if
        /// there's at least one character available using InputWaiting().
        /// </summary>
        /// <returns>one character read</returns>
        public char ReadChar()
        {
            char ch;
            lock (inQueue) // all access to inQueue and outQueue needs to be atomic.
            {
                ch = inQueue.Dequeue();
            }
            return ch;
        }
        
        /// <summary>
        /// Get a string of all available charactesr from the client.
        /// If no such chars are available it will not throw an exception.  Instead it just returns a zero length string.
        /// </summary>
        /// <returns>All currently available input characters returned in one string.</returns>
        public string ReadAll()
        {
            StringBuilder sb = new StringBuilder();
            lock (inQueue) // all access to inQueue and outQueue needs to be atomic.
            {
                while (inQueue.Count > 0)
                    sb.Append(inQueue.Dequeue());
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Determine if the input queue has pending chars to read.  If it isn't, then attempts to call ReadOneChar will throw an exception.
        /// </summary>true if input is currently Queued</returns>
        public bool InputWaiting()
        {
            bool returnVal;
            lock (inQueue)
            {
                returnVal = inQueue.Count > 0;
            }
            return returnVal;
        }
        
        /// <summary>
        /// Write a character out to the telnet client.  You can pretend the telnet client is a
        /// psuedo-terminal that understands the command codes in UnicodeCommand.
        /// <br/>
        /// This TelnetSingletonServer will run what you give it through a command code
        /// mapper for whichever terminal type is actually attached to the telnet client.
        /// <br/>
        /// Remember that when sending strings to this method, end-of-lines need
        /// to be expressed as "\r\n", because you are writing using the raw internet
        /// ASCII standard.
        /// </summary>
        /// <param name="ch">character to write</param>
        public void Write(char ch)
        {
            lock(outQueue) // all access to inQueue and outQueue needs to be atomic.
            {
                outQueue.Enqueue(ch);
            }
        }

        /// <summary>
        /// Write a string out to the telnet client.  You can pretend the telnet client is a
        /// psuedo-terminal that understands the command codes in UnicodeCommand.
        /// <br/>
        /// This TelnetSingletonServer will run what you give it through a command code
        /// mapper for whichever terminal type is actually attached to the telnet client.
        /// <br/>
        /// Remember that when sending strings to this method, end-of-lines need
        /// to be expressed as "\r\n", because you are writing using the raw internet
        /// ASCII standard.
        /// </summary>
        /// <param name="str">string to write</param>
        public void Write(string str)
        {
            lock(outQueue) // all access to inQueue and outQueue needs to be atomic.
            {
                foreach (char ch in str)
                    outQueue.Enqueue(ch);
            }
        }
        
        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        /// <param name="str"></param>
        private void SendTextRaw(char[] buff)
        {
            SendTextRaw(new string(buff));
        }

        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        /// <param name="str"></param>
        private void SendTextRaw(string str)
        {
            byte[] outBuff = System.Text.Encoding.UTF8.GetBytes(str);
            SendTextRaw(outBuff);
        }

        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        /// <param name="str"></param>
        private void SendTextRaw(byte[] buff)
        {
            rawStream.Write(buff, 0, buff.Length);
            System.Console.WriteLine("eraseme: Just wrote the following buffer chunk out to the client:");
            for (int i=0; i<buff.Length; ++i) { System.Console.WriteLine("eraseme: buff["+i+"] = (int)"+ (int)buff[i] + " = '" + (char)buff[i] + "'"); }
        }
        
        public void StopListening()
        {
            SendTextRaw("\r\nDisconnecting from the kOS Terminal Server.\r\n");
            // Must use SendTextRaw, not Write, because we're about to kill the outThread before it has time to process the Write chars:
            SendTextRaw(terminalMapper.OutputConvert( (char)UnicodeCommand.TITLEBEGIN + "disconnected from kOS terminal server" + (char)UnicodeCommand.TITLEEND ));
            
            inThread.Abort();
            inThread = null; // dispose old thread.
            outThread.Abort();
            outThread = null; // dispose old thread.
            
            rawStream.Close();
            DisconnectFromProcessor();
        }

        public void StartListening()
        {
            // Must use SendTextRaw, not Write, because the outThread needs to be started for Write() to work:
            SendTextRaw("Connected to the kOS Terminal Server.\r\n");
            SendTextRaw(terminalMapper.OutputConvert( (char)UnicodeCommand.TITLEBEGIN + "Connected from kOS terminal server" + (char)UnicodeCommand.TITLEEND ));
            
            inThread.Start();
            outThread.Start();
            
            LineAtATime(false); 
            AllowTerminalResize(true);
            AllowTerminalTypeInfo(true);
        }
        
        /// <summary>
        /// Tell the telnet client that we'd like to operate in line-at-a-time mode, or
        /// conversely, in char-at-a-time mode.  As per RFC 857.
        /// </summary>
        /// <param name="modeOn">true = line-at-a-time, false = char-at-a-time</param>
        public void LineAtATime(bool modeOn)
        {
            isLineAtATime = modeOn;
            if (modeOn)
            {
                // Send some telnet protocol stuff telling the other side how I'd like it to behave:
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC857_ECHO},0, 3); // do local-echo.
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // don't send one char at a time without buffering lines.
            }
            else
            {
                // Send some telnet protocol stuff telling the other side how I'd like it to behave:
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC857_ECHO},0, 3); // don't local-echo.
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC858_SUPPRESS_GOAHEAD}, 0, 3); // do send one char at a time without buffering lines.
            }
        }
        
        /// <summary>
        /// Tell the telnet client that we'd like to allow it to resize its window, and if it does
        /// it should tell us what the new size is.  As per RFC 1073.
        /// </summary>
        /// <param name="modeOn">true = client should send resize messages whenever it feels like.  false = it should not send us resize messages.</param>
        public void AllowTerminalResize(bool modeOn)
        {
            allowResize = modeOn;
            if (modeOn)
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC1073_NAWS},0, 3); // do allow Negotiate About Window Size
            else
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC1073_NAWS},0, 3); // dont allow Negotiate About Window Size                
        }
        
        /// <summary>
        /// Tell the telnet client that we'd like it to send us ID strings when queried about its terminal model.
        /// </summary>
        /// <param name="modeOn">true = client should send terminal ident information.</param>
        public void AllowTerminalTypeInfo(bool modeOn)
        {
            if (modeOn)
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DO, RFC1091_TERMTYPE},0, 3); // do request terminal type info negotiations from client
            else
                rawStream.Write( new byte[] {RFC854_IAC, RFC854_DONT, RFC1091_TERMTYPE},0, 3); // dont request terminal type info from client              
        }

        /// <summary>
        /// Read the input from the telnet client forever, scraping the bytes for telnet protocol
        /// information, and placing the cooked bytes into the queue for the rest of KOS to read from.
        /// </summary>
        private void DoInThread()
        {
            // All threads in a KSP mod must be careful NOT to access any KSP or Unity methods
            // from inside their execution, because KSP and Unity are not threadsafe

            while (true)
            {
                // Detect if the client closed from its end:
                if (!client.Connected)
                {
                    StopListening();
                    outThread.Abort();
                    
                    // If you add code to this clause, remember to insert it above this
                    // point, not after it.  The next line probably prevents the execution
                    // of any code that comes after it.
                    inThread.Abort();
                    break; // It probably never reaches this line, but it's here just in case.
                }
                
                if (welcomeMenu == null)
                {
                    if (ConnectedProcessor == null)
                        SpawnWelcomeMenu();
                }
                else if (ConnectedProcessor != null) // welcome menu is attached but we now have a processor picked, so detach it.
                {
                    welcomeMenu.enabled = false; // turn it off so it stops trying to read the input in its Update().
                    welcomeMenu = null ; // let it get garbage collected.  Now it's the ConnectedProcessor's turn to do the work.
                    // If ConnectedProcessor gets disconnected again, a new welcomeMenu instance should get spawned by the check above.
                }
                
                int numRead = rawStream.Read(rawReadBuffer, 0, rawReadBuffer.Length); // This is blocking, so this thread will be idle when client isn't typing.
                if (numRead > 0 )
                {
                    char[] scrapedBytes = Encoding.UTF8.GetChars(TelnetProtocolScrape(rawReadBuffer, numRead));
                    string sendOut = (terminalMapper == null) ? (new string(scrapedBytes)) : terminalMapper.InputConvert(scrapedBytes);
                    lock (inQueue) // all access to inQueue and outQueue needs to be atomic
                    {
                        foreach (char ch in sendOut)
                        {
                            inQueue.Enqueue(ch);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Forever loop, taking whatever appears on the output queue (that some other part of kOS put there)
        /// and sending it out to the client as soon as it appears on the queue.
        /// </summary>
        private void DoOutThread()
        {
            // All threads in a KSP mod must be careful NOT to access any KSP or Unity methods
            // from inside their execution, because KSP and Unity are not threadsafe
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                sb.Remove(0,sb.Length); // clear the whole thing.
                lock(outQueue) // all access to inQueue and outQueue needs to be atomic.
                {
                    while (outQueue.Count > 0)
                    {
                        char ch = outQueue.Dequeue();
                        System.Console.WriteLine("eraseme: Just dequeued (int)"+(int)ch+", '"+(char)ch+"'");
                        sb.Append(ch);
                    }
                }
                if (sb.Length > 0)
                {
                    if (terminalMapper == null)
                        SendTextRaw(sb.ToString());
                    else
                        SendTextRaw(terminalMapper.OutputConvert(sb.ToString()));
                }
            }
        }
        
        /// <summary>
        /// When the telnet client connects but is not associated with a particular kOSProcessor at the moment,
        /// this will attach the welcome menu to it instead of having it attached to a kOSProcessor.  This should
        /// occur BOTH when the telnet client first connects, and whenever it detaches from a kOSProcessor.
        /// (It should go back to this menu, rather than get disconnected).
        /// <br/>
        /// The purpose of the welcome menu is to let the user pick which kOSProcessor to attach to.
        /// </summary>
        private void SpawnWelcomeMenu()
        {
            var gObj = new GameObject( "TelnetWelcomeMenu_" + this.GetInstanceID(), typeof(TelnetWelcomeMenu) );
            DontDestroyOnLoad(gObj);
            welcomeMenu = (TelnetWelcomeMenu)gObj.GetComponent(typeof(TelnetWelcomeMenu));
            welcomeMenu.Setup(this);
        }
        
        
        /// <summary>
        /// Tells the telnet client to send me back a terminal ident string.
        /// </summary>
        private void TelnetAskForTerminalType()
        {
            rawStream.Write(new byte[] {RFC854_IAC, RFC854_SB, RFC1091_TERMTYPE, RFC1091_SEND, RFC854_IAC, RFC854_SE}, 0, 6);
        }

        /// <summary>
        /// Given a chunk of raw input bytes received from the telnet client, scrape it clean of
        /// all telnet protocol stuff, returning just the cleaned-up string with none of that in it.
        /// <br/>
        /// As it scrapes off the protocol bytes, it will also interpret and understand the ones that
        /// have been implemented in this server.
        /// <br/>
        /// Not every message in the telnet protocol is understood by kOS's telnet server, but even
        /// the stuff that isn't will still get scraped away to clean the input.  The SuperVerbose log
        /// will contain information about what has been scraped away and ignored.
        /// </summary>
        /// <param name="inRawBuff">buffer pre-scraping</param>
        /// <param name="rawLength">how much of the buffer to scrape (which might not be the whole byte array).</param>
        /// <returns>The scraped input.</returns>
        private byte[] TelnetProtocolScrape(byte[] inRawBuff, int rawLength)
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
                            case RFC854_DO:
                                rawIndex += TelnetConsumeDo(inRawBuff, rawIndex);
                                break;
                            case RFC854_WILL:
                                rawIndex += TelnetConsumeWill(inRawBuff, rawIndex);
                                break;
                            case RFC854_IAC:
                                break; // pass through to normal behaviour when two IAC's are back to back - that's how a real IAC char is encoded.
                            case RFC854_BREAK:
                                lock (inQueue) { inQueue.Enqueue((char)UnicodeCommand.BREAK); } // async send it out of order, right now.
                                ++rawIndex;
                                break;
                            default:
                                rawIndex += TelnetConsumeOther(commandByte, inRawBuff, rawIndex); 
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
            byte[] returnValue = new byte[cookedIndex];
            Array.Copy(inCookedBuff, returnValue, cookedIndex);
            return returnValue;
        }
        
        /// <summary>
        /// Whenever an IAC DO command is seen (see RFC 854), this reads through it and undrstands some parts of it.
        /// </summary>
        /// <param name="remainingBuff"> the buffer of raw stuff</param>
        /// <param name="index">the offset into the buffer where the RFC654_DO byte started</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeDo( byte[] remainingBuff, int index)
        {
            int offset = 0;
            byte option = remainingBuff[index + (++offset)];
            switch (option)
            {
                // Note, for some infuriating reason, in my testing I often found that telnet clients ignored
                // the fact that I am the server and I'm supposed to send the DO's while they are supposed to reply
                // with the WILL or WONT codes.  Some clients (I'm looking at YOU, Putty) sometimes take on the 
                // active side of the negotiation (the DO/DONT) and sometimes take on the passive side of it (the
                // WILL/WONT responses to the DO's), mixed up all willy-nilly and not in adherence with the RFC's
                // rules about which side does which part.  Therefore even though I JUST got done telling the client
                // I'll do these things, it sometimes won't listen and demands that I respond to it's DO's instead.
                // That's what this section of code is for:
                
                // If other side orders me to go char-at-a-time, agree:
                case RFC857_ECHO:
                    rawStream.Write(new byte[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO}, 0, 3);
                    break;
                // If other side orders me to go char-at-a-time, agree:
                case RFC858_SUPPRESS_GOAHEAD:
                    rawStream.Write(new byte[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD}, 0, 3);
                    break;
                // if other side orders me to allow resizes, agree:
                case RFC1073_NAWS:
                    rawStream.Write(new byte[] {RFC854_IAC, RFC854_WILL, RFC1073_NAWS}, 0, 3);                    
                    break;
                // if other side orders me to accept terminal ident strings, agree, and send it back the signal that it
                // should tell me its ident right away.
                case RFC1091_TERMTYPE:
                    TelnetAskForTerminalType();
                    break;
                default:
                    offset += TelnetConsumeOther(RFC854_DO, remainingBuff, offset);
                    break;
            }

            // Everything below here is to help debug:
            StringBuilder sb = new StringBuilder();
            sb.Append("{"+RFC854_DO+"}");
            sb.Append("{"+option+"}");
            kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "kOS: telnet protocol DO message from client: " + sb.ToString());

            return offset;
        }

        
        /// <summary>
        /// Whenever an IAC WILL command is seen (see RFC 854), this reads through it and undrstands some parts of it.
        /// </summary>
        /// <param name="remainingBuff"> the buffer of raw stuff</param>
        /// <param name="index">the offset into the buffer where the RFC654_DO byte started</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeWill( byte[] remainingBuff, int index)
        {
            int offset = 0;
            byte option = remainingBuff[index + (++offset)];
            switch (option)
            {
                case RFC1091_TERMTYPE:
                    // Good- other side responded positively to terminal type negotiations.  So send it the request now.
                    TelnetAskForTerminalType();
                    break;
                default:
                    offset += TelnetConsumeOther(RFC854_WILL, remainingBuff, offset);
                    break;
            }
            
            // Everything below here is to help debug:
            StringBuilder sb = new StringBuilder();
            sb.Append("{"+RFC854_DO+"}");
            sb.Append("{"+option+"}");
            kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "kOS: telnet protocol WILL message from client: " + sb.ToString());

            return offset;
        }

        /// <summary>
        /// Consume any other RFC854_IAC - initiated protocol message that isn't caught by something
        /// more specific.
        /// </summary>
        /// <param name="commandByte"> the byte immediately after the IAC byte</param>
        /// <param name="remainingBuff"> the buffer to be parsed</param>
        /// <param name="index">how far into the buffer to start from (where the commandByte was)</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeOther( byte commandByte, byte[] remainingBuff, int index)
        {
            bool handled = false;
            int offset = 0;
            StringBuilder sb = new StringBuilder();
            switch (commandByte)
            {
                case RFC854_SB:
                    if (index+offset >= remainingBuff.Length)
                        break;
                    ++offset;
                    byte subCommandByte = remainingBuff[index+offset];
                    switch (subCommandByte)
                    {
                        case RFC1073_NAWS:
                            offset += TelnetConsumeNAWS(remainingBuff, index+offset, out handled);
                            break;
                        case RFC1091_TERMTYPE:
                            offset += TelnetConsumeTERMTYPE(remainingBuff, index+offset, out handled);
                            break;
                        default:
                            break;
                    }
                    // Consume the rest of the subnegotiation command:
                    while ((index + offset) <= remainingBuff.Length && remainingBuff[index+offset-1] != RFC854_SE)
                        ++offset;

                    // Everything below here is to help debug:
                    if (!handled)
                    {
                        sb.Append("{"+commandByte+"}");
                        for( int i = index; i < index+offset ; ++i )
                            sb.Append("{"+remainingBuff[i]+"}");
                        kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "kOS: telnet protocol submessage from client: " + sb.ToString());
                    }

                    break;
                default:
                    ++offset;

                    // Everything below here is to help debug:
                    sb.Append("{"+commandByte+"}");
                    sb.Append("{"+remainingBuff[index+offset]+"}");
                    kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "kOS: telnet protocol command from client: " + sb.ToString());

                    break;
            }
            return offset;
        }
        
        /// <summary>
        /// Consume the Negotiate About Window Size (RFC 1073) sub-message, i.e if the client wants to 
        /// tell us the window size is now 350 cells wide by 40 tall, it will send us this
        /// sequence of bytes:
        /// <br/>IAC NAWS 1 94 0 40 IAC SE </br>
        /// (1 94 is the encoding of 350 in two bytes, as in 256 + 94.  The protocol sends
        /// 16-bit numbers, allowing a very large max terminal size.
        /// </summary>
        /// <param name="remainingBuff"> the buffer to be parsed</param>
        /// <param name="index">how far into the buffer to start from (where the commandByte was)</param>
        /// <param name="handled">is true if it was handled properly (else it should get logged as a problem)</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeNAWS(byte[] remainingBuff, int index, out bool handled)
        {
            int offset = 0;
                        
            byte code = remainingBuff[index + (offset++)];
            if (code != RFC1073_NAWS)
            {
                kOS.Safe.Utilities.Debug.Logger.Log("kOS: Bug in telnet server - expected NAWS byte {" + RFC1073_NAWS + "} (RFC1073) but instead got {" + (int)code + "}.");
                handled = false;
                return offset;
            }

            if (remainingBuff.Length < (index + offset + 3))
            {
                kOS.Safe.Utilities.Debug.Logger.Log("kOS: Telnet client is trying to send me a window resize (RFC1073) command without actual width/height fields.  WTF?");
                handled = false;
                return offset;
            }

            byte widthHighByte = remainingBuff[index + (offset++)];
            if (widthHighByte == RFC854_IAC) ++offset; // special case - to send this byte value, telnet clients have to encode it by sending it twice consecutively.
            byte widthLowByte = remainingBuff[index + (offset++)];
            if (widthLowByte == RFC854_IAC) ++offset; // special case - to send this byte value, telnet clients have to encode it by sending it twice consecutively.
            byte heightHighByte = remainingBuff[index + (offset++)];
            if (heightHighByte == RFC854_IAC) ++offset; // special case - to send this byte value, telnet clients have to encode it by sending it twice consecutively.
            byte heightLowByte = remainingBuff[index + (offset++)];
            if (heightLowByte == RFC854_IAC) ++offset; // special case - to send this byte value, telnet clients have to encode it by sending it twice consecutively.
            
            int width = (((int)widthHighByte)<<8) + widthLowByte;
            int height = (((int)heightHighByte)<<8) + heightLowByte;

            kOS.Safe.Utilities.Debug.Logger.SuperVerbose( "kOS: Telnet client just told me its window size is " + width + "x" + height+".");
            
            // Only *actually* set the width and height if the values are nonzero.  The telnet protocol allows the
            // client to send one or the other as zero, which does not really mean zero but rather "ignore this field".
            // It's doubtful any terminal program uses this feature, but just in case it does, this protects us from
            // trying to set a zero height or zero width screen, which would likely cause a crash somewhere in ScreenBuffer:
            if (width > 0) ClientWidth = width;
            if (height > 0) ClientHeight = height;

            lock (inQueue) // all access to inQueue and outQueue needs to be atomic
            {
                inQueue.Enqueue( (char)UnicodeCommand.RESIZESCREEN );
                inQueue.Enqueue( (char)ClientWidth );
                inQueue.Enqueue( (char)ClientHeight );
            }
            
            handled = true;
            return offset;
        }
        
        /// <summary>
        /// Consume the Terminal Type submessage (RFC1091) where the telnet client is telling us the model ident
        /// of its terminal type (i.e. "VT100" for example)
        /// <br/
        /// </summary>
        /// <param name="remainingBuff"> the buffer to be parsed</param>
        /// <param name="index">how far into the buffer to start from (where the commandByte was)</param>
        /// <param name="handled">is true if it was handled properly (else it should get logged as a problem)</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeTERMTYPE(byte[] remainingBuff, int index, out bool handled)
        {
            int offset = 0;
            
            // expect TERMTYPE code char:
            byte code = remainingBuff[index + (offset++)];
            if (code != RFC1091_TERMTYPE)
            {
                kOS.Safe.Utilities.Debug.Logger.Log("kOS: Bug in telnet server - expected TERMTYPE byte {" + RFC1091_TERMTYPE + "} (RFC10791) but instead got {" + (int)code + "}.");
                handled = false;
                return offset;
            }
            
            // expect IS code char:
            code = remainingBuff[index + (offset++)];
            if (code != RFC1091_IS)
            {
                kOS.Safe.Utilities.Debug.Logger.Log("kOS: Bug in telnet server - expected [IS] byte {" + RFC1091_IS + "} (RFC10791) but instead got {" + (int)code + "}.");
                handled = false;
                return offset;
            }
            
            // Consume everything until the pattern IAC SE is found, which marks the end of the ident string:
            StringBuilder sb = new StringBuilder();
            byte last = (byte)0;
            byte penultimate = (byte)0;
            while ( (index + offset) <= remainingBuff.Length && !(penultimate == RFC854_IAC && last == RFC854_SE) )
            {
                sb.Append(Encoding.UTF8.GetString(remainingBuff, index + offset, 1));
                penultimate = last;
                ++offset;
                last = remainingBuff[index+offset];
            }
            // If it ended properly with the delimiter, then we have the string:
            if (penultimate == RFC854_IAC && last == RFC854_SE)
            {
                ClientTerminalType = sb.ToString().Substring(0, sb.Length - 1); // -1 because it will have the RFC854_IAC byte still stuck on the end.
                kOS.Safe.Utilities.Debug.Logger.SuperVerbose("kOS: Telnet client just told us its terminal type is: \""+ClientTerminalType+"\".");
                handled = true;
            }
            else
            {
                kOS.Safe.Utilities.Debug.Logger.Log("kOS: Telnet client sent us a garbled attempt at a terminal type ident string.");                
                handled = false;
            }
            // remove the final two delimiter bytes:
            return offset;
        }
    }
}

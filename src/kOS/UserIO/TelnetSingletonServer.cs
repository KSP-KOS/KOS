using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using kOS.Module;
using kOS.Safe.Utilities;
using UnityEngine;
using kOS.Safe.UserIO;
using Object = UnityEngine.Object;

namespace kOS.UserIO
{
    /// <summary>
    /// Spawned by TelnetMainServer, this handles the connection to just one
    /// single telnet client, leaving the main server free to go back to listening
    /// to other new connecting clients.
    /// </summary>
    public class TelnetSingletonServer
    {
        private const double HUNG_CHECK_INTERVAL = 10; // Seconds that a keep alive must have failed to give a response before assuming telnet client is dead.

        // ReSharper disable SuggestUseVarKeywordEvident
        
        // I want to communicate the difference between cases where the fact that the
        // value starts at zero is irrelevant to the algorithm from cases where it's
        // actually part of the algorithm:
        // ReSharper disable RedundantDefaultFieldInitializer
        
        // These exist just to track the state of whether or not the telnet client
        // got told about a change to these statuses yet or not:
        public bool ReverseScreen {get; set;}
        public bool VisualBeep {get; set;}
        
        private volatile TcpClient client;
        
        private readonly TelnetMainServer whoLaunchedMe;
        
        /// <summary>
        /// The raw socket stream used to talk directly to the client across the network.
        /// It is bidirectional - handling both the input from and output to the client.
        /// </summary>
        private readonly NetworkStream rawStream;
        
        /// <summary>
        /// The queue that other parts of KOS can use to read characters from the telnet client.
        /// </summary>
        private volatile Queue<char> inQueue;
        private readonly object inQueueAccess = new object(); // To make all access of the inQueue atomic between threads.

        /// <summary>
        /// The queue that other parts of kOS can use to write characters to the telnet client.
        /// </summary>
        private volatile Queue<char> outQueue;
        private readonly object outQueueAccess = new object(); // To make all access of the outQueue atomic between threads.
        
        public kOSProcessor ConnectedProcessor { get; private set; }
        
        private Thread inThread;
        private Thread outThread;
        
        private TelnetWelcomeMenu welcomeMenu;
        
        private TerminalUnicodeMapper terminalMapper;
        
        /// <summary>
        /// If true, then all input should get consumed and ignored the next time inThread checks, until the input stream is empty again.
        /// </summary>
        private bool flushPendingInput;
                        
        /// <summary>
        /// What order was I launched in?  Was I the 1st TelnetSingletonServer? The 2nd?  The 423rd?
        /// </summary>
        private int MySpawnOrder { get; set; }
        
        private bool isLineAtATime;

        public int ClientWidth {get; private set;}
        public int ClientHeight {get; private set;}

        private string termTypeBackingField; // This is deliberately NOT named as just a lower-case version of ClientTerminalType,
                                             // so as to prevent accidentally typing this name instead of the property.  It's essential
                                             // that all the access even inside this class itself, be done via the property to
                                             // force it to keep the terminalMapper updated to match.
                                             
        private readonly object keepAliveAccess = new object(); // because the timestamps can be seen by both in and out threads.

        private bool gotSomeRecentTraffic = true; // start off assuming it's alive.
        private DateTime keepAliveSendTimeStamp;
        private bool deadSocket = false;
        
        private bool alreadyDisconnecting = false;

        public string ClientTerminalType
        {
            get{ return termTypeBackingField;}
            private set
            {
                termTypeBackingField = value; 
                terminalMapper = TerminalUnicodeMapper.TerminalMapperFactory(value);
            }
        }

        // Special telnet protocol bytes with magic meaning, taken from Internet RFC's:
        // Many of these will go unused at first, but it's important to get them down for future
        // embetterment.
        //
        // For documentation on how these telnet controls work, and how they're expected
        // to be passed back and forth, do an Internet search based on the RFC number of the
        // constant (i.e. to see how the byte code RFC854_IAC is used, go look up "RFC 854" on
        // the Internet and look at what it says about a byte code called "IAC".)

        private const byte RFC854_SE       = 240; //  End of subnegotiation parameters.
        private const byte RFC854_NOP      = 241; //  No operation.
        private const byte RFC854_DATAMARK = 242; //  The data stream portion of a Synch.
                                                  // This should always be accompanied
                                                  // by a TCP Urgent notification.
        private const byte RFC854_BREAK    = 243; //  NVT character BRK.
        private const byte RFC854_IP       = 244; //  The function IP.
        private const byte RFC854_AO       = 245; //  The function AO.
        private const byte RFC854_AYT      = 246; //  The function AYT. ("Are you There"). Used to check for keepalive timings.
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
        
        public TelnetSingletonServer(TelnetMainServer mainServer, TcpClient client, int spawnOrder)
        {
            whoLaunchedMe = mainServer;
            MySpawnOrder = spawnOrder;
            this.client = client;
            rawStream = client.GetStream();
            lock (keepAliveAccess)
                gotSomeRecentTraffic = true;
            outQueue = new Queue<char>();
            inQueue = new Queue<char>();
            SpawnWelcomeMenu();
            inThread = new Thread(DoInThread);
            outThread = new Thread(DoOutThread);
            ClientWidth = 80; // common default for a lot of terminal progs.  Will allow resize via RFC1073.
            ClientHeight = 24; // common default for a lot of terminal progs.  Will allow resize via RFC1073.
            ClientTerminalType = "INITIAL_UNSET"; // will be set by telnet client as described by RFC 1091
        }

        ~TelnetSingletonServer()
        {
            if (welcomeMenu != null)
            {
                welcomeMenu.Detach();
                Object.Destroy(welcomeMenu);
                welcomeMenu = null;
            }
        }

        /// <summary>
        /// Connect this TelnetSingletonServer to a CPU to start acting as its terminal.
        /// </summary>
        /// <param name="processor">The kOSProcessor PartModule to attach to</param>
        public void ConnectToProcessor(kOSProcessor processor)
        {
            ConnectedProcessor = processor;
            ConnectedProcessor.GetScreen().SetSize(ClientHeight, ClientWidth); // Reset the GUI terminal to match the telnet window.
            ConnectedProcessor.GetWindow().AttachTelnet(this); // Tell the GUI window that I am one of its telnets now (even when closed, it still does the heavy lifting).
            ConnectedProcessor.GetWindow().SendTitleToTelnet(this);
            ConnectedProcessor.GetWindow().RepaintTelnet(this, true); // Need to start with a full paint of the terminal.
        }

        public void DisconnectFromProcessor()
        {
            if (ConnectedProcessor != null && !alreadyDisconnecting)
            {
                alreadyDisconnecting = true; // telnet tells window to disconnect, window tells telnet to disconnect.  This stops an infinite recursion.
                string detachMessage = "Detaching from " + ConnectedProcessor.GetWindow().TitleText;
                SendTextRaw( (char)UnicodeCommand.TITLEBEGIN + detachMessage + (char)UnicodeCommand.TITLEEND );
                SendTextRaw("\r\n{" + detachMessage + "}\r\n");
                ConnectedProcessor.GetWindow().DetachTelnet(this);

                // Very important, else CPU parts that go away (from explosion, load distance, scene switch ,etc) refuse to be orphaned and won't garbage collect.
                // That can cause subsequent reconnections from the TelnetWelcomeMenu to end up connecting you to the now dead CPU, with confusing results:
                ConnectedProcessor = null;
                alreadyDisconnecting = false;

                // Disconnecting the CPU from this, so connect the welcome menu instead
                if (welcomeMenu != null)
                {
                    welcomeMenu.Attach(this);
                }
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
            lock (inQueueAccess) // all access to inQueue and outQueue needs to be atomic.
            {
                ch = inQueue.Dequeue();
            }
            return ch;
        }
        
        /// <summary>
        /// Get a string of all available characters from the client.
        /// If no such chars are available it will not throw an exception.  Instead it just returns a zero length string.
        /// </summary>
        /// <returns>All currently available input characters returned in one string.</returns>
        public string ReadAll()
        {
            StringBuilder sb = new StringBuilder();
            lock (inQueueAccess) // all access to inQueue and outQueue needs to be atomic.
            {
                while (inQueue.Count > 0)
                    sb.Append(inQueue.Dequeue());
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Determine if the input queue has pending chars to read.  If it isn't, then attempts to call ReadOneChar will throw an exception.
        /// </summary>
        /// <returns>
        /// true if input is currently Queued
        /// </returns>
        public bool InputWaiting()
        {
            bool returnVal;
            lock (inQueueAccess)
            {
                returnVal = inQueue.Count > 0;
            }
            return returnVal;
        }
        
        /// <summary>
        /// Write a character out to the telnet client.  You can pretend the telnet client is a
        /// pseudo-terminal that understands the command codes in UnicodeCommand.
        /// <br/>
        /// This TelnetSingletonServer will run what you give it through a command code
        /// mapper for whichever terminal type is actually attached to the telnet client.
        /// <br/>
        /// Remember that when sending strings to this method, end-of-lines need
        /// to be expressed as "\r\n", because you are writing using the raw Internet
        /// ASCII standard.
        /// </summary>
        /// <param name="ch">character to write</param>
        public void Write(char ch)
        {
            lock(outQueueAccess) // all access to inQueue and outQueue needs to be atomic.
            {
                outQueue.Enqueue(ch);
            }
        }

        /// <summary>
        /// Write a string out to the telnet client.  You can pretend the telnet client is a
        /// pseudo-terminal that understands the command codes in UnicodeCommand.
        /// <br/>
        /// This TelnetSingletonServer will run what you give it through a command code
        /// mapper for whichever terminal type is actually attached to the telnet client.
        /// <br/>
        /// Remember that when sending strings to this method, end-of-lines need
        /// to be expressed as "\r\n", because you are writing using the raw Internet
        /// ASCII standard.
        /// </summary>
        /// <param name="str">string to write</param>
        public void Write(string str)
        {
            lock(outQueueAccess) // all access to inQueue and outQueue needs to be atomic.
            {
                foreach (char ch in str)
                    outQueue.Enqueue(ch);
            }
        }
        
        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        private void SendTextRaw(char[] buff)
        {
            SendTextRaw(new string(buff));
        }

        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        private void SendTextRaw(string str)
        {
            byte[] outBuff = Encoding.UTF8.GetBytes(str);
            SendTextRaw(outBuff);
        }

        /// <summary>
        /// Bypasses the Queues and just sends text directly to the socket stream.
        /// </summary>
        private void SendTextRaw(byte[] buff)
        {
            const bool VERBOSE_DEBUG_SEND = false; // enable to print a verbose dump of every char going to the client.
            try
            {
                rawStream.Write(buff, 0, buff.Length);
            }
            catch (Exception e)
            {
                // If the client closed its side just before we were about to write something out, the above write can fail and
                // cause an exception that would have killed the DoOutThread() before it had a chance to do its cleanup.
                if (e is System.IO.IOException || e is ObjectDisposedException)
                    deadSocket = true;
                else
                    throw; // Not one of the expected thread-closed IO exceptions, so don't hide it - let it get reported.
            }

#pragma warning disable CS0162
            if (VERBOSE_DEBUG_SEND) // compiler warning - this code block is hardcoded to be unreachable.  But that's deliberate.
            {
                StringBuilder logMessage = new StringBuilder();
                logMessage.Append("kOS Telnet server:  Just wrote the following buffer chunk out to the client:");
                for (int i=0; i<buff.Length; ++i)
                {
                    // Not using SuperVerbose because it's flagged by verboseDebugSend and maybe we want to ask users
                    // to be able to send us logs when they issue bug reports:
                    logMessage.Append("Send buff["+i+"] = (int)"+ (int)buff[i] + " = '" + (char)buff[i] + "'\n");
                }
                SafeHouse.Logger.Log(logMessage.ToString());
            }
#pragma warning restore CS0162
        }

        public void StopListening()
        {
            DisconnectFromProcessor();
            if (!deadSocket)
            {
                SendTextRaw("\r\nDisconnecting from the kOS Terminal Server.\r\n");
                // Must use SendTextRaw, not Write, because we're about to kill the outThread before it has time to process the queue that Write uses:
                SendTextRaw(terminalMapper.OutputConvert( (char)UnicodeCommand.TITLEBEGIN + "Thank you for using kOS terminal server" + (char)UnicodeCommand.TITLEEND ));
            }
            whoLaunchedMe.SingletonStopped(this);
            
            inThread.Abort();
            outThread.Abort();
            inThread = null; // dispose old thread.
            outThread = null; // dispose old thread.
            
            rawStream.Close();

            // Get rid of the welcome menu too, which has a reference to this server and should prevent GC
            if (welcomeMenu != null)
            {
                welcomeMenu.Detach();
                Object.Destroy(welcomeMenu);
                welcomeMenu = null;
            }
        }

        public void StartListening()
        {
            lock (keepAliveAccess)
                gotSomeRecentTraffic = true; // Just in case it's been a long time between construction and StartListening.
            inThread.Start();
            outThread.Start();
            LineAtATime(false); 
            AllowTerminalResize(true);
            AllowTerminalTypeInfo(true);

            SendTextRaw("Connected to the kOS Terminal Server.\r\n");
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
                SendTextRaw( new[] {RFC854_IAC, RFC854_WONT, RFC857_ECHO}); // we will not be echoing the client input back to it.
                SendTextRaw( new[] {RFC854_IAC, RFC854_DO, RFC857_ECHO}); // so the client should do its own local echoing.
                SendTextRaw( new[] {RFC854_IAC, RFC854_WONT, RFC858_SUPPRESS_GOAHEAD}); // don't send one char at a time, buffer the lines.
            }
            else
            {
                SendTextRaw( new[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO}); // we will echo the client's input back to it.
                SendTextRaw( new[] {RFC854_IAC, RFC854_DONT, RFC857_ECHO}); // so the client shouldn't be doing a local echo (or the user would see text twice).
                SendTextRaw( new[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD}); // do send one char at a time without buffering lines.
            }
        }
        
        /// <summary>
        /// Tell the telnet client that we'd like to allow it to resize its window, and if it does
        /// it should tell us what the new size is.  As per RFC 1073.
        /// </summary>
        /// <param name="modeOn">true = client should send resize messages whenever it feels like.  false = it should not send us resize messages.</param>
        public void AllowTerminalResize(bool modeOn)
        {
            SendTextRaw(modeOn
                ? new[] { RFC854_IAC, RFC854_DO, RFC1073_NAWS } // do allow Negotiate About Window Size
                : new[] { RFC854_IAC, RFC854_DONT, RFC1073_NAWS } // don't allow Negotiate About Window Size
                ); 
        }

        /// <summary>
        /// Tell the telnet client that we'd like it to send us ID strings when queried about its terminal model.
        /// </summary>
        /// <param name="modeOn">true = client should send terminal ident information.</param>
        public void AllowTerminalTypeInfo(bool modeOn)
        {
            SendTextRaw(modeOn
                ? new[] { RFC854_IAC, RFC854_DO, RFC1091_TERMTYPE } // do request terminal type info negotiations from client
                : new[] { RFC854_IAC, RFC854_DONT, RFC1091_TERMTYPE } // don't request terminal type info from client  
                );
        }

        /// <summary>
        /// Detect if the client is stuck, using some dummy sends.
        /// The low level TCP keepalive would be another way to do this, but .NET did a little bit TOO much abstraction
        /// of network sockets, and hid the ability to change that setting.   It's stuck at its default value of 2 hours.
        /// To change it from 2 hours would require dropping into the Windows OS-specific libraries and we're trying to
        /// avoid that so KOS can run on Mac and Linux.  (Rant: The ability to choose the TCP/IP keepalive interval
        /// is old bog-standard Internet stuff and in no way is it Windows-specific so there's no reason to restrict
        /// it to the windows-specific part of the API, dammit!  It should be part of .NET because every OS can do it!!)
        /// </summary>
        private bool IsHung()
        {
            bool returnValue = false;
            lock (keepAliveAccess)
            {
                if (DateTime.Now > keepAliveSendTimeStamp)
                {
                    // By the time it's time to send a second keepalive, we had better have gotten the reply from the previous one:
                    returnValue = ! gotSomeRecentTraffic;

                    // The telnet protocol has no keepalive from server to client message in the protocol, so
                    // we'll use the terminal type request as a make-do version of a keepalive.  It should force the
                    // telnet client to send some sort of bytes back to us as it answers the terminal type request:
                    TelnetAskForTerminalType();
                    keepAliveSendTimeStamp = DateTime.Now + System.TimeSpan.FromSeconds(HUNG_CHECK_INTERVAL);

                    // This will get set to true when we receive any bytes at all from the client,
                    // whether they're the answer to our query, or something else like user typing.
                    gotSomeRecentTraffic = false;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Read the input from the telnet client forever, scraping the bytes for telnet protocol
        /// information, and placing the cooked bytes into the queue for the rest of KOS to read from.
        /// </summary>
        private void DoInThread()
        {
            // All threads in a KSP mod must be careful NOT to access any KSP or Unity methods
            // from inside their execution, because KSP and Unity are not threadsafe

            byte[] rawReadBuffer = new byte[4096];

            while (true)
            {
                if (flushPendingInput) // When an async interrupt (like CTRL-C) is used, ignore whatever else was typed and throw it away:
                {
                    while (rawStream.DataAvailable)
                    {
                        // This is blocking, so the rawStream.DataAvailable check is vital to prevent hang:
                        rawStream.Read(rawReadBuffer, 0, rawReadBuffer.Length);
                        // But still remember the traffic counts as keepalive proof, even if we ignore it:
                        keepAliveSendTimeStamp = DateTime.Now + System.TimeSpan.FromSeconds(HUNG_CHECK_INTERVAL);
                    }
                    flushPendingInput = false;
                }

                int numRead = rawStream.Read(rawReadBuffer, 0, rawReadBuffer.Length); // This is blocking, so this thread will be idle when client isn't typing.
                if (numRead > 0 )
                {
                    // As long as some input came recently, no matter what it is, we don't need to bother sending the keepalive:
                    lock (keepAliveAccess)
                        gotSomeRecentTraffic = true;
                    keepAliveSendTimeStamp = DateTime.Now + System.TimeSpan.FromSeconds(HUNG_CHECK_INTERVAL);
                    
                    // Process the input bytes that arrived:
                    char[] scrapedBytes = Encoding.UTF8.GetChars(TelnetProtocolScrape(rawReadBuffer, numRead));
                    string sendOut = (terminalMapper == null) ? (new string(scrapedBytes)) : terminalMapper.InputConvert(scrapedBytes);
                    lock (inQueueAccess) // all access to inQueue and outQueue needs to be atomic
                    {
                        foreach (char ch in sendOut)
                        {
                            inQueue.Enqueue(ch);
                            if (flushPendingInput)
                                break; // stop processing the rest of this data chunk.
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
            
            // Unlike the DoInThread, this thread doesn't have a blocking stream read to operate on.  Instead
            // it has to busy-poll the outQueue looking for new content.  Because of this, ContinuousChecks()
            // has to be called from here, not the In thread:
            //
            // To prevent the busy polling from being too busy, the loop sleeps longer and longer while there's
            // no activity, up to a slowest rate of sleepTimeMax.  Once there's queue activity, it goes back to
            // running fast without sleep again.

            // Tweakable settings:
            const int SLEEP_TIME_INC = 20;
            const int SLEEP_TIME_MAX = 200; // Don't let this get too slow, because ContinuousChecks() needs to run.
            int sleepTime = SLEEP_TIME_MAX; // At first - will speed up once there's something in the queue.

            while (true)
            {
                ContinuousChecks();

                sb.Remove(0,sb.Length); // clear the whole thing.
                lock(outQueueAccess) // all access to inQueue and outQueue needs to be atomic.
                {
                    while (outQueue.Count > 0)
                    {
                        char ch = outQueue.Dequeue();
                        sb.Append(ch);
                    }
                }
                if (sb.Length > 0)
                {
                    sleepTime = 0; // Saw at least one char, so reset the sleep-slowdown.
                    string content = sb.ToString(); // instead of calling ToString() over and over.
                    if (terminalMapper == null)
                        SendTextRaw(content);
                    else
                        SendTextRaw(terminalMapper.OutputConvert(content));
                    
                    if (content.IndexOf((char)UnicodeCommand.DIE) >= 0)
                        StopListening();
                }
                else
                {
                    Thread.Sleep(sleepTime);
                    if (sleepTime < SLEEP_TIME_MAX)
                        sleepTime += SLEEP_TIME_INC;
                }
            }
        }
        
        private void ContinuousChecks()
        {
            // Detect if the client closed from its end:
            if ( (!client.Connected) || IsHung() || deadSocket)
            {
                deadSocket = true;
                StopListening();                    
            }
            
            if (! welcomeMenu.IsAttached)
            {
                if (ConnectedProcessor == null)
                    welcomeMenu.Attach(this);
            }
            else if (ConnectedProcessor != null) // welcome menu is attached but we now have a processor picked, so detach it.
            {
                welcomeMenu.Detach();
            }
        }
        
        /// <summary>
        /// When the telnet client connects but is not associated with a particular kOSProcessor at the moment,
        /// this will attach the welcome menu to it instead of having it attached to a kOSProcessor.
        /// <br/>
        /// The purpose of the welcome menu is to let the user pick which kOSProcessor to attach to.
        /// </summary>
        private void SpawnWelcomeMenu()
        {
            var gObj = new GameObject( "TelnetWelcomeMenu_" + MySpawnOrder, typeof(TelnetWelcomeMenu) );
            Object.DontDestroyOnLoad(gObj);
            welcomeMenu = (TelnetWelcomeMenu)gObj.GetComponent(typeof(TelnetWelcomeMenu));
            welcomeMenu.Attach(this);
        }
        
        
        /// <summary>
        /// Tells the telnet client to send me back a terminal ident string.
        /// </summary>
        private void TelnetAskForTerminalType()
        {
            SendTextRaw(new[] {RFC854_IAC, RFC854_SB, RFC1091_TERMTYPE, RFC1091_SEND, RFC854_IAC, RFC854_SE});
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
                            case RFC854_DONT:
                                rawIndex += TelnetConsumeDont(inRawBuff, rawIndex);
                                break;
                            case RFC854_WILL:
                                rawIndex += TelnetConsumeWill(inRawBuff, rawIndex);
                                break;
                            case RFC854_WONT:
                                rawIndex += TelnetConsumeWont(inRawBuff, rawIndex);
                                break;
                            case RFC854_IAC:
                                break; // pass through to normal behavior when two IAC's are back to back - that's how a real IAC char is encoded.
                            case RFC854_BREAK:
                            case RFC854_IP:
                                lock (inQueueAccess) { inQueue.Enqueue((char)UnicodeCommand.BREAK); } // async send it out of order, right now
                                flushPendingInput = true; // and ignore anything in the type-ahead buffer that was entered prior to the break.
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
        /// Whenever an IAC DO command is seen (see RFC 854), this reads through it and understands some parts of it.
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
                
                // If other side orders me to go char-at-a-time, agree or disagree depending on setting:
                // TODO: maybe some day alter this to obey the other side's wishes.
                case RFC857_ECHO:
                    SendTextRaw(isLineAtATime
                        ? new[] {RFC854_IAC, RFC854_WONT, RFC857_ECHO}
                        : new[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO});
                    break;
                // If other side orders me to go char-at-a-time, agree or disagree depending on setting:
                // TODO: maybe some day alter this to obey the other side's wishes.
                case RFC858_SUPPRESS_GOAHEAD:
                    SendTextRaw(isLineAtATime
                        ? new[] {RFC854_IAC, RFC854_WONT, RFC858_SUPPRESS_GOAHEAD}
                        : new[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD});
                    break;
                // if other side orders me to allow resizes, agree:
                case RFC1073_NAWS:
                    SendTextRaw(new[] {RFC854_IAC, RFC854_WILL, RFC1073_NAWS});                    
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
            SafeHouse.Logger.SuperVerbose( "telnet protocol DO message from client: " + sb);

            return offset;
        }
        /// <summary>
        /// Whenever an IAC DONT command is seen (see RFC 854), this reads through it and understands some parts of it.
        /// </summary>
        /// <param name="remainingBuff"> the buffer of raw stuff</param>
        /// <param name="index">the offset into the buffer where the RFC654_DONT byte started</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeDont( byte[] remainingBuff, int index)
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
                
                // If other side orders me to go char-at-a-time, agree or disagree depending on my setting:
                // TODO: maybe some day alter this to obey the other side's wishes.
                case RFC857_ECHO:
                    SendTextRaw(isLineAtATime
                        ? new[] {RFC854_IAC, RFC854_WONT, RFC857_ECHO}
                        : new[] {RFC854_IAC, RFC854_WILL, RFC857_ECHO});
                    break;
                // If other side orders me to go char-at-a-time, agree or disagree depending on my setting:
                // TODO: maybe some day alter this to obey the other side's wishes.
                case RFC858_SUPPRESS_GOAHEAD:
                    SendTextRaw(isLineAtATime
                        ? new[] {RFC854_IAC, RFC854_WONT, RFC858_SUPPRESS_GOAHEAD}
                        : new[] {RFC854_IAC, RFC854_WILL, RFC858_SUPPRESS_GOAHEAD});
                    break;
                default:
                    offset += TelnetConsumeOther(RFC854_DONT, remainingBuff, offset);
                    break;
            }

            // Everything below here is to help debug:
            StringBuilder sb = new StringBuilder();
            sb.Append("{"+RFC854_DO+"}");
            sb.Append("{"+option+"}");
            SafeHouse.Logger.SuperVerbose( "telnet protocol DO message from client: " + sb);

            return offset;
        }

        
        /// <summary>
        /// Whenever an IAC WONT command is seen (see RFC 854), this reads through it and understands some parts of it.
        /// </summary>
        /// <param name="remainingBuff"> the buffer of raw stuff</param>
        /// <param name="index">the offset into the buffer where the RFC654_WILL byte started</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeWont( byte[] remainingBuff, int index)
        {
            int offset = 0;
            byte option = remainingBuff[index + (++offset)];
            switch (option)
            {
                case RFC1091_TERMTYPE:
                    // Bad- other side responded negative to terminal type negotiations.
                    // Issue complaint and quit - this depends on this working right:
                    Write("{YOUR TELNET CLIENT WONT IMPLEMENT RFC1091 (Terminal type).  kOS CANNOT WORK WITH IT.}" +
                          UnicodeCommand.DIE);
                    break;
                // if other side cannot to allow resizes, give up:
                case RFC1073_NAWS:
                    Write("{YOUR TELNET CLIENT WONT IMPLEMENT RFC1073 (Terminal dimensions).  kOS CANNOT WORK WITH IT.}" +
                          UnicodeCommand.DIE);
                    break;
                default:
                    offset += TelnetConsumeOther(RFC854_WONT, remainingBuff, offset);
                    break;
            }
            
            // Everything below here is to help debug:
            StringBuilder sb = new StringBuilder();
            sb.Append("{"+RFC854_DO+"}");
            sb.Append("{"+option+"}");
            SafeHouse.Logger.SuperVerbose( "telnet protocol WILL message from client: " + sb);

            return offset;
        }

                /// <summary>
        /// Whenever an IAC WILL command is seen (see RFC 854), this reads through it and understands some parts of it.
        /// </summary>
        /// <param name="remainingBuff"> the buffer of raw stuff</param>
        /// <param name="index">the offset into the buffer where the RFC654_WILL byte started</param>
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
            SafeHouse.Logger.SuperVerbose( "telnet protocol WILL message from client: " + sb);

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
                            offset += TelnetConsumeTermtype(remainingBuff, index+offset, out handled);
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
                        SafeHouse.Logger.SuperVerbose( "telnet protocol submessage from client: " + sb);
                    }

                    break;
                default:
                    ++offset;

                    // Everything below here is to help debug:
                    sb.Append("{"+commandByte+"}");
                    sb.Append("{"+remainingBuff[index+offset]+"}");
                    SafeHouse.Logger.SuperVerbose( "telnet protocol command from client: " + sb);

                    break;
            }
            return offset;
        }
        
        /// <summary>
        /// Consume the Negotiate About Window Size (RFC 1073) sub-message, i.e if the client wants to 
        /// tell us the window size is now 350 cells wide by 40 tall, it will send us this
        /// sequence of bytes:
        /// <br/>IAC NAWS 1 94 0 40 IAC SE <br/>
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
                SafeHouse.Logger.Log(string.Format("Bug in telnet server - expected NAWS byte {{{0}}} (RFC1073) but instead got {{{1}}}.", RFC1073_NAWS, (int)code));
                handled = false;
                return offset;
            }

            if (remainingBuff.Length < (index + offset + 3))
            {
                SafeHouse.Logger.Log("Telnet client is trying to send me a window resize (RFC1073) command without actual width/height fields.  WTF?");
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
            
            int width = (widthHighByte<<8) + widthLowByte;
            int height = (heightHighByte<<8) + heightLowByte;

            SafeHouse.Logger.SuperVerbose( "Telnet client just told me its window size is " + width + "x" + height+".");
            
            // Only *actually* set the width and height if the values are nonzero.  The telnet protocol allows the
            // client to send one or the other as zero, which does not really mean zero but rather "ignore this field".
            // It's doubtful any terminal program uses this feature, but just in case it does, this protects us from
            // trying to set a zero height or zero width screen, which would likely cause a crash somewhere in ScreenBuffer:
            bool widthChange = false;
            bool heightChange = false;
            if (width > 0 && ClientWidth != width)
            {
                widthChange = true;
                ClientWidth = width;
            }
            if (height > 0 && ClientHeight != height)
            {
                heightChange = true;
                ClientHeight = height;
            }

            if (widthChange || heightChange)                
                lock (inQueueAccess) // all access to inQueue and outQueue needs to be atomic
                {
                    inQueue.Enqueue((char)UnicodeCommand.RESIZESCREEN);
                    inQueue.Enqueue((char)ClientWidth);
                    inQueue.Enqueue((char)ClientHeight);
                    if (welcomeMenu != null)
                         welcomeMenu.NotifyResize();
                   if (ConnectedProcessor != null)
                        inQueue.Enqueue((char)UnicodeCommand.REQUESTREPAINT);
                }
            
            handled = true;
            return offset;
        }
        
        /// <summary>
        /// Consume the Terminal Type submessage (RFC1091) where the telnet client is telling us the model ident
        /// of its terminal type (i.e. "VT100" for example)
        /// </summary>
        /// <param name="remainingBuff"> the buffer to be parsed</param>
        /// <param name="index">how far into the buffer to start from (where the commandByte was)</param>
        /// <param name="handled">is true if it was handled properly (else it should get logged as a problem)</param>
        /// <returns>how many bytes of the buffer should get skipped over because I dealt with them.</returns>
        private int TelnetConsumeTermtype(byte[] remainingBuff, int index, out bool handled)
        {
            int offset = 0;
            
            // expect TERMTYPE code char:
            byte code = remainingBuff[index + (offset++)];
            if (code != RFC1091_TERMTYPE)
            {
                SafeHouse.Logger.Log("Bug in telnet server - expected TERMTYPE byte {" + RFC1091_TERMTYPE + "} (RFC10791) but instead got {" + (int)code + "}.");
                handled = false;
                return offset;
            }
            
            // expect IS code char:
            code = remainingBuff[index + (offset++)];
            if (code != RFC1091_IS)
            {
                SafeHouse.Logger.Log("Bug in telnet server - expected [IS] byte {" + RFC1091_IS + "} (RFC10791) but instead got {" + (int)code + "}.");
                handled = false;
                return offset;
            }
            
            // Consume everything until the pattern IAC SE is found, which marks the end of the ident string:
            StringBuilder sb = new StringBuilder();
            byte last = 0;
            byte penultimate = 0;
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
                string newTermType = sb.ToString().Substring(0, sb.Length - 1); // -1 because it will have the RFC854_IAC byte still stuck on the end.

                if (ClientTerminalType != newTermType)
                {
                    // The above check may seem like a redundant check but it's not.
                    // It's to avoid reconstructing the terminal mapper every time there's a keepalive heartbeat response.
                    ClientTerminalType = newTermType;
                }

                SafeHouse.Logger.SuperVerbose(string.Format("Telnet client just told us its terminal type is: \"{0}\".", ClientTerminalType));
                handled = true;
            }
            else
            {
                SafeHouse.Logger.Log("Telnet client sent us a garbled attempt at a terminal type ident string.");                
                handled = false;
            }
            // remove the final two delimiter bytes:
            return offset;
        }
    }
}

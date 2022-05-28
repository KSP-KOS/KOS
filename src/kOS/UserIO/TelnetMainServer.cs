using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Net.Sockets;
using kOS.Safe.Utilities;
using KSP.IO;
using kOS.Suffixed;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace kOS.UserIO
{

    /// <summary>
    /// A single instance of the telnet server embedded into kOS.
    /// Presents clients with the initial choices and connects
    /// them to instances of terminals in the game.
    /// kOS should create exactly one and only one instance of this class,
    /// and control its options from the Applauncher panel.
    /// <br/>
    /// Implemented as a Monobehavior so that it can simulate the concept of forking
    /// and threading by having multiple instances of itself responding to their own
    /// Update() calls.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TelnetMainServer : MonoBehaviour
    {
        private TcpListener server = null;
        private IPAddress bindAddr;
        private string bindAddrName = ""; // used to compare new address values to old
        private Int32 port;
        private readonly List<TelnetSingletonServer> telnets;
        private bool isListening;

        // For status displays for the app control panel to see:
        public static TelnetMainServer Instance {get; private set;}
        public IPAddress BindAddr {get { return bindAddr;}}
        public bool IsListening {get { return isListening;}}
        public int ClientCount {get { return telnets.Count;}}
        
        /// <summary>The current user permission to turn the CONFIG:TELENT setting on.</summary>
        private bool tempListenPermission;
        /// <summary>The current user permission to turn the CONFIG:LOOPBACK setting off.</summary>
        private bool tempRealIPPermission;
        
        private bool activeOptInDialog;
        private bool activeRealIPDialog;
        private bool activeBgSimDialog;

        private Rect optInRect = new Rect( 200, 200, 500, 400);
        private Rect realIPRect = new Rect( 240, 140, 500, 400); // offset just in case multiples are up at the same time, to ensure visible bits to click on.  They aren't movable.
        private Rect bgSimRect = new Rect( 280, 100, 400, 300); // offset just in case multiples are up at the same time, to ensure visible bits to click on.  They aren't movable.
        private const string HELP_URL = "https://ksp-kos.github.io/KOS_DOC/general/telnet.html";

        public TelnetMainServer()
        {
            isListening = false;
            telnets = new List<TelnetSingletonServer>();
        }

        /// <summary>
        /// Gets the address the server is listening to *right now*, as opposed
        /// to the one it's configured to listen to *next time it restarts*.
        /// Returns loopback as a default if it's not currently running.
        /// </summary>
        /// <returns>The running address.</returns>
        public IPAddress GetRunningAddress()
        {
            if ( (!IsListening) || server == null || server.Server == null || (!server.Server.IsBound) )
                return IPAddress.Loopback;
            return ((IPEndPoint)server.LocalEndpoint).Address;
        }

        /// <summary>
        /// Gets the port number the server is listening to *right now*, as opposed
        /// to the one it's configured to listen to *next time it restarts*.
        /// Returns 0 as a default if it's not currently running.
        /// </summary>
        /// <returns>The running address.</returns>
        public int GetRunningPort()
        {
            if ((!IsListening) || server == null || server.Server == null || (!server.Server.IsBound))
                return 0;
            return ((IPEndPoint)server.LocalEndpoint).Port;
        }


        /// <summary>
        /// Return the user's permanent ("don't remind me again") status for the 
        /// permission to have telnet listen turned on.  This is stored in the
        /// kOS settings file and presumed to have a value of false if the setting is
        /// missing entirely from the file (which it will be the first time the
        /// user runs a version of the mod with this code in it.)
        /// </summary>
        /// <returns>The permission as read from the kOS settings file</returns>
        private bool GetPermanentListenPermission()
        {
            try
            {
                PluginConfiguration savedPermissions = PluginConfiguration.CreateForType<TelnetMainServer>();
                savedPermissions.load();
                return savedPermissions.GetValue<bool>("PermanentListenPermission");
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Exception loading telnet config options: {1}", KSPLogger.LOGGER_PREFIX, ex.Message));
            }
            return false;
        }

        /// <summary>
        /// Change the permanent ("don't remind me again") status of the permission
        /// in the kOS settings file.
        /// </summary>
        private void SetPermanentListenPermission(bool newValue)
        {
            try
            {
                PluginConfiguration savedPermissions = PluginConfiguration.CreateForType<TelnetMainServer>();
                savedPermissions.load();
                savedPermissions.SetValue("PermanentListenPermission", newValue);
                savedPermissions.save();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Exception loading telnet config options: {1}", KSPLogger.LOGGER_PREFIX, ex.Message));
            }
        }

        /// <summary>
        /// Return the user's permanent ("don't remind me again") status for the
        /// permission to have the telnet server listen to an address other than
        /// loopback.  (false = please restrict to only loopback.)  The permission
        /// will be stored in the kOS settings file and will be presumed to be set
        /// to false if the setting is missing from the file (as it will be the first
        /// time the user runs a version of the mod with this code in it.
        /// </summary>
        /// <returns>The permission as read from the kOS settings file</returns>
        private bool GetPermanentRealIPPermission()
        {
            try
            {
                PluginConfiguration savedPermissions = PluginConfiguration.CreateForType<TelnetMainServer>();
                savedPermissions.load();
                return savedPermissions.GetValue<bool>("PermanentRealIPPermission");
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Exception loading telnet config options: {1}", KSPLogger.LOGGER_PREFIX, ex.Message));
            }
            return false; // In principle it should never reach here.
        }

        /// <summary>
        /// Change the permanent ("don't remind me again") status of the permission
        /// in the kOS settings file.
        /// </summary>
        private void SetPermanentRealIPPermission(bool newValue)
        {
            try
            {
                PluginConfiguration savedPermissions = PluginConfiguration.CreateForType<TelnetMainServer>();
                savedPermissions.load();
                savedPermissions.SetValue("PermanentRealIPPermission", newValue);
                savedPermissions.save();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Exception loading telnet config options: {1}", KSPLogger.LOGGER_PREFIX, ex.Message));
            }
        }
        
        public void SetConfigEnable(bool newVal)
        {
            if (newVal == isListening) // nothing changed about the settings on this pass through.
                return;

            if (newVal)
            {
                bool isLoopback = IPAddress.IsLoopback(bindAddr);
                if (tempListenPermission && (tempRealIPPermission || isLoopback) && GameSettings.SIMULATE_IN_BACKGROUND)
                    StartListening();
                else
                {
                    // Set the config value back to false without turning the server on.
                    // This prevents us from continuing to check the value until
                    // the dialog boxes finish their work.  If permission is accepted
                    // the dialog box will set EnableTelnet to true again
                    SafeHouse.Config.EnableTelnet = false;

                    // Depending on which reasons it was for the denial, activate the explanatory dialog windows:
                    if (!tempListenPermission)
                        activeOptInDialog = true;
                    else // If the initial opt-in hasn't been permitted, that should be the only dialog seen.  Only show these if that hurdle is passed:
                    {
                        if (!isLoopback)
                            activeRealIPDialog = true;
                        if (! GameSettings.SIMULATE_IN_BACKGROUND)
                            activeBgSimDialog = true;
                    }
                }
            }
            else
                StopListening();
        }
        
        public void StartListening()
        {
            // calling StartListening when already started can cause problems, so quit if already started:
            if (isListening)
                return;

            // Check to see if the IP address is available (valid) on this computer
            if (bindAddr == null)
            {
                // because bindAddr should be set during Update, if it's null the
                // stored value was invalid and we shouldn't start the server.
                // This should only be possible if the config file was manually
                // edited to contain an invalid IP address, because invalid values
                // cannot be set from the UI nor by directly setting it on CONFIG
                Utilities.Utils.DisplayPopupAlert(
                    "kOS Telnet",
                    "Selected IP address (\"{0}\") is not valid.  Defaulting to local loopback (\"127.0.0.1\").  Please review the selection from the toolbar kOS control panel.",
                    SafeHouse.Config.TelnetIPAddrString);
                SafeHouse.Config.EnableTelnet = false;
                SafeHouse.Config.TelnetIPAddrString = IPAddress.Loopback.ToString(); // note to @dunbaratu: do we want to default back to loopback, or just prevent starting
                return;
            }
            if (!GetAllAddresses().Contains(bindAddrName))
            {
                // This IP address is no longer valid for this computer,
                // alert the user and don't start the server.
                Utilities.Utils.DisplayPopupAlert(
                    "kOS Telnet",
                    "Selected IP address (\"{0}\") is not currently available on this computer.  Please select a new address and re-enable the telnet server.",
                    bindAddrName);
                SafeHouse.Config.EnableTelnet = false;
                SafeHouse.Config.TelnetIPAddrString = IPAddress.Loopback.ToString(); // note to @dunbaratu: do we want to default back to loopback, or just prevent starting
                return;
            }
            
            // Build the server settings here, not in the constructor, because the settings might have been altered by the user post-init:

            // refresh the port information, don't need to refresh address because it's refreshed every Update
            port = SafeHouse.Config.TelnetPort; 

            server = new TcpListener(bindAddr, port);
            server.Start();
            SafeHouse.Logger.Log(string.Format("{2} TelnetMainServer started listening on {0} {1}", bindAddr, port, KSPLogger.LOGGER_PREFIX));
            isListening = true;
        }

        public void StopListening()
        {
            // calling StopListening when already stopped can cause problems, so quit if already stopped:
            if (!isListening)
                return;
            isListening = false;

            SafeHouse.Logger.Log(string.Format("{2} TelnetMainServer stopped listening on {0} {1}", bindAddr, port, KSPLogger.LOGGER_PREFIX));
            server.Stop();

            // Have to use a temp copy of the list to iterate over because telnet.StopListening()
            // can remove a telnet from the telnets list (while we're trying to use foreach on it):
            TelnetSingletonServer[] tempTelnets = new TelnetSingletonServer[telnets.Count];
            telnets.CopyTo(tempTelnets);
            foreach (TelnetSingletonServer telnet in tempTelnets)
                telnet.StopListening();

            // If it was only on for the one go, then it needs to get turned off again so
            // the message reappears the next time it's turned on:
            tempListenPermission = GetPermanentListenPermission();
        }

        internal void SingletonStopped(TelnetSingletonServer telnet)
        {
            telnets.Remove(telnet);
        }

        public void Start()
        {
            Instance = this;

            tempListenPermission = GetPermanentListenPermission();
            tempRealIPPermission = GetPermanentRealIPPermission();
            
            DontDestroyOnLoad(gameObject); // Otherwise Unity will stop calling my Update() on the next scene change because my gameObject went away.
        }

        /// <summary>
        /// Kill listener and all spawned singleton servers when the game is quitting and making me go away.
        /// Without this the game can hang forever when attempting to do a hard-quit,
        /// because the threads of the child singleton servers are still running.
        /// </summary>
        void OnDestroy()
        {
            StopListening();
        }
        
        public void Update()
        {
            SetBindAddrFromString(SafeHouse.Config.TelnetIPAddrString); // referesh the server address
            SetConfigEnable(SafeHouse.Config.EnableTelnet);
            
            int howManySpawned = 0;

            if (!isListening)
            {
                // Doing the command:
                //     SET CONFIG:TELNET TO FALSE.
                // should kill any singleton telnet servers that may have been started in the past
                // when it was true.  This does that:0

                // Have to use a temp copy of the list to iterate over because we
                // remove things from the telnets list (while we're trying to use foreach on it):
                TelnetSingletonServer[] tempTelnets = new TelnetSingletonServer[telnets.Count];
                telnets.CopyTo(tempTelnets);
                foreach (TelnetSingletonServer singleServer in tempTelnets)
                {
                    singleServer.StopListening();
                    telnets.Remove(singleServer);
                }
                
                // disabled, so don't listen for connections.
                return;
            }

            // .NET's TCP handler only gives you blocking socket accepting, but for the Unity
            // Update(), we need to always finish quickly and never block, so check if anything
            // is pending first to simulate the effect of a nonblocking socket accept check:
            if (!server.Pending())
                return;

            TcpClient incomingClient = server.AcceptTcpClient();
            
            string remoteIdent = ((IPEndPoint)(incomingClient.Client.RemoteEndPoint)).Address.ToString();
            SafeHouse.Logger.Log(string.Format("{0} telnet server got an incoming connection from {1}", KSPLogger.LOGGER_PREFIX, remoteIdent));
            
            TelnetSingletonServer newServer = new TelnetSingletonServer(this, incomingClient, ++howManySpawned);
            telnets.Add(newServer);
            newServer.StartListening();
        }

        /// <summary>
        /// Tells the telnet server to bind to the given address (as a string) on the
        /// next time it starts the server.  Note the user has to stop/start the
        /// server before the change really takes effect though.
        /// </summary>
        /// <returns>false if the address is not formatted well</returns>
        public bool SetBindAddrFromString(string s)
        {
            if (bindAddrName != s)
            {
                IPAddress newAddr;
                if (IPAddress.TryParse(s, out newAddr)) // try to parse the address
                {
                    // the address is valid, so use it and return true to indicate it changed
                    bindAddr = newAddr;
                    bindAddrName = bindAddr.ToString();
                    if (isListening)
                        StopListening();
                    return true;
                }
                // The string was not a valid IP address, return false to indicate the value has not changed
                return false;
            }
            return false; // return false to indicate the value has not changed
        }

        void OnGUI()
        {
            if (activeOptInDialog)
                optInRect = GUILayout.Window(401123, // any made up number unlikely to clash is okay here
                                             optInRect, OptInOnGui, "kOS Telnet Opt-In Permisssion");
            if (activeRealIPDialog)
                realIPRect = GUILayout.Window(401124, // any made up number unlikely to clash is okay here
                                              realIPRect, RealIPOnGui, "kOS Telnet Non-Loopback Permisssion");
            if (activeBgSimDialog)
                bgSimRect = GUILayout.Window(401125, // any made up number unlikely to clash is okay here
                                             bgSimRect, BgSimGui, "kOS Telnet \"Simulate in Background\" notice.");
        }
        
        void OptInOnGui(int id)
        {
            const string OPT_IN_TEXT = "You are attempting to turn on the telnet server embedded inside kOS.\n" +
                                     " \n" +
                                     "SQUAD has created a rule that all mods for KSP that wish to use network traffic " +
                                     "are required to contain an accurate and informative opt-in question asking for user " +
                                     "permission first. (We at kOS agree with the intent behind this rule.)\n" +
                                     " \n" +
                                     "If you turn on the kOS telnet server, this is what you are agreeing to:\n" +
                                     " \n" +
                                     "        kOS will keep a server running within the game that allows a commonly available " +
                                     "external program called a \"telnet client\" to control the kOS terminal screens " +
                                     "exactly as they can be used within the game's GUI.\n" +
                                     "        This means the external telnet client program can type the same exact commands " +
                                     "that you type at the terminal, with the same exact effect.\n" +
                                     " \n" +
                                     "        If this sounds dangerous, remember that you can force this feature to only " +
                                     "work between programs running on your own computer, never allowing access from " +
                                     "external computers, by using the LOOPBACK address (127.0.0.1) for the telnet server\n" +
                                     " \n" +
                                     "Further information can be found at: \n" +
                                     "        " + HELP_URL + "\n";

            // Note, the unnecessary curly braces below are there to help enforce an indentation that won't be
            // clobbered by auto-indenter tools.  Conceptually it helps show which "begin" matches with "end"
            // for layout purposes.  If approved, I might want to go through our other GUI stuff and do a
            // similar thing because I think it makes everything clearer:
            GUILayout.BeginVertical();
            {
                GUILayout.Label(OPT_IN_TEXT, HighLogic.Skin.textArea);
                GUILayout.BeginHorizontal();
                {
                    // By putting an expandwidth field on either side, it centers the middle part:
                    GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                    GUILayout.Label("____________________",HighLogic.Skin.label);
                    GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                } GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        // By putting an expandwidth field on either side, it centers the middle part:
                        GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                        GUILayout.Label("Do you wish to enable the telnet server?",HighLogic.Skin.textArea);
                        GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                    } GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        bool noClicked = GUILayout.Button("No", HighLogic.Skin.button);
                        bool yesNeverAgainClicked = GUILayout.Button("Yes\n and never show this message again", HighLogic.Skin.button);
                        bool yesClicked = GUILayout.Button("Yes\n just this once", HighLogic.Skin.button);
                        
                        // If any was clicked, this dialog window should go away next OnGui():
                        if (noClicked || yesNeverAgainClicked || yesClicked)
                            activeOptInDialog = false;
                        
                        if (yesClicked || yesNeverAgainClicked)
                        {
                            tempListenPermission = true;
                            SafeHouse.Config.EnableTelnet = true; // should get noticed next Update() and turn on the server.
                        }
                        
                        if (yesNeverAgainClicked)
                            SetPermanentListenPermission(true);
                        
                    } GUILayout.EndHorizontal();
                } GUILayout.EndVertical();
            } GUILayout.EndVertical();
            GUI.DragWindow();
        }
        
        void RealIPOnGui(int id)
        {
            string realIPText =
                "You are trying to set the kOS telnet server's IP address to your machine's real IP address " +
                "of " + bindAddr.ToString() + " rather than the loopback address (127.0.0.1).\n" +
                " \n" +
                "The use of the loopback address is a safety measure to ensure that only telnet clients on " +
                "your own computer can connect to your KSP game.\n" +
                " \n" +
                "If this is a local-only IP address (for example an address in the 192.168 range), or if your " +
                "computer sits behind a router for which the necessary port forwarding has not been set up, " +
                "then this is probably safe to use a non-loopback address, but on the other hand if this is a " +
                "public IP address you should think of the implications first.\n" +
                " \n" +
                "If you want better security, and this is a public IP address, then it's recommended that you " +
                "only let telnet use the loopback address (127.0.0.1) and instead provide remote access by the use of an SSH tunnel " +
                "you can control access to.  The subject of setting up an SSH tunnel is an advanced but well documented "+
                "network administrator topic for which help can be found with Internet searches.\n" +
                " \n" +
                "If you open your KSP game to other telnet clients outside your computer, you are choosing " +
                "to accept the security implications and take on the responsibility for them yourself.\n" +
                " \n" +
                "If you are thinking \"What's the harm? It's just letting people mess with my Kerbal Space Program Game?\", " +
                "then think about the existence of the kOS LOG command, which can write files to your computer's hard drive.\n" +
                " \n" +
                "Further information can be found at: \n" +
                "        " + HELP_URL + "\n";

            // Note, the unnecessary curly braces below are there to help enforce a begin/end indentation that won't be
            // clobbered by auto-indenter tools.
            GUILayout.BeginVertical();
            {
                GUILayout.Label(realIPText, HighLogic.Skin.textArea);
                GUILayout.BeginHorizontal();
                {
                    // By putting an expandwidth field on either side, it centers the middle part:
                    GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                    GUILayout.Label("____________________", HighLogic.Skin.label);
                    GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                } GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        // By putting an expandwidth field on either side, it centers the middle part:
                        GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                        GUILayout.Label("Do you really want to use a non-loopback address?", HighLogic.Skin.textArea);
                        GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                    } GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        bool noClicked = GUILayout.Button("No (address will revert to loopback)", HighLogic.Skin.button);
                        bool yesNeverAgainClicked = GUILayout.Button("Yes\n and never show this message again", HighLogic.Skin.button);
                        bool yesClicked = GUILayout.Button("Yes\n just this once", HighLogic.Skin.button);
                        
                        // If any was clicked, this dialog window should go away next OnGui():
                        if (noClicked || yesNeverAgainClicked || yesClicked)
                            activeRealIPDialog = false;
                        
                        if (noClicked)
                        {
                            SafeHouse.Config.TelnetIPAddrString = IPAddress.Loopback.ToString();
                            tempRealIPPermission = false;
                        }

                        if (yesClicked || yesNeverAgainClicked)
                        {
                            SafeHouse.Config.TelnetIPAddrString = bindAddr.ToString();
                            tempRealIPPermission = true;
                            StartListening();
                        }

                        if (yesNeverAgainClicked)
                            SetPermanentRealIPPermission(true);
                        
                    } GUILayout.EndHorizontal();
                } GUILayout.EndVertical();
            } GUILayout.EndVertical();            
            GUI.DragWindow();
        }

        void BgSimGui(int id)
        {
            string bgSimText =
                "<b>The KSP main settings option 'Simulate in Background' is turned off.</b>\n\n" +
                "This option must be enabled in order for kOS's telnet server to work.\n" +
                "(Leaving it turned off causes the game to pause whenever you switch to another active window," +
                "making it impossible to control the game from a telnet client.)\n\n" +
                "To allow kOS's telnet server to work, you must perform the following steps:\n\n" +
                "(1) Exit back to the first KSP title screen.\n" +
                "(2) Click \"Settings\".\n" +
                "(3) Click the \"General\" Tab on the settings screen.\n" +
                "(4) Enable the setting called \"Simulate in Background\".\n" +
                "(5) Click \"Accept\"\n" +
                "(6) <b>This is important</b>: Quit and Restart KSP.  (The change does not take effect until you do this.)\n" +
                "\n" +
                "Remember you MUST restart KSP for the change to actually do anything.\n";

            // Note, the unnecessary curly braces below are there to help enforce a begin/end indentation that won't be
            // clobbered by auto-indenter tools.
            GUILayout.BeginVertical();
            {
                GUILayout.Label(bgSimText, HighLogic.Skin.textArea);
                GUILayout.BeginHorizontal();
                {
                    bool OkayClicked = GUILayout.Button("Okay", HighLogic.Skin.button);

                    if (OkayClicked)
                        activeBgSimDialog = false; // makes window stop existing.
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        /// <summary>
        /// Return a list of all the addresses that exist on this
        /// machine, rendered into strings.  (i.e. "127.0.0.1" as
        /// a string rather than as 4 integer bytes.)
        /// </summary>
        /// <returns>The all addresses.</returns>
        public static List<string> GetAllAddresses()
        {
            List<string> ipAddrList = new List<string>();
            ipAddrList.Add(IPAddress.Loopback.ToString()); // always ensure loopbacks exist.

            // We ended up having to implement this this kind of inefficiently by trying two
            // different techniques to do the same thing.
            //
            // The technique that finds IP addresses via DNS will find zero hits on some Macs installs
            // because they are configured to use their own "bonjour" system instead of DNS.
            //
            // The technique that walks all network interfaces will find zero hits on some Linux installs
            // because the Mono variant in Unity has a bugged implementation of GetAllNetworkInterfaces()
            //
            // So we decided to just try both techniques and keep the union of the two results.


            // This is the attempt to find IP address via walking the DNS information:
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            try
            {
                SafeHouse.Logger.Log("Technique 1: Walking all DNS hostnames of this machine to find all IP addresses.");
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress addr in localIPs)
                {
                    if (!addr.Equals(IPAddress.Loopback))
                    {
                        ipAddrList.Add(addr.ToString());
                        SafeHouse.Logger.Log(string.Format("Found an IP address via DNS walk.  Adding it: {0}", addr.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0} Exception getting ip addresses using DNS technique: {1}", KSPLogger.LOGGER_PREFIX, ex.Message));
            }

            // This is the attempt to find IP address via walking the network interfaces:
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            try
            {
                SafeHouse.Logger.Log("Technique 2: Walking all NetworkInterfaces to find all IP addresses.");
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                if (nics != null && nics.Length > 0)
                {
                    foreach (NetworkInterface adapter in nics)
                    {
                        if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        {
                            var address = adapter.GetIPProperties().UnicastAddresses;
                            foreach (var item in address)
                            {
                                if (!item.Address.Equals(IPAddress.Loopback) && item.Address.AddressFamily.Equals(System.Net.Sockets.AddressFamily.InterNetwork))
                                {
                                    if (ipAddrList.Contains(item.Address.ToString()))
                                    {
                                        SafeHouse.Logger.Log(string.Format("  - Found IP address {0} : It's already in the list so skipping it.", item.Address.ToString()));
                                    }
                                    else
                                    {
                                        ipAddrList.Add(item.Address.ToString());
                                        SafeHouse.Logger.Log(string.Format("  - Found IP address {0} : Adding it.", item.Address.ToString()));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch the DNS error in the Fallback method on a "ceartain" OS to avoid infinite retry loop / log spam, thanks bonjour.
                SafeHouse.Logger.LogError(string.Format("Exception getting ip addresses from network interfaces: {0}",ex.Message));
            }
            return ipAddrList;
        }
    }
}

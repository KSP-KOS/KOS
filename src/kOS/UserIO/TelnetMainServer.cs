using System;
using System.Net;
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
        
        private Rect optInRect = new Rect( 200, 200, 500, 400);
        private Rect realIPRect = new Rect( 240, 140, 500, 400); // offset just in case both are up at the same time, to ensure visible bits to click on.  They aren't movable.

        private const string HELP_URL = "http://ksp-kos.github.io/KOS_DOC/general/telnet.html";

        public TelnetMainServer()
        {
            isListening = false;
            telnets = new List<TelnetSingletonServer>();
            
            DontDestroyOnLoad(transform.gameObject); // Otherwise Unity will stop calling my Update() on the next scene change because my gameObject went away.

            Console.WriteLine("kOS TelnetMainServer class exists."); // Console.Writeline used because this occurs before kSP's logger is set up.
            Instance = this;
            
            tempListenPermission = GetPermanentListenPermission();
            tempRealIPPermission = GetPermanentRealIPPermission();
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
                return (bool)(savedPermissions["PermanentListenPermission"]);
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
                return (bool)(savedPermissions["PermanentRealIPPermission"]);
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
            bool isLoopback = Equals(bindAddr, IPAddress.Loopback);
            bool loopBackStatusChanged = (isLoopback != SafeHouse.Config.TelnetLoopback);
            
            if (loopBackStatusChanged)
                StopListening(); // we'll be forcing a new restart of the telnet server on the new IP address.
            else
                if (newVal == isListening) // nothing changed about the settings on this pass through.
                    return;
            
            if (newVal)
            {
                if (tempListenPermission && ((tempRealIPPermission || SafeHouse.Config.TelnetLoopback)))
                    StartListening();
                else
                {
                    SafeHouse.Config.EnableTelnet = false; // Turn it right back off, never having allowed the server to turn on.
                    
                    // Depending on which reason it was for the denial, activate the proper dialog window:
                    if (!tempListenPermission)
                        activeOptInDialog = true;
                    else
                        activeRealIPDialog = true;
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
            
            // Build the server settings here, not in the constructor, because the settings might have been altered by the user post-init:

            port = SafeHouse.Config.TelnetPort;
            bindAddr = SafeHouse.Config.TelnetLoopback ? 
                IPAddress.Loopback : 
                GetRealAddress();

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
            foreach (TelnetSingletonServer telnet in telnets)
                telnet.StopListening();

            // If it was only on for the one go, then it needs to get turned off again so
            // the message reappears the next time it's turned on:
            tempListenPermission = GetPermanentListenPermission();
        }

        internal void SingletonStopped(TelnetSingletonServer telnet)
        {
            telnets.Remove(telnet);
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
            SetConfigEnable(SafeHouse.Config.EnableTelnet);
            
            int howManySpawned = 0;

            if (!isListening)
            {
                // Doing the command:
                //     SET CONFIG:TELNET TO FALSE.
                // should kill any singleton telnet servers that may have been started in the past
                // when it was true.  This does that:
                foreach (TelnetSingletonServer singleServer in telnets)
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
        
        private IPAddress GetRealAddress()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            if (localIPs.Length > 0)
                return localIPs[0]; // Hardcoded to only use the first IP it finds - good enough for most home network setups.
            else
                return IPAddress.Parse("127.0.0.1");
        }
        
        void OnGUI()
        {
            if (activeOptInDialog)
                optInRect = GUILayout.Window(401123, // any made up number unlikely to clash is okay here
                                             optInRect, OptInOnGui, "kOS Telnet Opt-In Permisssion");
            if (activeRealIPDialog)
                realIPRect = GUILayout.Window(401124, // any made up number unlikely to clash is okay here
                                              realIPRect, RealIPOnGui, "kOS Telnet Non-Loopback Permisssion");
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
                                     "external computers.  This is the default way the mod ships, and the setting " +
                                     "can be changed with the LOOPBACK config option.\n" +
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
                "of " + GetRealAddress() + " rather than the loopback address of 127.0.0.1.\n" +
                " \n" +
                "The use of the loopback address by default is a safety measure to ensure that only telnet clients on " +
                "your own computer can connect to your KSP game.\n" +
                " \n" +
                "If this is a local-only IP address (for example an address in the 192.168 range), or if your " +
                "computer sits behind a router for which the necessary port forwarding has not been set up, " +
                "then this is probably safe to turn the LOOPBACK option off, but on the other hand if this is a " +
                "public IP address you should think of the implications first.\n" +
                " \n" +
                "If you want better security, and this is a public IP address, then it's recommended that you " +
                "leave the LOOPBACK setting turned on and instead provide remote access by the use of an SSH tunnel " +
                "you can control access to.  The subject of setting up an SSH tunnel is an advanced but well documented "+
                "network administrator topic for which help can be found with Internet searches.\n" +
                " \n" +
                "If you open your KSP game to other telnet clients outside your computer, you are choosing " +
                "to accept the security implications and take on the responsibility for them yourself.\n" +
                " \n" +
                "If you are thinking \"What's the harm? It's just letting people mess with my Kerbal Space Program Game?\", " +
                "then think about the existence of the kOS LOG command, which writes files on your computer's hard drive.\n" +
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
                        GUILayout.Label("Do you wish to turn off safe loopback mode?", HighLogic.Skin.textArea);
                        GUILayout.Label(" ", GUILayout.ExpandWidth(true));
                    } GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        bool noClicked = GUILayout.Button("No (loopback stays on)", HighLogic.Skin.button);
                        bool yesNeverAgainClicked = GUILayout.Button("Yes\n and never show this message again", HighLogic.Skin.button);
                        bool yesClicked = GUILayout.Button("Yes\n just this once", HighLogic.Skin.button);
                        
                        // If any was clicked, this dialog window should go away next OnGui():
                        if (noClicked || yesNeverAgainClicked || yesClicked)
                            activeRealIPDialog = false;
                        
                        if (noClicked)
                        {
                            SafeHouse.Config.TelnetLoopback = true;
                            tempRealIPPermission = false;
                        }

                        if (yesClicked || yesNeverAgainClicked)
                        {
                            SafeHouse.Config.TelnetLoopback = false;
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
    }
}

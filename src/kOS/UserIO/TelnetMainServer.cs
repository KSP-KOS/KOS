using System;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using UnityEngine;

namespace kOS.UserIO
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]

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
    public class TelnetMainServer : MonoBehaviour
    {
        private TcpListener server = null;
        private IPAddress bindAddr;
        private Int32 port;
        private List<TelnetSingletonServer> telnets;
        private bool isListening;

        // For status displays for the app control panel to see:
        public static TelnetMainServer Instance {get; private set;}
        public IPAddress BindAddr {get { return bindAddr;}}
        public bool IsListening {get { return isListening;}}
        public int ClientCount {get { return telnets.Count;}}
        
        public TelnetMainServer()
        {
            isListening = false;
            telnets = new List<TelnetSingletonServer>();
            
            DontDestroyOnLoad(transform.gameObject); // Otherwise Unity will stop calling my Update() on the next scene change because my gameObject went away.

            Console.WriteLine("kOS TelnetMainServer class exists."); // Console.Writeline used because this occurs before kSP's logger is set up.
            Instance = this;
        }
        
        public void SetConfigEnable(bool newVal)
        {
            if (newVal == isListening)
                return;
            
            if (newVal)
                StartListening();
            else
                StopListening();
        }
        
        public void StartListening()
        {
            // calling StartListening when already started can cause problems, so quit if already started:
            if (isListening)
                return;
            
            // Build the server settings here, not in the constructor, because the settings might have been altered by the user post-init:

            port = Config.Instance.TelnetPort;
            if (Config.Instance.TelnetLoopback)
                bindAddr = IPAddress.Parse("127.0.0.1");
            else
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                if (localIPs.Length > 0)
                    bindAddr = localIPs[0]; // Hardcoded to only use the first IP it finds - good enough for most home network setups.
                else
                    bindAddr = IPAddress.Parse("127.0.0.1");
            }

            server = new TcpListener(bindAddr, port);
            server.Start();
            kOS.Safe.Utilities.Debug.Logger.Log("kOS TelnetMainServer started listening on " + bindAddr + " " + port);
            isListening = true;
        }

        public void StopListening()
        {
            // calling StopListening when already stopped can cause problems, so quit if already stopped:
            if (!isListening)
                return;
            isListening = false;

            kOS.Safe.Utilities.Debug.Logger.Log("kOS TelnetMainServer stopped listening on " + bindAddr + " " + port);
            server.Stop();
            foreach (TelnetSingletonServer telnet in telnets)
                telnet.StopListening();
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
            SetConfigEnable(Config.Instance.EnableTelnet);
            
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
            kOS.Safe.Utilities.Debug.Logger.Log("kOS telnet server got an incoming connection from " + remoteIdent);
            
            TelnetSingletonServer newServer = new TelnetSingletonServer(this, incomingClient, ++howManySpawned);
            telnets.Add(newServer);
            newServer.StartListening();
        }
    }
}

using System;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using UnityEngine;

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
    public class TelnetMainServer : MonoBehaviour
    {
        private TcpListener server = null;
        private IPAddress bindAddr;
        private Int32 port;
        private List<TelnetSingletonServer> telnets;
        private bool isListening;
        
        public TelnetMainServer()
        {
            isListening = false;
            gameObject.SetActive(false); // Should prevent the Update() from triggering until we turn it on.
            telnets = new List<TelnetSingletonServer>();

            kOS.Safe.Utilities.Debug.Logger.Log("kOS TelnetMainServer class exists.");
        }
        
        public void StartListening()
        {
            // calling StartListening when already started can cause problems, so quit if already started:
            if (isListening)
                return;
            
            // Build the server settings here, not in the constructor, because the settings might have been altered by the user post-init:

            port = Config.Instance.TelnetPort;
            if (Config.Instance.TelnetUsesLoopback)
                bindAddr = IPAddress.Parse("127.0.0.1");
            else
                bindAddr = IPAddress.Any;
            
            server = new TcpListener(bindAddr, port);
            server.Start();
            kOS.Safe.Utilities.Debug.Logger.Log("kOS TelnetMainServer started listening on " + bindAddr + " " + port);
            gameObject.SetActive(true);
            isListening = true;
        }

        public void StopListening()
        {
            // calling StopListening when already stopped can cause problems, so quit if already stopped:
            if (!isListening)
                return;

            gameObject.SetActive(false);
            kOS.Safe.Utilities.Debug.Logger.Log("kOS TelnetMainServer stopped listening on " + bindAddr + " " + port);
            server.Stop();
            foreach (TelnetSingletonServer telnet in telnets)
                telnet.StopListening();
    }
        
        public void Update()
        {
            Console.WriteLine("PROOF UPDATE IS CALLED A");
            if (!isListening)
                return;
            Console.WriteLine("PROOF UPDATE IS CALLED B");
            
            // .NET's TCP handler only gives you blocking socket accepting, but for the Unity
            // Update(), we need to always finish quickly and never block, so check if anything
            // is pending first to simulate the effect of a nonblocking socket accept check:
            if (!server.Pending())
                return;
            Console.WriteLine("PROOF UPDATE IS CALLED C");

            TcpClient incomingClient = server.AcceptTcpClient();
            Console.WriteLine("PROOF UPDATE IS CALLED D");
            
            string remoteIdent = ((IPEndPoint)(incomingClient.Client.RemoteEndPoint)).Address.ToString();
            kOS.Safe.Utilities.Debug.Logger.Log("kOS telnet server got an incoming connection from " + remoteIdent);
            Console.WriteLine("PROOF UPDATE IS CALLED E");
            
            // Perform something akin to a 'fork', but in Unity by making a new MonoBehavior to handle this
            // single client and talk to it, while the main server goes back to just listening to new clients:
            telnets.Add(new TelnetSingletonServer(incomingClient));
            telnets[telnets.Count].StartListening();
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using kOS.Safe.Utilities;
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
        private TcpClient client;
        private NetworkStream stream;

        private StringBuilder inBuffer = new StringBuilder();
        private int inBufferNextPos = 0; // rather than inefficiently shrinking the buffer from the left end, requiring a new buffer, just track where we are in it.

        private StringBuilder outBuffer = new StringBuilder();
        private int outBufferNextPos = 0; // rather than inefficiently shrinking the buffer from the left end, requiring a new buffer, just track where we are in it.

        private byte[] rawByteBuffer = new byte[32]; // use small chunks to spread across multiple Update()'s
        private char[] rawCharBuffer = new char[32]; // use small chunks to spread across multiple Update()'s

        private bool closeWhenFlushed;
        // ReSharper enable RedundantDefaultFieldInitializer
        // ReSharper enable SuggestUseVarKeywordEvident

        public TelnetSingletonServer(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();

            enabled = false; // don't start running the Update() until StartListening().
        }
        
        public void StartListening()
        {
            kOS.Safe.Utilities.Debug.Logger.Log("kOS telnet singleton server started listening.");
            enabled = true;
            closeWhenFlushed = false;
            SendText("\r\n\r\nkOS telnet server connection established.\r\n\r\n");
        }

        public void StopListening()
        {
            kOS.Safe.Utilities.Debug.Logger.Log("kOS telnet singleton server stopped listening.");
            SendText("\r\n\r\nkOS telnet server signing off.\r\n\r\n");
            closeWhenFlushed = true;
        }

        /// <summary>
        /// Buffers text to start being sent out to the client on the next Update().  You can
        /// make multiple calls to this to keep appending more text to the send buffer.  It will
        /// start going out to the client as soon as the next Update() hits.
        /// </summary>
        /// <param name="str"></param>
        public void SendText(string str)
        {
            outBuffer.Append(str);
        }
                        
        /// <summary>
        /// Get the next buffer chunk of data from the client stream, or print the next buffer chunk of
        /// bytes out to the client stream, or both.  Update()'s must finish
        /// fast, so don't try to consume or print everything in one call.  If there's too much
        /// just leave the remainder for the next Update() call.
        /// </summary>
        public virtual void Update()
        {
            // Detect if the client closed from its end:
            if (!client.Connected)
            {
                enabled = false;
                return;
            }

            OutputSome();
            InputSome();
        }
        
        /// <summary>
        /// Consume a small chunk from the outBuffer to the client stream. Update()'s must finish
        /// fast, so don't try to print everything in one call.  If there's too much
        /// just leave the remainder for the next Update() call.
        /// </summary>
        public void OutputSome()
        {
            // do nothing if outbuffer is flushed out.
            if (outBuffer.Length == 0)
            {
                // If we were being closed but waiting for flush first, now it's time to close:
                if (closeWhenFlushed)
                {
                    enabled = false;
                    client.Close();
                }
                return;
            }

            // If it got this far, it has to be because there's at least 1 char to print out.

            // Write the next few chars out:
            int numToWrite = System.Math.Min(outBuffer.Length - outBufferNextPos, rawCharBuffer.Length);
            inBuffer.CopyTo(outBufferNextPos, rawCharBuffer, 0, numToWrite);
            byte[] bytesFromChars = System.Text.Encoding.UTF8.GetBytes(rawCharBuffer, 0, numToWrite);
            stream.Write(bytesFromChars, 0, bytesFromChars.Length);

            // Track where in the buffer we are:
            outBufferNextPos += numToWrite;
            
            // If we printed everything and it's all flushed out, then just empty the output buffer entirely and reset the counter:
            if (outBufferNextPos >= outBuffer.Length)
            {
                outBuffer.Remove(0, outBuffer.Length);
                outBufferNextPos = 0;
            }
        }
        
        /// <summary>
        /// Read a small chunk from the client stream and add it to the inBuffer. Update()'s must finish
        /// fast, so don't try to read everything in one call.  If there's too much
        /// just leave the remainder for the next Update() call.
        /// </summary>
        public void InputSome()
        {
            // do nothing if socket has nothing coming in.
            if (!stream.DataAvailable)
                return;
            
            // TODO - make this better - right now I'm ignoring the inBuffer and just echoing this out
            // to the out buffer to print it back to the user for a test:
            int numRead = stream.Read(rawByteBuffer, 0, rawByteBuffer.Length);
            outBuffer.AppendFormat("[{0}]",System.Text.Encoding.UTF8.GetString(rawByteBuffer, 0, numRead));
            
        }
    }
}

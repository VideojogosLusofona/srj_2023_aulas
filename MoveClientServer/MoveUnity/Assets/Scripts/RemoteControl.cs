using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class RemoteControl : MonoBehaviour
{
    [SerializeField] private int port = 1234;

    private SpriteRenderer  spriteRenderer;
    private Socket          listenerSocket;
    private Socket          clientSocket;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (listenerSocket == null)
        {
            spriteRenderer.color = Color.red;
            OpenConnection();
        }
        else
        {
            spriteRenderer.color = Color.yellow;
            if (clientSocket == null)
            {
                spriteRenderer.color = Color.yellow;

                // Wait for a connection to be made - a new socket is created when that happens
                try
                {
                    clientSocket = listenerSocket.Accept();
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                    {
                        // The error was that this operation would block
                        // That's to be expected in our case while we don't have a a connection
                        return;
                    }
                    else
                    {
                        Debug.LogError(e);
                    }
                }

                if (clientSocket != null)
                {
                    Debug.Log("Connected!");

                    clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                }
            }
            else 
            {
                spriteRenderer.color = Color.green;

                ReceiveCommands();
            }
        }
    }

    void OpenConnection()
    {
        try
        {
            // Create listener socket
            // Prepare an endpoint for the socket, at port 80
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            // Create a Socket that will use TCP protocol
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // A Socket must be associated with an endpoint using the Bind method
            listenerSocket.Bind(localEndPoint);

            // Specify how many requests a Socket can listen before it gives Server busy response.
            // We will listen 1 request at a time
            listenerSocket.Listen(1);

            listenerSocket.Blocking = false;
        }
        catch (SocketException e)
        {
            Debug.LogError(e);
        }
    }

    int Receive(byte[] data, bool accountForLittleEndian = true)
    {
        try
        {
            // Normal path - received something
            int nBytes = clientSocket.Receive(data);

            if (accountForLittleEndian && (!BitConverter.IsLittleEndian))
                Array.Reverse(data);

            return nBytes;
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.WouldBlock)
            {
                // Didn't receive any data, just return 0
                return 0;
            }
            else
            {
                // Error => log it
                Debug.LogError(e);
            }
        }
        // Return -1 if there's an error
        return -1;
    }

    void ReceiveCommands()
    {
        if (clientSocket.Connected)
        {
            var lenBytes = new byte[4];

            int nBytes = Receive(lenBytes, true);

            if (nBytes == 4)
            {
                // Convert lenBytes from 4 bytes to a Uint32
                UInt32 commandLen = BitConverter.ToUInt32(lenBytes);

                var commandBytes = new byte[commandLen];
                nBytes = Receive(commandBytes, false);

                if (nBytes == commandLen)
                {
                    string command = Encoding.ASCII.GetString(commandBytes);
                    if (command == "up")
                    {
                        transform.position += Vector3.up * 0.25f;
                    }
                    else if (command == "down")
                    {
                        transform.position += Vector3.down * 0.25f;
                    }
                    else if (command == "right")
                    {
                        transform.position += Vector3.right * 0.25f;
                    }
                    else if (command == "left")
                    {
                        transform.position += Vector3.left * 0.25f;
                    }
                    else
                    {
                        Debug.LogError($"Unknown command {command}!");
                    }
                }
            }
            else
            {
                try
                {
                    if (clientSocket.Poll(1, SelectMode.SelectRead))
                    {
                    }
                }
                catch (SocketException e)
                {
                    Debug.LogError(e);

                    // Close the socket if it's not connected anymore
                    clientSocket.Close();
                    clientSocket = null;
                }
            }
        }
        else
        {
            // Close the socket if it's not connected anymore
            clientSocket.Close();
            clientSocket = null;
        }
    }
}

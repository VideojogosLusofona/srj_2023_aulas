using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleServer
{
    class Program
    {
        static Mutex        requestMutex;
        static List<Socket> requests;
        static Mutex        exitMutex;
        static bool         exitFlag;
        static int          nThreads = 4;

        static void Main(string[] args)
        {
            // Create a mutex to protect the requests list
            requestMutex = new Mutex();
            // Create the requests list
            requests = new List<Socket>();
            // Set the initial state, we don't want to exit
            exitFlag = false;
            // Create the mutex to protect exitFlag
            exitMutex = new Mutex();

            // Create the threads that will process the requests
            Thread[] threads = new Thread[nThreads];
            for (int i = 0; i<  nThreads; i++)
            {
                threads[i] = new Thread(RequestThread);
                threads[i].Start();
            }

            try
            {
                // Prepare an endpoint for the socket, at port 80
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 80);

                // Create a Socket that will use TCP protocol
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);

                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                while (true)
                {
                    // Wait for a connection to be made - a new socket is created when that happens
                    Console.WriteLine("Waiting for a connection...");
                    Socket handler = listener.Accept();

                    // Processes the request
                    Console.WriteLine("Connection received, processing...");
                    requestMutex.WaitOne();
                    requests.Add(handler);
                    requestMutex.ReleaseMutex();

                    // Check the (protected) exitFlag
                    // When it is true, exit this loop
                    exitMutex.WaitOne();
                    bool b = exitFlag;
                    exitMutex.ReleaseMutex();
                    if (b) break;
                }

                // Close the listener socket - no need for shutdown, since it's not
                // connected to anything
                listener.Close();
            }
            catch (SocketException e)
            {
                // In case of error, just write it
                Console.WriteLine($"SocketException : {e}");
            }

            // Wait for all threads to terminate
            for (int i = 0; i < nThreads; i++)
            {
                threads[i].Join();
            }

            Console.WriteLine("Application can exit now!");
        }

        // Thread function
        static void RequestThread()
        {            
            while (true)
            {
                requestMutex.WaitOne();
                if (requests.Count > 0)
                {
                    Socket socket = requests[0];
                    requests.RemoveAt(0);
                    requestMutex.ReleaseMutex();

                    ProcessRequest(socket);
                }
                else
                {
                    requestMutex.ReleaseMutex();
                }

                // Check the (protected) exitFlag
                // When it is true, exit this loop (and the thread)
                exitMutex.WaitOne();
                bool b = exitFlag;
                exitMutex.ReleaseMutex();
                if (b) break;
            }
        }

        static void ProcessRequest(Socket handler)
        {
            try
            {
                // Prepare space for request
                string incommingRequest = "";
                byte[] bytes = new byte[1024];

                while (true)
                {
                    // Read a max. of 1024 bytes
                    int bytesRec = handler.Receive(bytes);
                    // Convert that to a string
                    incommingRequest += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    // Check if the last 4 bytes we received was "\r\n"
                    if (bytesRec > 4)
                    {
                        // No more data to receive, just exit
                        if ((bytes[bytesRec - 4] == '\r') &&
                            (bytes[bytesRec - 3] == '\n') &&
                            (bytes[bytesRec - 2] == '\r') &&
                            (bytes[bytesRec - 1] == '\n'))
                        {
                            break;
                        }
                    }
                }

                // Write message received
                Console.WriteLine($"Message received:\n{incommingRequest}");

                // Split request in lines
                string[] lines = incommingRequest.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.TrimEntries);
                if (lines.Length >= 1)
                {
                    // Split the first line (GET ....) in components to get the address we want
                    string[] components = lines[0].Split(new[] { " " }, StringSplitOptions.TrimEntries);
                    if (components.Length >= 3)
                    {
                        // Check what we the user wants
                        if (components[1] == "/test1")
                        {
                            string payload = "<html><body>This is the response from my C# server, in case of test1 being requested!</body></html>";
                            SendResponse(handler, "200 OK", payload);
                        }
                        else if (components[1] == "/test2")
                        {
                            string payload = "<html><body>This is the response from my C# server, in case of test2 being requested!</body></html>";
                            SendResponse(handler, "200 OK", payload);
                        }
                        else if (components[1] == "/exit")
                        {
                            // Set the (protected) exitFlag to true
                            exitMutex.WaitOne();
                            exitFlag = true;
                            exitMutex.ReleaseMutex();

                            string payload = "<html><body>Server will now shutdown!</body></html>";
                            SendResponse(handler, "200 OK", payload);
                        }
                        else
                        {
                            string payload = "<html><body>This is the response from my C# server, in case of something not available being requested!</body></html>";
                            SendResponse(handler, "404 Not Found", payload);
                        }
                    }
                    else
                    {
                        // payload has the actual HTML we want to send
                        string payload = "<html><body>This is the response from my C# server when there's a malformed request</body></html>";

                        SendResponse(handler, "400 Bad Request", payload);

                    }
                }
            }
            catch (SocketException e)
            {
                // In case of error, just write it
                Console.WriteLine($"SocketException : {e}");
            }
            try
            {
                // Shutdown the socket, informing the other side
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (SocketException e)
            {
                // In case of error, just write it
                Console.WriteLine($"SocketException : {e}");
            }
        }

        static void SendResponse(Socket handler, string errorCode, string payload)
        {
            // Creates the headers
            // First indicate that the error code
            string response = $"HTTP/1.1 {errorCode}\r\n";
            // Then what kind of payload we're sending - HTML in this case
            response += "content-type: text/html; charset=UTF-8\r\n";
            // How many bytes are we sending back
            response += $"content-length: {payload.Length}\r\n";
            // An empty line
            response += "\r\n";
            // The actual payload
            response += payload;

            // Convert to bytes
            byte[] msg = Encoding.ASCII.GetBytes(response);
            // Send the message to the new socket that belongs to this actual request
            handler.Send(msg);

            // Just log something
            Console.WriteLine($"Response sent back to {handler.RemoteEndPoint}...");
        }
    }
}

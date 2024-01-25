using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MoveClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the hostname or IP address (enter for localhost): ");
            string server = Console.ReadLine();
            if (server == "") server = "localhost";

            try
            {
                // Convert the server hostname/IP to an IP address 
                // For maximum compatibility, I'm using IPv4, so I make sure I'm using
                // a IPv4 interface
                IPHostEntry ipHost = Dns.GetHostEntry(server);
                IPAddress   ipAddress = null;
                for (int i = 0; i < ipHost.AddressList.Length; i++)
                {
                    if (ipHost.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = ipHost.AddressList[i];
                    }
                }
                IPEndPoint  remoteEP = new IPEndPoint(ipAddress, 1234);

                // Create and connect the socket to the remote endpoint (TCP)
                Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);                
                socket.Connect(remoteEP);
                Console.WriteLine($"Connected to {server}!");

                Console.WriteLine("---------------");
                Console.WriteLine("Type 'exit' to leave");
                while (true)
                {
                    Console.Write(">");
                    string command = Console.ReadLine().Trim().ToLower();
                    if (command == "exit")
                    {
                        break;
                    }
                    else
                    {
                        UInt32 len = (UInt32)command.Length;
                        
                        var bytes = new byte[256];
                        SetBytes(len, bytes, 0);
                        SetBytes(command, bytes, 4);

                        socket.Send(bytes, command.Length + 4, SocketFlags.None);
                    }
                }

                // Close down the socket
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine($"ArgumentNullException: {e}");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
        }

        static void SetBytes(UInt32 value, byte[] bytes, int offset)
        {
            byte[] lenBytes = BitConverter.GetBytes(value);

            // Ensure little-endian byte order
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);

            // Copy the length bytes into the first 4 bytes of the 'bytes' array
            Array.Copy(lenBytes, 0, bytes, offset, lenBytes.Length);
        }

        static void SetBytes(string value, byte[] bytes, int offset)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(value);

            // Copy the length bytes into the first 4 bytes of the 'bytes' array
            Array.Copy(strBytes, 0, bytes, offset, strBytes.Length);
        }
    }
}

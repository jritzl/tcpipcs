using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace server
{
    public class SocketServer
    {
        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<string> _nicknames = new List<string>();
        private static Dictionary<string, int> path = new Dictionary<string, int>();
        private static Dictionary<int, string> reverse_path = new Dictionary<int, string>();
        private static Dictionary<string, bool> connection_status = new Dictionary<string, bool>();
        private static List<string> store = new List<string>();

        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Server is getting ready");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 100));
            _serverSocket.Listen(0);
            _serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server is ready");
        }


        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = _serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _clientSockets.Add(socket); // we add socket to list to use it later
            socket.BeginReceive(_buffer, 0, 1024, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("someone arrived");
            _serverSocket.BeginAccept(AcceptCallback, null); //continue to accept for multiple clients
        }

        private static void ReceiveCallback(IAsyncResult AR) // this is not the best function
        {

            Socket current = (Socket)AR.AsyncState;
            int received;


            try
            {
                try
                {
                    received = current.EndReceive(AR);
                }
                catch (SocketException)
                {
                    Console.WriteLine("someone gone");


                    current.Close();

                    _clientSockets.Remove(current);

                    return;
                }

                byte[] recBuf = new byte[received];
                Array.Copy(_buffer, recBuf, received);
                string coming = Encoding.ASCII.GetString(recBuf);
                string[] two = current.RemoteEndPoint.ToString().Split(':');
                int my_PORT = Int32.Parse(two[two.Length - 1]); //client's local port adress

                if (coming[0] == '0') // understanding this is a hello msg
                {

                    string[] three = coming.Split(':');

                    string nickname = three[1]; //client's nickname

                    Console.WriteLine("Hello msg from: " + nickname);

                    _nicknames.Remove(nickname); //to avoid duplicates we are removing first
                    _nicknames.Add(nickname);
                    path.Remove(nickname);
                    reverse_path.Remove(my_PORT);
                    path.Add(nickname, my_PORT); //this dictionary help us to match nickname and port
                    reverse_path.Add(my_PORT, nickname);//this dictionary help us to match nickname and port
                    connection_status.Remove(nickname);
                    connection_status.Add(nickname, true);// this dictionary keeps online status of client

                    current.BeginReceive(_buffer, 0, 1024, SocketFlags.None, ReceiveCallback, current);
                }
                else if (coming == "1:exit") //understand exit msg
                {

                    string[] one = coming.Split(':');
                    string text = one[one.Length - 1];
                    string target = one[1];
                    Console.WriteLine(text);
                    string myName = reverse_path[my_PORT];

                    if ((one[one.Length - 1]).ToLower() == "exit") // this if is not necessary
                    {
                        current.Shutdown(SocketShutdown.Both);
                        current.Close();

                        _clientSockets.Remove(current);
                        Console.WriteLine(myName + "gone1");
                        connection_status[myName] = false;
                        return;
                    }
                }
                else // our main text process client should send msg in format => target:text
                {
                    string[] one = coming.Split(':');
                    string text = one[one.Length - 1];
                    string target = one[1];
                    if (!path.ContainsKey(target)) // if target is not a client of server
                    {
                        throw new FormatException("wrong");
                    }
                    int myKey = path[target]; // we have target's key
                    string myName = reverse_path[my_PORT]; //we have our nickname


                    byte[] data3 = Encoding.ASCII.GetBytes("sent\n"); // giving feedback to client
                    byte[] data33 = Encoding.ASCII.GetBytes("will sent when s/he is online \n"); // giving feedback to client
                    try
                    {
                        if (connection_status[target])
                        {
                            current.Send(data3);
                        }
                        else
                        {
                            current.Send(data33);
                        }
                            
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine(myName + "gone2");
                        current.Close();
                        connection_status[myName] = false;
                        _clientSockets.Remove(current);

                        return;
                    }




                    try
                    {
                        current.BeginReceive(_buffer, 0, 1024, SocketFlags.None, ReceiveCallback, current);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine(myName + " gone3");
                        connection_status[myName] = false;
                        current.Close();
                        _clientSockets.Remove(current);

                        return;
                    }

                    try
                    {
                        //sending msg to target
                        byte[] data4 = Encoding.ASCII.GetBytes(myName + ": " + text + ".\n");
                        string data45 = myName + ":" + target + ":" + text; // if client is offline this data will stored

                        for (int i = 0; i < _clientSockets.Count; i++)
                        {
                            string[] four = _clientSockets[i].RemoteEndPoint.ToString().Split(':');
                            int target_port = Int32.Parse(four[four.Length - 1]);

                            if (connection_status[target])
                            {
                                if (target_port == myKey) // we try every port number in our client list, and if port numbers matches we are sending msg.
                                {

                                    _clientSockets[i].Send(data4);
                                    Console.WriteLine(myName + ": " + text + ".\n");
                                    Console.WriteLine(current.RemoteEndPoint + " " + myName + " sends msg to " + _clientSockets[i].RemoteEndPoint + " " + target);

                                }
                            }
                            else
                            {
                                if (!store.Contains(data45))
                                {
                                    store.Add(data45);// we stored msg to send it later store format => whosendmsg:towho:msg
                                }

                            }

                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        feedback(current);
                    }

                }
            }
            catch (FormatException)
            {
                feedback(current);

            }
            catch (KeyNotFoundException)
            {
                feedback(current);
            }
            catch (IndexOutOfRangeException)
            {
                feedback(current);
            }


            while (store.Count > 0) // this while loop sends msg in store when target become online again
            {
                for (int i = 0; i < store.Count; i++)
                {
                    try
                    {
                        string dumm = store[i];
                        string[] one = store[i].Split(':');
                        string text = one[2];
                        string target = one[1];
                        string host = one[0];
                        int myKey = path[target];
                        byte[] data4 = Encoding.ASCII.GetBytes(host + ": " + text + ".\n");
                        for (int j = 0; j < _clientSockets.Count; j++)
                        {
                            if (connection_status[target] == true)
                            {
                                string[] four = _clientSockets[j].RemoteEndPoint.ToString().Split(':');
                                int target_port = Int32.Parse(four[four.Length - 1]);
                                store.Remove(dumm);
                                if (target_port == myKey) // same key-target match method
                                {
                                    _clientSockets[j].Send(data4);
                                    Console.WriteLine(data4);
                                    Console.WriteLine(host + " sends msg to " + target);

                                }
                            }

                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return;
                    }
                    catch (KeyNotFoundException)
                    {
                        return;
                    }

                }
            }
        }
        private static void feedback(Socket current)
        {
            byte[] data3 = Encoding.ASCII.GetBytes(" not valid1\n");
            try
            {
                current.Send(data3);
                current.BeginReceive(_buffer, 0, 1024, SocketFlags.None, ReceiveCallback, current);
            }
            catch (SocketException)
            {
                Console.WriteLine("someone gone3");
                current.Close();
                _clientSockets.Remove(current);


            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading;


public class SocketClient
{
    private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //we create a socket
    private const int PORT = 100;
    private static string id = "";



    static void Main(string[] args) //we have different client names, these names are important to separate clients from each other.
    {

        if (1 == args.Length)// checking client name valid or not
        {
            id = args[0];
        }
        Console.Title = "Client: " + id; 
        mainn(); 

    }

    private static void ConnectToServer()
    {
        int attempts = 0;

        while (!_clientSocket.Connected)
        {
            try
            {
                attempts++;
                Console.WriteLine("Connection attempt " + attempts);
                Thread.Sleep(1000);
                _clientSocket.Connect(IPAddress.Loopback, PORT);
            }
            catch (SocketException)
            {
                Console.Clear();
            }
            catch (InvalidOperationException) // when the server shutdown, these 'catch' help us to regenerate a socket for client.
            {
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainn();
            }

        }

        Console.Clear();
        Console.WriteLine("Connected");
    }

    private static void msgLoop()
    {
        try
        {// since client cannot listen and sendmsg at the same time, we create a thread
            Thread th1 = new Thread(new ThreadStart(Sendmsg));
            Thread th2 = new Thread(new ThreadStart(ReceiveResponse));

            th2.Start();
            th1.Start();
            th2.Join();

            while (true)
            {

                msgLoop();
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("Server is offline");
            ConnectToServer();
        }


    }

    private static void Exit()
    {
        SendString(id + " has left");

        Environment.Exit(0);
    }

    private static void Sendmsg()
    {
        Console.Write("Choose a destination and send a msg: \n ");
        string request = Console.ReadLine();

        string msg = "1:" + request; // when server see 1, it will understand it is a text msg
        SendString(msg);

        if (request.ToLower() == "exit")
        {
            Exit();
        }
    }

    private static void SendString(string text)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(text);
        _clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
    }

    private static void ReceiveResponse()
    {
        var buffer = new byte[1024];
        try
        {
            int received = _clientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            Console.WriteLine(text);
        }
        catch (SocketException)
        {
            Console.WriteLine("Server is offline");
            ConnectToServer();
        }



    }
  
    public static void mainn() // this is our main process
    {

        ConnectToServer();
      
        string msg1 = "0:" + id; // this is a hello msg to server to create a new client in server.
        
        SendString(msg1);
        
        Console.WriteLine(@"<Type ""exit"" to properly disconnect client>");
        msgLoop();
        Exit();
    }


}

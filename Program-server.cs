using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Text;
using System.Security.Cryptography;

/* ChatShell Server */
namespace ChatShell
{
    class Program
    {

        // Private message - "!pm reciever sender message_body"

        public static Hashtable clients = new Hashtable();

        static void Main(string[] args)
        {
            ChatShell();

            TcpListener listner = null;

            int port = 6969;

            try
            {
                listner = new TcpListener(IPAddress.Any, port);
                listner.Start();
                Console.WriteLine("ChatShell server started.");
                Console.WriteLine("Awaiting clients...");

                while (true)
                {
                    if (listner.Pending())
                    {
                        TcpClient client = default(TcpClient);
                        client = listner.AcceptTcpClient();

                        // Get username from client;    
                        byte[] inStream = new byte[100];
                        NetworkStream networkStream = client.GetStream();
                        int bytesRead = networkStream.Read(inStream, 0, inStream.Length);
                        string clientData = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                        if (clients.ContainsKey(clientData))
                        {
                            // If user with the same username exists
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"User with username {clientData} tried to connect. But was rejected as that username already exists");
                            Console.ForegroundColor = ConsoleColor.White;

                            byte[] outStream = Encoding.ASCII.GetBytes("!error user-exists");
                            networkStream.Write(outStream, 0, outStream.Length);
                        }
                        else
                        {
                            byte[] outStream = Encoding.ASCII.GetBytes("!connect");
                            networkStream.Write(outStream, 0, outStream.Length);

                            //Display message in server CLI
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{clientData} connected to the chat");
                            Console.BackgroundColor = ConsoleColor.Black;

                            //Broadcast message to other clients
                            BroadcastMessage($"{clientData} connected to the chat", clientData, false);

                            //Add new client to client list;
                            clients.Add(clientData, client);

                            // Send already connected user list to clients
                            if (clients.Count != 0)
                            {
                                BroadcastMessage(getUserList(), clientData, false);
                            }

                            //Create ClientClass object
                            ClientClass clientObject = new ClientClass();

                            clientObject.username = clientData;
                            clientObject.client = client;

                            //Create new thread to handle client
                            Thread thread = new Thread(HandleClient);
                            thread.Start(clientObject);
                        }
                    }
                   
                }

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {

            }



            Console.ReadKey();
        }

        static void ChatShell()
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("   ____ _           _   ____  _          _ _ ");
            Console.WriteLine("  / ___| |__   __ _| |_/ ___|| |__   ___| | |");
            Console.WriteLine(" | |   | '_ \\ / _` | __\\___ \\| '_ \\ / _ \\ | |");
            Console.WriteLine(" | |___| | | | (_| | |_ ___) | | | |  __/ | |");
            Console.WriteLine("  \\____|_| |_|\\__,_|\\__|____/|_| |_|\\___|_|_|");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\t\t\t\tServer");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n\n");
        }

        static void HandleClient(object clientObject)
        {

            ClientClass recievedClientObject = (ClientClass)clientObject;

            TcpClient client = recievedClientObject.client;
            string username = recievedClientObject.username;

            try
            {
                
                string dataFromClient = null;

                do
                {
                    byte[] inStream = new byte[4096];
                    NetworkStream networkStream = client.GetStream();
                    int bytesRead = networkStream.Read(inStream, 0, inStream.Length);
                    dataFromClient = Encoding.ASCII.GetString(inStream, 0, bytesRead);
                    if (!dataFromClient.Equals("/exit"))
                    {
                        if (dataFromClient.StartsWith("!pm"))
                        {
                            Console.WriteLine($"{dataFromClient}");
                            BroadcastPrivateMessage(dataFromClient, username);
                        }
                        else
                        {
                            Console.WriteLine($"{username}: {dataFromClient}");
                            BroadcastMessage(dataFromClient, username, true);
                        }
                    }

                } while (!(dataFromClient.Equals("/exit") || dataFromClient == null));

                clients.Remove(username);

                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{username} disconnected from the chat");
                Console.BackgroundColor = ConsoleColor.Black;

                BroadcastMessage($"!dc {username} disconnected from the chat", username, false);

                // Send updated user list to clients
                if (clients.Count != 0)
                {
                    BroadcastMessage(getUserList(), username, false);
                }


            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if(client != null)
                {
                    client.Close();
                }
            }
        }

        /* Method to broadcast messages to other clients */
        static void BroadcastMessage(string message, string username, bool flag)
        {
            foreach (DictionaryEntry Client in clients)
            {

                TcpClient client = (TcpClient)Client.Value;

                NetworkStream broadcastStream = client.GetStream();

                if (flag)
                {
                    byte[] outStream = Encoding.ASCII.GetBytes(($"{username} >>> {message}"));
                    broadcastStream.Write(outStream, 0, outStream.Length);
                }
                else
                {
                    byte[] outStream = Encoding.ASCII.GetBytes(message);
                    broadcastStream.Write(outStream, 0, outStream.Length);
                }

                broadcastStream.Flush();
            }
        }

        static void BroadcastPrivateMessage(string message, string username)
        {
            string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            string send_to = elements[1]; 

            foreach (DictionaryEntry Client in clients)
            {
                TcpClient client = (TcpClient)Client.Value;

                if (Client.Key.Equals(send_to))
                {
                    NetworkStream broadcastStream = client.GetStream();

                      byte[] outStream = Encoding.ASCII.GetBytes(message);
                      broadcastStream.Write(outStream, 0, outStream.Length);
                      broadcastStream.Flush();
                }
            }
        }

        static string getUserList()
        {
            string userlist = "!users";

            foreach (DictionaryEntry Client in clients)
            {
                string username = (string)Client.Key;

                userlist += $" {username}";
            }

            return userlist;

        }
    }

    public class ClientClass
    {

        public TcpClient client
        {
            get;
            set;
        }

        public string username
        {
            get;
            set;
        }


    }
}

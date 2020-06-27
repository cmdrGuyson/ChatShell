/* 
 *
 * Created By Gayanga Kuruppu
 *
 */
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Text;

/* ChatShell Server */
namespace ChatShell
{
    class Server
    {

        // Encryptor to encrypt and decrypt messages (Caesar Cipher) and commands. 8 is used as the encryption key
        private static Encryptor encryptor = new Encryptor(8);

        // Hashtable to maintain connected clients
        private static Hashtable clients = new Hashtable();

        static void Main(string[] args)
        {
            // Display logo
            ChatShell();

            TcpListener listner = null;
            // The server will be listening to the below port for client connections
            int port = 6969;

            try
            {
                // Listen for client connections on any ip address on above declared port
                listner = new TcpListener(IPAddress.Any, port);
                listner.Start();
                Console.WriteLine("ChatShell server started.");
                Console.WriteLine("Awaiting clients...");

                // Run on an infinite loop
                while (true)
                {
                    if (listner.Pending())
                    {
                        TcpClient client = default(TcpClient);
                        client = listner.AcceptTcpClient();

                        // Get username from client as a byte array and then encode to ASCII string;    
                        byte[] inStream = new byte[100];
                        NetworkStream networkStream = client.GetStream();
                        int bytesRead = networkStream.Read(inStream, 0, inStream.Length);
                        string recievedData = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                        //Decrypt data gotten from client
                        recievedData = encryptor.decrypt(recievedData);

                        //Get connection password and username (stored in client data)
                        string pass = recievedData.Substring(0, 4);
                        string clientData = recievedData.Remove(0, 4);

                        //If invalid connection password block user and send error message
                        if (!pass.Equals("PASS"))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Forceful connection declined!");
                            Console.ForegroundColor = ConsoleColor.White;

                            // Send an encoded and encrypted message to the client saying that the username alredy exists
                            byte[] outStream = Encoding.ASCII.GetBytes("Invalid client. Please use ChatShell to connect");
                            networkStream.Write(outStream, 0, outStream.Length);
                        }
                        // If user with the same username exists
                        else if (clients.ContainsKey(clientData))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"User with username {clientData} tried to connect. But was rejected as that username already exists");
                            Console.ForegroundColor = ConsoleColor.White;

                            // Send an encoded and encrypted message to the client saying that the username alredy exists
                            byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt("!error user-exists"));
                            networkStream.Write(outStream, 0, outStream.Length);
                        }
                        else
                        {
                            // Send a message to the client allowing the connection after encryption. Encrypted message is turned into a bye array when sending
                            byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt("!connect"));
                            networkStream.Write(outStream, 0, outStream.Length);
                            
                            //Prevent network stream messages from overlapping
                            Thread.Sleep(10);

                            //Display message in server CLI
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{clientData} connected to the chat");
                            Console.BackgroundColor = ConsoleColor.Black;

                            //Broadcast message to other clients
                            broadcastMessage($"!rc {clientData} connected to the chat", clientData, false); 

                            //Add new client to client list;
                            clients.Add(clientData, client);

                            // Send already connected user list to clients
                            if (clients.Count != 0)
                            {
                                broadcastMessage(getUserList(), clientData, false);
                            }

                            //Create ClientClass object and set variable values
                            ClientClass clientObject = new ClientClass();

                            clientObject.username = clientData;
                            clientObject.client = client;

                            //Create new thread to handle client
                            Thread thread = new Thread(handleClient);
                            thread.Start(clientObject);
                        }
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }

        // Method to display logo
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

        // Method to handle a single client (will run on multiple threads for different clients)
        static void handleClient(object clientObject)
        {
            // Cast the recieved ClientClass object
            ClientClass recievedClientObject = (ClientClass)clientObject;

            // Get the TcpClient object and username from the ClientClass object
            TcpClient client = recievedClientObject.client;
            string username = recievedClientObject.username;

            try
            {
                
                string dataFromClient = null;

                // Until client sends a disconnecting command run this loop
                do
                {
                    // Recive data from the client using the network stream
                    byte[] inStream = new byte[4096];
                    NetworkStream networkStream = client.GetStream();
                    int bytesRead = networkStream.Read(inStream, 0, inStream.Length);
                    dataFromClient = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                    // Decrypt the data taken from client
                    dataFromClient = encryptor.decrypt(dataFromClient);

                    // If the client doesnt send a disconnect command
                    if (!dataFromClient.Equals("!exit"))
                    {
                        // If the client wants to send a private message to another client
                        if (dataFromClient.StartsWith("!pm"))
                        {
                            Console.WriteLine($"{dataFromClient}");
                            broadcastPrivateMessage(dataFromClient);
                        }
                        //If the client message is public
                        else
                        {
                            Console.WriteLine($"{username}: {dataFromClient}");
                            broadcastMessage(dataFromClient, username, true);
                        }
                    }

                    // Exit loop after client sends disconnect command
                } while (!(dataFromClient.Equals("!exit") || dataFromClient == null));

                // After the client had disconnected

                // Remove the client from the hashtable
                clients.Remove(username);

                // Display message on server
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{username} disconnected from the chat");
                Console.BackgroundColor = ConsoleColor.Black;

                // Broadcast to other clients in an encoded message that the client has disconnected
                broadcastMessage($"!dc {username} disconnected from the chat", username, false);

                // Send updated user list to clients if there are connected clients
                if (clients.Count != 0)
                {
                    broadcastMessage(getUserList(), username, false);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Finally close the created TcpClient object
                if(client != null)
                {
                    client.Close();
                }
            }
        }

        /* Method to broadcast messages to other clients */
        static void broadcastMessage(string message, string username, bool flag)
        {
            // Send the message/command recived to all members of the client hashtable
            foreach (DictionaryEntry Client in clients)
            {
                // Cast the value taken from the dictionary into TcpClient and get the NetworkStream
                TcpClient client = (TcpClient)Client.Value;
                NetworkStream broadcastStream = client.GetStream();

                // If it is a regular message
                if (flag)
                {
                    // Encrypt message and send through network stream
                    byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt($"{username} >>> {message}"));
                    broadcastStream.Write(outStream, 0, outStream.Length);
                }
                // If it is a command
                else
                {
                    // Encrypt command and send through network stream
                    byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt(message));
                    broadcastStream.Write(outStream, 0, outStream.Length);
                }
                broadcastStream.Flush();
            }
        }

        /* Method to broadcast private messages */
        static void broadcastPrivateMessage(string message)
        {
            //Decode the private message
            string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            string send_to = elements[1];

            foreach (DictionaryEntry Client in clients)
            {
                TcpClient client = (TcpClient)Client.Value;

                // Send the private message to only the correct client
                if (Client.Key.Equals(send_to))
                {
                    NetworkStream broadcastStream = client.GetStream();

                      byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt(message));
                      broadcastStream.Write(outStream, 0, outStream.Length);
                      broadcastStream.Flush();
                }
            }
        }

        // Method to generate the user list of the current connected clients to the server
        // This method will be called and user list will be sent after each connection and disconnect
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

    // ClientClass Class used to manage TcpClient objects and client usernames
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

    // Class to encrypt and decrypt messages using simple Caesar Cipher
    public class Encryptor
    {
        // Key used to ecrypt data
        private int key;

        public Encryptor(int key)
        {
            this.key = key;
        }
        private char cipher(char ch)
        {
            // If the charachter is not a letter keep it as it is
            if (!char.IsLetter(ch))
            {
                return ch;
            }
            // If the charachter is a letter depending on which case it is shift the charachter by the key (if key is 1 'a' becomes 'b')
            char d = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + key) - d) % 26) + d);
        }

        private char decipher(char ch)
        {
            // The new shift (key) value for deciphering is 26-key
            int shift = 26 - key;

            // If the charachter is not a letter keep it as it is
            if (!char.IsLetter(ch))
            {
                return ch;
            }
            // (if old key was 1, 'b' becomes 'a')
            char d = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + shift) - d) % 26) + d);
        }

        // Method used to encrypt strings
        public string encrypt(string input)
        {
            string output = string.Empty;

            // For each charachter in the input string call the cipher method to encrypt
            foreach (char ch in input)
                output += cipher(ch);

            return output;
        }

        // Method used to decrypt strings
        public string decrypt(string input)
        {
            string output = string.Empty;

            // For each charachter in the input string call the decipher method to decrypt
            foreach (char ch in input)
                output += decipher(ch);

            return output;
        }
    }
}

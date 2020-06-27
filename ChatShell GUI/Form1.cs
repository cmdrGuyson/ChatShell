/* 
 *
 * Created By Gayanga Kuruppu
 *
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Windows.Forms;

namespace ChatShell_GUI
{
    public partial class MainClientForm : Form
    {
        // Varibales to store TcpClient object and NetworkStream
        private TcpClient client;
        private NetworkStream broadcastStream;
        private Encryptor encryptor;

        // Hashtable containing all private chats that were started
        private Hashtable activeChats = new Hashtable();

        // Username of client
        private string username;

        // If connected to server
        private bool connected;

        

        public MainClientForm()
        {
            InitializeComponent();
            // Setting initial values and making sure backgroundWorker can report progress and is allowed to cancel
            client = null;
            broadcastStream = null;
            connected = false;
            encryptor = new Encryptor(8);
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            // ================================================

            textBox1.AutoSize = false;
            textBox1.Size = new System.Drawing.Size(384, 29);
        }


        /* When 'connect' button is clicked */
        private void connectButton_Click(object sender, EventArgs e)
        {
            
            /* If not connected */
            if (!connected)
            {
                username = textBox2.Text;

                if (username.Equals("")||username.Contains(" "))
                {
                    // If an empty string is kept for username or if username has more than one word display error message
                    MessageBox.Show("Please enter a valid username", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    try
                    {
                        /* Connect to server */
                        client = new TcpClient("127.0.0.1", 6969);

                        /* Send client username to server */
                        broadcastStream = client.GetStream();
                        byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt("PASS"+username));
                        broadcastStream.Write(outStream, 0, outStream.Length);

                        /* Recive information whether client accepted connection */
                        byte[] inStream = new byte[100];
                        int bytesRead = broadcastStream.Read(inStream, 0, inStream.Length);
                        string serverData = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                        /* Decrypt data recieved */
                        serverData = encryptor.decrypt(serverData);

                        // If client accepts connection
                        if (serverData.Equals("!connect"))
                        {
                            // Handle UI for connection and run backgroundWorker
                            connected = true;
                            connectButton.Text = "Disconnect";
                            textBox2.Enabled = false;
                            backgroundWorker1.RunWorkerAsync();
                        }
                        // If client doesnt accept connection
                        else
                        {
                            MessageBox.Show("Someone is already connected using that username", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        textBox2.Enabled = true;
                    }
                }
            }
            /* If already connected */
            else
            {
                this.Close();
            }
        }

        /* When this.Close() is called */
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (connected)
            {
                try
                {
                    // Cancel backgroundWorker asynchronously
                    backgroundWorker1.CancelAsync();

                    // Send message to the server to disconnect from it after encrypting message
                    byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt("!exit"));
                    broadcastStream.Write(outStream, 0, outStream.Length);
                    broadcastStream.Flush();

                    MessageBox.Show("Successfully disconnected from the server!", "Disconnected!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /* When 'send' is clicked */
        private void button1_Click(object sender, EventArgs e)
        {
            // If connected to server send the message else display error
            if (connected)
            {
                sendMessage();
            }
            else
            {
                MessageBox.Show("Connect to the server first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /* Recieve messages in the background */
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // ===============================
            BackgroundWorker bgw = (BackgroundWorker) sender;
            byte[] inStream = new byte[4096];
            int bytesRead;
            string returndata = " ";
            // ===============================

            // Run in an infinite loop until break;
            while (true)
            {
                // Recive messages from the server
                bytesRead = broadcastStream.Read(inStream, 0, inStream.Length);
                returndata = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                // Decrypt message
                returndata = encryptor.decrypt(returndata);

                // If message is a command
                if (returndata.StartsWith("!users")|| returndata.StartsWith("!pm")|| returndata.StartsWith("!dc") || returndata.StartsWith("!rc"))
                {
                    // Direct command to background worker using report progress
                    bgw.ReportProgress(1,returndata);
                }
                // If message is a message
                else if(bytesRead>0)
                {
                    // Direct command to background worker using report progress after adding !dispText
                    bgw.ReportProgress(1, "!dispText" + returndata);
                }

                // If there is a cancellation pending for the background worker close client and network stream then exit the loop
                if (bgw.CancellationPending)
                {
                    e.Cancel = true;
                    if (client != null) { client.Close(); client = null; }
                    if (broadcastStream != null)  { broadcastStream.Close(); broadcastStream = null; } 
                    break;
                }

            }
        }

        /* Send message to public chat */
        private async void sendMessage()
        {
            if (connected)
            {
                // Get message to be sent from the text box
                string message = textBox1.Text;

                // If user tries to send a command to the server manually
                if (message.StartsWith("!"))
                {
                    MessageBox.Show("You are not allowed to send messages starting with '!'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //If the string doesnt only contain whitespaces
                else if (message.Replace(" ", "").Length!=0)
                {
                    try
                    {
                        // Send the message to the server after encrypting using the network strean asynchronously
                        byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt(message));
                        await broadcastStream.WriteAsync(outStream, 0, outStream.Length);

                        // Clear the text box
                        textBox1.Clear();
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        /* Send message to private chat (This method will be called by PrivateChatForm objects)*/
        public async void sendMessage(string message)
        {
            if (connected)
            {
                try
                {
                    // Send the message to the server using the network strean asynchronously after encryption
                    byte[] outStream = Encoding.ASCII.GetBytes(encryptor.encrypt(message));
                    await broadcastStream.WriteAsync(outStream, 0, outStream.Length);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /* When enter key is pressed */
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // If enter is pressed while focused on the text box send the message
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                sendMessage();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // ================================================
            // if (e.Cancelled) MessageBox.Show("Cancelled");
            // ================================================
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Take message from arguments
            string message = e.UserState.ToString();

            // If the server had sent the new users list
            if (message.StartsWith("!users"))
            {

                listBox1.Items.Clear();
                // Update user list
                string[] users = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < users.Length; i++)
                {
                    // Add all new users given from the server to the list box
                    listBox1.Items.Add(users[i]);
                }
            }
            // If the message should be displayed on the rich text box
            else if (message.StartsWith("!dispText"))
            {
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText($"{message.Remove(0, 9)}\n");
            }
            // If someone had connected to the server
            else if (message.StartsWith("!rc"))
            {
                // Display connection message in green.
                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText($"{message.Remove(0, 4)}\n");

                // Split the message by white spaces to decode
                string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                string rc_user = elements[1];

                // If a user reconnects to the chat and has private chats activate them.
                if (activeChats.Contains(rc_user))
                {
                    // If chat is already active
                    PrivateChatForm privateForm = (PrivateChatForm)activeChats[rc_user];
                    privateForm.activateChat();
                }
            }
            // If someone had disconnected from the server
            else if (message.StartsWith("!dc"))
            {
                // Display disconnection message in red.
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText($"{message.Remove(0, 4)}\n");

                string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                string dc_user = elements[1];

                // Some user has disconnected, therefore all private chats with that user are disabled
                if (activeChats.Contains(dc_user))
                {
                    // If chat is already active
                    PrivateChatForm privateForm = (PrivateChatForm)activeChats[dc_user];
                    privateForm.deactivateChat();
                }
            }
            // If a private message is sent to the client
            else if (message.StartsWith("!pm"))
            {
                string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                string sent_by = elements[2];
                string message_body = "";

                // Create the message body from the decoded message (as it was split)
                for (int i=3; i<elements.Length; i++)
                {
                    message_body += elements[i] + " ";
                }

                if (activeChats.ContainsKey(sent_by))
                {
                    // If chat is already active
                    PrivateChatForm privateForm = (PrivateChatForm)activeChats[sent_by];

                    // If the private chat window is not open, open it.
                    if (!privateForm.isActive)
                    {
                        privateForm.showForm();
                    }

                    // Call displayMessage() on the PrivateForm to display the new message
                    privateForm.displayMessage(message_body);
                }
                else
                {
                    // Activate chat
                    PrivateChatForm privateForm = new PrivateChatForm(sent_by, username, message_body, this);
                    // Add chat to acive chats
                    activeChats.Add(sent_by, privateForm);
                    // Open private chat window
                    privateForm.Show();
                }
            }
        }

        // When double clicked on a user
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string send_to = listBox1.SelectedItem.ToString();

            // If a private chat with that user is not initiated and the cliked user is not this client.
            if (!activeChats.Contains(send_to)&&!username.Equals(send_to))
            {
                // Activate chat
                PrivateChatForm privateForm = new PrivateChatForm(send_to, username, this);
                // Add chat to acive chats
                activeChats.Add(send_to, privateForm);
                privateForm.showForm();
            }else if (activeChats.Contains(send_to))
            {
                // If chat is already active
                PrivateChatForm privateForm = (PrivateChatForm)activeChats[send_to];

                // If the private chat window is not open, open it.
                if (!privateForm.isActive)
                {
                    privateForm.showForm();
                }
            }
        }
    }

    // Class to encrypt and decrypt messages using simple Caesar Cipher
    public class Encryptor
    {
        private int key;

        public Encryptor(int key)
        {
            this.key = key;
        }

        private char cipher(char ch)
        {
            if (!char.IsLetter(ch))
            {

                return ch;
            }

            char d = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + key) - d) % 26) + d);
        }

        private char decipher(char ch)
        {
            int shift = 26 - key;

            if (!char.IsLetter(ch))
            {

                return ch;
            }
            char d = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + shift) - d) % 26) + d);
        }

        public string encrypt(string input)
        {
            string output = string.Empty;

            foreach (char ch in input)
                output += cipher(ch);

            return output;
        }

        public string decrypt(string input)
        {
            string output = string.Empty;

            foreach (char ch in input)
                output += decipher(ch);

            return output;
        }
    }
}

/* 
 *
 * Created By Gayanga Kuruppu
 *
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatShell_GUI
{
    public partial class Form1 : Form
    {

        TcpClient client;
        NetworkStream broadcastStream;

        private Hashtable activeChats = new Hashtable();

        private string username;

        private bool connected = false;

        public Form1()
        {
            InitializeComponent();
            // ================================================
            client = null;
            broadcastStream = null;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            // ================================================
        }


        /* When 'connect' button is clicked */
        private void connectButton_Click(object sender, EventArgs e)
        {
            
            /* If not connected */
            if (!connected)
            {
                username = textBox2.Text;

                if (username.Equals(""))
                {
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
                        byte[] outStream = Encoding.ASCII.GetBytes(username);
                        broadcastStream.Write(outStream, 0, outStream.Length);

                        byte[] inStream = new byte[100];
                        int bytesRead = broadcastStream.Read(inStream, 0, inStream.Length);
                        string serverData = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                        if (serverData.Equals("!connect"))
                        {
                            /* Change the text of button to disconnect */
                            connected = true;
                            connectButton.Text = "Disconnect";
                            textBox2.Enabled = false;
                            backgroundWorker1.RunWorkerAsync();
                        }
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
                    backgroundWorker1.CancelAsync();

                    byte[] outStream = Encoding.ASCII.GetBytes("/exit");
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
            sendMessage();
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

            while (true)
            {
                bytesRead = broadcastStream.Read(inStream, 0, inStream.Length);
                returndata = Encoding.ASCII.GetString(inStream, 0, bytesRead);

                if (returndata.StartsWith("!users"))
                {
                    bgw.ReportProgress(1,returndata);
                }
                else if (returndata.StartsWith("!pm"))
                {
                    bgw.ReportProgress(1, returndata);
                }
                else if (returndata.StartsWith("!dc"))
                {
                    bgw.ReportProgress(1, returndata);
                }
                else if(bytesRead>0)
                {
                    bgw.ReportProgress(1, "!dispText" + returndata);
                }

                // ================================
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
                string message = textBox1.Text;

                if (message.Equals("!exit"))
                {
                    this.Close();
                }
                try
                {
                    byte[] outStream = Encoding.ASCII.GetBytes(message);
                    await broadcastStream.WriteAsync(outStream, 0, outStream.Length);
                    textBox1.Clear();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /* Send message to private chat */
        public async void sendMessage(string message)
        {
            if (connected)
            {
                try
                {
                    byte[] outStream = Encoding.ASCII.GetBytes(message);
                    await broadcastStream.WriteAsync(outStream, 0, outStream.Length);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /* WHen enter is pressed */
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
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
            // ============================================
            string message = e.UserState.ToString();
            if (message.StartsWith("!users"))
            {

                listBox1.Items.Clear();
                // Update user list
                string[] users = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < users.Length; i++)
                {
                    listBox1.Items.Add(users[i]);
                }
            }
            else if (message.StartsWith("!dispText"))
            {
                richTextBox1.Text += Environment.NewLine + message.Remove(0, 9);
            }
            else if (message.StartsWith("!dc"))
            {
                // Some user has disconnected, therefore all private chats with that user are disabled
                richTextBox1.Text += Environment.NewLine + message.Remove(0, 4);
                string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                string dc_user = elements[1];

                if (activeChats.Contains(dc_user))
                {
                    // If chat is already active
                    PrivateChatForm privateForm = (PrivateChatForm)activeChats[dc_user];
                    privateForm.deactivateChat();
                }
            }
            else if (message.StartsWith("!pm"))
            {
                string[] elements = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                string sent_by = elements[2];
                string message_body = "";

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

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string send_to = listBox1.SelectedItem.ToString();

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
}

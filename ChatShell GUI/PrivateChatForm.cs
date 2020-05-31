using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatShell_GUI
{
    public partial class PrivateChatForm : Form
    {
        // String variables for the username of the remote client and this client respectively
        private string remote_user, local_user;

        // Is active should have a getter and setter so that it can be accessed by form 1
        public bool isActive { get; set; }

        // The main client window
        private Form1 parent;

        // Constructor to be used if initiating after a private message is recived by the client
        public PrivateChatForm(string remote_user, string local_user, string initial_message, Form1 parent)
        {
            InitializeComponent();
            this.parent = parent;
            this.remote_user = remote_user;
            this.isActive = true;
            this.local_user = local_user;
            richTextBox1.AppendText($"{initial_message}\n");
            this.Text = $"Chat with {remote_user}";
            toolStripStatusLabel1.Text = $"Connected to private chat with {remote_user}";
        }

        // Constructor to be used if client is initiating the private chat
        public PrivateChatForm(string remote_user, string local_user, Form1 parent)
        {
            InitializeComponent();
            this.parent = parent;
            this.remote_user = remote_user;
            this.local_user = local_user;
            this.isActive = true;
            this.Text = $"Chat with {remote_user}";
            toolStripStatusLabel1.Text = $"Connected to private chat with {remote_user}";
        }

        // Method called by Form1 (Main client window) to display a recived private message
        public void displayMessage(string message)
        {
            richTextBox1.AppendText($"{message}\n");
        }

        // Method called by Form1 to deactivate the chat if the remote user had disconnected
        public void deactivateChat()
        {
            this.isActive = false;
            textBox1.Enabled = false;
            button1.Enabled = false;
            toolStripStatusLabel1.Text = $"{remote_user} has disconnected!";
        }

        // Method called by Form1 to activate the chat if the remote user had disconnected and has reconnected
        public void activateChat()
        {
            this.isActive = true;
            textBox1.Enabled = true;
            button1.Enabled = true;
            toolStripStatusLabel1.Text = $"{remote_user} connected back!";
        }

        // Method called by Form1 to activate and show the private chat window after it is hidden
        public void showForm()
        {
            this.Show();
            this.isActive = true;
        }

        // When clicked on "X" do not close window but hide it
        private void PrivateChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                this.isActive = false;
            }
        }

        // When enter is pressed to send a message
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                sendMessage();
            }
        }

        // When send button is clicked
        private void button1_Click(object sender, EventArgs e)
        {
            sendMessage();
        }

        // Sending a message
        private void sendMessage()
        {
            //Update the rich text box
            richTextBox1.AppendText($"{local_user}>>>{ textBox1.Text}\n");
            //Encode the message as a private message to a specific user
            string message = $"!pm {remote_user} {local_user} {local_user}>>>{textBox1.Text}";
            //Give the message to Form1's sendMessage() method to be sent to the server
            parent.sendMessage(message);
            //Clear the text box
            textBox1.Clear();
        }
    }
}

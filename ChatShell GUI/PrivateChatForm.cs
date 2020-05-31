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
        private string remote_user, local_user;
        public bool isActive { get; set; }

        private List<string> messages = new List<string>();
        private Form1 parent;

        public PrivateChatForm(string remote_user, string local_user, string initial_message, Form1 parent)
        {
            InitializeComponent();
            this.parent = parent;
            this.remote_user = remote_user;
            this.isActive = true;
            this.local_user = local_user;
            messages.Add(initial_message);
            richTextBox1.Text += Environment.NewLine + initial_message;
            this.Text = $"Chat with {remote_user}";
            toolStripStatusLabel1.Text = $"Connected to private chat with {remote_user}";
        }

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

        public void displayMessage(string message)
        {
            messages.Add(message);
            richTextBox1.Text += Environment.NewLine + message;
        }

        public void deactivateChat()
        {
            this.isActive = false;
            textBox1.Enabled = false;
            button1.Enabled = false;
            toolStripStatusLabel1.Text = $"{remote_user} has disconnected!";
        }

        public void showForm()
        {
            this.Show();
            this.isActive = true;
        }

        private void PrivateChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                this.isActive = false;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Return))
            {
                sendMessage();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            sendMessage();
        }

        private void sendMessage()
        {
            richTextBox1.Text += $"{Environment.NewLine}{ remote_user}>>>{ textBox1.Text}";
            string message = $"!pm {remote_user} {local_user} {remote_user}>>>{textBox1.Text}";
            parent.sendMessage(message);
            textBox1.Clear();
        }
    }
}

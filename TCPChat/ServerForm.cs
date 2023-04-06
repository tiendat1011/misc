using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace TCPChat
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            InitializeComponent();
        }
        TcpListener server;
        Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();

        void StartServer()
        {
            server = new TcpListener(IPAddress.Parse(tbIP.Text), Int32.Parse(tbPort.Text));
            server.Start();
            rtbMessage.Text += ("Server đang lắng nghe\n");
            Task.Run(() =>
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    StreamReader reader = new StreamReader(stream);
                    string name = reader.ReadToEnd();
                    Thread thread = new Thread(() => UpdateClientList());
                    clients.Add(name, client);
                    DateTime now = DateTime.Now;
                    rtbMessage.Text += ($"({now.ToString()}) {name} đã kết nối\n");
                    SendAllClient(name + " đã kết nối ");
                    Thread thread1 = new Thread(() => HandleClient(name) );
                    thread1.Start();
                }
            });
        }
        void HandleClient(string name)
        {
            DateTime now = DateTime.Now;
            TcpClient client = clients[name];
            while (true)
            {
                try
                {
                    string msg = ReceiveClient(client);
                    rtbMessage.Text += ($"({now.ToString()}) {name}: {msg}\n");
                    SendAllClient(name + " : " + msg);
                }
                catch
                {
                    clients.Remove(name);
                    SendAllClient(name + " đã ngắt kết nối");
                }    
            }
        }
        string ReceiveClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytes = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        void SendAllClient(string msg)
        {
            DateTime now = DateTime.Now;
            TcpClient Client;
            byte[] buffer = Encoding.UTF8.GetBytes($"({now.ToString()}) {msg}");
            foreach(var key in clients.Keys)
            {
                Client = clients[key];
                if (Client.Connected)
                {
                    NetworkStream stream = Client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }    
            }

        }
        void UpdateClientList()
        {
            List<string> names = new List<string>(clients.Keys);
            dgvClientList.DataSource = names;
            dgvClientList.Columns[0].HeaderText = "Name";
        }
        private void btnCreate_Click(object sender, EventArgs e)
        {
            btnCreate.Enabled = false;
            StartServer();
        }
        string strg = string.Empty;
        private void dgvClientList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            strg = dgvClientList.CurrentCell.ToString();
        }
        void RemoveClient()
        {
            TcpClient client;
            if (clients.TryGetValue(strg, out client))
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = Encoding.UTF8.GetBytes("Bạn đã bi ngắt kết nối khỏi phòng chat");
                stream.Write(buffer, 0, buffer.Length);
                stream.Close();
                client.Close();
                clients.Remove(strg);
                SendAllClient(strg + " đã bị ngắt kết nối");
            }
            else
            {
                MessageBox.Show("Client " + strg + " not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            RemoveClient();
        }
    }
}

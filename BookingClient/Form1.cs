// BookingClient/Form1.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BookingClient
{
    public partial class Form1 : Form
    {
        private TextBox _txtClientId = null!;
        private TextBox _txtServerIp = null!;
        private ComboBox _cbRoom = null!;
        private ComboBox _cbSlot = null!;
        private Button _btnConnect = null!;
        private Button _btnRequest = null!;
        private Button _btnRelease = null!;
        private TextBox _txtLog = null!;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _connected = false;

        public Form1()
        {
            InitializeComponent();
            SetupUi();
        }

        private void SetupUi()
        {
            this.Text = "Client - Room Booking";
            this.Width = 600;
            this.Height = 400;

            Label lblId = new Label { Text = "Client ID:", Left = 10, Top = 10, Width = 70 };
            this.Controls.Add(lblId);
            _txtClientId = new TextBox { Left = 90, Top = 8, Width = 100, Text = "C1" };
            this.Controls.Add(_txtClientId);

            Label lblIp = new Label { Text = "Server IP:", Left = 210, Top = 10, Width = 70 };
            this.Controls.Add(lblIp);
            _txtServerIp = new TextBox { Left = 290, Top = 8, Width = 100, Text = "127.0.0.1" };
            this.Controls.Add(_txtServerIp);

            _btnConnect = new Button { Text = "Connect", Left = 410, Top = 6, Width = 80 };
            _btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(_btnConnect);

            Label lblRoom = new Label { Text = "Room:", Left = 10, Top = 40, Width = 50 };
            this.Controls.Add(lblRoom);
            _cbRoom = new ComboBox { Left = 70, Top = 38, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbRoom.Items.AddRange(new object[] { "A101", "A102", "B201" });
            _cbRoom.SelectedIndex = 0;
            this.Controls.Add(_cbRoom);

            Label lblSlot = new Label { Text = "Slot:", Left = 170, Top = 40, Width = 40 };
            this.Controls.Add(lblSlot);
            _cbSlot = new ComboBox { Left = 220, Top = 38, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbSlot.Items.AddRange(new object[] { "S1", "S2", "S3" });
            _cbSlot.SelectedIndex = 0;
            this.Controls.Add(_cbSlot);

            _btnRequest = new Button { Text = "REQUEST", Left = 320, Top = 36, Width = 80 };
            _btnRequest.Click += BtnRequest_Click;
            this.Controls.Add(_btnRequest);

            _btnRelease = new Button { Text = "RELEASE", Left = 410, Top = 36, Width = 80 };
            _btnRelease.Click += BtnRelease_Click;
            this.Controls.Add(_btnRelease);

            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Left = 10,
                Top = 70,
                Width = 560,
                Height = 280
            };
            this.Controls.Add(_txtLog);
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_connected) return;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_txtServerIp.Text.Trim(), 5000);
                _stream = _client.GetStream();
                _connected = true;
                Log("[CLIENT] Connected to server");

                // đọc data từ server
                _ = Task.Run(ReadLoopAsync);
            }
            catch (Exception ex)
            {
                Log("[ERROR] " + ex.Message);
            }
        }

        private async Task ReadLoopAsync()
        {
            if (_stream == null) return;

            var buffer = new byte[4096];
            var sb = new StringBuilder();

            try
            {
                while (true)
                {
                    int bytes = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytes == 0) break;

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytes));

                    while (true)
                    {
                        var text = sb.ToString();
                        var idx = text.IndexOf('\n');
                        if (idx < 0) break;

                        var line = text[..idx].Trim();
                        sb.Remove(0, idx + 1);

                        if (line.Length > 0)
                        {
                            HandleServerMessage(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("[ERROR] ReadLoop: " + ex.Message);
            }
            finally
            {
                Log("[CLIENT] Disconnected from server");
                _connected = false;
                this.Text = "Client - Disconnected";
            }
        }

        private void HandleServerMessage(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(HandleServerMessage), msg);
                return;
            }

            Log("[SERVER] " + msg);

            var parts = msg.Split('|');

            switch (parts[0])
            {
                case "GRANT":
                    // GRANT|room|slot
                    if (parts.Length >= 3)
                    {
                        this.Text = $"Client - GRANTED {parts[1]}-{parts[2]}";
                    }
                    break;

                case "QUEUED":
                    // QUEUED|room|slot|pos
                    if (parts.Length >= 4)
                    {
                        var posText = parts[3];
                        this.Text = $"Client - QUEUED pos {posText}";
                    }
                    break;

                case "INFO":
                    if (parts.Length >= 2)
                    {
                        var infoType = parts[1];

                        if (infoType == "ERROR")
                        {
                            // INFO|ERROR|message...
                            this.Text = "Client - ERROR";
                        }
                        else if (infoType == "ALREADY_HOLDER" && parts.Length >= 4)
                        {
                            // INFO|ALREADY_HOLDER|room|slot
                            this.Text = $"Client - GRANTED {parts[2]}-{parts[3]}";
                        }
                        else if (infoType == "ALREADY_QUEUED" && parts.Length >= 5)
                        {
                            // INFO|ALREADY_QUEUED|room|slot|pos
                            var posText = parts[4];
                            this.Text = $"Client - QUEUED pos {posText}";
                        }
                        else if (infoType == "CANCELLED" && parts.Length >= 4)
                        {
                            // INFO|CANCELLED|room|slot
                            this.Text = "Client - IDLE (cancelled)";
                        }
                        else if (infoType == "RELEASED" && parts.Length >= 4)
                        {
                            // INFO|RELEASED|room|slot
                            this.Text = "Client - IDLE (released)";
                        }
                    }
                    break;
            }
        }

        private async void BtnRequest_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null) { Log("Not connected"); return; }

            var clientId = _txtClientId.Text.Trim();
            var room = _cbRoom.SelectedItem?.ToString() ?? "A101";
            var slot = _cbSlot.SelectedItem?.ToString() ?? "S1";

            var msg = $"REQUEST|{clientId}|{room}|{slot}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);
            Log("[CLIENT] Sent " + msg.Trim());
        }

        private async void BtnRelease_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null) { Log("Not connected"); return; }

            var clientId = _txtClientId.Text.Trim();
            var room = _cbRoom.SelectedItem?.ToString() ?? "A101";
            var slot = _cbSlot.SelectedItem?.ToString() ?? "S1";

            var msg = $"RELEASE|{clientId}|{room}|{slot}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);
            Log("[CLIENT] Sent " + msg.Trim());
        }

        private void Log(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), text);
                return;
            }

            _txtLog.AppendText(text + Environment.NewLine);
        }
    }
}

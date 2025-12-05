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
        private TextBox _txtUserId = null!;
        private TextBox _txtPassword = null!;
        private ComboBox _cbRoom = null!;
        private ComboBox _cbSlot = null!;
        private Button _btnConnect = null!;
        private Button _btnRequest = null!;
        private Button _btnRelease = null!;
        private TextBox _txtLog = null!;
        private Label _lblStatus = null!;
        private ComboBox _cbSlotStart = null!;
        private ComboBox _cbSlotEnd = null!;
        private Button _btnRequestRange = null!;
        private Button _btnReleaseRange = null!;   // NEW

        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _connected = false;
        private string? _currentUserId;
        private string? _currentUserType;
        // private TextBox _txtForceUserId = null!;
        // private Button _btnForceGrant = null!;
        public Form1()
        {
            InitializeComponent();
            SetupUi();
        }

        private void SetupUi()
        {
            this.Text = "Client - Room Booking";
            this.Width = 700;
            this.Height = 520;
            this.StartPosition = FormStartPosition.CenterScreen;

            // ===== Group 1: connection + login =====
            var grpConn = new GroupBox
            {
                Text = "Connection & Login",
                Left = 10,
                Top = 10,
                Width = 660,
                Height = 80
            };
            this.Controls.Add(grpConn);

            var lblIp = new Label { Text = "Server IP:", Left = 10, Top = 30, Width = 70 };
            _txtServerIp = new TextBox { Left = 80, Top = 27, Width = 110, Text = "127.0.0.1" };
            _btnConnect = new Button { Text = "Connect", Left = 200, Top = 25, Width = 80 };
            _btnConnect.Click += BtnConnect_Click;

            var lblUser = new Label { Text = "User ID:", Left = 300, Top = 30, Width = 60 };
            _txtUserId = new TextBox { Left = 360, Top = 27, Width = 80, Text = "sv001" };

            var lblPwd = new Label { Text = "Password:", Left = 450, Top = 30, Width = 70 };
            _txtPassword = new TextBox
            {
                Left = 520,
                Top = 27,
                Width = 80,
                UseSystemPasswordChar = true,
                Text = "sv123"
            };
            var btnLogin = new Button { Text = "Login", Left = 605, Top = 25, Width = 50 };
            btnLogin.Click += BtnLogin_Click;

            grpConn.Controls.AddRange(new Control[]
            {
        lblIp, _txtServerIp, _btnConnect,
        lblUser, _txtUserId,
        lblPwd, _txtPassword,
        btnLogin
            });

            // ===== Group 2: booking request =====
            var grpRequest = new GroupBox
            {
                Text = "Booking Request",
                Left = 10,
                Top = 100,
                Width = 660,
                Height = 100    // tăng lên cho đủ chỗ
            };
            this.Controls.Add(grpRequest);

            var lblRoom = new Label { Text = "Room:", Left = 10, Top = 30, Width = 50 };
            _cbRoom = new ComboBox
            {
                Left = 65,
                Top = 27,
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbRoom.Items.AddRange(new object[]
            {
    "A08","A16","A24","A32",
    "A21","A22","A23",
    "A24-A25","A31","A32-A33","A34-A35"
            });
            _cbRoom.SelectedIndex = 0;

            var lblSlot = new Label { Text = "Ca:", Left = 220, Top = 30, Width = 30 };
            _cbSlot = new ComboBox
            {
                Left = 250,
                Top = 27,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 14; i++)
            {
                _cbSlot.Items.Add(i.ToString());
            }
            _cbSlot.SelectedIndex = 0;

            _btnRequest = new Button { Text = "REQUEST", Left = 330, Top = 25, Width = 100, Enabled = false };
            _btnRequest.Click += BtnRequest_Click;

            _btnRelease = new Button { Text = "RELEASE", Left = 440, Top = 25, Width = 100, Enabled = false };
            _btnRelease.Click += BtnRelease_Click;

            // NEW: range controls
            var lblRange = new Label { Text = "Range:", Left = 10, Top = 65, Width = 50 };
            _cbSlotStart = new ComboBox
            {
                Left = 65,
                Top = 62,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbSlotEnd = new ComboBox
            {
                Left = 135,
                Top = 62,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 14; i++)
            {
                _cbSlotStart.Items.Add(i.ToString());
                _cbSlotEnd.Items.Add(i.ToString());
            }
            _cbSlotStart.SelectedIndex = 0;
            _cbSlotEnd.SelectedIndex = 0;

            _btnRequestRange = new Button
            {
                Text = "REQUEST RANGE",
                Left = 220,
                Top = 60,
                Width = 150,
                Enabled = false       // sẽ bật sau khi login OK
            };
            _btnRequestRange.Click += BtnRequestRange_Click;

            _btnReleaseRange = new Button
            {
                Text = "RELEASE RANGE",
                Left = 380,          // đặt ngay bên phải REQUEST RANGE
                Top = 60,
                Width = 150,
                Enabled = false
            };
            _btnReleaseRange.Click += BtnReleaseRange_Click;

            grpRequest.Controls.AddRange(new Control[]
            {
    lblRoom, _cbRoom,
    lblSlot, _cbSlot,
    _btnRequest, _btnRelease,
    lblRange, _cbSlotStart, _cbSlotEnd, _btnRequestRange,_btnReleaseRange
            });


            //     // ===== Group 3: admin tools =====
            //     var grpAdmin = new GroupBox
            //     {
            //         Text = "Admin tools",
            //         Left = 10,
            //         Top = 180,
            //         Width = 660,
            //         Height = 60
            //     };
            //     this.Controls.Add(grpAdmin);

            //     var lblForceUser = new Label { Text = "Force user:", Left = 10, Top = 28, Width = 70 };
            //     _txtForceUserId = new TextBox { Left = 85, Top = 25, Width = 120, Text = "sv001" };
            //     _btnForceGrant = new Button
            //     {
            //         Text = "Force GRANT (Admin)",
            //         Left = 215,
            //         Top = 23,
            //         Width = 160,
            //         Enabled = false
            //     };
            //     _btnForceGrant.Click += BtnForceGrant_Click;

            //     grpAdmin.Controls.AddRange(new Control[]
            //     {
            // lblForceUser, _txtForceUserId, _btnForceGrant
            //     });

            // ===== Status bar =====
            _lblStatus = new Label
            {
                Left = 10,
                Top = 250,
                Width = 660,
                Height = 25,
                Text = "Status: IDLE (not requested)",
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(_lblStatus);

            // ===== Log =====
            var grpLog = new GroupBox
            {
                Text = "Client log",
                Left = 10,
                Top = 285,
                Width = 660,
                Height = 190
            };
            this.Controls.Add(grpLog);

            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };
            grpLog.Controls.Add(_txtLog);
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
                SetStatus("CONNECTED", "Connected to server", System.Drawing.Color.LightGreen);

                // đọc data từ server
                _ = Task.Run(ReadLoopAsync);
            }
            catch (Exception ex)
            {
                Log("[ERROR] " + ex.Message);
                SetStatus("ERROR", ex.Message, System.Drawing.Color.LightCoral);
            }
        }
        // private async void BtnForceGrant_Click(object? sender, EventArgs e)
        // {
        //     if (!_connected || _stream == null)
        //     {
        //         SetStatus("ERROR", "Not connected", System.Drawing.Color.LightCoral);
        //         return;
        //     }

        //     if (string.IsNullOrEmpty(_currentUserId) ||
        //         string.IsNullOrEmpty(_currentUserType) ||
        //         (_currentUserType != "Staff" && _currentUserType != "Admin"))
        //     {
        //         SetStatus("ERROR", "Chỉ Admin/Staff mới dùng FORCE_GRANT", System.Drawing.Color.LightCoral);
        //         return;
        //     }

        //     var targetUserId = _txtForceUserId.Text.Trim();
        //     if (string.IsNullOrWhiteSpace(targetUserId))
        //     {
        //         SetStatus("ERROR", "Target UserId không được trống", System.Drawing.Color.LightCoral);
        //         return;
        //     }

        //     var room = _cbRoom.SelectedItem?.ToString() ?? "A08";
        //     var slotNumber = _cbSlot.SelectedItem?.ToString() ?? "1";
        //     var slotId = "S" + slotNumber;

        //     var msg = $"FORCE_GRANT|{_currentUserId}|{targetUserId}|{room}|{slotId}\n";
        //     var data = Encoding.UTF8.GetBytes(msg);
        //     await _stream.WriteAsync(data, 0, data.Length);

        //     Log("[CLIENT] Sent " + msg.Trim());
        //     SetStatus("WAITING", $"FORCE_GRANT sent for {targetUserId} {room}-{slotId}", System.Drawing.Color.LightYellow);
        // }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null)
            {
                SetStatus("ERROR", "Not connected to server", System.Drawing.Color.LightCoral);
                return;
            }

            var userId = _txtUserId.Text.Trim();
            var password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrEmpty(password))
            {
                SetStatus("ERROR", "UserId / Password không được để trống", System.Drawing.Color.LightCoral);
                return;
            }

            var msg = $"LOGIN|{userId}|{password}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);
            Log("[CLIENT] Sent " + msg.Trim());
            SetStatus("LOGINING", "Đang gửi yêu cầu đăng nhập...", System.Drawing.Color.LightYellow);

            // chờ server trả INFO|LOGIN_OK hoặc LOGIN_FAIL trong ReadLoopAsync
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
                SetStatus("DISCONNECTED", "Disconnected from server", System.Drawing.Color.LightGray);
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
                        string room = parts[1];
                        string slot = parts[2];
                        this.Text = $"Client - GRANTED {room}-{slot}";
                        SetStatus("GRANTED", $"You are GRANTED {room}-{slot}", System.Drawing.Color.LightGreen);
                    }
                    break;

                case "GRANT_RANGE":
                    // GRANT_RANGE|room|slotStart|slotEnd
                    if (parts.Length >= 4)
                    {
                        var room = parts[1];
                        var sStart = parts[2];
                        var sEnd = parts[3];

                        this.Text = $"Client - GRANTED RANGE {room} {sStart}-{sEnd}";
                        SetStatus("GRANTED", $"Granted {room} {sStart}-{sEnd}", System.Drawing.Color.LightGreen);
                    }
                    break;

                case "QUEUED":
                    // QUEUED|room|slot|pos
                    if (parts.Length >= 4)
                    {
                        var room = parts[1];
                        var slot = parts[2];
                        var posText = parts[3];
                        this.Text = $"Client - QUEUED pos {posText}";
                        SetStatus("QUEUED", $"Waiting for {room}-{slot} at position {posText}", System.Drawing.Color.Khaki);
                    }
                    break;

                case "INFO":
                    if (parts.Length >= 2)
                    {
                        var infoType = parts[1];
                        if (infoType == "LOGIN_OK" && parts.Length >= 3)
                        {
                            var userType = parts[2];
                            _currentUserId = _txtUserId.Text.Trim();
                            _currentUserType = userType;

                            this.Text = $"Client - Logged in as {_currentUserId} ({userType})";
                            SetStatus("LOGIN_OK", $"Logged in as {_currentUserId} ({userType})", System.Drawing.Color.LightGreen);

                            _btnRequest.Enabled = true;
                            _btnRelease.Enabled = true;
                            _btnRequestRange.Enabled = true;   // NEW
                            _btnReleaseRange.Enabled = true;

                            // CHỈ ADMIN / STAFF mới bật FORCE_GRANT
                            // if (userType == "Staff" || userType == "Admin")
                            // {
                            //     _btnForceGrant.Enabled = true;
                            // }
                            // else
                            // {
                            //     _btnForceGrant.Enabled = false;
                            // }
                        }

                        else if (infoType == "LOGIN_FAIL" && parts.Length >= 3)
                        {
                            var reason = parts[2];
                            _currentUserId = null;
                            _currentUserType = null;

                            this.Text = "Client - Login failed";
                            SetStatus("LOGIN_FAIL", reason, System.Drawing.Color.LightCoral);

                            _btnRequest.Enabled = false;
                            _btnRelease.Enabled = false;
                            _btnRequestRange.Enabled = false;
                            _btnReleaseRange.Enabled = false;
                            // _btnForceGrant.Enabled = false;
                        }
                        // else if (infoType == "ERROR")
                        // {
                        //     // INFO|ERROR|message...
                        //     string msgErr = parts.Length >= 3 ? parts[2] : "Unknown error";
                        //     this.Text = "Client - ERROR";
                        //     SetStatus("ERROR", msgErr, System.Drawing.Color.LightCoral);
                        // }
                        else if (infoType == "ALREADY_HOLDER" && parts.Length >= 4)
                        {
                            // INFO|ALREADY_HOLDER|room|slot
                            var room = parts[2];
                            var slot = parts[3];
                            this.Text = $"Client - GRANTED {room}-{slot}";
                            SetStatus("GRANTED", $"Already holder of {room}-{slot}", System.Drawing.Color.LightGreen);
                        }
                        else if (infoType == "ALREADY_QUEUED" && parts.Length >= 5)
                        {
                            // INFO|ALREADY_QUEUED|room|slot|pos
                            var room = parts[2];
                            var slot = parts[3];
                            var posText = parts[4];
                            this.Text = $"Client - QUEUED pos {posText}";
                            SetStatus("QUEUED", $"Already queued for {room}-{slot} at position {posText}", System.Drawing.Color.Khaki);
                        }
                        else if (infoType == "CANCELLED" && parts.Length >= 4)
                        {
                            // INFO|CANCELLED|room|slot
                            this.Text = "Client - IDLE (cancelled)";
                            SetStatus("IDLE", "Request cancelled", System.Drawing.Color.LightGray);
                        }
                        else if (infoType == "RELEASEED" && parts.Length >= 4)
                        {
                            // typo safety – but trong server là RELEASED
                            this.Text = "Client - IDLE (released)";
                            SetStatus("IDLE", "Released", System.Drawing.Color.LightGray);
                        }
                        else if (infoType == "RELEASED" && parts.Length >= 4)
                        {
                            // INFO|RELEASED|room|slot
                            this.Text = "Client - IDLE (released)";
                            SetStatus("IDLE", "Released", System.Drawing.Color.LightGray);
                        }
                        // else if (infoType == "FORCE_GRANTED" && parts.Length >= 5)
                        // {
                        //     var targetUser = parts[2];
                        //     var room = parts[3];
                        //     var slot = parts[4];

                        //     this.Text = $"Client - FORCE_GRANTED {targetUser} {room}-{slot}";
                        //     SetStatus("FORCE_GRANTED",
                        //         $"Admin granted {room}-{slot} to {targetUser}",
                        //         System.Drawing.Color.LightGreen);
                        // }

                        else if (infoType == "RANGE_RELEASED" && parts.Length >= 5)
                        {
                            var room = parts[2];
                            var sStart = parts[3];
                            var sEnd = parts[4];

                            this.Text = "Client - IDLE (range released)";
                            SetStatus("IDLE", $"Released range {room} {sStart}-{sEnd}",
                                System.Drawing.Color.LightGray);
                        }
                        else if (infoType == "ERROR" && parts.Length >= 3)
                        {
                            string msgErr = parts[2];

                            if (msgErr == "SLOT_LOCKED_FOR_EVENT")
                            {
                                this.Text = "Client - Slot locked for event";
                                SetStatus("LOCKED_FOR_EVENT",
                                    "Phòng/ca này đã được khóa cho sự kiện.",
                                    System.Drawing.Color.LightBlue);
                            }
                            else
                            {
                                this.Text = "Client - ERROR";
                                SetStatus("ERROR", msgErr, System.Drawing.Color.LightCoral);
                            }
                        }


                    }
                    break;
            }
        }

        private async void BtnRequest_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null)
            {
                Log("Not connected");
                SetStatus("ERROR", "Not connected", System.Drawing.Color.LightCoral);
                return;
            }

            if (string.IsNullOrEmpty(_currentUserId))
            {
                SetStatus("ERROR", "Bạn cần đăng nhập trước khi REQUEST", System.Drawing.Color.LightCoral);
                return;
            }

            var clientId = _currentUserId;  // user đã login
            var room = _cbRoom.SelectedItem?.ToString() ?? "A08";
            var slotNumber = _cbSlot.SelectedItem?.ToString() ?? "1";
            var slotId = "S" + slotNumber;

            var msg = $"REQUEST|{clientId}|{room}|{slotId}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);
            Log("[CLIENT] Sent " + msg.Trim());
            SetStatus("WAITING", $"REQUEST sent for {room}-{slotId}", System.Drawing.Color.LightYellow);
        }
        private async void BtnRequestRange_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null)
            {
                Log("Not connected");
                SetStatus("ERROR", "Not connected", System.Drawing.Color.LightCoral);
                return;
            }

            if (string.IsNullOrEmpty(_currentUserId))
            {
                SetStatus("ERROR", "Bạn cần đăng nhập trước khi REQUEST_RANGE", System.Drawing.Color.LightCoral);
                return;
            }

            var clientId = _currentUserId;
            var room = _cbRoom.SelectedItem?.ToString() ?? "A08";

            var slotStartNum = _cbSlotStart.SelectedItem?.ToString() ?? "1";
            var slotEndNum = _cbSlotEnd.SelectedItem?.ToString() ?? "1";

            if (!int.TryParse(slotStartNum, out var sStart) ||
                !int.TryParse(slotEndNum, out var sEnd))
            {
                SetStatus("ERROR", "Slot start/end không hợp lệ", System.Drawing.Color.LightCoral);
                return;
            }

            if (sStart > sEnd)
            {
                SetStatus("ERROR", "SlotStart phải <= SlotEnd", System.Drawing.Color.LightCoral);
                return;
            }

            var slotStartId = "S" + sStart;
            var slotEndId = "S" + sEnd;

            var msg = $"REQUEST_RANGE|{clientId}|{room}|{slotStartId}|{slotEndId}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);

            Log("[CLIENT] Sent " + msg.Trim());
            SetStatus("WAITING", $"REQUEST_RANGE sent for {room} {slotStartId}-{slotEndId}", System.Drawing.Color.LightYellow);

        }
        private async void BtnReleaseRange_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null)
            {
                Log("Not connected");
                SetStatus("ERROR", "Not connected", System.Drawing.Color.LightCoral);
                return;
            }

            if (string.IsNullOrEmpty(_currentUserId))
            {
                SetStatus("ERROR", "Bạn cần đăng nhập trước khi RELEASE_RANGE", System.Drawing.Color.LightCoral);
                return;
            }

            var clientId = _currentUserId;
            var room = _cbRoom.SelectedItem?.ToString() ?? "A08";

            var slotStartNum = _cbSlotStart.SelectedItem?.ToString() ?? "1";
            var slotEndNum = _cbSlotEnd.SelectedItem?.ToString() ?? "1";

            if (!int.TryParse(slotStartNum, out var sStart) ||
                !int.TryParse(slotEndNum, out var sEnd))
            {
                SetStatus("ERROR", "Slot start/end không hợp lệ", System.Drawing.Color.LightCoral);
                return;
            }

            if (sStart > sEnd)
            {
                SetStatus("ERROR", "SlotStart phải <= SlotEnd", System.Drawing.Color.LightCoral);
                return;
            }

            var slotStartId = "S" + sStart;
            var slotEndId = "S" + sEnd;

            var msg = $"RELEASE_RANGE|{clientId}|{room}|{slotStartId}|{slotEndId}\n";
            var data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length);

            Log("[CLIENT] Sent " + msg.Trim());
            SetStatus("WAITING",
                $"RELEASE_RANGE sent for {room} {slotStartId}-{slotEndId}",
                System.Drawing.Color.LightYellow);
        }

        private async void BtnRelease_Click(object? sender, EventArgs e)
        {
            if (!_connected || _stream == null)
            {
                Log("Not connected");
                SetStatus("ERROR", "Not connected", System.Drawing.Color.LightCoral);
                return;
            }

            if (string.IsNullOrEmpty(_currentUserId))
            {
                SetStatus("ERROR", "Bạn cần đăng nhập trước khi RELEASE", System.Drawing.Color.LightCoral);
                return;
            }

            var clientId = _currentUserId;  // user đã login
            var room = _cbRoom.SelectedItem?.ToString() ?? "A08";
            var slotNumber = _cbSlot.SelectedItem?.ToString() ?? "1";
            var slotId = "S" + slotNumber;

            var msg = $"RELEASE|{clientId}|{room}|{slotId}\n";
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
            _txtLog.SelectionStart = _txtLog.Text.Length;
            _txtLog.ScrollToCaret();
        }

        private void SetStatus(string shortStatus, string detail, System.Drawing.Color backColor)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, System.Drawing.Color>(SetStatus), shortStatus, detail, backColor);
                return;
            }

            _lblStatus.Text = $"Status: {shortStatus} - {detail}";
            _lblStatus.BackColor = backColor;
        }
    }
}

// BookingServer/Form1.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace BookingServer;

public partial class Form1 : Form
{
    private Button _btnStart = null!;
    private DataGridView _gridSlots = null!;
    private Label _lblQueueTitle = null!;
    private ListBox _lstQueue = null!;
    private TextBox _txtLog = null!;
    private DateTimePicker _dtDate = null!;   // CHỌN NGÀY

    private TcpListener? _listener;
    private bool _running = false;
    private readonly ServerState _state = new();
    private Button _btnViewBookings = null!;
    private readonly WinFormsTimer _noShowTimer;
    private GroupBox _grpCheckin = null!;
    private Label _lblSelectedSlot = null!;
    private Label _lblBookingUser = null!;
    private Label _lblBookingStatus = null!;
    private Button _btnCheckIn = null!;
    private Button _btnComplete = null!;
    private TextBox _txtEventNote = null!;
    private Button _btnLockEvent = null!;
    private Button _btnUnlockEvent = null!;
    private TextBox _txtForceUserId = null!;
private Button _btnForceGrant = null!;
private Button _btnForceRelease = null!;
    public Form1()
    {
        InitializeComponent();
        SetupUi();

        _noShowTimer = new WinFormsTimer();
        _noShowTimer.Interval = 60_000; // mỗi 60s quét NO_SHOW
        _noShowTimer.Tick += NoShowTimer_Tick;
        _noShowTimer.Start();

        // mặc định dùng ngày hôm nay
        _state.SetCurrentDate(DateTime.Today, new UiLogger(this));
        RefreshSlotsSafe();
        // _gridSlots.SelectionChanged += GridSlots_SelectionChanged;

    }

    private void SetupUi()
    {
        // ===== Form =====
        this.Text = "Server - Centralized Mutual Exclusion";
        this.Width = 1100;
        this.Height = 700;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ===== Top bar: start server + date =====
        _btnStart = new Button
        {
            Text = "Start Server",
            Left = 10,
            Top = 10,
            Width = 120,
            Height = 30
        };
        _btnStart.Click += BtnStart_Click;
        this.Controls.Add(_btnStart);

        _dtDate = new DateTimePicker
        {
            Left = 150,
            Top = 10,
            Width = 200,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };
        _dtDate.ValueChanged += DtDate_ValueChanged;
        this.Controls.Add(_dtDate);

        // ===== Left: slot overview =====
        var grpSlots = new GroupBox
        {
            Text = "Slot overview (current date)",
            Left = 10,
            Top = 45,
            Width = 430,
            Height = 540
        };
        this.Controls.Add(grpSlots);

        _gridSlots = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _gridSlots.SelectionChanged += GridSlots_SelectionChanged;
        grpSlots.Controls.Add(_gridSlots);

        _btnViewBookings = new Button
        {
            Text = "View Bookings",
            Left = 10,
            Top = 595,
            Width = 120,
            Height = 30
        };
        _btnViewBookings.Click += BtnViewBookings_Click;
        this.Controls.Add(_btnViewBookings);

        // ===== Right top: queue of selected slot =====
        var grpQueue = new GroupBox
        {
            Left = 450,
            Top = 45,
            Width = 620,
            Height = 140
        };
        this.Controls.Add(grpQueue);

        _lblQueueTitle = new Label
        {
            Left = 10,
            Top = 20,
            Width = 590,
            Height = 20,
            Text = "Queue for: (select a room/slot)"
        };
        grpQueue.Controls.Add(_lblQueueTitle);

        _lstQueue = new ListBox
        {
            Left = 10,
            Top = 45,
            Width = 590,
            Height = 75
        };
        grpQueue.Controls.Add(_lstQueue);

        // ===== Right middle: user management =====
        var grpUser = new GroupBox
        {
            Text = "User Management (Admin on Server)",
            Left = 450,
            Top = 400,        // dời xuống dưới Check-in
            Width = 620,
            Height = 130
        };
        this.Controls.Add(grpUser);

        var lblUid = new Label { Left = 15, Top = 30, Width = 60, Text = "UserId:" };
        var txtUid = new TextBox { Left = 80, Top = 26, Width = 120 };

        var lblName = new Label { Left = 220, Top = 30, Width = 60, Text = "Name:" };
        var txtName = new TextBox { Left = 285, Top = 26, Width = 200 };

        var lblType = new Label { Left = 15, Top = 65, Width = 60, Text = "Type:" };
        var cbType = new ComboBox
        {
            Left = 80,
            Top = 61,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbType.Items.AddRange(new object[] { "Student", "Lecturer", "Staff" });
        cbType.SelectedIndex = 0;

        var lblPwd = new Label { Left = 220, Top = 65, Width = 70, Text = "Password:" };
        var txtPwd = new TextBox { Left = 285, Top = 61, Width = 200 };

        var btnCreateUser = new Button
        {
            Left = 500,
            Top = 58,
            Width = 100,
            Text = "Create / Add"
        };
        btnCreateUser.Click += (s, e) =>
        {
            var user = new UserInfo
            {
                UserId = txtUid.Text.Trim(),
                FullName = txtName.Text.Trim(),
                UserType = cbType.SelectedItem?.ToString() ?? "Student"
            };

            var ok = _state.CreateUser(user, txtPwd.Text.Trim(), out var err);
            if (!ok)
            {
                Log("[USER MGMT] " + err);
                MessageBox.Show(err, "User Management",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Log($"[USER MGMT] User {user.UserId} created ({user.UserType})");
                MessageBox.Show("User created", "User Management");
            }
        };

        grpUser.Controls.AddRange(new Control[]
        {
        lblUid, txtUid, lblName, txtName,
        lblType, cbType, lblPwd, txtPwd, btnCreateUser
        });

        // ===== Right bottom: server log =====
        var grpLog = new GroupBox
        {
            Text = "Server log",
            Left = 450,
            Top = 540,        // dời xuống tí cho đủ chỗ
            Width = 620,
            Height = 150
        };
        this.Controls.Add(grpLog);

        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill
        };
        grpLog.Controls.Add(_txtLog);

        // ===== Right middle: Check-in / Complete =====
        _grpCheckin = new GroupBox
        {
            Text = "Check-in / Complete (Admin)",
            Left = 450,
            Top = 195,        // dưới Queue
            Width = 620,
            Height = 350      // cao hơn để đủ 3 label + 2 nút
        };
        this.Controls.Add(_grpCheckin);

        // Label: Slot đang chọn
        _lblSelectedSlot = new Label
        {
            Left = 10,
            Top = 20,
            Width = 600,
            Text = "Slot: (chưa chọn)"
        };
        _grpCheckin.Controls.Add(_lblSelectedSlot);

        // Label: User
        _lblBookingUser = new Label
        {
            Left = 10,
            Top = 40,
            Width = 600,
            Text = "User: -"
        };
        _grpCheckin.Controls.Add(_lblBookingUser);

        // Label: Status
        _lblBookingStatus = new Label
        {
            Left = 10,
            Top = 60,
            Width = 600,
            Text = "Status: -"
        };
        _grpCheckin.Controls.Add(_lblBookingStatus);

        // Nút CHECK-IN (xuống dòng dưới Status)
        _btnCheckIn = new Button
        {
            Left = 10,
            Top = 80,
            Width = 150,
            Text = "CHECK-IN",
            Enabled = false
        };
        _btnCheckIn.Click += BtnCheckIn_Click;
        _grpCheckin.Controls.Add(_btnCheckIn);

        // Nút COMPLETE & RELEASE (cùng hàng với CHECK-IN)
        _btnComplete = new Button
        {
            Left = 170,
            Top = 80,
            Width = 150,
            Text = "Complete & Release",
            Enabled = false
        };
        _btnComplete.Click += BtnComplete_Click;
        _grpCheckin.Controls.Add(_btnComplete);

        // ===== Event Lock (trong cùng group Check-in) =====
        var lblEventNote = new Label
        {
            Left = 10,
            Top = 105,
            Width = 80,
            Text = "Event note:"
        };
        _grpCheckin.Controls.Add(lblEventNote);

        _txtEventNote = new TextBox
        {
            Left = 90,
            Top = 105,
            Width = 240
        };
        _grpCheckin.Controls.Add(_txtEventNote);

        // Nút Lock cho event
        _btnLockEvent = new Button
        {
            Left = 340,
            Top = 105,
            Width = 120,
            Text = "Lock for Event"
        };
        _btnLockEvent.Click += BtnLockEvent_Click;
        _grpCheckin.Controls.Add(_btnLockEvent);

        // Nút Unlock event
        _btnUnlockEvent = new Button
        {
            Left = 470,
            Top = 105,
            Width = 120,
            Text = "Unlock Event"
        };
        _btnUnlockEvent.Click += BtnUnlockEvent_Click;
        _grpCheckin.Controls.Add(_btnUnlockEvent);

// ===== Admin override (Force GRANT / RELEASE) =====
var lblForceUser = new Label
{
    Left = 10,
    Top = 140,
    Width = 80,
    Text = "Force user:"
};
_grpCheckin.Controls.Add(lblForceUser);

_txtForceUserId = new TextBox
{
    Left = 90,
    Top = 140,
    Width = 140
};
_grpCheckin.Controls.Add(_txtForceUserId);

_btnForceGrant = new Button
{
    Left = 240,
    Top = 140,
    Width = 120,
    Text = "Force GRANT"
};
_btnForceGrant.Click += BtnForceGrant_Click;
_grpCheckin.Controls.Add(_btnForceGrant);

_btnForceRelease = new Button
{
    Left = 370,
    Top = 140,
    Width = 120,
    Text = "Force RELEASE"
};
_btnForceRelease.Click += BtnForceRelease_Click;
_grpCheckin.Controls.Add(_btnForceRelease);

// tăng chiều cao group một chút cho đủ chỗ
_grpCheckin.Height = 165;

    }

    private void DtDate_ValueChanged(object? sender, EventArgs e)
    {
        _state.SetCurrentDate(_dtDate.Value.Date, new UiLogger(this));
        RefreshSlotsSafe();
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_running) return;

        int port = 5000;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _running = true;
        Log($"[SERVER] Listening on port {port}...");

        RefreshSlotsSafe();

        while (_running)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Log("[SERVER] New client connected");
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private void NoShowTimer_Tick(object? sender, EventArgs e)
    {
        // quét NO_SHOW cho ngày đang chọn trên DateTimePicker
        _state.RunNoShowSweep(DateTime.Now, new UiLogger(this));  // ✅
        RefreshSlotsSafe();
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        using (tcpClient)
        using (var stream = tcpClient.GetStream())
        using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
        {
            string? line;
            string? clientId = null;
            string? currentUserType = null;

            try
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var msg = line.Trim();
                    if (string.IsNullOrEmpty(msg)) continue;

                    Log($"[RECV] {msg}");

                    var parts = msg.Split('|');
                    var cmd = parts[0].ToUpperInvariant();

                    switch (cmd)
                    {
                        case "LOGIN":
                            if (parts.Length != 3)
                            {
                                await SendAsync(stream, "INFO|LOGIN_FAIL|Invalid format\n");
                                break;
                            }
                            {
                                var userId = parts[1];
                                var password = parts[2];

                                var (success, userType, error) = _state.ValidateUserCredentials(userId, password);
                                if (!success || userType == null)
                                {
                                    await SendAsync(stream, $"INFO|LOGIN_FAIL|{error}\n");
                                    Log($"[LOGIN FAIL] {userId} - {error}");
                                }
                                else
                                {
                                    clientId = userId;
                                    currentUserType = userType;
                                    await SendAsync(stream, $"INFO|LOGIN_OK|{userType}\n");
                                    Log($"[LOGIN OK] {userId} ({userType})");
                                }
                            }
                            break;

                        case "REQUEST":
                            if (parts.Length != 4)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid REQUEST format\n");
                                break;
                            }

                            if (clientId == null)
                            {
                                await SendAsync(stream, "INFO|ERROR|NOT_AUTHENTICATED\n");
                                break;
                            }

                            if (!string.Equals(clientId, parts[1], StringComparison.OrdinalIgnoreCase))
                            {
                                await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                break;
                            }

                            _state.HandleRequest(clientId, parts[2], parts[3], stream, new UiLogger(this));
                            RefreshSlotsSafe();
                            break;

                        case "RELEASE":
                            if (parts.Length != 4)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid RELEASE format\n");
                                break;
                            }

                            if (clientId == null)
                            {
                                await SendAsync(stream, "INFO|ERROR|NOT_AUTHENTICATED\n");
                                break;
                            }

                            if (!string.Equals(clientId, parts[1], StringComparison.OrdinalIgnoreCase))
                            {
                                await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                break;
                            }

                            _state.HandleRelease(clientId, parts[2], parts[3], stream, new UiLogger(this));
                            RefreshSlotsSafe();
                            break;

                        case "FORCE_GRANT":
                            // EXPECT: FORCE_GRANT|adminId|targetUserId|roomId|slotId
                            if (parts.Length != 5)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid FORCE_GRANT format\n");
                                break;
                            }

                            if (clientId == null)
                            {
                                await SendAsync(stream, "INFO|ERROR|NOT_AUTHENTICATED\n");
                                break;
                            }

                            var adminId = parts[1];
                            var targetUserId = parts[2];
                            var fgRoomId = parts[3];
                            var fgSlotId = parts[4];

                            // connection này phải đúng adminId đã login
                            if (!string.Equals(clientId, adminId, StringComparison.OrdinalIgnoreCase))
                            {
                                await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                break;
                            }

                            // check quyền admin
                            if (!_state.IsAdmin(adminId))
                            {
                                await SendAsync(stream, "INFO|ERROR|NOT_ADMIN\n");
                                break;
                            }

                            _state.HandleForceGrant(adminId, targetUserId, fgRoomId, fgSlotId, stream, new UiLogger(this));
                            RefreshSlotsSafe();
                            break;
                        case "REQUEST_RANGE":
                            {
                                if (clientId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_AUTHENTICATED\n");
                                    break;
                                }
                                // REQUEST_RANGE|UserId|RoomId|SlotStart|SlotEnd
                                if (parts.Length >= 5 && clientId != null)
                                {
                                    var userIdMsg = parts[1];
                                    var roomId = parts[2];
                                    var slotStart = parts[3];
                                    var slotEnd = parts[4];

                                    // if (!string.Equals(userIdMsg, clientId, StringComparison.OrdinalIgnoreCase))
                                    // {
                                    //     await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    //     break;
                                    // }
                                    // đảm bảo userId trong message khớp clientId đã login
                                    if (!string.Equals(userIdMsg, clientId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                        break;
                                    }

                                    _state.HandleRequestRange(clientId, roomId, slotStart, slotEnd, stream, new UiLogger(this));
                                    RefreshSlotsSafe();
                                }
                                else
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid REQUEST_RANGE\n");
                                }
                                break;
                            }
                        case "RELEASE_RANGE":
                            {
                                if (clientId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_AUTHENTICATED\n");
                                    break;
                                }

                                // RELEASE_RANGE|UserId|RoomId|SlotStart|SlotEnd
                                if (parts.Length >= 5)
                                {
                                    var userIdMsg = parts[1];
                                    var roomId = parts[2];
                                    var slotStart = parts[3];
                                    var slotEnd = parts[4];

                                    if (!string.Equals(userIdMsg, clientId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                        break;
                                    }

                                    _state.HandleReleaseRange(
                                        clientId,
                                        roomId,
                                        slotStart,
                                        slotEnd,
                                        stream,
                                        new UiLogger(this));

                                    RefreshSlotsSafe();
                                }
                                else
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid RELEASE_RANGE\n");
                                }
                                break;
                            }

                        default:
                            await SendAsync(stream, "INFO|ERROR|Unknown command\n");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Client {clientId ?? "UNKNOWN"}: {ex.Message}");
            }
            finally
            {
                Log($"[SERVER] Client {clientId ?? "UNKNOWN"} disconnected");
                if (clientId != null)
                {
                    _state.HandleDisconnect(clientId, new UiLogger(this));
                    RefreshSlotsSafe();
                }
            }
        }
    }

    private Task SendAsync(NetworkStream stream, string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        return stream.WriteAsync(data, 0, data.Length);
    }

    public void Log(string text)
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

    private void RefreshSlotsSafe()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefreshSlots));
            return;
        }
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        var summaries = _state.GetAllSlotSummaries();
        _gridSlots.DataSource = null;
        _gridSlots.DataSource = summaries;

        if (_gridSlots.Rows.Count > 0 && _gridSlots.CurrentRow == null)
        {
            _gridSlots.Rows[0].Selected = true;
        }

        UpdateQueueViewForSelected();
        UpdateCheckinPanel();
    }

    // private void GridSlots_SelectionChanged(object? sender, EventArgs e)
    // {
    //     UpdateQueueViewForSelected();
    // }

    private void GridSlots_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateQueueViewForSelected();
        UpdateCheckinPanel();
    }
    private void UpdateCheckinPanel()
    {
        _btnCheckIn.Enabled = false;
        _btnComplete.Enabled = false;

        if (_gridSlots.CurrentRow == null)
        {
            _lblSelectedSlot.Text = "Slot: (chưa chọn)";
            _lblBookingUser.Text = "User: -";
            _lblBookingStatus.Text = "Status: -";
            return;
        }

        // Lấy RoomId & SlotId từ dòng đang chọn
        var row = _gridSlots.CurrentRow;
        var roomIdObj = row.Cells["RoomId"].Value;
        var slotIdObj = row.Cells["SlotId"].Value;

        var roomId = roomIdObj?.ToString();
        var slotId = slotIdObj?.ToString();

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
        {
            _lblSelectedSlot.Text = "Slot: (invalid)";
            _lblBookingUser.Text = "User: -";
            _lblBookingStatus.Text = "Status: -";
            return;
        }

        _lblSelectedSlot.Text = $"Slot: {roomId} - {slotId} - {_dtDate.Value:yyyy-MM-dd}";

        // Gọi ServerState để lấy booking hiện tại của slot
        // -> Bạn sẽ cần viết hàm này trong ServerState
        var date = _dtDate.Value.Date;
        var booking = _state.GetCurrentBookingForSlot(date, roomId, slotId);

        if (booking == null)
        {
            _lblBookingUser.Text = "User: (không có booking)";
            _lblBookingStatus.Text = "Status: FREE";
            return;
        }

        // Giả sử BookingView có các field: UserId, FullName, UserType, Status
        _lblBookingUser.Text = $"User: {booking.UserId} - {booking.FullName} ({booking.UserType})";
        _lblBookingStatus.Text = $"Status: {booking.Status}";

        // Enable nút theo trạng thái
        // APPROVED  -> chỉ CHECK-IN
        // IN_USE    -> chỉ COMPLETE
        // Các trạng thái khác: disable cả hai
        switch (booking.Status)
        {
            case "APPROVED":
                _btnCheckIn.Enabled = true;
                _btnComplete.Enabled = false;
                break;
            case "IN_USE":
                _btnCheckIn.Enabled = false;
                _btnComplete.Enabled = true;
                break;
            default:
                _btnCheckIn.Enabled = false;
                _btnComplete.Enabled = false;
                break;
        }
    }
    private void BtnCheckIn_Click(object? sender, EventArgs e)
    {
        if (_gridSlots.CurrentRow == null)
            return;

        var row = _gridSlots.CurrentRow;
        var roomId = row.Cells["RoomId"].Value?.ToString();
        var slotId = row.Cells["SlotId"].Value?.ToString();

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
            return;

        var date = _dtDate.Value.Date;
        var logger = new UiLogger(this);

        // Giả định hàm trả bool và out errorMessage
        if (!_state.CheckInSlot(date, roomId, slotId, logger, out var error))
        {
            if (!string.IsNullOrEmpty(error))
                MessageBox.Show(this, error, "CHECK-IN failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        RefreshSlotsSafe();
        UpdateCheckinPanel();
    }
    private void BtnComplete_Click(object? sender, EventArgs e)
    {
        if (_gridSlots.CurrentRow == null)
            return;

        var row = _gridSlots.CurrentRow;
        var roomId = row.Cells["RoomId"].Value?.ToString();
        var slotId = row.Cells["SlotId"].Value?.ToString();

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
            return;

        var date = _dtDate.Value.Date;
        var logger = new UiLogger(this);

        // Hỏi nhẹ cho chắc (tuỳ bạn)
        var confirm = MessageBox.Show(
            this,
            $"Xác nhận COMPLETE & RELEASE phòng {roomId} - {slotId}?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        if (!_state.CompleteAndReleaseSlot(date, roomId, slotId, logger, out var error))
        {
            if (!string.IsNullOrEmpty(error))
                MessageBox.Show(this, error, "Complete failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        RefreshSlotsSafe();
        UpdateCheckinPanel();
    }
private void BtnLockEvent_Click(object? sender, EventArgs e)
{
    if (_gridSlots.CurrentRow == null)
    {
        MessageBox.Show(this, "Hãy chọn 1 phòng / ca trong bảng Slot trước.",
            "Lock for Event", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var row = _gridSlots.CurrentRow;
    var roomId = row.Cells["RoomId"].Value?.ToString();
    var slotId = row.Cells["SlotId"].Value?.ToString();

    if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
    {
        MessageBox.Show(this, "Dòng đang chọn không hợp lệ.", 
            "Lock for Event", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var date = _dtDate.Value.Date;
    var note = _txtEventNote.Text.Trim();
    var logger = new UiLogger(this);

    if (!_state.LockSlotForEvent(date, roomId, slotId, note, logger, out var error))
    {
        MessageBox.Show(this, error, "Lock for Event thất bại",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    Log($"[UI] Locked {roomId}-{slotId} on {date:yyyy-MM-dd} for event. Note='{note}'");
    RefreshSlotsSafe();
    UpdateCheckinPanel();
}

private void BtnUnlockEvent_Click(object? sender, EventArgs e)
{
    if (_gridSlots.CurrentRow == null)
    {
        MessageBox.Show(this, "Hãy chọn 1 phòng / ca trong bảng Slot trước.",
            "Unlock Event", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var row = _gridSlots.CurrentRow;
    var roomId = row.Cells["RoomId"].Value?.ToString();
    var slotId = row.Cells["SlotId"].Value?.ToString();

    if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
    {
        MessageBox.Show(this, "Dòng đang chọn không hợp lệ.",
            "Unlock Event", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var date = _dtDate.Value.Date;
    var logger = new UiLogger(this);

    if (!_state.UnlockSlotFromEvent(date, roomId, slotId, logger, out var error))
    {
        MessageBox.Show(this, error, "Unlock Event thất bại",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    Log($"[UI] Unlocked event at {roomId}-{slotId} on {date:yyyy-MM-dd}");
    RefreshSlotsSafe();
    UpdateCheckinPanel();
}

private void BtnForceGrant_Click(object? sender, EventArgs e)
{
    if (_gridSlots.CurrentRow == null)
    {
        MessageBox.Show(this, "Hãy chọn 1 phòng/ca trong bảng Slot trước.",
            "Force GRANT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var row = _gridSlots.CurrentRow;
    var roomId = row.Cells["RoomId"].Value?.ToString();
    var slotId = row.Cells["SlotId"].Value?.ToString();

    if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
    {
        MessageBox.Show(this, "Dòng đang chọn không hợp lệ.",
            "Force GRANT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var targetUserId = _txtForceUserId.Text.Trim();
    if (string.IsNullOrEmpty(targetUserId))
    {
        MessageBox.Show(this, "Target UserId không được trống.",
            "Force GRANT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var date = _dtDate.Value.Date;
    var logger = new UiLogger(this);

    if (!_state.ForceGrantFromServerUi(date, roomId, slotId, targetUserId, logger, out var error))
    {
        MessageBox.Show(this, error, "Force GRANT failed",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    Log($"[ADMIN-UI] Force GRANT {targetUserId} -> {roomId}-{slotId} ({date:yyyy-MM-dd})");
    RefreshSlotsSafe();
    UpdateCheckinPanel();
}

private void BtnForceRelease_Click(object? sender, EventArgs e)
{
    if (_gridSlots.CurrentRow == null)
    {
        MessageBox.Show(this, "Hãy chọn 1 phòng/ca trong bảng Slot trước.",
            "Force RELEASE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var row = _gridSlots.CurrentRow;
    var roomId = row.Cells["RoomId"].Value?.ToString();
    var slotId = row.Cells["SlotId"].Value?.ToString();

    if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotId))
    {
        MessageBox.Show(this, "Dòng đang chọn không hợp lệ.",
            "Force RELEASE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var date = _dtDate.Value.Date;
    var logger = new UiLogger(this);

    if (!_state.ForceReleaseFromServerUi(date, roomId, slotId, logger, out var error))
    {
        MessageBox.Show(this, error, "Force RELEASE failed",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    Log($"[ADMIN-UI] Force RELEASE {roomId}-{slotId} ({date:yyyy-MM-dd})");
    RefreshSlotsSafe();
    UpdateCheckinPanel();
}

    private void UpdateQueueViewForSelected()
    {
        if (_gridSlots.CurrentRow == null ||
            _gridSlots.CurrentRow.DataBoundItem is not SlotSummary summary)
        {
            _lblQueueTitle.Text = "Queue for: (select a room/slot)";
            _lstQueue.Items.Clear();
            _lstQueue.Items.Add("No selection");
            return;
        }

        var roomId = summary.RoomId;
        var slotId = summary.SlotId;

        _lblQueueTitle.Text = $"Queue for: {summary.Date} - {roomId}-{slotId}";

        var clients = _state.GetQueueClients(roomId, slotId);

        _lstQueue.Items.Clear();
        if (clients.Count == 0)
        {
            _lstQueue.Items.Add("Queue empty");
        }
        else
        {
            for (int i = 0; i < clients.Count; i++)
            {
                _lstQueue.Items.Add($"{i + 1}. {clients[i]}");
            }
        }
    }


    private class UiLogger : TextWriter
    {
        private readonly Form1 _form;
        public UiLogger(Form1 form) => _form = form;
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value)
        {
            if (value != null) _form.Log(value);
        }
    }
    // Form hiển thị danh sách Booking
    // Form hiển thị danh sách Booking
    public class BookingListForm : Form
    {
        private DataGridView _grid = null!;

        public BookingListForm(System.Collections.Generic.List<BookingView> data)
        {
            Text = "Booking History";
            Width = 1000;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Controls.Add(_grid);
            _grid.DataSource = data;
        }
    }

    private void BtnViewBookings_Click(object? sender, EventArgs e)
    {
        var data = _state.GetBookingViews();
        var dlg = new BookingListForm(data);
        dlg.Show(this);
    }


}

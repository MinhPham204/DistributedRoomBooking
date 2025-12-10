// BookingServer/Form1.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsTimer = System.Windows.Forms.Timer;
using System.Collections.Generic;
using System.Linq;
namespace BookingServer;

using System.Net;
using System.Net.Mail;
public class Form1 : Form
{
    // ===== Layout chính =====
    private Panel _panelTop = null!;
    private SplitContainer _mainSplit = null!;

    // ===== Left: Tab nhỏ (Hôm nay / Theo phòng) =====
    private TabControl _tabLeft = null!;
    private TabPage _tabLeftToday = null!;
    private TabPage _tabLeftByRoom = null!;

    // Tab Hôm nay: dùng grid slots hiện tại
    private DataGridView _gridSlots = null!; // giữ tên cũ để các hàm khác không lỗi

    // Tab Theo phòng
    private ComboBox _cbRoomFilter = null!;
    private DateTimePicker _dtRoomFilterDate = null!;
    private Button _btnRoomFilterSearch = null!;
    private DataGridView _gridRoomDaily = null!;

    // ===== Right: Tab lớn =====
    private TabControl _tabRight = null!;
    private TabPage _tabSlotDetail = null!;
    private TabPage _tabRoomMgmt = null!;
    private TabPage _tabUserMgmt = null!;
    private TabPage _tabBookingDetail = null!;
    private TabPage _tabStatistics = null!;
    private TabPage _tabSettings = null!;
    private TabPage _tabLogTab = null!;   // tránh trùng tên với Log()

    // ===== Top controls (Start + Date) =====
    private Button _btnStart = null!;
    private DateTimePicker _dtDate = null!;   // ngày đang xem
    private Label _lblNow = null!;            // label hiển thị thời gian Now
    private WinFormsTimer _nowTimer;          // timer cập nhật Now

    // ===== Tab Slot detail / Check-in =====
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
    private ComboBox _cbForceSlotStart = null!;
    private ComboBox _cbForceSlotEnd = null!;
    private Button _btnForceGrantRange = null!;
    private Button _btnForceReleaseRange = null!;
    private const int SERVER_TCP_PORT = 5000;
    private const int DISCOVERY_UDP_PORT = 5001;

    // Queue view (chuyển qua tab Slot detail)
    private Label _lblQueueTitle = null!;
    private ListBox _lstQueue = null!;

    // ===== Tab Room Management =====
    private DataGridView _gridRooms = null!;
    private TextBox _txtRoomId = null!;
    private TextBox _txtRoomBuilding = null!;
    private NumericUpDown _numRoomCapacity = null!;
    private CheckBox _chkRoomProjector = null!;
    private CheckBox _chkRoomPC = null!;
    private CheckBox _chkRoomAC = null!;
    private CheckBox _chkRoomMic = null!;
    private ComboBox _cbRoomStatus = null!;
    private Button _btnRoomAdd = null!;
    private Button _btnRoomUpdate = null!;
    private Button _btnRoomDelete = null!;

    // Fixed room config (UI khung, chưa cần logic)
    private TextBox _txtFixedSubjectCode = null!;
    private TextBox _txtFixedSubjectName = null!;
    private TextBox _txtFixedClass = null!;
    private ComboBox _cbFixedRoom = null!;
    private DateTimePicker _dtFixedFrom = null!;
    private DateTimePicker _dtFixedTo = null!;
    // cấu hình lịch cố định
    private ComboBox _cbFixedDayOfWeek = null!;
    private ComboBox _cbFixedSlotStart = null!;
    private ComboBox _cbFixedSlotEnd = null!;
    private Button _btnFixedApply = null!;
    // ===== Tab User Management =====
    private DataGridView _gridUsers = null!;
    private TextBox _txtUserId = null!;
    private TextBox _txtUserFullName = null!;
    private ComboBox _cbUserType = null!;
    private TextBox _txtUserStudentId = null!;
    private TextBox _txtUserLecturerId = null!;
    private TextBox _txtUserClass = null!;
    private TextBox _txtUserFaculty = null!;
    private TextBox _txtUserEmail = null!;
    private TextBox _txtUserPhone = null!;
    private CheckBox _chkUserActive = null!;
    private Button _btnUserAdd = null!;
    private Button _btnUserUpdate = null!;
    private Button _btnUserDelete = null!;
    private Button _btnUserResetPwd = null!;

    private TextBox _txtSearchUserId = null!;
    private Button _btnSearchUser = null!;
    private Button _btnSearchClear = null!;

    private ComboBox _cbUserFaculty;
    private TextBox _txtPassword;


    // ===== Tab Booking detail =====
    private DataGridView _gridBookings = null!;
    private DateTimePicker _dtBookingFrom = null!;
    private DateTimePicker _dtBookingTo = null!;
    private ComboBox _cbBookingRoom = null!;
    private TextBox _txtBookingUserIdFilter = null!;
    private TextBox _txtBookingUserId = null!;
    private ComboBox _cbBookingUserType = null!;
    private ComboBox _cbBookingStatus = null!;
    private Button _btnBookingSearch = null!;
    private Button _btnBookingExportExcel = null!;
    private Button _btnBookingExportPdf = null!;

    // ===== Tab Statistics =====
    // Theo phòng
    private DateTimePicker _dtStatsFrom = null!;
    private DateTimePicker _dtStatsTo = null!;
    private DataGridView _gridRoomStats = null!;

    // Theo loại user
    private DataGridView _gridUserTypeStats = null!;

    // ===== Tab Settings =====
    private DataGridView _gridSlotConfig = null!;
    private NumericUpDown _numCheckinDeadlineMinutes = null!;
    private CheckBox _chkSendEmailForce = null!;
    private CheckBox _chkSendEmailNoShow = null!;
    private CheckBox _chkNotifyClient = null!;

    // SMTP controls
    private TextBox _txtSmtpHost = null!;
    private NumericUpDown _numSmtpPort = null!;
    private CheckBox _chkSmtpSsl = null!;
    private TextBox _txtSmtpUser = null!;
    private TextBox _txtSmtpPassword = null!;
    private TextBox _txtSmtpFrom = null!;

    private Button _btnSettingsSave = null!;

    // ===== Tab Log =====
    private TextBox _txtLog = null!;   // giữ tên cũ, chỉ đổi parent sang tab Log

    // ===== Logic và timer (giữ nguyên như bạn) =====
    private TcpListener? _listener;
    private bool _running = false;
    private readonly ServerState _state = new();
    private Button _btnViewBookings = null!; // nếu vẫn muốn dùng popup BookingListForm
    private readonly WinFormsTimer _noShowTimer;


    // ===== Tab room management =====
    private TextBox _txtSearchRoomId = null!;
    private Button _btnSearchRoom = null!;
    private Button _btnSearchRoomAll = null!;
    public Form1()
    {
        // InitializeComponent();
        SetupUi();

        // ===== Timer cập nhật Label Now mỗi 1 giây =====
        _nowTimer = new WinFormsTimer();
        _nowTimer.Interval = 1000; // 1 giây
        _nowTimer.Tick += (s, e) =>
        {
            var now = _state.Now;  // dùng demo time nếu đang bật
            if (_lblNow != null && !_lblNow.IsDisposed)
            {
                _lblNow.Text = $"Now: {now:yyyy-MM-dd HH:mm:ss}";
            }
        };
        _nowTimer.Start();

        _noShowTimer = new WinFormsTimer();
        _noShowTimer.Interval = 60_000; // mỗi 60s quét NO_SHOW
        _noShowTimer.Tick += NoShowTimer_Tick;
        _noShowTimer.Start();

        var logger = new UiLogger(this);
        var snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        // 1) Load snapshot nếu có
        _state.LoadSnapshotIfExists(snapshotPath, logger);

        // 2) Đảm bảo current date = hôm nay và đã có slots cho hôm nay
        _state.SetCurrentDate(DateTime.Today, logger);


        // 3) Refresh grid
        RefreshSlotsSafe();
        RefreshUserGrid();

        // 🔹 Lưu snapshot khi đóng form
        this.FormClosing += Form1_FormClosing;

    }


    private void SetupUi()
    {
        // 👉 QUAN TRỌNG: dọn sạch mọi control mà InitializeComponent() đã add
        this.Controls.Clear();
        this.AutoScaleMode = AutoScaleMode.None;
        this.MinimumSize = new System.Drawing.Size(1200, 750);

        // ===== Form =====
        this.Text = "Server - Room Booking (Centralized Mutex) -  NEW UI ";
        this.ClientSize = new System.Drawing.Size(1200, 750);   // dùng ClientSize thay vì Width/Height
        this.StartPosition = FormStartPosition.CenterScreen;

        // (tuỳ thích)
        // this.WindowState = FormWindowState.Maximized;

        // ===== TOP: Start Server + DatePicker =====
        _panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45
        };
        this.Controls.Add(_panelTop);

        _btnStart = new Button
        {
            Text = "Start Server",
            Left = 10,
            Top = 8,
            Width = 120,
            Height = 28
        };
        _btnStart.Click += BtnStart_Click;
        _panelTop.Controls.Add(_btnStart);

        _dtDate = new DateTimePicker
        {
            Left = 150,
            Top = 8,
            Width = 200,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };
        _dtDate.ValueChanged += DtDate_ValueChanged;
        _panelTop.Controls.Add(_dtDate);

        // ===== Label hiển thị thời gian Now =====
        _lblNow = new Label
        {
            Left = 360,              // nằm cạnh DateTimePicker
            Top = 12,
            AutoSize = true,
            Text = $"Now: {_state.Now:yyyy-MM-dd HH:mm:ss}"
        };
        _panelTop.Controls.Add(_lblNow);

        // ===== MAIN SPLIT: Left (slot list) / Right (tabs) =====
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 450,     // cho panel trái rộng hơn tí
            BorderStyle = BorderStyle.Fixed3D
        };
        this.Controls.Add(_mainSplit);

        _mainSplit.Panel1.Padding = new Padding(0, 50, 0, 0);   // hạ tab trái xuống 8px
        _mainSplit.Panel2.Padding = new Padding(0, 50, 0, 0);   // hạ tab phải xuống 8px

        BuildLeftTabs();   // Tab nhỏ bên trái
        BuildRightTabs();  // Tab lớn bên phải
    }
    private void BuildLeftTabs()
    {
        // TabControl trái: Hôm nay / Theo phòng
        _tabLeft = new TabControl
        {
            Dock = DockStyle.Fill
        };
        _tabLeft.Appearance = TabAppearance.Normal;
        _tabLeft.ItemSize = new System.Drawing.Size(80, 24);
        _tabLeft.SizeMode = TabSizeMode.Fixed;
        _mainSplit.Panel1.Controls.Add(_tabLeft);

        _tabLeftToday = new TabPage("Hôm nay");
        _tabLeftByRoom = new TabPage("Theo phòng");
        _tabLeft.TabPages.AddRange(new[] { _tabLeftToday, _tabLeftByRoom });

        // ----- Tab Hôm nay: Grid slot giống hiện tại -----
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

        _tabLeftToday.Controls.Add(_gridSlots);

        // Nút View Bookings (mở popup như code cũ)
        _btnViewBookings = new Button
        {
            Text = "View Booking List (Popup)",
            Dock = DockStyle.Bottom,
            Height = 35
        };
        _btnViewBookings.Click += BtnViewBookings_Click;
        _tabLeftToday.Controls.Add(_btnViewBookings);

        // ----- Tab Theo phòng -----
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45
        };
        _tabLeftByRoom.Controls.Add(pnlFilter);

        var lblRoom = new Label
        {
            Left = 10,
            Top = 15,
            Width = 60,
            Text = "Phòng:"
        };
        pnlFilter.Controls.Add(lblRoom);

        _cbRoomFilter = new ComboBox
        {
            Left = 70,
            Top = 11,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        pnlFilter.Controls.Add(_cbRoomFilter);

        var lblDate = new Label
        {
            Left = 210,
            Top = 15,
            Width = 50,
            Text = "Ngày:"
        };
        pnlFilter.Controls.Add(lblDate);

        _dtRoomFilterDate = new DateTimePicker
        {
            Left = 260,
            Top = 11,
            Width = 120,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };
        pnlFilter.Controls.Add(_dtRoomFilterDate);

        _btnRoomFilterSearch = new Button
        {
            Left = 390,
            Top = 11,
            Width = 80,
            Text = "Xem"
        };
        pnlFilter.Controls.Add(_btnRoomFilterSearch);
        _btnRoomFilterSearch.Click += BtnRoomFilterSearch_Click;

        // ===== Panel chứa grid, có padding để hạ grid xuống =====
        var pnlRoomDaily = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 5, 0, 0)  // Top = 5px -> tạo khoảng trống cho header
        };
        _tabLeftByRoom.Controls.Add(pnlRoomDaily);

        _gridRoomDaily = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        pnlRoomDaily.Controls.Add(_gridRoomDaily);
    }

    private void BuildRightTabs()
    {
        _tabRight = new TabControl
        {
            Dock = DockStyle.Fill
        };
        _mainSplit.Panel2.Controls.Add(_tabRight);

        _tabSlotDetail = new TabPage("Slot detail / Check-in");
        _tabRoomMgmt = new TabPage("Room management");
        _tabUserMgmt = new TabPage("User management");
        _tabBookingDetail = new TabPage("Booking detail");
        _tabStatistics = new TabPage("Statistics");
        _tabSettings = new TabPage("Settings");
        _tabLogTab = new TabPage("Log");

        _tabRight.TabPages.AddRange(new[]
        {
        _tabSlotDetail,
        _tabRoomMgmt,
        _tabUserMgmt,
        _tabBookingDetail,
        _tabStatistics,
        _tabSettings,
        _tabLogTab
    });

        BuildTabSlotDetail();
        BuildTabRoomManagement();
        // InitUserManagementUi();
        BuildTabUserManagement();
        WireUpUserGridEvents();
        RefreshUserGrid();
        BuildTabBookingDetail();
        BuildTabStatistics();
        BuildTabSettings();
        BuildTabLog();
    }
    private void BuildTabSlotDetail()
    {
        // Group: Slot info
        var grpSlotInfo = new GroupBox
        {
            Text = "Slot info",
            Left = 10,
            Top = 10,
            Width = 740,
            Height = 70
        };
        _tabSlotDetail.Controls.Add(grpSlotInfo);

        _lblSelectedSlot = new Label
        {
            Left = 10,
            Top = 25,
            Width = 700,
            Text = "Slot: (chưa chọn)"
        };
        grpSlotInfo.Controls.Add(_lblSelectedSlot);

        // Group: Booking hiện tại
        var grpBooking = new GroupBox
        {
            Text = "Current booking",
            Left = 10,
            Top = 90,
            Width = 740,
            Height = 110
        };
        _tabSlotDetail.Controls.Add(grpBooking);

        _lblBookingUser = new Label
        {
            Left = 10,
            Top = 25,
            Width = 700,
            Text = "User: -"
        };
        grpBooking.Controls.Add(_lblBookingUser);

        _lblBookingStatus = new Label
        {
            Left = 10,
            Top = 45,
            Width = 700,
            Text = "Status: -"
        };
        grpBooking.Controls.Add(_lblBookingStatus);

        // TODO: sau này thêm label Purpose, CreatedAt, CheckinDeadline, CheckinTime...

        // Group: Admin actions (Check-in / Complete / Force)
        _grpCheckin = new GroupBox
        {
            Text = "Admin actions",
            Left = 10,
            Top = 210,
            Width = 740,
            Height = 170   // tăng chiều cao một chút để đủ chỗ range
        };
        _tabSlotDetail.Controls.Add(_grpCheckin);

        _btnCheckIn = new Button
        {
            Left = 10,
            Top = 25,
            Width = 150,
            Text = "CHECK-IN",
            Enabled = false
        };
        _btnCheckIn.Click += BtnCheckIn_Click;
        _grpCheckin.Controls.Add(_btnCheckIn);

        _btnComplete = new Button
        {
            Left = 170,
            Top = 25,
            Width = 150,
            Text = "Complete & Release",
            Enabled = false
        };
        _btnComplete.Click += BtnComplete_Click;
        _grpCheckin.Controls.Add(_btnComplete);

        var lblForceUser = new Label
        {
            Left = 10,
            Top = 65,
            Width = 80,
            Text = "Force user:"
        };
        _grpCheckin.Controls.Add(lblForceUser);

        _txtForceUserId = new TextBox
        {
            Left = 90,
            Top = 62,
            Width = 140
        };
        _grpCheckin.Controls.Add(_txtForceUserId);

        _btnForceGrant = new Button
        {
            Left = 240,
            Top = 60,
            Width = 120,
            Text = "Force GRANT (single)"
        };
        _btnForceGrant.Click += BtnForceGrant_Click;
        _grpCheckin.Controls.Add(_btnForceGrant);

        _btnForceRelease = new Button
        {
            Left = 370,
            Top = 60,
            Width = 120,
            Text = "Force RELEASE (single)"
        };
        _btnForceRelease.Click += BtnForceRelease_Click;
        _grpCheckin.Controls.Add(_btnForceRelease);

        // ====== RANGE FORCE (mới) ======
        var lblRange = new Label
        {
            Left = 10,
            Top = 100,
            Width = 80,
            Text = "Range:"
        };
        _grpCheckin.Controls.Add(lblRange);

        _cbForceSlotStart = new ComboBox
        {
            Left = 90,
            Top = 97,
            Width = 60,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbForceSlotEnd = new ComboBox
        {
            Left = 160,
            Top = 97,
            Width = 60,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // fill S1..S14
        for (int i = 1; i <= 14; i++)
        {
            var sid = $"S{i}";
            _cbForceSlotStart.Items.Add(sid);
            _cbForceSlotEnd.Items.Add(sid);
        }
        if (_cbForceSlotStart.Items.Count > 0) _cbForceSlotStart.SelectedIndex = 0;
        if (_cbForceSlotEnd.Items.Count > 0) _cbForceSlotEnd.SelectedIndex = 0;

        _grpCheckin.Controls.Add(_cbForceSlotStart);
        _grpCheckin.Controls.Add(_cbForceSlotEnd);

        _btnForceGrantRange = new Button
        {
            Left = 240,
            Top = 95,
            Width = 120,
            Text = "Force GRANT RANGE"
        };
        _btnForceGrantRange.Click += BtnForceGrantRange_Click;
        _grpCheckin.Controls.Add(_btnForceGrantRange);

        _btnForceReleaseRange = new Button
        {
            Left = 370,
            Top = 95,
            Width = 140,
            Text = "Force RELEASE RANGE"
        };
        _btnForceReleaseRange.Click += BtnForceReleaseRange_Click;
        _grpCheckin.Controls.Add(_btnForceReleaseRange);


        // Group: Event lock
        var grpEvent = new GroupBox
        {
            Text = "Event lock",
            Left = 10,
            Top = 350,
            Width = 740,
            Height = 80
        };
        _tabSlotDetail.Controls.Add(grpEvent);

        var lblEventNote = new Label
        {
            Left = 10,
            Top = 35,
            Width = 80,
            Text = "Event note:"
        };
        grpEvent.Controls.Add(lblEventNote);

        _txtEventNote = new TextBox
        {
            Left = 90,
            Top = 35,
            Width = 260
        };
        grpEvent.Controls.Add(_txtEventNote);

        _btnLockEvent = new Button
        {
            Left = 360,
            Top = 35,
            Width = 120,
            Text = "Lock for Event"
        };
        _btnLockEvent.Click += BtnLockEvent_Click;
        grpEvent.Controls.Add(_btnLockEvent);

        _btnUnlockEvent = new Button
        {
            Left = 490,
            Top = 35,
            Width = 120,
            Text = "Unlock Event"
        };
        _btnUnlockEvent.Click += BtnUnlockEvent_Click;
        grpEvent.Controls.Add(_btnUnlockEvent);

        // Group: Queue view
        var grpQueue = new GroupBox
        {
            Text = "Queue",
            Left = 10,
            Top = 440,
            Width = 740,
            Height = 160
        };
        _tabSlotDetail.Controls.Add(grpQueue);

        _lblQueueTitle = new Label
        {
            Left = 10,
            Top = 20,
            Width = 700,
            Text = "Queue for: (select a room/slot)"
        };
        grpQueue.Controls.Add(_lblQueueTitle);

        _lstQueue = new ListBox
        {
            Left = 10,
            Top = 40,
            Width = 700,
            Height = 100
        };
        grpQueue.Controls.Add(_lstQueue);
        _tabSlotDetail.AutoScroll = true;

    }
    private void BuildTabRoomManagement()
    {
        _tabRoomMgmt.Controls.Clear();
        _tabRoomMgmt.AutoScroll = true;

        // ===== TABLELAYOUT: 2 CỘT (TRÁI LIST / PHẢI DETAIL + FIXED) =====
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // trái 50%
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // phải 50%
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _tabRoomMgmt.Controls.Add(table);

        // =================================================================
        //  CỘT 1: ROOM LIST + THANH SEARCH
        // =================================================================
        var grpList = new GroupBox
        {
            Text = "Room list",
            Dock = DockStyle.Fill
        };
        table.Controls.Add(grpList, 0, 0);

        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill
        };
        grpList.Controls.Add(pnlLeft);

        // ---- Thanh search ở trên ----
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40
        };
        pnlLeft.Controls.Add(pnlSearch);

        var lblSearch = new Label
        {
            Left = 5,
            Top = 11,
            Width = 70,
            Text = "RoomId:"
        };
        pnlSearch.Controls.Add(lblSearch);

        _txtSearchRoomId = new TextBox
        {
            Left = 75,
            Top = 8,
            Width = 120
        };
        pnlSearch.Controls.Add(_txtSearchRoomId);

        _btnSearchRoom = new Button
        {
            Left = 200,
            Top = 6,
            Width = 60,
            Text = "Tìm"
        };
        _btnSearchRoom.Click += (s, e) =>
        {
            var keyword = _txtSearchRoomId.Text.Trim();
            RefreshRoomGrid(keyword);
        };
        pnlSearch.Controls.Add(_btnSearchRoom);

        _btnSearchRoomAll = new Button
        {
            Left = 265,
            Top = 6,
            Width = 60,
            Text = "All"
        };
        _btnSearchRoomAll.Click += (s, e) =>
        {
            _txtSearchRoomId.Text = "";
            RefreshRoomGrid(null);
        };
        pnlSearch.Controls.Add(_btnSearchRoomAll);

        // ---- Grid nằm dưới, Dock Fill ----
        _gridRooms = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        pnlLeft.Controls.Add(_gridRooms);
        _gridRooms.BringToFront();

        // =================================================================
        //  CỘT 2: PHẢI – ROOM DETAIL (TRÊN) + FIXED CONFIG (DƯỚI)
        // =================================================================
        var rightTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        rightTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 230)); // detail cố định ~230px
        rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // fixed fill phần còn lại
        table.Controls.Add(rightTable, 1, 0);

        // ===== Group: Room detail (phải trên) =====
        var grpRoomDetail = new GroupBox
        {
            Text = "Room detail",
            Dock = DockStyle.Fill
        };
        rightTable.Controls.Add(grpRoomDetail, 0, 0);

        // Giữ nguyên layout bên trong (tọa độ), chỉ khác là groupbox Dock = Fill
        var lblRid = new Label { Left = 10, Top = 25, Width = 80, Text = "RoomId:" };
        _txtRoomId = new TextBox { Left = 100, Top = 22, Width = 160 };

        var lblBuilding = new Label { Left = 10, Top = 55, Width = 80, Text = "Building:" };
        _txtRoomBuilding = new TextBox { Left = 100, Top = 52, Width = 160 };

        var lblCap = new Label { Left = 10, Top = 85, Width = 80, Text = "Capacity:" };
        _numRoomCapacity = new NumericUpDown
        {
            Left = 100,
            Top = 82,
            Width = 80,
            Minimum = 0,
            Maximum = 500,
            Value = 60
        };

        _chkRoomProjector = new CheckBox { Left = 10, Top = 115, Width = 100, Text = "Projector" };
        _chkRoomPC = new CheckBox { Left = 120, Top = 115, Width = 60, Text = "PC" };
        _chkRoomAC = new CheckBox { Left = 190, Top = 115, Width = 60, Text = "A/C" };
        _chkRoomMic = new CheckBox { Left = 10, Top = 140, Width = 100, Text = "Mic" };

        var lblStatus = new Label { Left = 120, Top = 140, Width = 60, Text = "Status:" };
        _cbRoomStatus = new ComboBox
        {
            Left = 180,
            Top = 137,
            Width = 100,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbRoomStatus.Items.AddRange(new object[] { "ACTIVE", "UNDER_MAINTENANCE", "DISABLED" });
        _cbRoomStatus.SelectedIndex = 0;

        _btnRoomAdd = new Button { Left = 10, Top = 170, Width = 70, Text = "Add" };
        _btnRoomUpdate = new Button { Left = 90, Top = 170, Width = 70, Text = "Update" };
        _btnRoomDelete = new Button { Left = 170, Top = 170, Width = 70, Text = "Delete" };

        grpRoomDetail.Controls.AddRange(new Control[]
        {
        lblRid, _txtRoomId,
        lblBuilding, _txtRoomBuilding,
        lblCap, _numRoomCapacity,
        _chkRoomProjector, _chkRoomPC, _chkRoomAC, _chkRoomMic,
        lblStatus, _cbRoomStatus,
        _btnRoomAdd, _btnRoomUpdate, _btnRoomDelete
        });

        // ===== Group: Fixed room config (phải dưới) =====
        var grpFixed = new GroupBox
        {
            Text = "Fixed room config (môn/lớp)",
            Dock = DockStyle.Fill
        };
        rightTable.Controls.Add(grpFixed, 0, 1);

        var lblSubCode = new Label { Left = 10, Top = 25, Width = 100, Text = "Subject code:" };
        _txtFixedSubjectCode = new TextBox { Left = 110, Top = 22, Width = 150 };

        var lblSubName = new Label { Left = 10, Top = 55, Width = 100, Text = "Subject name:" };
        _txtFixedSubjectName = new TextBox { Left = 110, Top = 52, Width = 150 };

        var lblClass = new Label { Left = 10, Top = 85, Width = 100, Text = "Class:" };
        _txtFixedClass = new TextBox { Left = 110, Top = 82, Width = 150 };

        var lblFixedRoom = new Label { Left = 10, Top = 115, Width = 100, Text = "Fixed room:" };
        _cbFixedRoom = new ComboBox
        {
            Left = 110,
            Top = 112,
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblFrom = new Label { Left = 10, Top = 145, Width = 100, Text = "From / To:" };
        _dtFixedFrom = new DateTimePicker
        {
            Left = 110,
            Top = 142,
            Width = 80,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };
        _dtFixedTo = new DateTimePicker
        {
            Left = 200,
            Top = 142,
            Width = 80,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };

        // ====== PHẦN MỚI: chọn thứ + ca + nút Apply lịch cố định ======

        var lblDow = new Label { Left = 10, Top = 175, Width = 100, Text = "Day of week:" };
        _cbFixedDayOfWeek = new ComboBox
        {
            Left = 110,
            Top = 172,
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbFixedDayOfWeek.Items.AddRange(new object[]
        {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
        });
        _cbFixedDayOfWeek.SelectedItem = DayOfWeek.Saturday; // ví dụ mặc định T7

        var lblSlots = new Label { Left = 10, Top = 205, Width = 100, Text = "Slots:" };
        _cbFixedSlotStart = new ComboBox
        {
            Left = 110,
            Top = 202,
            Width = 70,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbFixedSlotEnd = new ComboBox
        {
            Left = 190,
            Top = 202,
            Width = 70,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        for (int i = 1; i <= 14; i++)
        {
            var id = $"S{i}";
            _cbFixedSlotStart.Items.Add(id);
            _cbFixedSlotEnd.Items.Add(id);
        }
        _cbFixedSlotStart.SelectedItem = "S1";
        _cbFixedSlotEnd.SelectedItem = "S4";

        _btnFixedApply = new Button
        {
            Left = 110,
            Top = 232,
            Width = 150,
            Text = "Apply fixed schedule"
        };
        _btnFixedApply.Click += BtnFixedApply_Click;

        grpFixed.Controls.AddRange(new Control[]
        {
        lblSubCode, _txtFixedSubjectCode,
        lblSubName, _txtFixedSubjectName,
        lblClass, _txtFixedClass,
        lblFixedRoom, _cbFixedRoom,
        lblFrom, _dtFixedFrom, _dtFixedTo,
        lblDow, _cbFixedDayOfWeek,
        lblSlots, _cbFixedSlotStart, _cbFixedSlotEnd,
        _btnFixedApply
        });


        _btnRoomAdd.Click += BtnRoomAdd_Click;
        _btnRoomUpdate.Click += BtnRoomUpdate_Click;
        _btnRoomDelete.Click += BtnRoomDelete_Click;
        // SAU KHI BUILD XONG: load list phòng
        RefreshRoomGrid();
        _gridRooms.SelectionChanged += GridRooms_SelectionChanged;

    }

    private void RefreshRoomGrid(string? filterRoomId = null)
    {
        if (_gridRooms == null) return;

        // 🔹 Lấy TẤT CẢ phòng từ ServerState
        var allRooms = _state.RoomsInfo
            .Select(kvp => kvp.Value)
            .ToList();

        // 🔹 Danh sách hiển thị trên grid (có thể filter)
        var rooms = allRooms;

        if (!string.IsNullOrWhiteSpace(filterRoomId))
        {
            var keyword = filterRoomId.Trim().ToLower();
            rooms = allRooms
                .Where(r => !string.IsNullOrEmpty(r.RoomId) &&
                            r.RoomId.ToLower().Contains(keyword))
                .ToList();
        }

        // ====== BIND GRID ROOMS ======
        _gridRooms.AutoGenerateColumns = false;
        _gridRooms.Columns.Clear();

        var colRoomId = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "RoomId",
            HeaderText = "RoomId",
            Width = 80
        };
        var colBuilding = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Building",
            HeaderText = "Building",
            Width = 120
        };
        var colCapacity = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Capacity",
            HeaderText = "Cap",
            Width = 60
        };

        _gridRooms.Columns.Add(colRoomId);
        _gridRooms.Columns.Add(colBuilding);
        _gridRooms.Columns.Add(colCapacity);

        _gridRooms.DataSource = rooms;

        // ====== FILL COMBO FIXED ROOM (dùng ALL ROOMS, không filter) ======
        if (_cbFixedRoom != null)
        {
            var current = _cbFixedRoom.SelectedItem?.ToString();

            _cbFixedRoom.Items.Clear();
            foreach (var r in allRooms)
            {
                _cbFixedRoom.Items.Add(r.RoomId);
            }

            if (!string.IsNullOrEmpty(current) && _cbFixedRoom.Items.Contains(current))
            {
                _cbFixedRoom.SelectedItem = current;
            }
            else if (_cbFixedRoom.Items.Count > 0)
            {
                _cbFixedRoom.SelectedIndex = 0;
            }
        }

        // ====== FILL COMBO "Theo phòng" (_cbRoomFilter) ======
        if (_cbRoomFilter != null)
        {
            var currentRoom = _cbRoomFilter.SelectedItem?.ToString();

            _cbRoomFilter.Items.Clear();
            foreach (var r in allRooms)
            {
                _cbRoomFilter.Items.Add(r.RoomId);
            }

            if (!string.IsNullOrEmpty(currentRoom) && _cbRoomFilter.Items.Contains(currentRoom))
            {
                _cbRoomFilter.SelectedItem = currentRoom;
            }
            else if (_cbRoomFilter.Items.Count > 0)
            {
                _cbRoomFilter.SelectedIndex = 0;
            }
        }
    }




    // Thêm ở đầu file Form1.cs (field)
    private void BuildTabUserManagement()
    {
        _tabUserMgmt.Controls.Clear();
        _tabUserMgmt.AutoScroll = true;

        // ===== TableLayout: chia 2 cột (list / detail) =====
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // cột trái 50%
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // cột phải 50%
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _tabUserMgmt.Controls.Add(table);

        // =====================================================================
        //  CỘT 1: USER LIST + THANH SEARCH
        // =====================================================================
        var grpList = new GroupBox
        {
            Text = "User list",
            Dock = DockStyle.Fill
        };
        table.Controls.Add(grpList, 0, 0);

        // Panel chứa search + grid
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill
        };
        grpList.Controls.Add(pnlLeft);

        // ---- Thanh search ở trên ----
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40
        };
        pnlLeft.Controls.Add(pnlSearch);

        var lblSearch = new Label
        {
            Left = 5,
            Top = 11,
            Width = 60,
            Text = "UserId:"
        };
        pnlSearch.Controls.Add(lblSearch);

        _txtSearchUserId = new TextBox
        {
            Left = 65,
            Top = 8,
            Width = 120
        };
        pnlSearch.Controls.Add(_txtSearchUserId);

        _btnSearchUser = new Button
        {
            Left = 190,
            Top = 6,
            Width = 60,
            Text = "Tìm"
        };
        _btnSearchUser.Click += (s, e) =>
        {
            var keyword = _txtSearchUserId.Text.Trim();
            RefreshUserGrid(keyword);
        };
        pnlSearch.Controls.Add(_btnSearchUser);

        _btnSearchClear = new Button
        {
            Left = 255,
            Top = 6,
            Width = 60,
            Text = "All"
        };
        _btnSearchClear.Click += (s, e) =>
        {
            _txtSearchUserId.Text = "";
            RefreshUserGrid(null);
        };
        pnlSearch.Controls.Add(_btnSearchClear);

        // ---- Grid nằm dưới, fill toàn bộ ----
        _gridUsers = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        // grid Dock Fill dưới thanh search
        pnlLeft.Controls.Add(_gridUsers);
        _gridUsers.BringToFront();

        // =====================================================================
        //  CỘT 2: USER DETAIL (giữ layout giống bạn, chỉ chỉnh lại tí width)
        // =====================================================================
        var grpUser = new GroupBox
        {
            Text = "User detail",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        table.Controls.Add(grpUser, 1, 0);

        int curY = 25;

        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "UserId:" });
        _txtUserId = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserId);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Full name:" });
        _txtUserFullName = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserFullName);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Type:" });
        _cbUserType = new ComboBox
        {
            Left = 100,
            Top = curY - 3,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbUserType.Items.AddRange(new object[] { "Student", "Lecturer", "Staff" });
        _cbUserType.SelectedIndex = 0;
        grpUser.Controls.Add(_cbUserType);
        _cbUserType.SelectedIndexChanged += (s, e) => UpdateUserTypeDependentFields();

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "StudentId:" });
        _txtUserStudentId = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserStudentId);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "LecturerId:" });
        _txtUserLecturerId = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserLecturerId);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Class:" });
        _txtUserClass = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserClass);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Faculty:" });
        // _txtUserFaculty = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        // grpUser.Controls.Add(_txtUserFaculty);

        _cbUserFaculty = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Left = 100,
            Top = 200,
            Width = 180
        };
        _cbUserFaculty.Items.AddRange(new[]
        {
    "CNTT2","IOT2","MKT2","CNDPT2","KT2","DTVT2","QTKD2","ATTT2"
});
        grpUser.Controls.Add(_cbUserFaculty);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Email:" });
        _txtUserEmail = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserEmail);

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Phone:" });
        _txtUserPhone = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserPhone);

        // _txtPassword = new TextBox
        // {
        //     Left = 100,
        //     Top = 230,
        //     Width = 180,
        //     UseSystemPasswordChar = true
        // };
        // grpUser.Controls.Add(_txtPassword);

        // Password (plain text để hash khi tạo user)
        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Password:" });
        _txtPassword = new TextBox
        {
            Left = 100,
            Top = curY - 3,
            Width = 180,
            UseSystemPasswordChar = true
        };
        grpUser.Controls.Add(_txtPassword);


        // curY += 30;
        // _chkUserActive = new CheckBox { Left = 100, Top = curY - 3, Width = 180, Text = "Active" };
        // _chkUserActive.Checked = true;
        // grpUser.Controls.Add(_chkUserActive);

        curY += 30;
        _chkUserActive = new CheckBox { Left = 100, Top = curY - 3, Width = 180, Text = "Active" };
        _chkUserActive.Checked = true;
        grpUser.Controls.Add(_chkUserActive);

        curY += 40;
        _btnUserAdd = new Button { Left = 10, Top = curY, Width = 80, Text = "Add" };
        _btnUserUpdate = new Button { Left = 100, Top = curY, Width = 80, Text = "Update" };
        _btnUserDelete = new Button { Left = 190, Top = curY, Width = 80, Text = "Delete" };

        curY += 35;
        // _btnUserResetPwd = new Button { Left = 10, Top = curY, Width = 260, Text = "Reset password" };
        _btnUserAdd.Click += BtnAddUser_Click;
        _btnUserUpdate.Click += BtnUpdateUser_Click;
        _btnUserDelete.Click += BtnDeleteUser_Click;
        grpUser.Controls.AddRange(new Control[]
        {
        _btnUserAdd, _btnUserUpdate, _btnUserDelete// _btnUserResetPwd
        });

        // Khi build xong tab thì load luôn dữ liệu list
        RefreshUserGrid();
        UpdateUserTypeDependentFields();

    }

    // private void BuildTabBookingDetail()
    // {
    //     // Filter panel
    //     var pnlFilter = new Panel
    //     {
    //         Dock = DockStyle.Top,
    //         Height = 60
    //     };
    //     _tabBookingDetail.Controls.Add(pnlFilter);

    //     var lblFrom = new Label { Left = 10, Top = 10, Width = 60, Text = "Từ ngày:" };
    //     _dtBookingFrom = new DateTimePicker
    //     {
    //         Left = 70,
    //         Top = 7,
    //         Width = 110,
    //         Format = DateTimePickerFormat.Custom,
    //         CustomFormat = "yyyy-MM-dd"
    //     };

    //     var lblTo = new Label { Left = 190, Top = 10, Width = 60, Text = "Đến:" };
    //     _dtBookingTo = new DateTimePicker
    //     {
    //         Left = 240,
    //         Top = 7,
    //         Width = 110,
    //         Format = DateTimePickerFormat.Custom,
    //         CustomFormat = "yyyy-MM-dd"
    //     };

    //     var lblRoom = new Label { Left = 360, Top = 10, Width = 60, Text = "Phòng:" };
    //     _cbBookingRoom = new ComboBox
    //     {
    //         Left = 420,
    //         Top = 7,
    //         Width = 100,
    //         DropDownStyle = ComboBoxStyle.DropDownList
    //     };

    //     var lblUserId = new Label { Left = 10, Top = 35, Width = 60, Text = "User:" };
    //     _txtBookingUserIdFilter = new TextBox { Left = 70, Top = 32, Width = 110 };

    //     var lblUserType = new Label { Left = 190, Top = 35, Width = 80, Text = "UserType:" };
    //     _cbBookingUserType = new ComboBox
    //     {
    //         Left = 260,
    //         Top = 32,
    //         Width = 90,
    //         DropDownStyle = ComboBoxStyle.DropDownList
    //     };
    //     _cbBookingUserType.Items.AddRange(new object[] { "ALL", "Student", "Lecturer", "Staff" });
    //     _cbBookingUserType.SelectedIndex = 0;

    //     var lblStatus = new Label { Left = 360, Top = 35, Width = 60, Text = "Status:" };
    //     _cbBookingStatus = new ComboBox
    //     {
    //         Left = 420,
    //         Top = 32,
    //         Width = 100,
    //         DropDownStyle = ComboBoxStyle.DropDownList
    //     };
    //     _cbBookingStatus.Items.AddRange(new object[]
    //     {
    //     "ALL","APPROVED","IN_USE","COMPLETED","CANCELLED","NO_SHOW"
    //     });
    //     _cbBookingStatus.SelectedIndex = 0;

    //     _btnBookingSearch = new Button
    //     {
    //         Left = 540,
    //         Top = 18,
    //         Width = 80,
    //         Text = "Search"
    //     };
    //     // TODO: gắn event: lấy list BookingView theo filter
    //     pnlFilter.Controls.AddRange(new Control[]
    //     {
    //     lblFrom, _dtBookingFrom,
    //     lblTo, _dtBookingTo,
    //     lblRoom, _cbBookingRoom,
    //     lblUserId, _txtBookingUserIdFilter,
    //     lblUserType, _cbBookingUserType,
    //     lblStatus, _cbBookingStatus,
    //     _btnBookingSearch
    //     });

    //     // Grid booking
    //     _gridBookings = new DataGridView
    //     {
    //         Dock = DockStyle.Fill,
    //         ReadOnly = true,
    //         AllowUserToAddRows = false,
    //         AllowUserToDeleteRows = false,
    //         AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    //         SelectionMode = DataGridViewSelectionMode.FullRowSelect
    //     };
    //     _tabBookingDetail.Controls.Add(_gridBookings);

    //     // Bottom: Export
    //     var pnlBottom = new Panel
    //     {
    //         Dock = DockStyle.Bottom,
    //         Height = 40
    //     };
    //     _tabBookingDetail.Controls.Add(pnlBottom);

    //     _btnBookingExportExcel = new Button
    //     {
    //         Left = 10,
    //         Top = 8,
    //         Width = 120,
    //         Text = "Export Excel"
    //     };
    //     _btnBookingExportPdf = new Button
    //     {
    //         Left = 140,
    //         Top = 8,
    //         Width = 120,
    //         Text = "Export PDF"
    //     };
    //     // TODO: gắn event export file
    //     pnlBottom.Controls.AddRange(new Control[]
    //     {
    //     _btnBookingExportExcel, _btnBookingExportPdf
    //     });
    // }

    //     private void BuildTabBookingDetail()
    // {
    //     _tabRightBookings.Controls.Clear();
    //     _tabRightBookings.AutoScroll = true;

    //     var main = new TableLayoutPanel
    //     {
    //         Dock = DockStyle.Fill,
    //         ColumnCount = 1,
    //         RowCount = 3
    //     };
    //     main.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // filter
    //     main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));      // grid
    //     main.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // bottom buttons

    //     _tabRightBookings.Controls.Add(main, 0, 0);

    //     // ===================== HÀNG FILTER TRÊN =====================
    //     var filterPanel = new FlowLayoutPanel
    //     {
    //         Dock = DockStyle.Fill,
    //         AutoSize = true,
    //         WrapContents = true
    //     };

    //     main.Controls.Add(filterPanel, 0, 0);

    //     // From date
    //     filterPanel.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(5, 8, 3, 3) });
    //     _dtBookingFrom = new DateTimePicker
    //     {
    //         Width = 120,
    //         Value = DateTime.Today.AddDays(-7)
    //     };
    //     filterPanel.Controls.Add(_dtBookingFrom);

    //     // To date
    //     filterPanel.Controls.Add(new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
    //     _dtBookingTo = new DateTimePicker
    //     {
    //         Width = 120,
    //         Value = DateTime.Today
    //     };
    //     filterPanel.Controls.Add(_dtBookingTo);

    //     // Room
    //     filterPanel.Controls.Add(new Label { Text = "Room:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
    //     _cbBookingRoom = new ComboBox
    //     {
    //         DropDownStyle = ComboBoxStyle.DropDownList,
    //         Width = 100
    //     };
    //     _cbBookingRoom.Items.Add("ALL");
    //     foreach (var roomId in _state.RoomsInfo.Keys.OrderBy(r => r))
    //         _cbBookingRoom.Items.Add(roomId);
    //     _cbBookingRoom.SelectedIndex = 0;
    //     filterPanel.Controls.Add(_cbBookingRoom);

    //     // UserId
    //     filterPanel.Controls.Add(new Label { Text = "UserId:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
    //     _txtBookingUserId = new TextBox
    //     {
    //         Width = 100
    //     };
    //     filterPanel.Controls.Add(_txtBookingUserId);

    //     // UserType
    //     filterPanel.Controls.Add(new Label { Text = "UserType:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
    //     _cbBookingUserType = new ComboBox
    //     {
    //         DropDownStyle = ComboBoxStyle.DropDownList,
    //         Width = 110
    //     };
    //     _cbBookingUserType.Items.AddRange(new object[]
    //     {
    //         "ALL", "Student", "Lecturer", "Staff"
    //     });
    //     _cbBookingUserType.SelectedIndex = 0;
    //     filterPanel.Controls.Add(_cbBookingUserType);

    //     // Status
    //     filterPanel.Controls.Add(new Label { Text = "Status:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
    //     _cbBookingStatus = new ComboBox
    //     {
    //         DropDownStyle = ComboBoxStyle.DropDownList,
    //         Width = 120
    //     };
    //     _cbBookingStatus.Items.AddRange(new object[]
    //     {
    //         "ALL",
    //         "APPROVED",
    //         "IN_USE",
    //         "COMPLETED",
    //         "CANCELLED",
    //         "NO_SHOW"
    //     });
    //     _cbBookingStatus.SelectedIndex = 0;
    //     filterPanel.Controls.Add(_cbBookingStatus);

    //     // Button search
    //     _btnBookingSearch = new Button
    //     {
    //         Text = "Lọc",
    //         AutoSize = true,
    //         Margin = new Padding(15, 4, 3, 3)
    //     };
    //     _btnBookingSearch.Click += (s, e) => ReloadBookingGrid();
    //     filterPanel.Controls.Add(_btnBookingSearch);

    //     // ===================== GRID CHÍNH =====================
    //     _gridBookings = new DataGridView
    //     {
    //         Dock = DockStyle.Fill,
    //         ReadOnly = true,
    //         AllowUserToAddRows = false,
    //         AllowUserToDeleteRows = false,
    //         SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    //         AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
    //     };
    //     _gridBookings.AutoGenerateColumns = false;

    //     // Tạo cột
    //     void AddCol(string name, string header, int fillWeight = 100)
    //     {
    //         _gridBookings.Columns.Add(new DataGridViewTextBoxColumn
    //         {
    //             DataPropertyName = name,
    //             HeaderText = header,
    //             Name = name,
    //             ReadOnly = true
    //         });
    //     }

    //     AddCol(nameof(BookingView.BookingId), "BookingId");
    //     AddCol(nameof(BookingView.UserId), "UserId");
    //     AddCol(nameof(BookingView.FullName), "FullName");
    //     AddCol(nameof(BookingView.UserType), "UserType");
    //     AddCol(nameof(BookingView.Email), "Email");
    //     AddCol(nameof(BookingView.Phone), "Phone");
    //     AddCol(nameof(BookingView.RoomId), "RoomId");
    //     AddCol(nameof(BookingView.Date), "Date");
    //     AddCol(nameof(BookingView.SlotStartId), "SlotStartId");
    //     AddCol(nameof(BookingView.SlotEndId), "SlotEndId");
    //     AddCol(nameof(BookingView.TimeRange), "TimeRange");
    //     AddCol(nameof(BookingView.IsRange), "IsRange");
    //     AddCol(nameof(BookingView.Purpose), "Purpose");
    //     AddCol(nameof(BookingView.Status), "Status");
    //     AddCol(nameof(BookingView.CheckinDeadline), "CheckinDeadline");
    //     AddCol(nameof(BookingView.CheckinTime), "CheckinTime");
    //     AddCol(nameof(BookingView.CreatedAt), "CreatedAt");
    //     AddCol(nameof(BookingView.UpdatedAt), "UpdatedAt");

    //     main.Controls.Add(_gridBookings, 0, 1);

    //     // Double click -> show detail
    //     _gridBookings.CellDoubleClick += GridBookings_CellDoubleClick;

    //     // ===================== HÀNG NÚT DƯỚI =====================
    //     var bottomPanel = new FlowLayoutPanel
    //     {
    //         Dock = DockStyle.Right,
    //         AutoSize = true,
    //         FlowDirection = FlowDirection.RightToLeft
    //     };

    //     _btnBookingExportPdf = new Button
    //     {
    //         Text = "Xuất PDF",
    //         AutoSize = true,
    //         Margin = new Padding(5)
    //     };
    //     _btnBookingExportPdf.Click += BtnBookingExportPdf_Click;

    //     _btnBookingExportExcel = new Button
    //     {
    //         Text = "Xuất Excel",
    //         AutoSize = true,
    //         Margin = new Padding(5)
    //     };
    //     _btnBookingExportExcel.Click += BtnBookingExportExcel_Click;

    //     bottomPanel.Controls.Add(_btnBookingExportPdf);
    //     bottomPanel.Controls.Add(_btnBookingExportExcel);

    //     main.Controls.Add(bottomPanel, 0, 2);

    //     // Lần đầu load
    //     ReloadBookingGrid();
    // }
    private void BuildTabBookingDetail()
    {
        _tabBookingDetail.Controls.Clear();
        _tabBookingDetail.AutoScroll = true;

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // filter
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));      // grid
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // bottom buttons

        // TabPage chỉ cần Add(control), không có tham số row/column
        _tabBookingDetail.Controls.Add(main);

        // ===================== HÀNG FILTER TRÊN =====================
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };

        main.Controls.Add(filterPanel, 0, 0);

        // From date
        filterPanel.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(5, 10, 3, 3) });
        _dtBookingFrom = new DateTimePicker
        {
            Width = 95,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Value = DateTime.Today.AddDays(-7),
            ShowUpDown = false  // nếu thích dạng spinner thì để true
        };
        filterPanel.Controls.Add(_dtBookingFrom);

        // To date
        filterPanel.Controls.Add(new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(10, 10, 3, 3) });
        _dtBookingTo = new DateTimePicker
        {
            Width = 95,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Value = DateTime.Today,
            ShowUpDown = false
        };
        filterPanel.Controls.Add(_dtBookingTo);

        // Room
        filterPanel.Controls.Add(new Label { Text = "Room:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
        _cbBookingRoom = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 100
        };
        _cbBookingRoom.Items.Add("ALL");
        foreach (var roomId in _state.RoomsInfo.Keys.OrderBy(r => r))
            _cbBookingRoom.Items.Add(roomId);
        _cbBookingRoom.SelectedIndex = 0;
        filterPanel.Controls.Add(_cbBookingRoom);

        // UserId
        filterPanel.Controls.Add(new Label { Text = "UserId:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
        _txtBookingUserId = new TextBox
        {
            Width = 100
        };
        filterPanel.Controls.Add(_txtBookingUserId);

        // UserType
        filterPanel.Controls.Add(new Label { Text = "UserType:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
        _cbBookingUserType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 110
        };
        _cbBookingUserType.Items.AddRange(new object[]
        {
        "ALL", "Student", "Lecturer", "Staff"
        });
        _cbBookingUserType.SelectedIndex = 0;
        filterPanel.Controls.Add(_cbBookingUserType);

        // Status
        filterPanel.Controls.Add(new Label { Text = "Status:", AutoSize = true, Margin = new Padding(10, 8, 3, 3) });
        _cbBookingStatus = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 120
        };
        _cbBookingStatus.Items.AddRange(new object[]
        {
        "ALL",
        "APPROVED",
        "IN_USE",
        "COMPLETED",
        "CANCELLED",
        "NO_SHOW"
        });
        _cbBookingStatus.SelectedIndex = 0;
        filterPanel.Controls.Add(_cbBookingStatus);

        // Button search
        _btnBookingSearch = new Button
        {
            Text = "Lọc",
            AutoSize = true,
            Margin = new Padding(15, 4, 3, 3)
        };
        _btnBookingSearch.Click += (s, e) => ReloadBookingGrid();
        filterPanel.Controls.Add(_btnBookingSearch);

        // ===================== GRID CHÍNH =====================
        _gridBookings = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        };
        _gridBookings.AutoGenerateColumns = false;

        void AddCol(string name, string header)
        {
            _gridBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = name,
                HeaderText = header,
                Name = name,
                ReadOnly = true
            });
        }

        AddCol(nameof(BookingView.BookingId), "BookingId");
        AddCol(nameof(BookingView.UserId), "UserId");
        AddCol(nameof(BookingView.FullName), "FullName");
        AddCol(nameof(BookingView.UserType), "UserType");
        AddCol(nameof(BookingView.Email), "Email");
        AddCol(nameof(BookingView.Phone), "Phone");
        AddCol(nameof(BookingView.RoomId), "RoomId");
        AddCol(nameof(BookingView.Date), "Date");
        AddCol(nameof(BookingView.SlotStartId), "SlotStartId");
        AddCol(nameof(BookingView.SlotEndId), "SlotEndId");
        AddCol(nameof(BookingView.TimeRange), "TimeRange");
        AddCol(nameof(BookingView.IsRange), "IsRange");
        AddCol(nameof(BookingView.Purpose), "Purpose");
        AddCol(nameof(BookingView.Status), "Status");
        AddCol(nameof(BookingView.CheckinDeadline), "CheckinDeadline");
        AddCol(nameof(BookingView.CheckinTime), "CheckinTime");
        AddCol(nameof(BookingView.CreatedAt), "CreatedAt");
        AddCol(nameof(BookingView.UpdatedAt), "UpdatedAt");

        main.Controls.Add(_gridBookings, 0, 1);

        _gridBookings.CellDoubleClick += GridBookings_CellDoubleClick;

        // ===================== HÀNG NÚT DƯỚI =====================
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnBookingExportPdf = new Button
        {
            Text = "Xuất PDF",
            AutoSize = true,
            Margin = new Padding(5)
        };
        _btnBookingExportPdf.Click += BtnBookingExportPdf_Click;

        _btnBookingExportExcel = new Button
        {
            Text = "Xuất Excel",
            AutoSize = true,
            Margin = new Padding(5)
        };
        _btnBookingExportExcel.Click += BtnBookingExportExcel_Click;

        bottomPanel.Controls.Add(_btnBookingExportPdf);
        bottomPanel.Controls.Add(_btnBookingExportExcel);

        main.Controls.Add(bottomPanel, 0, 2);

        // Lần đầu load
        ReloadBookingGrid();
    }

    private void ReloadBookingGrid()
    {
        var all = _state.GetBookingViews();

        var from = _dtBookingFrom.Value.Date;
        var to = _dtBookingTo.Value.Date;
        if (to < from)
        {
            // Nếu user chọn nhầm, đảo lại cho an toàn
            var tmp = from;
            from = to;
            to = tmp;
        }

        string roomFilter = _cbBookingRoom.SelectedItem?.ToString() ?? "ALL";
        string userIdFilter = _txtBookingUserId.Text.Trim();
        string userTypeFilter = _cbBookingUserType.SelectedItem?.ToString() ?? "ALL";
        string statusFilter = _cbBookingStatus.SelectedItem?.ToString() ?? "ALL";

        // Logic: nếu UserId != rỗng -> lọc theo UserId.
        // Nếu UserId rỗng thì mới xét UserType.
        var query = all.Where(b =>
        {
            if (!DateTime.TryParse(b.Date, out var d))
                return false;

            d = d.Date;
            if (d < from || d > to)
                return false;

            if (roomFilter != "ALL" && b.RoomId != roomFilter)
                return false;

            if (!string.IsNullOrEmpty(userIdFilter))
            {
                if (!b.UserId.Contains(userIdFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (userTypeFilter != "ALL")
            {
                if (!string.Equals(b.UserType, userTypeFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (statusFilter != "ALL")
            {
                if (!string.Equals(b.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }).ToList();

        _gridBookings.DataSource = query;
    }
    private void GridBookings_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        var row = _gridBookings.Rows[e.RowIndex];
        if (row?.DataBoundItem is not BookingView view)
            return;

        ShowBookingDetailDialog(view);
    }

    private void ShowBookingDetailDialog(BookingView b)
    {
        var dlg = new Form
        {
            Text = $"Booking detail - {b.BookingId}",
            Width = 600,
            Height = 500,
            StartPosition = FormStartPosition.CenterParent
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

        dlg.Controls.Add(table);

        void AddRow(string label, string value)
        {
            int row = table.RowCount;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowCount++;

            table.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5)
            }, 0, row);

            table.Controls.Add(new TextBox
            {
                Text = value,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            }, 1, row);
        }

        AddRow("BookingId", b.BookingId.ToString());
        AddRow("UserId", b.UserId);
        AddRow("FullName", b.FullName);
        AddRow("UserType", b.UserType);
        AddRow("Email", b.Email);
        AddRow("Phone", b.Phone);

        AddRow("RoomId", b.RoomId);
        AddRow("Date", b.Date);
        AddRow("SlotStartId", b.SlotStartId);
        AddRow("SlotEndId", b.SlotEndId);
        AddRow("TimeRange", b.TimeRange);
        AddRow("IsRange", b.IsRange ? "Yes" : "No");

        AddRow("Purpose", b.Purpose);
        AddRow("Status", b.Status);

        AddRow("CheckinDeadline", b.CheckinDeadline?.ToString("dd/MM/yyyy HH:mm") ?? "");
        AddRow("CheckinTime", b.CheckinTime?.ToString("dd/MM/yyyy HH:mm") ?? "");

        AddRow("CreatedAt", b.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"));
        AddRow("UpdatedAt", b.UpdatedAt.ToString("dd/MM/yyyy HH:mm:ss"));

        dlg.ShowDialog(this);
    }
    private List<BookingView> GetCurrentBookingGridData()
    {
        if (_gridBookings.DataSource is List<BookingView> list)
            return list;

        // fallback nếu DataSource là kiểu khác
        return _gridBookings.Rows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as BookingView)
            .Where(v => v != null)
            .Cast<BookingView>()
            .ToList();
    }
    private void BtnBookingExportExcel_Click(object? sender, EventArgs e)
    {
        var data = GetCurrentBookingGridData();
        if (data.Count == 0)
        {
            MessageBox.Show(this, "Không có dữ liệu để xuất.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"Booking_{_state.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",",
            "BookingId",
            "UserId",
            "FullName",
            "UserType",
            "Email",
            "Phone",
            "RoomId",
            "Date",
            "SlotStartId",
            "SlotEndId",
            "TimeRange",
            "IsRange",
            "Purpose",
            "Status",
            "CheckinDeadline",
            "CheckinTime",
            "CreatedAt",
            "UpdatedAt"));

        // Rows
        foreach (var b in data)
        {
            string Esc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

            sb.AppendLine(string.Join(",",
                Esc(b.BookingId.ToString()),
                Esc(b.UserId),
                Esc(b.FullName),
                Esc(b.UserType),
                Esc(b.Email),
                Esc(b.Phone),
                Esc(b.RoomId),
                Esc(b.Date),
                Esc(b.SlotStartId),
                Esc(b.SlotEndId),
                Esc(b.TimeRange),
                Esc(b.IsRange ? "1" : "0"),
                Esc(b.Purpose),
                Esc(b.Status),
                Esc(b.CheckinDeadline?.ToString("dd/MM/yyyy HH:mm") ?? ""),
                Esc(b.CheckinTime?.ToString("dd/MM/yyyy HH:mm") ?? ""),
                Esc(b.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")),
                Esc(b.UpdatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
            ));
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show(this, "Đã xuất CSV (mở bằng Excel).", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private void BtnBookingExportPdf_Click(object? sender, EventArgs e)
    {
        var data = GetCurrentBookingGridData();
        if (data.Count == 0)
        {
            MessageBox.Show(this, "Không có dữ liệu để xuất.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"Booking_{_state.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("BOOKING REPORT");
        sb.AppendLine($"Generated at: {_state.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine(new string('=', 80));

        foreach (var b in data)
        {
            sb.AppendLine($"BookingId       : {b.BookingId}");
            sb.AppendLine($"User            : {b.UserId} - {b.FullName} ({b.UserType})");
            sb.AppendLine($"Email / Phone   : {b.Email} / {b.Phone}");
            sb.AppendLine($"Room / Date     : {b.RoomId} / {b.Date}");
            sb.AppendLine($"Slot            : {b.SlotStartId} -> {b.SlotEndId} ({b.TimeRange})");
            sb.AppendLine($"IsRange         : {(b.IsRange ? "Yes" : "No")}");
            sb.AppendLine($"Purpose         : {b.Purpose}");
            sb.AppendLine($"Status          : {b.Status}");
            sb.AppendLine($"CheckinDeadline : {b.CheckinDeadline?.ToString("dd/MM/yyyy HH:mm") ?? ""}");
            sb.AppendLine($"CheckinTime     : {b.CheckinTime?.ToString("dd/MM/yyyy HH:mm") ?? ""}");
            sb.AppendLine($"CreatedAt       : {b.CreatedAt:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"UpdatedAt       : {b.UpdatedAt:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine(new string('-', 80));
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show(this, "Đã xuất report dạng text. Sau này có thể thay bằng thư viện PDF.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BuildTabStatistics()
    {
        // Filter ngày dùng chung
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55
        };
        _tabStatistics.Controls.Add(pnlFilter);

        var lblFrom = new Label { Left = 10, Top = 12, Width = 60, Text = "Từ ngày:" };
        _dtStatsFrom = new DateTimePicker
        {
            Left = 70,
            Top = 8,
            Width = 110,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };

        var lblTo = new Label { Left = 190, Top = 12, Width = 60, Text = "Đến:" };
        _dtStatsTo = new DateTimePicker
        {
            Left = 240,
            Top = 8,
            Width = 110,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        };

        _dtStatsTo.Value = DateTime.Today;
        _dtStatsFrom.Value = DateTime.Today.AddDays(-7);

        var btnRefreshStats = new Button
        {
            Left = 370,
            Top = 8,
            Width = 80,
            Text = "Refresh"
        };

        btnRefreshStats.Click += (s, e) => ReloadStatistics();

        pnlFilter.Controls.AddRange(new Control[]
        {
            lblFrom, _dtStatsFrom,
            lblTo, _dtStatsTo,
            btnRefreshStats
        });

        // Group: Room stats
        var grpRoom = new GroupBox
        {
            Text = "Statistics by room",
            Left = 10,
            Top = 55,
            Width = 540,
            Height = 540
        };
        _tabStatistics.Controls.Add(grpRoom);

        _gridRoomStats = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        grpRoom.Controls.Add(_gridRoomStats);

        // Group: UserType stats
        var grpUserType = new GroupBox
        {
            Text = "Statistics by user type",
            Left = 560,
            Top = 55,
            Width = 300,
            Height = 540
        };
        _tabStatistics.Controls.Add(grpUserType);

        _gridUserTypeStats = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        grpUserType.Controls.Add(_gridUserTypeStats);

        ReloadStatistics();

    }

    // Reload lại 2 grid thống kê dựa trên khoảng ngày
    private void ReloadStatistics()
    {
        if (_dtStatsFrom == null || _dtStatsTo == null)
            return;

        var from = _dtStatsFrom.Value.Date;
        var to = _dtStatsTo.Value.Date;

        // Nếu user chọn nhầm (to < from) thì đảo lại
        if (to < from)
        {
            var tmp = from;
            from = to;
            to = tmp;
        }

        // ===== 1) THỐNG KÊ THEO PHÒNG =====
        var roomStats = _state.GetRoomStatistics(from, to);

        // Map thêm cột % NoShow
        var roomView = roomStats.Select(r => new
        {
            r.RoomId,
            r.TotalBookings,
            r.NoShowCount,
            r.CancelledCount,
            // Tỉ lệ NoShow % = NoShow / Total * 100
            NoShowPercent = r.TotalBookings > 0
                ? Math.Round(r.NoShowCount * 100.0 / r.TotalBookings, 1)
                : 0.0
        }).ToList();

        _gridRoomStats.AutoGenerateColumns = false;
        _gridRoomStats.Columns.Clear();

        var colRoomId = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "RoomId",
            HeaderText = "Room",
            Width = 80
        };
        var colTotal = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TotalBookings",
            HeaderText = "Total",
            Width = 60
        };
        var colNoShow = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "NoShowCount",
            HeaderText = "NoShow",
            Width = 60
        };
        var colCancelled = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "CancelledCount",
            HeaderText = "Cancelled",
            Width = 60
        };
        var colNoShowPercent = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "NoShowPercent",
            HeaderText = "NoShow %",
            Width = 80,
            DefaultCellStyle = { Format = "N1" } // 1 chữ số thập phân
        };

        _gridRoomStats.Columns.AddRange(
            colRoomId,
            colTotal,
            colNoShow,
            colCancelled,
            colNoShowPercent
        );

        _gridRoomStats.DataSource = roomView;

        // ===== 2) THỐNG KÊ THEO LOẠI USER =====
        var userStats = _state.GetUserTypeStatistics(from, to);

        var userView = userStats.Select(u => new
        {
            u.UserType,
            u.TotalBookings,
            u.NoShowCount,
            NoShowPercent = u.TotalBookings > 0
                ? Math.Round(u.NoShowCount * 100.0 / u.TotalBookings, 1)
                : 0.0
        }).ToList();

        _gridUserTypeStats.AutoGenerateColumns = false;
        _gridUserTypeStats.Columns.Clear();

        var colUserType = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "UserType",
            HeaderText = "User type",
            Width = 100
        };
        var colUTTotal = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TotalBookings",
            HeaderText = "Total",
            Width = 60
        };
        var colUTNoShow = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "NoShowCount",
            HeaderText = "NoShow",
            Width = 60
        };
        var colUTNoShowPercent = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "NoShowPercent",
            HeaderText = "NoShow %",
            Width = 80,
            DefaultCellStyle = { Format = "N1" }
        };

        _gridUserTypeStats.Columns.AddRange(
            colUserType,
            colUTTotal,
            colUTNoShow,
            colUTNoShowPercent
        );

        _gridUserTypeStats.DataSource = userView;
    }


    // private void BuildTabSettings()
    // {
    //     // Group: Cấu hình ca học
    //     var grpSlots = new GroupBox
    //     {
    //         Text = "Slot time configuration",
    //         Left = 10,
    //         Top = 10,
    //         Width = 540,
    //         Height = 540
    //     };
    //     _tabSettings.Controls.Add(grpSlots);

    //     _gridSlotConfig = new DataGridView
    //     {
    //         Dock = DockStyle.Fill,
    //         AllowUserToAddRows = false,
    //         AllowUserToDeleteRows = false,
    //         AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    //     };
    //     // TODO: bind 14 dòng: Ca, StartTime, EndTime
    //     grpSlots.Controls.Add(_gridSlotConfig);

    //     // Group: General settings
    //     var grpGeneral = new GroupBox
    //     {
    //         Text = "General settings",
    //         Left = 560,
    //         Top = 10,
    //         Width = 300,
    //         Height = 200
    //     };
    //     _tabSettings.Controls.Add(grpGeneral);

    //     var lblDeadline = new Label
    //     {
    //         Left = 10,
    //         Top = 25,
    //         Width = 200,
    //         Text = "Check-in deadline (minutes):"
    //     };
    //     _numCheckinDeadlineMinutes = new NumericUpDown
    //     {
    //         Left = 210,
    //         Top = 22,
    //         Width = 60,
    //         Minimum = 0,
    //         Maximum = 120,
    //         Value = 15
    //     };

    //     _chkSendEmailForce = new CheckBox
    //     {
    //         Left = 10,
    //         Top = 55,
    //         Width = 260,
    //         Text = "Send email on FORCE_GRANT/RELEASE"
    //     };

    //     _chkSendEmailNoShow = new CheckBox
    //     {
    //         Left = 10,
    //         Top = 80,
    //         Width = 260,
    //         Text = "Send email on NO_SHOW"
    //     };

    //     // TODO: nút Save / Load config nếu cần
    //     grpGeneral.Controls.AddRange(new Control[]
    //     {
    //     lblDeadline, _numCheckinDeadlineMinutes,
    //     _chkSendEmailForce, _chkSendEmailNoShow
    //     });
    // }
    private void BuildTabSettings()
    {
        _tabSettings.Controls.Clear();

        // ===== Group: Cấu hình ca học =====
        var grpSlots = new GroupBox
        {
            Text = "Slot time configuration",
            Left = 10,
            Top = 10,
            Width = 540,
            Height = 540
        };
        _tabSettings.Controls.Add(grpSlots);

        _gridSlotConfig = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        grpSlots.Controls.Add(_gridSlotConfig);

        // ===== Group: General settings =====
        var grpGeneral = new GroupBox
        {
            Text = "General settings",
            Left = 560,
            Top = 10,
            Width = 300,
            Height = 160
        };
        _tabSettings.Controls.Add(grpGeneral);

        var lblDeadline = new Label
        {
            Left = 10,
            Top = 25,
            Width = 200,
            Text = "Check-in deadline (minutes):"
        };
        _numCheckinDeadlineMinutes = new NumericUpDown
        {
            Left = 210,
            Top = 22,
            Width = 60,
            Minimum = 0,
            Maximum = 120,
            Value = 15
        };

        _chkSendEmailForce = new CheckBox
        {
            Left = 10,
            Top = 55,
            Width = 260,
            Text = "Send email on FORCE_GRANT/RELEASE"
        };

        _chkSendEmailNoShow = new CheckBox
        {
            Left = 10,
            Top = 80,
            Width = 260,
            Text = "Send email on NO_SHOW"
        };

        _chkNotifyClient = new CheckBox
        {
            Left = 10,
            Top = 105,
            Width = 260,
            Text = "Send notification to client"
        };

        grpGeneral.Controls.AddRange(new Control[]
        {
            lblDeadline, _numCheckinDeadlineMinutes,
            _chkSendEmailForce, _chkSendEmailNoShow, _chkNotifyClient
        });

        // ===== Group: SMTP settings =====
        var grpSmtp = new GroupBox
        {
            Text = "SMTP / Email server",
            Left = 560,
            Top = 180,
            Width = 300,
            Height = 220
        };
        _tabSettings.Controls.Add(grpSmtp);

        var lblHost = new Label { Left = 10, Top = 25, Width = 80, Text = "Host:" };
        _txtSmtpHost = new TextBox { Left = 90, Top = 22, Width = 180 };

        var lblPort = new Label { Left = 10, Top = 55, Width = 80, Text = "Port:" };
        _numSmtpPort = new NumericUpDown
        {
            Left = 90,
            Top = 52,
            Width = 80,
            Minimum = 1,
            Maximum = 65535,
            Value = 587
        };

        _chkSmtpSsl = new CheckBox
        {
            Left = 180,
            Top = 54,
            Width = 80,
            Text = "SSL"
        };

        var lblUser = new Label { Left = 10, Top = 85, Width = 80, Text = "User:" };
        _txtSmtpUser = new TextBox { Left = 90, Top = 82, Width = 180 };

        var lblPwd = new Label { Left = 10, Top = 115, Width = 80, Text = "Password:" };
        _txtSmtpPassword = new TextBox
        {
            Left = 90,
            Top = 112,
            Width = 180,
            UseSystemPasswordChar = true
        };

        var lblFrom = new Label { Left = 10, Top = 145, Width = 80, Text = "From:" };
        _txtSmtpFrom = new TextBox { Left = 90, Top = 142, Width = 180 };

        grpSmtp.Controls.AddRange(new Control[]
        {
            lblHost, _txtSmtpHost,
            lblPort, _numSmtpPort, _chkSmtpSsl,
            lblUser, _txtSmtpUser,
            lblPwd, _txtSmtpPassword,
            lblFrom, _txtSmtpFrom
        });

        // ===== Nút Save =====
        _btnSettingsSave = new Button
        {
            Text = "Save settings",
            Left = 560,
            Top = 540,   // bên dưới nhóm Time một chút
            Width = 150,
            Height = 30
        };
        _btnSettingsSave.Click += BtnSettingsSave_Click;
        _tabSettings.Controls.Add(_btnSettingsSave);

        // ===== Group: Demo time (đặt dưới SMTP) =====
        var grpTime = new GroupBox
        {
            Text = "Demo time",
            Left = 560,
            Top = 410,   // ngay dưới grpSmtp (top 180 + height 220 = 400, cộng thêm 10px)
            Width = 300,
            Height = 120
        };
        _tabSettings.Controls.Add(grpTime);

        var dtDemo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy HH:mm",
            Width = 160,
            Left = 10,
            Top = 25
        };
        grpTime.Controls.Add(dtDemo);

        var btnUseDemo = new Button
        {
            Text = "Use demo time",
            Left = 180,
            Top = 22,
            Width = 110
        };
        grpTime.Controls.Add(btnUseDemo);

        var btnUseSystem = new Button
        {
            Text = "Use system time",
            Left = 180,
            Top = 55,
            Width = 110
        };
        grpTime.Controls.Add(btnUseSystem);

        btnUseDemo.Click += (_, __) =>
        {
            var logger = new UiLogger(this);
            _state.SetDemoNow(dtDemo.Value, logger);
        };

        btnUseSystem.Click += (_, __) =>
        {
            var logger = new UiLogger(this);
            _state.ResetDemoNow(logger);
        };

        // ===== Load dữ liệu từ ServerState vào UI =====
        LoadSettingsToUi();
    }
    private void LoadSettingsToUi()
    {
        var s = _state.Settings ?? AppSettings.CreateDefault();

        // ---- Grid slot config: 14 dòng ----
        _gridSlotConfig.AutoGenerateColumns = false;
        _gridSlotConfig.Columns.Clear();
        _gridSlotConfig.Rows.Clear();

        var colIndex = new DataGridViewTextBoxColumn
        {
            Name = "colIndex",
            HeaderText = "Ca",
            Width = 40,
            ReadOnly = true
        };
        var colStart = new DataGridViewTextBoxColumn
        {
            Name = "colStart",
            HeaderText = "Start",
            Width = 80
        };
        var colEnd = new DataGridViewTextBoxColumn
        {
            Name = "colEnd",
            HeaderText = "End",
            Width = 80
        };

        _gridSlotConfig.Columns.AddRange(colIndex, colStart, colEnd);

        var list = (s.SlotTimes != null && s.SlotTimes.Count == 14)
            ? s.SlotTimes.OrderBy(r => r.Index).ToList()
            : AppSettings.CreateDefault().SlotTimes;

        foreach (var row in list)
        {
            _gridSlotConfig.Rows.Add(row.Index, row.Start, row.End);
        }

        // ---- General settings ----
        if (s.CheckinDeadlineMinutes >= 0 && s.CheckinDeadlineMinutes <= 120)
            _numCheckinDeadlineMinutes.Value = s.CheckinDeadlineMinutes;
        else
            _numCheckinDeadlineMinutes.Value = 15;

        _chkSendEmailForce.Checked = s.SendEmailOnForceGrantRelease;
        _chkSendEmailNoShow.Checked = s.SendEmailOnNoShow;
        _chkNotifyClient.Checked = s.SendNotificationToClient;

        // ---- SMTP ----
        _txtSmtpHost.Text = s.Smtp?.Host ?? "";
        _numSmtpPort.Value = s.Smtp?.Port > 0 ? s.Smtp.Port : 587;
        _chkSmtpSsl.Checked = s.Smtp?.EnableSsl ?? true;
        _txtSmtpUser.Text = s.Smtp?.Username ?? "";
        _txtSmtpPassword.Text = s.Smtp?.Password ?? "";
        _txtSmtpFrom.Text = s.Smtp?.FromEmail ?? "";
    }
    private void BtnSettingsSave_Click(object? sender, EventArgs e)
    {
        var newSettings = new AppSettings();

        // 1) đọc 14 dòng slot từ grid
        if (_gridSlotConfig.Rows.Count < 14)
        {
            MessageBox.Show("Slot config cần đủ 14 dòng (S1..S14).");
            return;
        }

        for (int i = 0; i < 14; i++)
        {
            var row = _gridSlotConfig.Rows[i];

            var idxObj = row.Cells["colIndex"].Value;
            int index = 0;
            if (idxObj == null || !int.TryParse(idxObj.ToString(), out index))
                index = i + 1;

            string startStr = row.Cells["colStart"].Value?.ToString() ?? "";
            string endStr = row.Cells["colEnd"].Value?.ToString() ?? "";

            if (!TimeSpan.TryParse(startStr, out var start) ||
                !TimeSpan.TryParse(endStr, out var end))
            {
                MessageBox.Show($"Dòng {index}: thời gian phải dạng HH:mm (vd 07:00).");
                return;
            }

            if (end <= start)
            {
                MessageBox.Show($"Dòng {index}: EndTime phải sau StartTime.");
                return;
            }

            newSettings.SlotTimes.Add(new SlotTimeConfigRow
            {
                Index = index,
                SlotId = $"S{index}",
                Start = start.ToString(@"hh\:mm"),
                End = end.ToString(@"hh\:mm")
            });
        }

        // 2) general
        newSettings.CheckinDeadlineMinutes = (int)_numCheckinDeadlineMinutes.Value;
        newSettings.SendEmailOnForceGrantRelease = _chkSendEmailForce.Checked;
        newSettings.SendEmailOnNoShow = _chkSendEmailNoShow.Checked;
        newSettings.SendNotificationToClient = _chkNotifyClient.Checked;

        // 3) SMTP
        newSettings.Smtp.Host = _txtSmtpHost.Text.Trim();
        newSettings.Smtp.Port = (int)_numSmtpPort.Value;
        newSettings.Smtp.EnableSsl = _chkSmtpSsl.Checked;
        newSettings.Smtp.Username = _txtSmtpUser.Text.Trim();
        newSettings.Smtp.Password = _txtSmtpPassword.Text;    // tạm lưu plain text
        newSettings.Smtp.FromEmail = _txtSmtpFrom.Text.Trim();

        _state.UpdateSettings(newSettings);

        MessageBox.Show(
            "Đã lưu Settings.\nThời gian ca & deadline mới sẽ áp dụng cho các booking mới.",
            "Settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }


    private void BuildTabLog()
    {
        _txtLog = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        _tabLogTab.Controls.Add(_txtLog);
    }

    private void DtDate_ValueChanged(object? sender, EventArgs e)
    {
        _state.SetCurrentDate(_dtDate.Value.Date, new UiLogger(this));
        RefreshSlotsSafe();
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_running) return;

        _listener = new TcpListener(IPAddress.Any, SERVER_TCP_PORT);
        _listener.Start();
        _running = true;
        Log($"[SERVER] Listening on TCP port {SERVER_TCP_PORT}...");

        // ✅ Start UDP discovery listener
        StartDiscoveryListener();

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
        _state.RunNoShowSweep(_state.Now, new UiLogger(this));  // ✅
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
                            {
                                // LOGIN|userId|password
                                if (parts.Length < 3)
                                {
                                    await SendAsync(stream, "LOGIN_FAIL|INVALID_FORMAT|Invalid LOGIN format\n");
                                    break;
                                }

                                var userId = parts[1];
                                var password = parts[2];

                                var result = _state.ValidateUserCredentials(userId, password);

                                if (!result.Success)
                                {
                                    string reasonCode;
                                    switch (result.Error)
                                    {
                                        case "User not found":
                                            reasonCode = "USER_NOT_FOUND";
                                            break;
                                        case "User inactive":
                                            reasonCode = "USER_INACTIVE";
                                            break;
                                        case "Invalid password":
                                            reasonCode = "INVALID_PASSWORD";
                                            break;
                                        default:
                                            reasonCode = "ERROR";
                                            break;
                                    }

                                    await SendAsync(stream, $"LOGIN_FAIL|{reasonCode}|{result.Error}\n");
                                    break;
                                }

                                // Lấy thêm thông tin user để trả về cho client
                                if (!_state.UsersInfo.TryGetValue(userId, out var user))
                                {
                                    await SendAsync(stream, "LOGIN_FAIL|ERROR|User data not found\n");
                                    break;
                                }

                                // Lưu lại clientId và loại user cho connection này
                                clientId = user.UserId;
                                currentUserType = user.UserType;

                                // LOGIN_OK|UserId|UserType|FullName|Email|Phone|StudentId|Class|Department|LecturerId|Faculty
                                var response =
                                    $"LOGIN_OK|{user.UserId}|{user.UserType}|{user.FullName}|" +
                                    $"{user.Email}|{user.Phone}|" +
                                    $"{user.StudentId}|{user.Class}|{user.Department}|" +
                                    $"{user.LecturerId}|{user.Faculty}\n";

                                await SendAsync(stream, response);
                                break;
                            }

                        case "FORGOT_PASSWORD":
                            {
                                // FORGOT_PASSWORD|email
                                if (parts.Length < 2)
                                {
                                    await SendAsync(stream, "FORGOT_FAIL|INVALID_FORMAT|Invalid FORGOT_PASSWORD format\n");
                                    break;
                                }

                                var email = parts[1];

                                // tìm user theo email (case-insensitive)

                                var user = _state.FindUserByEmail(email); if (user == null)
                                {
                                    await SendAsync(stream, "FORGOT_FAIL|EMAIL_NOT_FOUND|Email not found\n");
                                    break;
                                }

                                // sinh mật khẩu mới
                                var newPassword = GenerateRandomPassword(10);

                                // hash và lưu vào PasswordHash
                                var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
                                user.PasswordHash = hashed;

                                // gửi email
                                try
                                {
                                    SendResetEmail(user.Email, user.FullName, newPassword);
                                }
                                catch (Exception ex)
                                {
                                    await SendAsync(stream, $"FORGOT_FAIL|SEND_MAIL_ERROR|{ex.Message}\n");
                                    break;
                                }

                                await SendAsync(stream, "FORGOT_OK|Mật khẩu mới đã được gửi tới email của bạn.\n");
                                break;
                            }


                        case "REQUEST":
                            {
                                if (parts.Length != 4)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid REQUEST format\n");
                                    break;
                                }

                                var userIdInMsg = parts[1];

                                // Cho phép đặt clientId bằng userIdInMsg nếu connection này chưa có user
                                if (clientId == null)
                                {
                                    clientId = userIdInMsg;
                                }
                                else if (!string.Equals(clientId, userIdInMsg, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleRequest(clientId, parts[2], parts[3], stream, new UiLogger(this));
                                // RefreshSlotsSafe();
                                break;
                            }



                        case "RELEASE":
                            {
                                if (parts.Length != 4)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid RELEASE format\n");
                                    break;
                                }

                                var userIdInMsg = parts[1];

                                if (clientId == null)
                                {
                                    clientId = userIdInMsg;
                                }
                                else if (!string.Equals(clientId, userIdInMsg, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleRelease(clientId, parts[2], parts[3], stream, new UiLogger(this));
                                // RefreshSlotsSafe();
                                break;
                            }


                        case "REQUEST_RANGE":
                            {
                                // REQUEST_RANGE|UserId|RoomId|SlotStart|SlotEnd
                                if (parts.Length < 5)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid REQUEST_RANGE\n");
                                    break;
                                }

                                var userIdMsg = parts[1];
                                var roomId = parts[2];
                                var slotStart = parts[3];
                                var slotEnd = parts[4];

                                // Cho phép gán clientId = userIdMsg cho connection đầu tiên
                                if (clientId == null)
                                {
                                    clientId = userIdMsg;
                                }
                                else if (!string.Equals(userIdMsg, clientId, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleRequestRange(clientId, roomId, slotStart, slotEnd, stream, new UiLogger(this));
                                // RefreshSlotsSafe();
                                break;
                            }

                        case "RELEASE_RANGE":
                            {
                                // RELEASE_RANGE|UserId|RoomId|SlotStart|SlotEnd
                                if (parts.Length < 5)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid RELEASE_RANGE\n");
                                    break;
                                }

                                var userIdMsg = parts[1];
                                var roomId = parts[2];
                                var slotStart = parts[3];
                                var slotEnd = parts[4];

                                if (clientId == null)
                                {
                                    clientId = userIdMsg;
                                }
                                else if (!string.Equals(userIdMsg, clientId, StringComparison.OrdinalIgnoreCase))
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

                                // RefreshSlotsSafe();
                                break;
                            }


                        case "GET_NOW":
                            {
                                // Lấy thời gian theo ServerState.Now (có thể là demo time hoặc system time)
                                var now = _state.Now;
                                await SendAsync(stream, $"NOW|{now:yyyy-MM-dd HH:mm:ss}\n");
                                break;
                            }
                        case "PING":
                            await SendAsync(stream, "PONG\n");
                            break;
                        case "GET_HOME_DATA":
                            {
                                // Format client gửi: GET_HOME_DATA|<UserId>
                                if (parts.Length < 2)
                                {
                                    await SendAsync(stream, "ERR|MissingUserId\n");
                                    break;
                                }

                                var userId = parts[1];

                                // ✅ Gọi qua _state
                                var bookingsToday = _state.GetTodayBookingsForUser(userId);

                                // ✅ Dùng SendAsync của Form1
                                await SendAsync(stream, "HOME_DATA_BEGIN\n");

                                // Mỗi booking -> 1 dòng SCHEDULE
                                foreach (var b in bookingsToday)
                                {
                                    // Col1: TimeRange
                                    // Col2: RoomId
                                    // Col3: Môn/Mục đích
                                    // Col4: GV / Owner
                                    // Col5: Status
                                    // Col6: Extra (để trống tạm)

                                    var subjectOrPurpose = string.IsNullOrWhiteSpace(b.Purpose)
                                        ? "(No purpose)"
                                        : b.Purpose;

                                    var teacherOrOwner = string.IsNullOrWhiteSpace(b.FullName)
                                        ? b.UserId
                                        : b.FullName;

                                    var lineSchedule =
                                        $"SCHEDULE|{b.TimeRange}|{b.RoomId}|{subjectOrPurpose}|{teacherOrOwner}|{b.Status}|";

                                    await SendAsync(stream, lineSchedule + "\n");
                                }

                                // Thông báo demo từ booking hôm nay (tạm thời)
                                foreach (var b in bookingsToday.Take(3))
                                {
                                    // ⚠️ Đừng dùng lại tên biến msg để khỏi trùng với msg ở ngoài
                                    var noti =
                                        $"NOTI|Booking {b.RoomId} {b.TimeRange} ngày {b.Date:dd/MM/yyyy} - trạng thái: {b.Status}";
                                    await SendAsync(stream, noti + "\n");
                                }

                                await SendAsync(stream, "HOME_DATA_END\n");
                                break;
                            }

                        case "GET_SLOT_CONFIG":
                            {
                                // Không cần auth cho lệnh này
                                var rows = _state.GetSlotTimeConfigs();

                                foreach (var r in rows)
                                {
                                    // SLOT|S1|07:00|08:00
                                    await SendAsync(stream, $"SLOT|{r.SlotId}|{r.Start}|{r.End}\n");
                                }

                                await SendAsync(stream, "END_SLOT_CONFIG\n");
                                break;
                            }

                        case "GET_MY_SCHEDULE":
                            {
                                // parts: [0] = GET_MY_SCHEDULE, [1] = userId, [2] = fromDate, [3] = toDate
                                if (parts.Length < 4)
                                {
                                    await SendAsync(stream, "ERROR|INVALID_ARGS\n");
                                    break;
                                }

                                var userId = parts[1];
                                if (!DateTime.TryParse(parts[2], out var fromDate))
                                    fromDate = DateTime.Today;
                                if (!DateTime.TryParse(parts[3], out var toDate))
                                    toDate = fromDate;

                                var list = _state.GetUserSchedule(userId, fromDate, toDate);

                                var sb = new StringBuilder();
                                sb.AppendLine("MY_SCHEDULE_BEGIN");
                                foreach (var b in list)
                                {
                                    // ITEM|Date|RoomId|SlotStartId|SlotEndId|TimeRange|Status|Purpose
                                    sb.AppendLine(
                                        $"ITEM|{b.Date}|{b.RoomId}|{b.SlotStartId}|{b.SlotEndId}|{b.TimeRange}|{b.Status}|{b.Purpose}");
                                }
                                sb.AppendLine("MY_SCHEDULE_END");

                                await SendAsync(stream, sb.ToString());
                                break;
                            }

                        case "UPDATE_CONTACT":
                            {
                                // UPDATE_CONTACT|UserId|Email|Phone
                                if (parts.Length < 4)
                                {
                                    await SendAsync(stream, "ERR|Invalid UPDATE_CONTACT format\n");
                                    break;
                                }

                                var userId = parts[1];
                                var email = parts[2];
                                var phone = parts[3];

                                if (_state.UpdateUserContact(userId, email, phone, out var error))
                                {
                                    await SendAsync(stream, "OK\n");
                                }
                                else
                                {
                                    await SendAsync(stream, "ERR|" + error + "\n");
                                }

                                break;
                            }
                        case "CHANGE_PASSWORD":
                            {
                                // CHANGE_PASSWORD|UserId|OldPwd|NewPwd
                                if (parts.Length < 4)
                                {
                                    await SendAsync(stream, "ERR|Invalid CHANGE_PASSWORD format\n");
                                    break;
                                }

                                var userId = parts[1];
                                var oldPwd = parts[2];
                                var newPwd = parts[3];

                                if (_state.ChangeUserPassword(userId, oldPwd, newPwd, out var error))
                                {
                                    await SendAsync(stream, "OK\n");
                                }
                                else
                                {
                                    await SendAsync(stream, "ERR|" + error + "\n");
                                }

                                break;
                            }
                        case "GET_ROOMS":
                            {
                                // Lấy tất cả phòng thật từ ServerState
                                var rooms = _state.RoomsInfo.Values;

                                // Serialize JSON để client đọc được đầy đủ thông tin
                                var json = System.Text.Json.JsonSerializer.Serialize(rooms);

                                await SendAsync(stream, "ROOMS_BEGIN\n");
                                await SendAsync(stream, json + "\n");
                                await SendAsync(stream, "ROOMS_END\n");

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
                    // RefreshSlotsSafe();
                }
            }
        }
    }

    private void StartDiscoveryListener()
    {
        // Chạy trên 1 Task riêng
        Task.Run(async () =>
        {
            try
            {
                using (var udp = new UdpClient(DISCOVERY_UDP_PORT))
                {
                    Log($"[DISCOVERY] UDP listening on port {DISCOVERY_UDP_PORT}...");

                    while (_running)
                    {
                        UdpReceiveResult result;
                        try
                        {
                            result = await udp.ReceiveAsync();
                        }
                        catch (ObjectDisposedException)
                        {
                            break; // socket đã bị dispose khi đóng app
                        }

                        var request = Encoding.UTF8.GetString(result.Buffer).Trim();
                        if (request == "DISCOVER_BOOKING_SERVER")
                        {
                            // Lấy IP "thật" của server (trên LAN)
                            var localIp = GetLocalIPv4() ?? "127.0.0.1";
                            var response = $"SERVER_INFO|{localIp}|{SERVER_TCP_PORT}";
                            var respBytes = Encoding.UTF8.GetBytes(response);
                            await udp.SendAsync(respBytes, respBytes.Length, result.RemoteEndPoint);

                            Log($"[DISCOVERY] Reply to {result.RemoteEndPoint} with {response}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("[DISCOVERY] Error: " + ex.Message);
            }
        });
    }

    private string? GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
        }
        return null;
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
    private void RefreshUserGrid(string? filterUserId = null)
    {
        if (_gridUsers == null) return;

        // Lấy list user từ ServerState
        var users = _state.GetUserViews();   // trả về List<UserInfo>

        // Lọc theo UserId nếu có keyword
        if (!string.IsNullOrWhiteSpace(filterUserId))
        {
            var keyword = filterUserId.Trim().ToLower();
            users = users
                .Where(u => !string.IsNullOrEmpty(u.UserId) &&
                            u.UserId.ToLower().Contains(keyword))
                .ToList();
        }

        _gridUsers.AutoGenerateColumns = false;
        _gridUsers.Columns.Clear();

        // Cột UserId
        var colUserId = new DataGridViewTextBoxColumn
        {
            Name = "UserId",
            DataPropertyName = "UserId",
            HeaderText = "UserId",
            Width = 120
        };
        _gridUsers.Columns.Add(colUserId);

        // Cột Họ tên
        var colFullName = new DataGridViewTextBoxColumn
        {
            Name = "FullName",
            DataPropertyName = "FullName",
            HeaderText = "Họ tên",
            Width = 150
        };
        _gridUsers.Columns.Add(colFullName);

        // Cột Type
        var colUserType = new DataGridViewTextBoxColumn
        {
            Name = "UserType",
            DataPropertyName = "UserType",
            HeaderText = "Type",
            Width = 80
        };
        _gridUsers.Columns.Add(colUserType);

        // Cột MSSV
        var colStudentId = new DataGridViewTextBoxColumn
        {
            Name = "StudentId",
            DataPropertyName = "StudentId",
            HeaderText = "MSSV",
            Width = 100
        };
        _gridUsers.Columns.Add(colStudentId);

        // Cột Mã GV
        var colLecturerId = new DataGridViewTextBoxColumn
        {
            Name = "LecturerId",
            DataPropertyName = "LecturerId",
            HeaderText = "Mã GV",
            Width = 100
        };
        _gridUsers.Columns.Add(colLecturerId);

        // Cột Email
        var colEmail = new DataGridViewTextBoxColumn
        {
            Name = "Email",
            DataPropertyName = "Email",
            HeaderText = "Email",
            Width = 150
        };
        _gridUsers.Columns.Add(colEmail);

        // Cột SĐT
        var colPhone = new DataGridViewTextBoxColumn
        {
            Name = "Phone",
            DataPropertyName = "Phone",
            HeaderText = "SĐT",
            Width = 100
        };
        _gridUsers.Columns.Add(colPhone);

        // Cột Active
        var colIsActive = new DataGridViewCheckBoxColumn
        {
            Name = "IsActive",
            DataPropertyName = "IsActive",
            HeaderText = "Active",
            Width = 60
        };
        _gridUsers.Columns.Add(colIsActive);

        // Bind data
        _gridUsers.DataSource = users;
    }
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
    private void BtnForceGrantRange_Click(object? sender, EventArgs e)
    {
        if (_gridSlots.CurrentRow == null)
        {
            MessageBox.Show(this, "Hãy chọn 1 phòng/ca bất kỳ trong bảng Slot (dùng để lấy RoomId & ngày).",
                "Force GRANT RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = _gridSlots.CurrentRow;
        var roomId = row.Cells["RoomId"].Value?.ToString();
        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Dòng đang chọn không hợp lệ (RoomId rỗng).",
                "Force GRANT RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var targetUserId = _txtForceUserId.Text.Trim();
        if (string.IsNullOrEmpty(targetUserId))
        {
            MessageBox.Show(this, "Target UserId không được trống.",
                "Force GRANT RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var slotStart = _cbForceSlotStart.SelectedItem?.ToString();
        var slotEnd = _cbForceSlotEnd.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(slotStart) || string.IsNullOrEmpty(slotEnd))
        {
            MessageBox.Show(this, "Hãy chọn đầy đủ SlotStart và SlotEnd.",
                "Force GRANT RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // kiểm tra start <= end (S1..S14)
        int sIdx = int.Parse(slotStart.Substring(1));
        int eIdx = int.Parse(slotEnd.Substring(1));
        if (eIdx < sIdx)
        {
            MessageBox.Show(this, "SlotEnd phải >= SlotStart.",
                "Force GRANT RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var date = _dtDate.Value.Date;
        var logger = new UiLogger(this);

        if (!_state.ForceGrantRangeFromServerUi(date, roomId, slotStart, slotEnd, targetUserId, logger, out var error))
        {
            MessageBox.Show(this, error, "Force GRANT RANGE failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log($"[ADMIN-UI] Force GRANT RANGE {targetUserId} -> {roomId} {slotStart}-{slotEnd} ({date:yyyy-MM-dd})");
        RefreshSlotsSafe();
        UpdateCheckinPanel();
    }

    private void BtnForceReleaseRange_Click(object? sender, EventArgs e)
    {
        if (_gridSlots.CurrentRow == null)
        {
            MessageBox.Show(this, "Hãy chọn 1 phòng/ca bất kỳ trong bảng Slot (dùng để lấy RoomId & ngày).",
                "Force RELEASE RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = _gridSlots.CurrentRow;
        var roomId = row.Cells["RoomId"].Value?.ToString();
        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Dòng đang chọn không hợp lệ (RoomId rỗng).",
                "Force RELEASE RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var slotStart = _cbForceSlotStart.SelectedItem?.ToString();
        var slotEnd = _cbForceSlotEnd.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(slotStart) || string.IsNullOrEmpty(slotEnd))
        {
            MessageBox.Show(this, "Hãy chọn đầy đủ SlotStart và SlotEnd.",
                "Force RELEASE RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int sIdx = int.Parse(slotStart.Substring(1));
        int eIdx = int.Parse(slotEnd.Substring(1));
        if (eIdx < sIdx)
        {
            MessageBox.Show(this, "SlotEnd phải >= SlotStart.",
                "Force RELEASE RANGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var date = _dtDate.Value.Date;
        var logger = new UiLogger(this);

        if (!_state.ForceReleaseRangeFromServerUi(date, roomId, slotStart, slotEnd, logger, out var error))
        {
            MessageBox.Show(this, error, "Force RELEASE RANGE failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Log($"[ADMIN-UI] Force RELEASE RANGE {roomId} {slotStart}-{slotEnd} ({date:yyyy-MM-dd})");
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

    private void BtnRoomAdd_Click(object? sender, EventArgs e)
    {
        var roomId = _txtRoomId.Text.Trim();
        var building = _txtRoomBuilding.Text.Trim();
        var capacity = (int)_numRoomCapacity.Value;
        var hasProj = _chkRoomProjector.Checked;
        var hasPC = _chkRoomPC.Checked;
        var hasAC = _chkRoomAC.Checked;
        var hasMic = _chkRoomMic.Checked;
        var status = _cbRoomStatus.SelectedItem?.ToString() ?? "ACTIVE";

        if (string.IsNullOrWhiteSpace(roomId))
        {
            MessageBox.Show(this, "RoomId không được trống.", "Add room",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var room = new RoomInfo
        {
            RoomId = roomId,
            Building = building,
            Capacity = capacity,
            HasProjector = hasProj,
            HasPC = hasPC,
            HasAirConditioner = hasAC,
            HasMic = hasMic,
            Status = status
        };

        if (!_state.CreateRoom(room, out var error))
        {
            MessageBox.Show(this, error, "Add room failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(this, "Đã thêm phòng.", "Add room",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshRoomGrid();
    }


    private void BtnRoomUpdate_Click(object? sender, EventArgs e)
    {
        var roomId = _txtRoomId.Text.Trim();
        var building = _txtRoomBuilding.Text.Trim();
        var capacity = (int)_numRoomCapacity.Value;
        var hasProj = _chkRoomProjector.Checked;
        var hasPC = _chkRoomPC.Checked;
        var hasAC = _chkRoomAC.Checked;
        var hasMic = _chkRoomMic.Checked;
        var status = _cbRoomStatus.SelectedItem?.ToString() ?? "ACTIVE";

        if (string.IsNullOrWhiteSpace(roomId))
        {
            MessageBox.Show(this, "RoomId không được trống.", "Update room",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var room = new RoomInfo
        {
            RoomId = roomId,
            Building = building,
            Capacity = capacity,
            HasProjector = hasProj,
            HasPC = hasPC,
            HasAirConditioner = hasAC,
            HasMic = hasMic,
            Status = status
        };

        if (!_state.UpdateRoom(room, out var error))
        {
            MessageBox.Show(this, error, "Update room failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(this, "Đã cập nhật phòng.", "Update room",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshRoomGrid();
    }

    private void BtnRoomDelete_Click(object? sender, EventArgs e)
    {
        var roomId = _txtRoomId.Text.Trim();
        if (string.IsNullOrWhiteSpace(roomId))
        {
            MessageBox.Show(this, "RoomId không được trống.", "Delete room",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Bạn chắc chắn muốn xoá phòng {roomId}?",
            "Xác nhận xoá phòng",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        if (!_state.DeleteRoom(roomId, out var error))
        {
            MessageBox.Show(this, error, "Delete room failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(this, "Đã xoá phòng.", "Delete room",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshRoomGrid();
    }

    private void GridRooms_SelectionChanged(object? sender, EventArgs e)
    {
        if (_gridRooms.CurrentRow == null)
            return;

        if (_gridRooms.CurrentRow.DataBoundItem is not RoomInfo room)
            return;

        _txtRoomId.Text = room.RoomId;
        _txtRoomBuilding.Text = room.Building;
        _numRoomCapacity.Value = room.Capacity;

        _chkRoomProjector.Checked = room.HasProjector;
        _chkRoomPC.Checked = room.HasPC;
        _chkRoomAC.Checked = room.HasAirConditioner;
        _chkRoomMic.Checked = room.HasMic;

        if (!string.IsNullOrEmpty(room.Status) &&
            _cbRoomStatus.Items.Contains(room.Status))
        {
            _cbRoomStatus.SelectedItem = room.Status;
        }
    }
    private static string GenerateRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rnd = new Random();
        var buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = chars[rnd.Next(chars.Length)];
        }
        return new string(buffer);
    }

    private void SendResetEmail(string toEmail, string fullName, string newPassword)
    {
        // CẤU HÌNH: nên để các giá trị này trong file config / appsettings
        var fromEmail = "n22dccn145@student.ptithcm.edu.vn";           // TODO: đổi thành email gửi thật
        var displayName = "AE PTIT";     // tên hiển thị
        var appPassword = "bouh lzvi nrty cyhc";       // TODO: app password (không phải mật khẩu thường)

        var subject = "Mật khẩu mới cho tài khoản đặt phòng";
        var body =
            $"Xin chào {fullName},\n\n" +
            "Hệ thống đã khôi phục mật khẩu cho tài khoản của bạn.\n\n" +
            $"Mật khẩu mới: {newPassword}\n\n" +
            "Vui lòng đăng nhập và đổi mật khẩu ngay sau khi sử dụng.\n\n" +
            "Trân trọng,\n" +
            "Hệ thống đặt phòng học.";

        var msg = new MailMessage();
        msg.From = new MailAddress(fromEmail, displayName);
        msg.To.Add(new MailAddress(toEmail));
        msg.Subject = subject;
        msg.Body = body;
        msg.IsBodyHtml = false;

        using (var client = new SmtpClient("smtp.gmail.com", 587))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(fromEmail, appPassword);
            client.Send(msg);
        }
    }

    private void BtnFixedApply_Click(object? sender, EventArgs e)
    {
        var subjectCode = _txtFixedSubjectCode.Text.Trim();
        var subjectName = _txtFixedSubjectName.Text.Trim();
        var className = _txtFixedClass.Text.Trim();
        var roomId = _cbFixedRoom.SelectedItem?.ToString();
        var fromDate = _dtFixedFrom.Value.Date;
        var toDate = _dtFixedTo.Value.Date;

        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Hãy chọn phòng cố định.", "Fixed schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cbFixedDayOfWeek.SelectedItem is not DayOfWeek dow)
        {
            MessageBox.Show(this, "Hãy chọn thứ trong tuần.", "Fixed schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var slotStartId = _cbFixedSlotStart.SelectedItem?.ToString();
        var slotEndId = _cbFixedSlotEnd.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(slotStartId) || string.IsNullOrEmpty(slotEndId))
        {
            MessageBox.Show(this, "Hãy chọn ca bắt đầu / kết thúc.", "Fixed schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var logger = new UiLogger(this);

        if (!_state.CreateFixedWeeklyClassSchedule(
                subjectCode,
                subjectName,
                className,
                roomId,
                dow,
                slotStartId,
                slotEndId,
                fromDate,
                toDate,
                logger,
                out var error))
        {
            MessageBox.Show(this, error, "Tạo lịch cố định thất bại",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(this, "Đã tạo lịch cố định cho môn học.", "Fixed schedule",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        // refresh view nếu ngày đang xem nằm trong khoảng
        RefreshSlotsSafe();
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
    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        var logger = new UiLogger(this);
        var snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        _state.SaveSnapshotToFile(snapshotPath, logger);
    }

    private void WireUpUserGridEvents()
    {
        if (_gridUsers == null) return;

        _gridUsers.SelectionChanged += (s, e) =>
        {
            if (_gridUsers.CurrentRow == null) return;

            // Lấy object đã bind vào dòng hiện tại
            if (!(_gridUsers.CurrentRow.DataBoundItem is UserInfo user))
                return;

            _txtUserId.Text = user.UserId;
            _txtUserFullName.Text = user.FullName;
            _cbUserType.SelectedItem = user.UserType;
            _txtUserStudentId.Text = user.StudentId;
            _txtUserLecturerId.Text = user.LecturerId;
            _txtUserClass.Text = user.Class;
            _txtUserEmail.Text = user.Email;
            _txtUserPhone.Text = user.Phone;
            _chkUserActive.Checked = user.IsActive;

            // Faculty: nếu là Student thì lấy Department, nếu là Lecturer thì lấy Faculty
            if (user.UserType == "Student")
            {
                _cbUserFaculty.SelectedItem = user.Department;   // CNTT2, IOT2,...
            }
            else if (user.UserType == "Lecturer")
            {
                _cbUserFaculty.SelectedItem = user.Faculty;      // CNTT2, IOT2,...
            }
            else
            {
                _cbUserFaculty.SelectedItem = null;
            }

            // Bật/tắt StudentId / LecturerId theo loại user
            UpdateUserTypeDependentFields();
        };
    }

    private void UpdateUserTypeDependentFields()
    {
        var type = _cbUserType.SelectedItem?.ToString() ?? "";

        bool isStudent = type == "Student";
        bool isLecturer = type == "Lecturer";

        _txtUserStudentId.Enabled = isStudent;
        _txtUserLecturerId.Enabled = isLecturer;

        // Class chỉ dùng cho Student
        _txtUserClass.Enabled = isStudent;

        if (!isStudent)
        {
            _txtUserStudentId.Text = "";
            _txtUserClass.Text = "";
        }

        if (!isLecturer)
        {
            _txtUserLecturerId.Text = "";
        }
    }


    private void BtnAddUser_Click(object? sender, EventArgs e)
    {
        var userId = _txtUserId.Text.Trim();
        var fullName = _txtUserFullName.Text.Trim();
        var userType = _cbUserType.SelectedItem?.ToString() ?? "";
        var email = _txtUserEmail.Text.Trim();
        var phone = _txtUserPhone.Text.Trim();
        var studentId = _txtUserStudentId.Text.Trim();
        var lecturerId = _txtUserLecturerId.Text.Trim();
        var facultySel = _cbUserFaculty.SelectedItem?.ToString() ?? "";

        var passwordPlain = _txtPassword.Text;
        if (string.IsNullOrWhiteSpace(passwordPlain))
        {
            MessageBox.Show("Password is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(userType))
        {
            MessageBox.Show("UserId, Họ tên, UserType là bắt buộc.");
            return;
        }

        var newUser = new UserInfo
        {
            UserId = userId,
            FullName = fullName,
            UserType = userType,
            Email = email,
            Phone = phone,
            StudentId = studentId,
            LecturerId = lecturerId,
            IsActive = true
        };

        if (userType == "Student")
        {
            newUser.Department = facultySel;
            newUser.Faculty = "";
        }
        else if (userType == "Lecturer")
        {
            newUser.Faculty = facultySel;
            newUser.Department = "";
        }

        string error;
        if (!_state.CreateUser(newUser, passwordPlain, out error))
        {
            MessageBox.Show(error, "CreateUser failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Tạo user thành công.");
        RefreshUserGrid();
    }
    private void BtnUpdateUser_Click(object? sender, EventArgs e)
    {
        var userId = _txtUserId.Text.Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            MessageBox.Show("UserId is required.");
            return;
        }

        var fullName = _txtUserFullName.Text.Trim();
        var userType = _cbUserType.SelectedItem?.ToString() ?? "";
        var email = _txtUserEmail.Text.Trim();
        var phone = _txtUserPhone.Text.Trim();
        var studentId = _txtUserStudentId.Text.Trim();
        var lecturerId = _txtUserLecturerId.Text.Trim();
        var facultySel = _cbUserFaculty.SelectedItem?.ToString() ?? "";
        var isActive = _chkUserActive.Checked;

        var updatedUser = new UserInfo
        {
            UserId = userId,
            FullName = fullName,
            UserType = userType,
            Email = email,
            Phone = phone,
            StudentId = studentId,
            LecturerId = lecturerId,
            IsActive = isActive
        };

        if (userType == "Student")
        {
            updatedUser.Department = facultySel;
            updatedUser.Faculty = "";
        }
        else if (userType == "Lecturer")
        {
            updatedUser.Faculty = facultySel;
            updatedUser.Department = "";
        }

        if (!_state.UpdateUser(updatedUser, out string error))
        {
            MessageBox.Show(error, "UpdateUser failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Cập nhật user thành công.");
        RefreshUserGrid();
    }
    private void BtnToggleActive_Click(object? sender, EventArgs e)
    {
        if (_gridUsers.CurrentRow == null)
        {
            MessageBox.Show("Chọn 1 user trước.");
            return;
        }

        var userId = _gridUsers.CurrentRow.Cells["UserId"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(userId)) return;

        bool isActive = false;
        bool.TryParse(_gridUsers.CurrentRow.Cells["IsActive"].Value?.ToString(), out isActive);

        var newActive = !isActive;

        if (!_state.SetUserActive(userId, newActive, out string error))
        {
            MessageBox.Show(error, "SetUserActive failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(newActive ? "User đã được mở khoá." : "User đã bị khóa.");
        RefreshUserGrid();
    }
    private void BtnDeleteUser_Click(object? sender, EventArgs e)
    {
        if (_gridUsers.CurrentRow == null)
        {
            MessageBox.Show("Chọn 1 user trước.");
            return;
        }

        var userId = _gridUsers.CurrentRow.Cells["UserId"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xoá user {userId}?",
            "Xác nhận xoá",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        if (!_state.DeleteUser(userId, out string error))
        {
            MessageBox.Show(error, "DeleteUser failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Xoá user thành công.");
        RefreshUserGrid();
    }
    private void BtnRoomFilterSearch_Click(object? sender, EventArgs e)
    {
        var roomId = _cbRoomFilter.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Hãy chọn phòng.", "Theo phòng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var date = _dtRoomFilterDate.Value.Date;

        var logger = new UiLogger(this);
        var list = _state.GetDailySchedule(date, roomId, logger);

        // Cấu hình cột cho grid nếu chưa cấu hình
        _gridRoomDaily.AutoGenerateColumns = false;
        if (_gridRoomDaily.Columns.Count == 0)
        {
            _gridRoomDaily.Columns.Clear();

            DataGridViewTextBoxColumn AddCol(string prop, string header, int width = 0)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = prop,
                    HeaderText = header,
                    Name = prop,
                    ReadOnly = true
                };
                if (width > 0) col.Width = width;
                _gridRoomDaily.Columns.Add(col);
                return col;
            }

            AddCol(nameof(RoomDailySlotView.SlotId), "Slot", 60);
            AddCol(nameof(RoomDailySlotView.TimeRange), "Time range", 120);
            AddCol(nameof(RoomDailySlotView.Status), "Status", 80);
            AddCol(nameof(RoomDailySlotView.UserId), "UserId", 100);
            AddCol(nameof(RoomDailySlotView.FullName), "FullName", 150);
            AddCol(nameof(RoomDailySlotView.BookingStatus), "BookingStatus", 100);
        }

        _gridRoomDaily.DataSource = list;
    }

}

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
using System.ComponentModel;
namespace BookingServer;

using System.Net;
using System.Net.Mail;
public partial class Form1 : Form
{
    // ===== Layout chính =====
    private Panel _panelTop = null!;
    private SplitContainer _mainSplit = null!;
    private Panel _panelSidebar = null!;
    private Panel _panelMain = null!;
    private FlowLayoutPanel _flpSidebar = null!;
    private Panel _navMonitor = null!;
    private Panel _navCheckin = null!;
    private Panel _navRoomMgmt = null!;
    private Panel _navUserMgmt = null!;
    private Panel _navReports = null!;
    private Panel _navSettings = null!;
    private Panel _navLogs = null!;
    private Panel _panelSectionHeader = null!;
    private Label _lblSectionTitle = null!;
    private Label _lblBreadcrumb = null!;
    private string _activeSection = "Monitor";
    private int _lastMonitorSplitterDistance = 450;

    // ===== Left: Tab nhỏ (Hôm nay / Theo phòng) =====
    private TabControl _tabLeft = null!;
    private TabPage _tabLeftToday = null!;
    private TabPage _tabLeftByRoom = null!;

    // Tab Hôm nay: dùng grid slots hiện tại
    private DataGridView _gridSlots = null!; // giữ tên cũ để các hàm khác không lỗi
    private BindingList<SlotSummary> _gridSlotsBinding = null!;
    private BindingSource _gridSlotsSource = null!;
    private bool _suppressSlotSelectionChanged;
    private string? _lastEnabledSlotRoomId;
    private string? _lastEnabledSlotSlotId;

    // Tab Theo phòng
    private ComboBox _cbRoomFilter = null!;
    private DateTimePicker _dtRoomFilterDate = null!;
    private Button _btnRoomFilterSearch = null!;
    private DataGridView _gridRoomDaily = null!;

    /////////////////////////////////////////////////////
    // Fixed schedule: student/lecturer selection
    private TextBox _txtFixedStudentId = null!;
    private Button _btnFixedAddStudent = null!;
    private TextBox _txtFixedStudentIdBulk = null!;  // Multi-line textbox for bulk add
    private Button _btnFixedAddStudentBulk = null!;  // Button for bulk add
    private ListBox _lstFixedStudents = null!;
    private Button _btnFixedRemoveStudent = null!;
    private TextBox _txtFixedLecturerId = null!;
    private Button _btnFixedAddLecturer = null!;
    private Label _lblFixedLecturer = null!;
    private Button _btnFixedRemoveLecturer = null!;
    private string? _fixedLecturerUserId = null;
    private List<string> _fixedStudentUserIds = new();

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
    private ComboBox _cbForceRoom = null!;
    private ComboBox _cbForceSlotStart = null!;
    private ComboBox _cbForceSlotEnd = null!;
    private Button _btnForceGrantRange = null!;
    private Button _btnForceReleaseRange = null!;
    private const int SERVER_TCP_PORT = 5000;
    private const int DISCOVERY_UDP_PORT = 5001;

    // Queue view (chuyển qua tab Slot detail)
    private Label _lblQueueTitle = null!;
    private ListBox _lstQueue = null!;

    // Queue User detail panel
    private GroupBox _grpQueueUserDetail = null!;
    private Label _lblQueueUserUserId = null!;
    private Label _lblQueueUserUserType = null!;
    private Label _lblQueueUserFullName = null!;
    private Label _lblQueueUserStudentId = null!;
    private Label _lblQueueUserClass = null!;
    private Label _lblQueueUserDepartment = null!;
    private Label _lblQueueUserLecturerId = null!;
    private Label _lblQueueUserFaculty = null!;
    private Label _lblQueueUserEmail = null!;
    private Label _lblQueueUserPhone = null!;
    private Label _lblQueueUserIsActive = null!;

    // Current booking details
    private Label _lblBookingId = null!;
    private Label _lblBookingUserId = null!;
    private Label _lblBookingRoomId = null!;
    private Label _lblBookingDate = null!;
    private Label _lblBookingSlotId = null!;
    private Label _lblBookingSlotStartId = null!;
    private Label _lblBookingSlotEndId = null!;
    private Label _lblBookingIsRange = null!;
    private Label _lblBookingPurpose = null!;
    private Label _lblBookingCreatedAt = null!;
    private Label _lblBookingUpdatedAt = null!;

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
    // Fixed schedule management
    private DataGridView _gridFixedSchedules = null!;
    private Button _btnDeleteFixedSchedule = null!;
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
    private CheckBox _chkSendEmailGrant = null!;
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
    private Button _btnResetAllData = null!;

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
        _noShowTimer.Interval = 60000; // mỗi 60s quét NO_SHOW
        _noShowTimer.Tick += NoShowTimer_Tick;
        _noShowTimer.Start();

        var logger = new UiLogger(this);
        var snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");

        // 1) Load snapshot nếu có
        _state.LoadSnapshotIfExists(snapshotPath, logger);

        // 2) Đảm bảo current date = hôm nay và đã có slots cho hôm nay
        _state.SetCurrentDate(DateTime.Today, logger);

        _state.StateChanged += () => RefreshSlotsSafe();
        
        // ✅ Subscribe vào event FixedScheduleCreated để refresh ngay lập tức
        _state.FixedScheduleCreated += () =>
        {
            Console.WriteLine($"[EVENT] FixedScheduleCreated triggered at {DateTime.Now:HH:mm:ss.fff}");
            RefreshSlotsSafe();
            RefreshFixedSchedulesGrid();
            Console.WriteLine($"[EVENT] UI refreshed at {DateTime.Now:HH:mm:ss.fff}");
        };

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

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ColumnCount = 2,
            RowCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        this.Controls.Add(root);

        _panelSidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(12)
        };
        root.Controls.Add(_panelSidebar, 0, 0);

        _panelMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
        root.Controls.Add(_panelMain, 1, 0);

        BuildDashboardSidebar();

        _panelSectionHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.White,
            Padding = new Padding(14, 10, 14, 8)
        };

        var hdr = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        hdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        hdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
        _panelSectionHeader.Controls.Add(hdr);

        _lblSectionTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblBreadcrumb = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(107, 114, 128),
            TextAlign = ContentAlignment.MiddleLeft
        };

        hdr.Controls.Add(_lblSectionTitle, 0, 0);
        hdr.Controls.Add(_lblBreadcrumb, 0, 1);

        // ===== TOP: Start Server + DatePicker =====
        _panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45
        };
        _panelTop.BackColor = Color.White;

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
            BorderStyle = BorderStyle.Fixed3D,
            IsSplitterFixed = false
        };
        // Set initial SplitterDistance safely
        int initialDistance = 450;
        TrySetSplitterDistance(_mainSplit, initialDistance);
        _panelMain.Controls.Add(_mainSplit);
        _panelMain.Controls.Add(_panelTop);
        _panelMain.Controls.Add(_panelSectionHeader);

        // Thiết lập chia đôi 50-50 responsive khi form load

        this.Load += (s, e) =>
        {
            if (!_mainSplit.Panel1Collapsed)
            {
                int dist = this.ClientSize.Width / 2;
                TrySetSplitterDistance(_mainSplit, dist);
            }
        };

        // Giữ tỷ lệ 50-50 khi resize form

        this.Resize += (s, e) =>
        {
            if (!_mainSplit.Panel1Collapsed)
            {
                int targetDistance = this.ClientSize.Width / 2;
                if (Math.Abs(_mainSplit.SplitterDistance - targetDistance) > 5)
                {
                    TrySetSplitterDistance(_mainSplit, targetDistance);
                }
            }
        };

        BuildLeftTabs();   // Tab nhỏ bên trái
        BuildRightTabs();  // Tab lớn bên phải

        NavigateDashboard("Monitor");
    }

    private void BuildDashboardSidebar()
    {
        _panelSidebar.Controls.Clear();

        var brand = new Label
        {
            Text = "ROOM BOOKING",
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _panelSidebar.Controls.Add(brand);

        _flpSidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        _panelSidebar.Controls.Add(_flpSidebar);

        _navMonitor = CreateSidebarItem("\uE7F4", "Monitor", "Tổng quan");
        _navCheckin = CreateSidebarItem("\uE8D4", "Checkin", "Check-in / Slot detail");
        _navRoomMgmt = CreateSidebarItem("\uE80F", "RoomMgmt", "Quản lý Phòng");
        _navUserMgmt = CreateSidebarItem("\uE716", "UserMgmt", "Quản lý Người dùng");
        _navReports = CreateSidebarItem("\uE9D2", "Reports", "Báo cáo / Thống kê");
        _navSettings = CreateSidebarItem("\uE713", "Settings", "Cài đặt");
        _navLogs = CreateSidebarItem("\uE9D9", "Logs", "Logs");

        _flpSidebar.Controls.Add(_navMonitor);
        _flpSidebar.Controls.Add(_navCheckin);
        _flpSidebar.Controls.Add(_navRoomMgmt);
        _flpSidebar.Controls.Add(_navUserMgmt);
        _flpSidebar.Controls.Add(_navReports);
        _flpSidebar.Controls.Add(_navSettings);
        _flpSidebar.Controls.Add(_navLogs);

        _panelSidebar.SizeChanged += (s, e) =>
        {
            var w = Math.Max(160, _panelSidebar.ClientSize.Width - 24);
            foreach (Control c in _flpSidebar.Controls)
            {
                c.Width = w;
            }
        };
    }

    private Panel CreateSidebarItem(string iconGlyph, string sectionKey, string text)
    {
        var item = new Panel
        {
            Height = 42,
            Width = Math.Max(160, _panelSidebar.ClientSize.Width - 24),
            BackColor = Color.FromArgb(17, 24, 39),
            Margin = new Padding(0, 6, 0, 0),
            Cursor = Cursors.Hand,
            Tag = sectionKey
        };

        var lblIcon = new Label
        {
            Left = 10,
            Top = 9,
            Width = 22,
            Height = 22,
            Font = new Font("Segoe MDL2 Assets", 14f, FontStyle.Regular),
            ForeColor = Color.FromArgb(229, 231, 235),
            Text = iconGlyph,
            TextAlign = ContentAlignment.MiddleCenter
        };
        item.Controls.Add(lblIcon);

        var lblText = new Label
        {
            Left = 40,
            Top = 0,
            Width = 170,
            Height = 42,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(229, 231, 235),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        item.Controls.Add(lblText);

        void ClickNav(object? s, EventArgs e)
        {
            NavigateDashboard(sectionKey);
        }

        item.Click += ClickNav;
        lblIcon.Click += ClickNav;
        lblText.Click += ClickNav;

        item.MouseEnter += (s, e) =>
        {
            if (!string.Equals(_activeSection, sectionKey, StringComparison.OrdinalIgnoreCase))
                item.BackColor = Color.FromArgb(31, 41, 55);
        };
        item.MouseLeave += (s, e) =>
        {
            if (!string.Equals(_activeSection, sectionKey, StringComparison.OrdinalIgnoreCase))
                item.BackColor = Color.FromArgb(17, 24, 39);
        };

        return item;
    }

    private void NavigateDashboard(string section)
    {
        _activeSection = section;

        if (_mainSplit != null && !_mainSplit.IsDisposed)
        {
            if (section == "Monitor")
            {
                _mainSplit.Panel1Collapsed = false;
                TrySetSplitterDistance(_mainSplit, _lastMonitorSplitterDistance);
                if (_tabRight != null && _tabSlotDetail != null)
                    _tabRight.SelectedTab = _tabSlotDetail;
            }
            else if (section == "Checkin")
            {
                _mainSplit.Panel1Collapsed = false;
                TrySetSplitterDistance(_mainSplit, _lastMonitorSplitterDistance);
                if (_tabRight != null && _tabSlotDetail != null)
                    _tabRight.SelectedTab = _tabSlotDetail;
            }
            else
            {
                if (!_mainSplit.Panel1Collapsed)
                    _lastMonitorSplitterDistance = _mainSplit.SplitterDistance;

                _mainSplit.Panel1Collapsed = true;

                if (_tabRight != null)
                {
                    if (section == "RoomMgmt" && _tabRoomMgmt != null)
                        _tabRight.SelectedTab = _tabRoomMgmt;
                    else if (section == "UserMgmt" && _tabUserMgmt != null)
                        _tabRight.SelectedTab = _tabUserMgmt;
                    else if (section == "Reports" && _tabStatistics != null)
                        _tabRight.SelectedTab = _tabStatistics;
                    else if (section == "Settings" && _tabSettings != null)
                        _tabRight.SelectedTab = _tabSettings;
                    else if (section == "Logs" && _tabLogTab != null)
                        _tabRight.SelectedTab = _tabLogTab;
                }
            }
        }

        if (_lblSectionTitle != null && _lblBreadcrumb != null)
        {
            if (section == "Monitor")
            {
                _lblSectionTitle.Text = "Monitor";
                _lblBreadcrumb.Text = "Dashboard / Monitor";
            }
            else if (section == "Checkin")
            {
                _lblSectionTitle.Text = "Slot detail / Check-in";
                _lblBreadcrumb.Text = "Dashboard / Monitor / Check-in";
            }
            else if (section == "RoomMgmt")
            {
                _lblSectionTitle.Text = "Quản lý Phòng";
                _lblBreadcrumb.Text = "Dashboard / Quản lý / Phòng";
            }
            else if (section == "UserMgmt")
            {
                _lblSectionTitle.Text = "Quản lý Người dùng";
                _lblBreadcrumb.Text = "Dashboard / Quản lý / Người dùng";
            }
            else if (section == "Reports")
            {
                _lblSectionTitle.Text = "Báo cáo / Thống kê";
                _lblBreadcrumb.Text = "Dashboard / Báo cáo";
            }
            else if (section == "Settings")
            {
                _lblSectionTitle.Text = "Cài đặt";
                _lblBreadcrumb.Text = "Dashboard / Cài đặt";
            }
            else if (section == "Logs")
            {
                _lblSectionTitle.Text = "Logs";
                _lblBreadcrumb.Text = "Dashboard / Logs";
            }
            else
            {
                _lblSectionTitle.Text = section;
                _lblBreadcrumb.Text = "Dashboard";
            }
        }

        UpdateSidebarSelection();
    }

    private void UpdateSidebarSelection()
    {
        void Apply(Panel? p, bool active)
        {
            if (p == null) return;
            p.BackColor = active ? Color.FromArgb(37, 99, 235) : Color.FromArgb(17, 24, 39);
            foreach (Control c in p.Controls)
            {
                if (c is Label lbl)
                    lbl.ForeColor = active ? Color.White : Color.FromArgb(229, 231, 235);
            }
        }

        Apply(_navMonitor, _activeSection == "Monitor");
        Apply(_navCheckin, _activeSection == "Checkin");
        Apply(_navRoomMgmt, _activeSection == "RoomMgmt");
        Apply(_navUserMgmt, _activeSection == "UserMgmt");
        Apply(_navReports, _activeSection == "Reports");
        Apply(_navSettings, _activeSection == "Settings");
        Apply(_navLogs, _activeSection == "Logs");
    }

    private bool TrySetSplitterDistance(SplitContainer split, int value)
    {
        int min1 = split.Panel1MinSize;
        int min2 = split.Panel2MinSize;

        int total = split.Orientation == Orientation.Vertical
            ? split.ClientSize.Width
            : split.ClientSize.Height;

        int minTotal = min1 + min2 + split.SplitterWidth;
        if (total <= minTotal)
            return false;

        int max = total - min2 - split.SplitterWidth;
        int clamped = value;
        if (clamped < min1) clamped = min1;
        if (clamped > max) clamped = max;

        try
        {
            split.SplitterDistance = clamped;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsRoomDisabledUi(string? roomId)
    {
        roomId = (roomId ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(roomId))
            return false;

        if (!_state.RoomsInfo.TryGetValue(roomId, out var r) || r == null)
            return false;

        return string.Equals((r.Status ?? "").Trim(), "DISABLED", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyRoomMgmtGridDisabledStyle()
    {
        if (_gridRooms == null) return;

        foreach (DataGridViewRow row in _gridRooms.Rows)
        {
            if (row.DataBoundItem is not RoomInfo r) continue;

            bool disabled = string.Equals((r.Status ?? "").Trim(), "DISABLED", StringComparison.OrdinalIgnoreCase);
            row.DefaultCellStyle.ForeColor = disabled ? Color.FromArgb(156, 163, 175) : Color.Black;
            row.DefaultCellStyle.BackColor = disabled ? Color.FromArgb(243, 244, 246) : Color.White;
            row.DefaultCellStyle.SelectionBackColor = disabled ? Color.FromArgb(229, 231, 235) : _gridRooms.DefaultCellStyle.SelectionBackColor;
            row.DefaultCellStyle.SelectionForeColor = disabled ? Color.FromArgb(107, 114, 128) : _gridRooms.DefaultCellStyle.SelectionForeColor;
        }
    }

    private void ApplySlotGridDisabledRoomStyle()
    {
        if (_gridSlots == null) return;

        foreach (DataGridViewRow row in _gridSlots.Rows)
        {
            if (row.DataBoundItem is not SlotSummary ss) continue;
            bool disabled = IsRoomDisabledUi(ss.RoomId);

            row.DefaultCellStyle.ForeColor = disabled ? Color.FromArgb(156, 163, 175) : _gridSlots.DefaultCellStyle.ForeColor;
            row.DefaultCellStyle.BackColor = disabled ? Color.FromArgb(243, 244, 246) : _gridSlots.DefaultCellStyle.BackColor;
            row.DefaultCellStyle.SelectionBackColor = disabled ? Color.FromArgb(229, 231, 235) : _gridSlots.DefaultCellStyle.SelectionBackColor;
            row.DefaultCellStyle.SelectionForeColor = disabled ? Color.FromArgb(107, 114, 128) : _gridSlots.DefaultCellStyle.SelectionForeColor;
        }
    }

    private void ConfigureRoomComboBox(ComboBox cb)
    {
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.DrawItem -= RoomComboBox_DrawItem;
        cb.DrawItem += RoomComboBox_DrawItem;

        cb.SelectionChangeCommitted -= RoomComboBox_SelectionChangeCommitted;
        cb.SelectionChangeCommitted += RoomComboBox_SelectionChangeCommitted;

        if (cb.Tag == null)
            cb.Tag = cb.SelectedItem?.ToString();
    }

    private void RoomComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox cb) return;
        if (e.Index < 0) return;

        var roomId = cb.Items[e.Index]?.ToString() ?? "";
        bool disabled = IsRoomDisabledUi(roomId);

        e.DrawBackground();
        var fg = disabled ? Color.FromArgb(156, 163, 175) : cb.ForeColor;
        TextRenderer.DrawText(e.Graphics, roomId, e.Font, e.Bounds, fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        e.DrawFocusRectangle();
    }

    private void RoomComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        if (sender is not ComboBox cb) return;
        var roomId = cb.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(roomId)) return;

        if (IsRoomDisabledUi(roomId))
        {
            var prev = cb.Tag as string;
            if (!string.IsNullOrWhiteSpace(prev) && cb.Items.Contains(prev))
            {
                cb.SelectedItem = prev;
            }
            else
            {
                foreach (var item in cb.Items)
                {
                    var id = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(id) && !IsRoomDisabledUi(id))
                    {
                        cb.SelectedItem = id;
                        cb.Tag = id;
                        return;
                    }
                }
            }

            return;
        }

        cb.Tag = roomId;
    }

    private void EnsureRoomComboSelectedEnabled(ComboBox cb)
    {
        var current = cb.SelectedItem?.ToString();

        if (!string.IsNullOrWhiteSpace(current) && !IsRoomDisabledUi(current))
        {
            cb.Tag = current;
            return;
        }

        foreach (var item in cb.Items)
        {
            var id = item?.ToString();
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (string.Equals(id, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                cb.SelectedItem = id;
                cb.Tag = id;
                return;
            }

            if (!IsRoomDisabledUi(id))
            {
                cb.SelectedItem = id;
                cb.Tag = id;
                return;
            }
        }
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

        _gridSlotsBinding = new BindingList<SlotSummary>();
        _gridSlotsSource = new BindingSource { DataSource = _gridSlotsBinding };
        _gridSlots.DataSource = _gridSlotsSource;

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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        _gridRoomDaily.SelectionChanged += GridRoomDaily_SelectionChanged;
        pnlRoomDaily.Controls.Add(_gridRoomDaily);
    }

    private void BuildRightTabs()
    {
        _tabRight = new TabControl
        {
            Dock = DockStyle.Fill
        };
        _mainSplit.Panel2.Controls.Add(_tabRight);

        _tabRight.Appearance = TabAppearance.FlatButtons;
        _tabRight.ItemSize = new System.Drawing.Size(0, 1);
        _tabRight.SizeMode = TabSizeMode.Fixed;
        _tabRight.Multiline = true;

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
        _tabSlotDetail.SuspendLayout();
        _tabSlotDetail.Controls.Clear();

        _tabSlotDetail.AutoScroll = false;
        _tabSlotDetail.Padding = new Padding(10);
        _tabSlotDetail.BackColor = Color.White;

        var main = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        _tabSlotDetail.Controls.Add(main);
        main.SizeChanged += (s, e) =>
        {
            var total = main.ClientSize.Width;
            if (total > 0)
            {
                var desiredMin = 360;
                var maxMin = Math.Max(0, (total - main.SplitterWidth) / 2 - 10);
                var min = Math.Min(desiredMin, maxMin);
                main.Panel1MinSize = min;
                main.Panel2MinSize = min;
            }
            TrySetSplitterDistance(main, 650);
        };
        TrySetSplitterDistance(main, 650);

        var leftStack = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        main.Panel1.Controls.Add(leftStack);

        // helper: tạo GroupBox xếp dọc
        GroupBox MakeGroup(string text, int height)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = height,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };
        }

        // =========================================================
        // 1) SLOT INFO
        // =========================================================
        var grpSlotInfo = MakeGroup("Slot info", 70);

        _lblSelectedSlot = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Slot: (chưa chọn)",
            AutoEllipsis = true
        };
        grpSlotInfo.Controls.Add(_lblSelectedSlot);

        // =========================================================
        // 2) CURRENT BOOKING
        // =========================================================
        var grpBooking = MakeGroup("Current booking", 250);
        _lblBookingUser = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "User: -",
            AutoEllipsis = true
        };
        grpBooking.Controls.Add(_lblBookingUser);
        _lblBookingUser.BringToFront(); // đảm bảo nằm trên bookingTable



        // dùng TableLayoutPanel để khỏi canh Top thủ công
        var bookingTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = false
        };
        bookingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bookingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        Label MakeBookingLabel(string text)
            => new Label { Dock = DockStyle.Fill, Height = 18, Text = text, AutoEllipsis = true };

        // mỗi AddRow sẽ tự tăng RowCount
        void AddRow(Control left, Control right)
        {
            bookingTable.RowCount += 1;
            bookingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            bookingTable.Controls.Add(left, 0, bookingTable.RowCount - 1);
            bookingTable.Controls.Add(right, 1, bookingTable.RowCount - 1);
        }

        // các label booking
        _lblBookingId = MakeBookingLabel("BookingId: -");
        _lblBookingUserId = MakeBookingLabel("UserId: -");
        _lblBookingRoomId = MakeBookingLabel("RoomId: -");
        _lblBookingDate = MakeBookingLabel("Date: -");
        _lblBookingSlotStartId = MakeBookingLabel("SlotStartId: -");
        _lblBookingSlotEndId = MakeBookingLabel("SlotEndId: -");
        _lblBookingIsRange = MakeBookingLabel("IsRangeBooking: -");
        _lblBookingStatus = MakeBookingLabel("Status: -");
        _lblBookingPurpose = MakeBookingLabel("Purpose: -");
        _lblBookingCreatedAt = MakeBookingLabel("CreatedAt: -");
        _lblBookingUpdatedAt = MakeBookingLabel("UpdatedAt: -");

        // xếp bảng
        AddRow(_lblBookingId, new Label());                 // right trống
        AddRow(_lblBookingUserId, new Label());
        AddRow(_lblBookingRoomId, new Label());
        AddRow(_lblBookingDate, new Label());
        AddRow(_lblBookingSlotStartId, _lblBookingSlotEndId);
        AddRow(_lblBookingIsRange, _lblBookingStatus);

        // Purpose chiếm 2 cột
        bookingTable.RowCount += 1;
        bookingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        bookingTable.Controls.Add(_lblBookingPurpose, 0, bookingTable.RowCount - 1);
        bookingTable.SetColumnSpan(_lblBookingPurpose, 2);

        bookingTable.RowCount += 1;
        bookingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        bookingTable.Controls.Add(_lblBookingCreatedAt, 0, bookingTable.RowCount - 1);
        bookingTable.SetColumnSpan(_lblBookingCreatedAt, 2);

        bookingTable.RowCount += 1;
        bookingTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        bookingTable.Controls.Add(_lblBookingUpdatedAt, 0, bookingTable.RowCount - 1);
        bookingTable.SetColumnSpan(_lblBookingUpdatedAt, 2);

        grpBooking.Controls.Add(bookingTable);
        bookingTable.Dock = DockStyle.Fill;

        // =========================================================
        // 3) ADMIN ACTIONS
        // =========================================================
        _grpCheckin = MakeGroup("Admin actions", 120);

        _btnCheckIn = new Button { Left = 10, Top = 25, Width = 150, Text = "CHECK-IN", Enabled = false };
        _btnCheckIn.Click += BtnCheckIn_Click;
        _grpCheckin.Controls.Add(_btnCheckIn);

        _btnComplete = new Button { Left = 170, Top = 25, Width = 150, Text = "Complete & Release", Enabled = false };
        _btnComplete.Click += BtnComplete_Click;
        _grpCheckin.Controls.Add(_btnComplete);

        var lblForceRoom = new Label { Left = 330, Top = 30, Width = 45, Text = "Room:" };
        _grpCheckin.Controls.Add(lblForceRoom);

        _cbForceRoom = new ComboBox
        {
            Left = 375,
            Top = 26,
            Width = 80,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _grpCheckin.Controls.Add(_cbForceRoom);

        var lblForceUser = new Label { Left = 10, Top = 62, Width = 80, Text = "Force user:" };
        _grpCheckin.Controls.Add(lblForceUser);

        _txtForceUserId = new TextBox { Left = 90, Top = 58, Width = 140, CharacterCasing = CharacterCasing.Upper };
        _grpCheckin.Controls.Add(_txtForceUserId);

        _btnForceGrant = new Button { Left = 240, Top = 56, Width = 80, Text = "GRANT" };
        _btnForceGrant.Click += BtnForceGrant_Click;
        _grpCheckin.Controls.Add(_btnForceGrant);

        _btnForceRelease = new Button { Left = 330, Top = 56, Width = 80, Text = "RELEASE" };
        _btnForceRelease.Click += BtnForceRelease_Click;
        _grpCheckin.Controls.Add(_btnForceRelease);

        var lblRange = new Label { Left = 430, Top = 62, Width = 55, Text = "Range:" };
        _grpCheckin.Controls.Add(lblRange);

        _cbForceSlotStart = new ComboBox
        {
            Left = 485,
            Top = 58,
            Width = 60,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbForceSlotEnd = new ComboBox
        {
            Left = 550,
            Top = 58,
            Width = 60,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

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
            Left = 620,
            Top = 56,
            Width = 100,
            Text = "GRANT RANGE"
        };
        _btnForceGrantRange.Click += BtnForceGrantRange_Click;
        _grpCheckin.Controls.Add(_btnForceGrantRange);

        // =========================================================
        // 4) EVENT LOCK
        // =========================================================
        var grpEvent = MakeGroup("Event lock", 90);

        var lblEventNote = new Label { Left = 10, Top = 35, Width = 80, Text = "Event note:" };
        grpEvent.Controls.Add(lblEventNote);

        _txtEventNote = new TextBox { Left = 90, Top = 32, Width = 260 };
        grpEvent.Controls.Add(_txtEventNote);

        _btnLockEvent = new Button { Left = 360, Top = 30, Width = 120, Text = "Lock for Event" };
        _btnLockEvent.Click += BtnLockEvent_Click;
        grpEvent.Controls.Add(_btnLockEvent);

        _btnUnlockEvent = new Button { Left = 490, Top = 30, Width = 120, Text = "Unlock Event" };
        _btnUnlockEvent.Click += BtnUnlockEvent_Click;
        grpEvent.Controls.Add(_btnUnlockEvent);

        // =========================================================
        // 5) QUEUE + USER DETAIL (Queue list nhỏ)
        // =========================================================
        var grpQueueWrap = MakeGroup("Queue & detail", 260);

        var queueContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,     // trái: queue, phải: detail
            SplitterWidth = 6,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        grpQueueWrap.Controls.Add(queueContainer);

        queueContainer.SizeChanged += (s, e) =>
        {
            var total = queueContainer.ClientSize.Width;
            if (total > 0)
            {
                var desiredMin = 220;
                var maxMin = Math.Max(0, (total - queueContainer.SplitterWidth) / 2 - 10);
                var min = Math.Min(desiredMin, maxMin);
                queueContainer.Panel1MinSize = min;
                queueContainer.Panel2MinSize = min;
            }
            TrySetSplitterDistance(queueContainer, 280);
        };
        TrySetSplitterDistance(queueContainer, 280);

        // Panel trái: Queue
        var grpQueue = new GroupBox { Text = "Queue", Dock = DockStyle.Fill, Padding = new Padding(10) };
        queueContainer.Panel1.Controls.Add(grpQueue);

        // 1️⃣ ADD LISTBOX TRƯỚC (Fill)
        _lstQueue = new ListBox
        {
            Dock = DockStyle.Fill
        };
        _lstQueue.SelectedIndexChanged += LstQueue_SelectedIndexChanged;
        grpQueue.Controls.Add(_lstQueue);

        // 2️⃣ ADD SPACER (Top)
        var spacer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35
        };
        grpQueue.Controls.Add(spacer);

        // 3️⃣ ADD LABEL (Top)
        _lblQueueTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "Queue for: (select a room/slot)",
            AutoEllipsis = true
        };
        grpQueue.Controls.Add(_lblQueueTitle);


        // Panel phải: User detail
        _grpQueueUserDetail = new GroupBox { Text = "Queue user detail", Dock = DockStyle.Fill, Padding = new Padding(10) };
        queueContainer.Panel2.Controls.Add(_grpQueueUserDetail);

        var qTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true
        };
        qTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _grpQueueUserDetail.Controls.Add(qTable);

        Label MakeQ(string text) => new Label { Dock = DockStyle.Top, Height = 18, Text = text, AutoEllipsis = true };

        _lblQueueUserUserId = MakeQ("UserId: -");
        _lblQueueUserUserType = MakeQ("UserType: -");
        _lblQueueUserFullName = MakeQ("FullName: -");
        _lblQueueUserStudentId = MakeQ("StudentId: -");
        _lblQueueUserClass = MakeQ("Class: -");
        _lblQueueUserDepartment = MakeQ("Department: -");
        _lblQueueUserLecturerId = MakeQ("LecturerId: -");
        _lblQueueUserFaculty = MakeQ("Faculty: -");
        _lblQueueUserEmail = MakeQ("Email: -");
        _lblQueueUserPhone = MakeQ("Phone: -");
        _lblQueueUserIsActive = MakeQ("IsActive: -");

        qTable.Controls.Add(_lblQueueUserUserId);
        qTable.Controls.Add(_lblQueueUserUserType);
        qTable.Controls.Add(_lblQueueUserFullName);
        qTable.Controls.Add(_lblQueueUserStudentId);
        qTable.Controls.Add(_lblQueueUserClass);
        qTable.Controls.Add(_lblQueueUserDepartment);
        qTable.Controls.Add(_lblQueueUserLecturerId);
        qTable.Controls.Add(_lblQueueUserFaculty);
        qTable.Controls.Add(_lblQueueUserEmail);
        qTable.Controls.Add(_lblQueueUserPhone);
        qTable.Controls.Add(_lblQueueUserIsActive);

        // =========================================================
        // ADD TO TAB (thứ tự: add từ DƯỚI lên trên vì Dock=Top)
        // =========================================================
        grpQueueWrap.Dock = DockStyle.Fill;
        main.Panel2.Controls.Add(grpQueueWrap);

        leftStack.Controls.Add(grpEvent);
        leftStack.Controls.Add(_grpCheckin);
        leftStack.Controls.Add(grpBooking);
        leftStack.Controls.Add(grpSlotInfo);

        _tabSlotDetail.ResumeLayout(true);
    }

    private void BuildTabRoomManagement()
    {
        _tabRoomMgmt.Controls.Clear();
        _tabRoomMgmt.AutoScroll = true;
        _tabRoomMgmt.BackColor = Color.White;

        // ===== TABLELAYOUT: 2 CỘT (TRÁI LIST / PHẢI DETAIL + FIXED) =====
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        table.Padding = new Padding(12);
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46)); // trái 50%
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54)); // phải 50%
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
        grpList.Padding = new Padding(10);
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
            Height = 44
        };
        pnlLeft.Controls.Add(pnlSearch);

        var searchTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 6)
        };
        searchTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        searchTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        searchTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        searchTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        pnlSearch.Controls.Add(searchTable);

        var lblSearch = new Label
        {
            Dock = DockStyle.Fill,
            Text = "RoomId:",
            TextAlign = ContentAlignment.MiddleLeft
        };
        searchTable.Controls.Add(lblSearch, 0, 0);

        _txtSearchRoomId = new TextBox
        {
            Dock = DockStyle.Fill
        };
        searchTable.Controls.Add(_txtSearchRoomId, 1, 0);

        _btnSearchRoom = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Tìm"
        };
        _btnSearchRoom.Click += (s, e) =>
        {
            var keyword = _txtSearchRoomId.Text.Trim();
            RefreshRoomGrid(keyword);
        };
        searchTable.Controls.Add(_btnSearchRoom, 2, 0);

        _btnSearchRoomAll = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Reset"
        };
        _btnSearchRoomAll.Click += (s, e) =>
        {
            _txtSearchRoomId.Text = "";
            RefreshRoomGrid(null);
        };
        searchTable.Controls.Add(_btnSearchRoomAll, 3, 0);

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
        _gridRooms.BackgroundColor = Color.White;
        _gridRooms.BorderStyle = BorderStyle.None;
        _gridRooms.RowHeadersVisible = false;
        _gridRooms.EnableHeadersVisualStyles = false;
        _gridRooms.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
        _gridRooms.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(17, 24, 39);
        _gridRooms.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _gridRooms.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        _gridRooms.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
        _gridRooms.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
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
        grpRoomDetail.Padding = new Padding(10);
        rightTable.Controls.Add(grpRoomDetail, 0, 0);

        // Giữ nguyên các control field nhưng đổi layout sang TableLayoutPanel cho gọn
        var roomForm = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0
        };
        roomForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        roomForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddFormRow(Control left, Control right, int height = 30)
        {
            roomForm.RowCount += 1;
            roomForm.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            left.Dock = DockStyle.Fill;
            if (left is Label ll) ll.TextAlign = ContentAlignment.MiddleLeft;
            right.Dock = DockStyle.Fill;
            roomForm.Controls.Add(left, 0, roomForm.RowCount - 1);
            roomForm.Controls.Add(right, 1, roomForm.RowCount - 1);
        }

        var lblRid = new Label { Text = "RoomId:" };
        _txtRoomId = new TextBox();
        AddFormRow(lblRid, _txtRoomId);

        var lblBuilding = new Label { Text = "Building:" };
        _txtRoomBuilding = new TextBox();
        AddFormRow(lblBuilding, _txtRoomBuilding);

        var lblCap = new Label { Text = "Capacity:" };
        _numRoomCapacity = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 500,
            Value = 60
        };
        AddFormRow(lblCap, _numRoomCapacity);

        var lblAmenities = new Label { Text = "Amenities:" };
        var amenities = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        _chkRoomProjector = new CheckBox { Text = "Projector", AutoSize = true, Margin = new Padding(0, 4, 14, 0) };
        _chkRoomPC = new CheckBox { Text = "PC", AutoSize = true, Margin = new Padding(0, 4, 14, 0) };
        _chkRoomAC = new CheckBox { Text = "A/C", AutoSize = true, Margin = new Padding(0, 4, 14, 0) };
        _chkRoomMic = new CheckBox { Text = "Mic", AutoSize = true, Margin = new Padding(0, 4, 14, 0) };
        amenities.Controls.Add(_chkRoomProjector);
        amenities.Controls.Add(_chkRoomPC);
        amenities.Controls.Add(_chkRoomAC);
        amenities.Controls.Add(_chkRoomMic);
        AddFormRow(lblAmenities, amenities, 34);

        var lblStatus = new Label { Text = "Status:" };
        _cbRoomStatus = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbRoomStatus.Items.AddRange(new object[] { "ACTIVE", "DISABLED" });
        _cbRoomStatus.SelectedIndex = 0;

        AddFormRow(lblStatus, _cbRoomStatus);

        var lblActions = new Label { Text = "Actions:" };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        _btnRoomAdd = new Button { Width = 80, Height = 28, Text = "Add", Margin = new Padding(0, 0, 10, 0) };
        _btnRoomUpdate = new Button { Width = 80, Height = 28, Text = "Update", Margin = new Padding(0, 0, 10, 0) };
        _btnRoomDelete = new Button { Width = 80, Height = 28, Text = "Delete" };
        actions.Controls.Add(_btnRoomAdd);
        actions.Controls.Add(_btnRoomUpdate);
        actions.Controls.Add(_btnRoomDelete);
        AddFormRow(lblActions, actions, 36);

        grpRoomDetail.Controls.Add(roomForm);

        // ===== Group: Fixed room config (phải dưới) =====
        var grpFixed = new GroupBox
        {
            Text = "Fixed room config (môn/lớp)",
            Dock = DockStyle.Fill
        };
        grpFixed.Padding = new Padding(10);
        rightTable.Controls.Add(grpFixed, 0, 1);

        var fixedSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        grpFixed.Controls.Add(fixedSplit);
        fixedSplit.SizeChanged += (s, e) =>
        {
            var total = fixedSplit.ClientSize.Width;
            if (total > 0)
            {
                var desiredMin = 280;
                var maxMin = Math.Max(0, (total - fixedSplit.SplitterWidth) / 2 - 10);
                var min = Math.Min(desiredMin, maxMin);
                fixedSplit.Panel1MinSize = min;
                fixedSplit.Panel2MinSize = min;
            }
            TrySetSplitterDistance(fixedSplit, 420);
        };
        TrySetSplitterDistance(fixedSplit, 420);

        var pnlFixedForm = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        fixedSplit.Panel1.Controls.Add(pnlFixedForm);

        var pnlFixedList = new Panel
        {
            Dock = DockStyle.Fill
        };
        fixedSplit.Panel2.Controls.Add(pnlFixedList);

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
        _cbFixedDayOfWeek.SelectedItem = DayOfWeek.Saturday;

        var lblSlots = new Label { Left = 10, Top = 205, Width = 100, Text = "Slots (From-To):" };
        _cbFixedSlotStart = new ComboBox
        {
            Left = 110,
            Top = 202,
            Width = 70,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        var lblSlotTo = new Label { Left = 185, Top = 205, Width = 15, Text = "→" };
        _cbFixedSlotEnd = new ComboBox
        {
            Left = 205,
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

        // === Student/Lecturer selection ===
        var lblStudentId = new Label { Left = 10, Top = 235, Width = 100, Text = "StudentId:" };
        _txtFixedStudentId = new TextBox { Left = 110, Top = 232, Width = 100 };
        _btnFixedAddStudent = new Button { Left = 220, Top = 230, Width = 60, Text = "Add" };
        _btnFixedRemoveStudent = new Button { Left = 290, Top = 230, Width = 60, Text = "Remove" };

        _txtFixedStudentId.Leave += (s, e) =>
        {
            _txtFixedStudentId.Text = (_txtFixedStudentId.Text ?? "").Trim().ToUpperInvariant();
        };
        
        // Bulk add StudentId (paste from Excel)
        var lblBulkAdd = new Label { Left = 10, Top = 260, Width = 100, Text = "Bulk add:" };
        _txtFixedStudentIdBulk = new TextBox 
        { 
            Left = 110, 
            Top = 260, 
            Width = 240, 
            Height = 60,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        _btnFixedAddStudentBulk = new Button { Left = 110, Top = 325, Width = 120, Text = "Add Multiple" };
        
        _lstFixedStudents = new ListBox { Left = 110, Top = 350, Width = 240, Height = 50 };

        var lblLecturerId = new Label { Left = 10, Top = 408, Width = 100, Text = "LecturerId:" };
        _txtFixedLecturerId = new TextBox { Left = 110, Top = 405, Width = 100 };
        _btnFixedAddLecturer = new Button { Left = 220, Top = 403, Width = 60, Text = "Set" };
        _btnFixedRemoveLecturer = new Button { Left = 290, Top = 403, Width = 60, Text = "Remove" };
        _lblFixedLecturer = new Label { Left = 110, Top = 435, Width = 240, Height = 25, Text = "(No lecturer set)" };

        _txtFixedLecturerId.Leave += (s, e) =>
        {
            _txtFixedLecturerId.Text = (_txtFixedLecturerId.Text ?? "").Trim().ToUpperInvariant();
        };

        _btnFixedAddStudent.Click += (s, e) =>
        {
            var studentId = (_txtFixedStudentId.Text ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(studentId)) return;
            var user = _state.UsersInfo.Values.FirstOrDefault(u =>
                string.Equals(u.StudentId, studentId, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                MessageBox.Show($"StudentId {studentId} not found.", "Not found");
                return;
            }
            if (_fixedStudentUserIds.Contains(user.UserId))
            {
                MessageBox.Show($"Student {studentId} already added.", "Duplicate");
                return;
            }
            _fixedStudentUserIds.Add(user.UserId);
            _lstFixedStudents.Items.Add($"{user.StudentId} - {user.FullName}");
            _txtFixedStudentId.Text = "";
        };
        _btnFixedRemoveStudent.Click += (s, e) =>
        {
            if (_lstFixedStudents.SelectedIndex >= 0)
            {
                _fixedStudentUserIds.RemoveAt(_lstFixedStudents.SelectedIndex);
                _lstFixedStudents.Items.RemoveAt(_lstFixedStudents.SelectedIndex);
            }
        };
        
        // Bulk add StudentId (paste from Excel)
        _btnFixedAddStudentBulk.Click += (s, e) =>
        {
            var bulkText = _txtFixedStudentIdBulk.Text.Trim();
            if (string.IsNullOrEmpty(bulkText))
            {
                MessageBox.Show("Vui lòng paste danh sách StudentId.", "Bulk Add", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse StudentIds: split by newline, comma, semicolon, tab
            var separators = new[] { '\n', '\r', ',', ';', '\t' };
            var rawIds = bulkText.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            
            var added = 0;
            var duplicates = 0;
            var notFound = new List<string>();

            foreach (var rawId in rawIds)
            {
                // Normalize: trim và uppercase
                var studentId = rawId.Trim().ToUpper();
                if (string.IsNullOrEmpty(studentId)) continue;

                // Tìm user theo StudentId
                var user = _state.UsersInfo.Values.FirstOrDefault(u => 
                    string.Equals(u.StudentId, studentId, StringComparison.OrdinalIgnoreCase));
                
                if (user == null)
                {
                    notFound.Add(studentId);
                    continue;
                }

                // Check duplicate
                if (_fixedStudentUserIds.Contains(user.UserId))
                {
                    duplicates++;
                    continue;
                }

                // Add to list
                _fixedStudentUserIds.Add(user.UserId);
                _lstFixedStudents.Items.Add($"{user.StudentId} - {user.FullName}");
                added++;
            }

            // Clear textbox
            _txtFixedStudentIdBulk.Text = "";

            // Show result
            var msg = $"Đã thêm: {added} sinh viên\n";
            if (duplicates > 0)
                msg += $"Trùng lặp: {duplicates}\n";
            if (notFound.Count > 0)
                msg += $"Không tìm thấy: {notFound.Count}\n{string.Join(", ", notFound.Take(10))}" + 
                       (notFound.Count > 10 ? "..." : "");

            MessageBox.Show(msg, "Bulk Add Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        
        _btnFixedAddLecturer.Click += (s, e) =>
        {
            var lecturerId = (_txtFixedLecturerId.Text ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(lecturerId)) return;
            var user = _state.UsersInfo.Values.FirstOrDefault(u =>
                string.Equals(u.LecturerId, lecturerId, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                MessageBox.Show($"LecturerId {lecturerId} not found.", "Not found");
                return;
            }
            _fixedLecturerUserId = user.UserId;
            _lblFixedLecturer.Text = $"{user.LecturerId} - {user.FullName}";
            _txtFixedLecturerId.Text = "";
        };
        _btnFixedRemoveLecturer.Click += (s, e) =>
        {
            _fixedLecturerUserId = null;
            _lblFixedLecturer.Text = "(No lecturer set)";
        };

        _btnFixedApply = new Button
        {
            Left = 110,
            Top = 465,
            Width = 150,
            Text = "Apply fixed schedule"
        };
        // Gắn event cho nút Apply fixed schedule
        if (_btnFixedApply != null)
        {
            _btnFixedApply.Click += (s, e) =>
            {
                // Lấy dữ liệu từ UI
                var subjectCode = _txtFixedSubjectCode?.Text.Trim() ?? "";
                var subjectName = _txtFixedSubjectName?.Text.Trim() ?? "";
                var className = _txtFixedClass?.Text.Trim() ?? "";
                var roomId = _cbFixedRoom?.SelectedItem?.ToString() ?? "";
                var from = _dtFixedFrom?.Value.Date ?? DateTime.Today;
                var to = _dtFixedTo?.Value.Date ?? DateTime.Today;
                var dowStr = _cbFixedDayOfWeek?.SelectedItem?.ToString() ?? "Monday";
                var slotStartId = _cbFixedSlotStart?.SelectedItem?.ToString() ?? "S1";
                var slotEndId = _cbFixedSlotEnd?.SelectedItem?.ToString() ?? "S1";
                var note = ""; // Có thể lấy từ UI nếu có
                var lecturerId = _fixedLecturerUserId ?? "";
                var studentIds = _fixedStudentUserIds.ToList();

                if (!Enum.TryParse<DayOfWeek>(dowStr, out var dow))
                {
                    MessageBox.Show($"DayOfWeek không hợp lệ: {dowStr}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Gọi ServerState để apply fixed schedule
                var (ok, msg, created, conflict) = _state.ApplyFixedSchedule(
                    subjectCode, subjectName, className, roomId,
                    from, to, dow, slotStartId, slotEndId, studentIds, lecturerId, note);
                if (ok)
                {
                    MessageBox.Show($"Tạo thành công {created} buổi học. Xung đột: {conflict}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Refresh grid để hiển thị Fixed Schedule mới tạo
                    RefreshFixedSchedulesGrid();
                }
                else
                {
                    MessageBox.Show($"Không tạo được lịch: {msg}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        // ===== Section: Danh sách Fixed Schedules đã tạo =====
        var lblFixedList = new Label 
        { 
            Left = 10, 
            Top = 500, 
            Width = 200, 
            Text = "Danh sách Fixed Schedules:",
            Font = new Font(Font, FontStyle.Bold)
        };
        
        _btnDeleteFixedSchedule = new Button
        {
            Left = 220,
            Top = 497,
            Width = 70,
            Text = "Delete"
        };
        _btnDeleteFixedSchedule.Click += BtnDeleteFixedSchedule_Click;
        
        _gridFixedSchedules = new DataGridView
        {
            Left = 10,
            Top = 530,
            Width = 360,
            Height = 200,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        pnlFixedForm.Controls.AddRange(new Control[]
        {
            lblSubCode, _txtFixedSubjectCode,
            lblSubName, _txtFixedSubjectName,
            lblClass, _txtFixedClass,
            lblFixedRoom, _cbFixedRoom,
            lblFrom, _dtFixedFrom, _dtFixedTo,
            lblDow, _cbFixedDayOfWeek,
            lblSlots, _cbFixedSlotStart, lblSlotTo, _cbFixedSlotEnd,
            lblStudentId, _txtFixedStudentId, _btnFixedAddStudent, _btnFixedRemoveStudent,
            lblBulkAdd, _txtFixedStudentIdBulk, _btnFixedAddStudentBulk,
            _lstFixedStudents,
            lblLecturerId, _txtFixedLecturerId, _btnFixedAddLecturer, _btnFixedRemoveLecturer, _lblFixedLecturer,
            _btnFixedApply
        });

        var pnlFixedListHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34
        };
        lblFixedList.Dock = DockStyle.Left;
        lblFixedList.TextAlign = ContentAlignment.MiddleLeft;
        _btnDeleteFixedSchedule.Dock = DockStyle.Right;
        _gridFixedSchedules.Dock = DockStyle.Fill;
        _gridFixedSchedules.Top = 0;
        _gridFixedSchedules.Left = 0;

        pnlFixedListHeader.Controls.Add(_btnDeleteFixedSchedule);
        pnlFixedListHeader.Controls.Add(lblFixedList);
        pnlFixedList.Controls.Add(_gridFixedSchedules);
        pnlFixedList.Controls.Add(pnlFixedListHeader);
        

        _btnRoomAdd.Click += BtnRoomAdd_Click;
        _btnRoomUpdate.Click += BtnRoomUpdate_Click;
        _btnRoomDelete.Click += BtnRoomDelete_Click;
        RefreshRoomGrid();
        _gridRooms.SelectionChanged += GridRooms_SelectionChanged;
        
        // Load danh sách Fixed Schedules
        RefreshFixedSchedulesGrid();
    }
    // Fix SplitContainer splitter crash: clamp SplitterDistance
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_mainSplit != null)
        {
            try
            {
                TrySetSplitterDistance(_mainSplit, _mainSplit.SplitterDistance);
            }
            catch { /* ignore */ }
        }
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
        ApplyRoomMgmtGridDisabledStyle();

        // ====== FILL COMBO FIXED ROOM (dùng ALL ROOMS, không filter) ======
        if (_cbFixedRoom != null)
        {
            ConfigureRoomComboBox(_cbFixedRoom);
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

            EnsureRoomComboSelectedEnabled(_cbFixedRoom);
        }

        // ====== FILL COMBO "Theo phòng" (_cbRoomFilter) ======
        if (_cbRoomFilter != null)
        {
            ConfigureRoomComboBox(_cbRoomFilter);
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

            EnsureRoomComboSelectedEnabled(_cbRoomFilter);
        }

        if (_cbForceRoom != null)
        {
            ConfigureRoomComboBox(_cbForceRoom);
            var current = _cbForceRoom.SelectedItem?.ToString();

            _cbForceRoom.Items.Clear();
            foreach (var r in allRooms)
            {
                _cbForceRoom.Items.Add(r.RoomId);
            }

            if (!string.IsNullOrEmpty(current) && _cbForceRoom.Items.Contains(current))
            {
                _cbForceRoom.SelectedItem = current;
            }
            else if (_cbForceRoom.Items.Count > 0)
            {
                _cbForceRoom.SelectedIndex = 0;
            }

            EnsureRoomComboSelectedEnabled(_cbForceRoom);
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
        _txtUserId.Leave += (s, e) =>
        {
            _txtUserId.Text = (_txtUserId.Text ?? "").Trim().ToUpperInvariant();
        };

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
        _txtUserStudentId.Leave += (s, e) =>
        {
            _txtUserStudentId.Text = (_txtUserStudentId.Text ?? "").Trim().ToUpperInvariant();
        };

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "LecturerId:" });
        _txtUserLecturerId = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserLecturerId);
        _txtUserLecturerId.Leave += (s, e) =>
        {
            _txtUserLecturerId.Text = (_txtUserLecturerId.Text ?? "").Trim().ToUpperInvariant();
        };

        curY += 30;
        grpUser.Controls.Add(new Label { Left = 10, Top = curY, Width = 80, Text = "Class:" });
        _txtUserClass = new TextBox { Left = 100, Top = curY - 3, Width = 180 };
        grpUser.Controls.Add(_txtUserClass);
        _txtUserClass.Leave += (s, e) =>
        {
            _txtUserClass.Text = (_txtUserClass.Text ?? "").Trim().ToUpperInvariant();
        };

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
        ConfigureRoomComboBox(_cbBookingRoom);
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
            Height = 185
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

        _chkSendEmailGrant = new CheckBox
        {
            Left = 10,
            Top = 55,
            Width = 260,
            Text = "Send email on GRANT"
        };

        _chkSendEmailForce = new CheckBox
        {
            Left = 10,
            Top = 80,
            Width = 260,
            Text = "Send email on FORCE_GRANT/RELEASE"
        };

        _chkSendEmailNoShow = new CheckBox
        {
            Left = 10,
            Top = 105,
            Width = 260,
            Text = "Send email on NO_SHOW"
        };

        _chkNotifyClient = new CheckBox
        {
            Left = 10,
            Top = 130,
            Width = 260,
            Text = "Send notification to client"
        };

        grpGeneral.Controls.AddRange(new Control[]
        {
            lblDeadline, _numCheckinDeadlineMinutes,
            _chkSendEmailGrant, _chkSendEmailForce, _chkSendEmailNoShow, _chkNotifyClient
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
            _state.BroadcastNow();
        };

        btnUseSystem.Click += (_, __) =>
        {
            var logger = new UiLogger(this);
            _state.ResetDemoNow(logger);
            _state.BroadcastNow();
        };

        // ===== Group: Dangerous Operations =====
        var grpDangerous = new GroupBox
        {
            Text = "",
            Left = 560,
            Top = 580,
            Width = 300,
            Height = 100,
            ForeColor = Color.Red
        };
        _tabSettings.Controls.Add(grpDangerous);

        var lblWarning = new Label
        {
            Left = 10,
            Top = 25,
            Width = 280,
            Height = 30,
            Text = " WARNING: This will delete ALL data!\n(Bookings, Fixed Schedules, Locks)",
            ForeColor = Color.Red
        };
        grpDangerous.Controls.Add(lblWarning);

        _btnResetAllData = new Button
        {
            Text = " Reset All Data",
            Left = 10,
            Top = 60,
            Width = 280,
            Height = 30,
            BackColor = Color.FromArgb(255, 100, 100),
            ForeColor = Color.White,
            Font = new Font(Font, FontStyle.Bold)
        };
        _btnResetAllData.Click += BtnResetAllData_Click;
        grpDangerous.Controls.Add(_btnResetAllData);

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

        _chkSendEmailGrant.Checked = s.SendEmailOnGrant;
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
        newSettings.SendEmailOnGrant = _chkSendEmailGrant.Checked;
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

    private void BtnResetAllData_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            this,
            "⚠️ WARNING: This will permanently delete ALL data:\n\n" +
            "• All bookings (QUEUED, APPROVED, IN_USE, COMPLETED, etc.)\n" +
            "• All fixed schedules\n" +
            "• All event locks\n\n" +
            "This action CANNOT be undone!\n\n" +
            "Are you absolutely sure you want to continue?",
            "⚠️ Confirm Reset All Data",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2
        );

        if (confirm != DialogResult.Yes)
            return;

        // Double confirmation
        var doubleConfirm = MessageBox.Show(
            this,
            "⚠️ FINAL WARNING!\n\n" +
            "This is your last chance to cancel.\n\n" +
            "Click YES to permanently delete all data.\n" +
            "Click NO to cancel.",
            "⚠️ Final Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Stop,
            MessageBoxDefaultButton.Button2
        );

        if (doubleConfirm != DialogResult.Yes)
            return;

        var log = new StringWriter();
        _state.ResetAllData(log);
        Log(log.ToString());

        MessageBox.Show(
            this,
            "✅ All data has been reset successfully!\n\n" +
            "• All bookings cleared\n" +
            "• All fixed schedules deleted\n" +
            "• All slots unlocked\n" +
            "• All clients notified",
            "Reset Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        // Refresh UI
        RefreshSlotsSafe();
        RefreshFixedSchedulesGrid();
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

            // Mỗi connection sinh 1 clientId (GUID)
            string clientId = Guid.NewGuid().ToString("N");
            string? currentUserId = null;
            string? currentUserType = null;

            Log($"[SERVER] New client connected: {clientId}");

            // ✅ M1-A: Register session online ngay khi accept TCP
            _state.RegisterClient(clientId, stream);

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
                                        case "User not found": reasonCode = "USER_NOT_FOUND"; break;
                                        case "User inactive": reasonCode = "USER_INACTIVE"; break;
                                        case "Invalid password": reasonCode = "INVALID_PASSWORD"; break;
                                        default: reasonCode = "ERROR"; break;
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

                                // Ghi nhận user cho connection này
                                currentUserId = user.UserId;
                                currentUserType = user.UserType;

                                // ✅ M1-B: Bind user vào session online
                                _state.BindUser(clientId, currentUserId);

                                // (tuỳ chọn) Nếu hệ thống cũ còn dùng mapping này thì giữ lại
                                _state.MapClientToUser(clientId, currentUserId);

                                // LOGIN_OK|UserId|UserType|FullName|Email|Phone|StudentId|Class|Department|LecturerId|Faculty
                                var response =
                                    $"LOGIN_OK|{user.UserId}|{user.UserType}|{user.FullName}|" +
                                    $"{user.Email}|{user.Phone}|" +
                                    $"{user.StudentId}|{user.Class}|{user.Department}|" +
                                    $"{user.LecturerId}|{user.Faculty}\n";

                                await SendAsync(stream, response);

                                // ⭐ Push fixed schedule ngay sau khi login thành công
                                _state.PushMyFixedSchedule(currentUserId);

                                // ⭐ Flush các notification bị lỡ khi user offline
                                _state.FlushPendingNotifications(currentUserId, new UiLogger(this));

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
                                // REQUEST|UserId|RoomId|SlotId|Date|Purpose
                                if (parts.Length < 6)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid REQUEST format\n");
                                    break;
                                }

                                var userIdInMsg = parts[1];
                                var roomId = parts[2];
                                var slotId = parts[3];
                                var dateStr = parts[4];
                                var purpose = parts[5];

                                // Bắt buộc phải LOGIN trước
                                if (currentUserId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_LOGGED_IN\n");
                                    break;
                                }

                                // Không cho giả mạo userId khác
                                if (!string.Equals(currentUserId, userIdInMsg, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                // Truyền clientId (GUID) xuống ServerState
                                _state.HandleRequest(clientId, roomId, slotId, dateStr, purpose, stream, new UiLogger(this));
                                break;
                            }

                        case "RELEASE":
                            {
                                // RELEASE|UserId|RoomId|SlotId|Date
                                if (parts.Length != 5)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid RELEASE format\n");
                                    break;
                                }

                                var userIdInMsg = parts[1];
                                var roomId = parts[2];
                                var slotId = parts[3];
                                var dateStr = parts[4];

                                if (currentUserId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_LOGGED_IN\n");
                                    break;
                                }

                                if (!string.Equals(currentUserId, userIdInMsg, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleRelease(clientId, roomId, slotId, dateStr, stream, new UiLogger(this));
                                break;
                            }

                        case "REQUEST_RANGE":
                            {
                                // REQUEST_RANGE|UserId|RoomId|SlotStart|SlotEnd|Date|Purpose
                                if (parts.Length < 7)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid REQUEST_RANGE\n");
                                    break;
                                }

                                var userIdMsg = parts[1];
                                var roomId = parts[2];
                                var slotStart = parts[3];
                                var slotEnd = parts[4];
                                var dateStr = parts[5];
                                var purpose = parts[6];


                                if (currentUserId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_LOGGED_IN\n");
                                    break;
                                }

                                if (!string.Equals(userIdMsg, currentUserId, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleRequestRange(clientId, roomId, slotStart, slotEnd, dateStr, purpose, stream, new UiLogger(this));
                                break;
                            }

                        case "RELEASE_RANGE":
                            {
                                // RELEASE_RANGE|UserId|RoomId|SlotStart|SlotEnd|Date
                                if (parts.Length < 6)
                                {
                                    await SendAsync(stream, "INFO|ERROR|Invalid RELEASE_RANGE\n");
                                    break;
                                }

                                var userIdMsg = parts[1];
                                var roomId = parts[2];
                                var slotStart = parts[3];
                                var slotEnd = parts[4];
                                var dateStr = parts[5];

                                if (currentUserId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_LOGGED_IN\n");
                                    break;
                                }

                                if (!string.Equals(userIdMsg, currentUserId, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                _state.HandleReleaseRange(
                                    clientId,
                                    roomId,
                                    slotStart,
                                    slotEnd,
                                    dateStr,
                                    stream,
                                    new UiLogger(this));

                                break;
                            }

                        case "SUB_HOME":
                            {
                                if (parts.Length < 2) { await SendAsync(stream, "ERR|Invalid SUB_HOME\n"); break; }
                                var userId = parts[1];
                                _state.SubHome(clientId, userId);
                                await SendAsync(stream, "OK|SUB_HOME\n");
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
                        case "GET_FIXED_SESSIONS":
                            {
                                // GET_FIXED_SESSIONS|userId|fromDate|toDate
                                if (parts.Length < 4)
                                {
                                    await SendAsync(stream, "ERROR|INVALID_ARGS\n");
                                    break;
                                }
                                var userId = parts[1];
                                var fromDate = parts[2];
                                var toDate = parts[3];
                                _state.HandleGetFixedSessions(clientId, userId, fromDate, toDate, stream, new UiLogger(this));
                                break;
                            }
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
                                    var st = (b.Status ?? "").Trim();
                                    if (string.Equals(st, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(st, "QUEUED", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

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

                                var homeNoti = _state.GetHomeNotifications(userId, max: 20);
                                foreach (var notiMsg in homeNoti)
                                {
                                    if (string.IsNullOrWhiteSpace(notiMsg)) continue;
                                    await SendAsync(stream, $"NOTI|{notiMsg}\n");
                                }

                                await SendAsync(stream, "HOME_DATA_END\n");
                                break;
                            }

                        case "GET_SLOT_CONFIG":
                            {
                                // Không cần auth cho lệnh này
                                var rows = _state.GetSlotTimeConfigs();

                                // THÊM: báo hiệu bắt đầu block
                                await SendAsync(stream, "SLOT_CONFIG_BEGIN\n");

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
                        case "SUB_MY_BOOKINGS":
                            {
                                // SUB_MY_BOOKINGS|UserId
                                if (parts.Length < 2) { await SendAsync(stream, "ERR|Invalid SUB_MY_BOOKINGS\n"); break; }

                                var userId = parts[1];
                                _state.SubMyBookings(clientId, userId);
                                await SendAsync(stream, "OK|SUB_MY_BOOKINGS\n");
                                break;
                            }
                        case "SUB_ROOMS":
                            {
                                _state.SubRooms(clientId);
                                await SendAsync(stream, "OK|SUB_ROOMS\n");
                                break;
                            }
                        case "SUB_SLOT_CONFIG":
                            {
                                _state.SubSlotConfig(clientId);
                                await SendAsync(stream, "OK|SUB_SLOT_CONFIG\n");
                                break;
                            }
                        case "SUB_ROOM_SLOTS":
                            {
                                // SUB_ROOM_SLOTS|RoomId|yyyy-MM-dd
                                if (parts.Length < 3) { await SendAsync(stream, "ERR|Invalid SUB_ROOM_SLOTS\n"); break; }

                                var roomId = parts[1];
                                var dateKey = parts[2];

                                _state.SubRoomSlots(clientId, roomId, dateKey);
                                await SendAsync(stream, "OK|SUB_ROOM_SLOTS\n");
                                break;
                            }

                        case "UNSUB_ROOM_SLOTS":
                            {
                                // UNSUB_ROOM_SLOTS|RoomId|yyyy-MM-dd
                                if (parts.Length < 3) { await SendAsync(stream, "ERR|Invalid UNSUB_ROOM_SLOTS\n"); break; }

                                var roomId = parts[1];
                                var dateKey = parts[2];

                                _state.UnsubRoomSlots(clientId, roomId, dateKey);
                                await SendAsync(stream, "OK|UNSUB_ROOM_SLOTS\n");
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
                        case "GET_ROOM_DAILY_SLOTS":
                            {
                                if (parts.Length < 3)
                                {
                                    await SendAsync(stream, "ERR|Invalid GET_ROOM_DAILY_SLOTS format\n");
                                    break;
                                }

                                var roomId = parts[1];
                                var dateStr = parts[2];

                                if (!DateTime.TryParse(dateStr, out var date))
                                {
                                    // fallback: dùng hôm nay nếu parse lỗi
                                    date = _state.Now.Date;
                                }

                                var slots = _state.GetDailySchedule(date, roomId, new UiLogger(this));

                                await SendAsync(stream, "ROOM_SLOTS_BEGIN\n");

                                foreach (var s in slots)
                                {
                                    // SLOT|SlotId|TimeRange|Status|UserId|FullName|BookingStatus|Purpose
                                    var lineOut =
                                        $"SLOT|{s.SlotId}|{s.TimeRange}|{s.Status}|{s.UserId}|{s.FullName}|{s.BookingStatus}|{s.Purpose}";
                                    await SendAsync(stream, lineOut + "\n");
                                }

                                await SendAsync(stream, "ROOM_SLOTS_END\n");
                                break;
                            }
                        case "GET_MY_BOOKINGS":
                            {
                                if (parts.Length < 2)
                                {
                                    await SendAsync(stream, "ERR|Invalid GET_MY_BOOKINGS format\n");
                                    break;
                                }

                                var userIdMsg = parts[1];

                                // BẮT BUỘC LOGIN trước
                                if (currentUserId == null)
                                {
                                    await SendAsync(stream, "INFO|ERROR|NOT_LOGGED_IN\n");
                                    break;
                                }

                                // Không cho giả mạo user khác
                                if (!string.Equals(userIdMsg, currentUserId, StringComparison.OrdinalIgnoreCase))
                                {
                                    await SendAsync(stream, "INFO|ERROR|USER_MISMATCH\n");
                                    break;
                                }

                                // Lấy lịch trong khoảng [hôm qua - 30 ngày tới]
                                var today = _state.Now.Date;
                                var from = today.AddDays(-1);
                                var to = today.AddDays(30);

                                var list = _state.GetUserSchedule(userIdMsg, from, to);

                                await SendAsync(stream, "MY_BOOKINGS_BEGIN\n");

                                foreach (var b in list)
                                {
                                    // FORMAT CHUẨN:
                                    // BOOKING|BookingId|Date|RoomId|SlotFrom|SlotTo|TimeRange|Status|Purpose|CreatedAt|CheckinDeadline|CheckinTime|UpdatedAt
                                    var lineOut =
                                        $"BOOKING" +
                                        $"|{b.BookingId}" +
                                        $"|{b.Date:yyyy-MM-dd}" +
                                        $"|{b.RoomId}" +
                                        $"|{b.SlotStartId}" +
                                        $"|{b.SlotEndId}" +
                                        $"|{b.TimeRange}" +
                                        $"|{b.Status}" +
                                        $"|{b.Purpose}" +
                                        $"|{b.CreatedAt:yyyy-MM-dd HH:mm:ss}" +
                                        $"|{b.CheckinDeadline:yyyy-MM-dd HH:mm:ss}" +
                                        $"|{(b.CheckinTime.HasValue ? b.CheckinTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}" +
                                        $"|{b.UpdatedAt:yyyy-MM-dd HH:mm:ss}";

                                    await SendAsync(stream, lineOut + "\n");
                                }

                                await SendAsync(stream, "MY_BOOKINGS_END\n");
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
                Log($"[SERVER] Client {clientId} disconnected");

                // 1️⃣ M2 – gỡ toàn bộ subscription trước
                _state.UnsubscribeAll(clientId);

                // 2️⃣ M1 – remove session + user mapping
                _state.UnregisterClient(clientId);

                // 3️⃣ (tuỳ chọn) UI / log thêm
                _state.HandleDisconnect(clientId, new UiLogger(this));
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
        Console.WriteLine($"[REFRESH_SLOTS] Called at {DateTime.Now:HH:mm:ss.fff}, InvokeRequired={InvokeRequired}");
        if (InvokeRequired)
        {
            // Dùng Invoke thay vì BeginInvoke để update ngay lập tức, không delay
            Console.WriteLine($"[REFRESH_SLOTS] Invoking to UI thread");
            Invoke(new Action(RefreshSlots));
            Console.WriteLine($"[REFRESH_SLOTS] Invoke completed at {DateTime.Now:HH:mm:ss.fff}");
            return;
        }
        RefreshSlots();
        Console.WriteLine($"[REFRESH_SLOTS] RefreshSlots completed at {DateTime.Now:HH:mm:ss.fff}");
    }

    private void RefreshSlots()
    {
        Console.WriteLine($"[REFRESH_SLOTS][DETAIL] Starting RefreshSlots at {DateTime.Now:HH:mm:ss.fff}");
        var summaries = _state.GetAllSlotSummaries();
        Console.WriteLine($"[REFRESH_SLOTS][DETAIL] Got {summaries.Count} summaries at {DateTime.Now:HH:mm:ss.fff}");

        string? selectedRoomId = null;
        string? selectedSlotId = null;
        if (_gridSlots.CurrentRow?.DataBoundItem is SlotSummary selected)
        {
            selectedRoomId = selected.RoomId;
            selectedSlotId = selected.SlotId;
        }

        _suppressSlotSelectionChanged = true;
        try
        {
            _gridSlotsBinding.RaiseListChangedEvents = false;
            _gridSlotsBinding.Clear();
            foreach (var s in summaries)
                _gridSlotsBinding.Add(s);
            _gridSlotsBinding.RaiseListChangedEvents = true;
            _gridSlotsSource.ResetBindings(false);

            ApplySlotGridDisabledRoomStyle();

            Console.WriteLine($"[REFRESH_SLOTS][DETAIL] Updated BindingList at {DateTime.Now:HH:mm:ss.fff}");
        }
        finally
        {
            _suppressSlotSelectionChanged = false;
        }

        if (_gridSlots.Rows.Count > 0 && _gridSlots.CurrentRow == null)
        {
            _gridSlots.Rows[0].Selected = true;
        }

        if (!string.IsNullOrEmpty(selectedRoomId) && !string.IsNullOrEmpty(selectedSlotId))
        {
            foreach (DataGridViewRow r in _gridSlots.Rows)
            {
                if (r.DataBoundItem is SlotSummary ss &&
                    string.Equals(ss.RoomId, selectedRoomId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(ss.SlotId, selectedSlotId, StringComparison.OrdinalIgnoreCase))
                {
                    _gridSlots.CurrentCell = r.Cells[0];
                    r.Selected = true;
                    break;
                }
            }
        }

        UpdateQueueViewForSelected();
        UpdateCheckinPanel();

        if (_tabLeft != null && _tabLeftByRoom != null && _tabLeft.SelectedTab == _tabLeftByRoom &&
            _gridRoomDaily != null && _cbRoomFilter != null)
        {
            var roomId = _cbRoomFilter.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                string? selectedDailySlotId = null;
                if (_gridRoomDaily.CurrentRow?.DataBoundItem is SlotSummary dailySelected)
                    selectedDailySlotId = dailySelected.SlotId;

                var roomDaily = summaries
                    .Where(s => string.Equals(s.RoomId, roomId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s =>
                    {
                        var t = (s.SlotId ?? "").Trim().ToUpperInvariant();
                        if (t.StartsWith("S") && int.TryParse(t.Substring(1), out var idx))
                            return idx;
                        return int.MaxValue;
                    })
                    .ToList();

                _gridRoomDaily.AutoGenerateColumns = true;
                _gridRoomDaily.DataSource = null;
                _gridRoomDaily.DataSource = roomDaily;

                if (!string.IsNullOrWhiteSpace(selectedDailySlotId))
                {
                    foreach (DataGridViewRow r in _gridRoomDaily.Rows)
                    {
                        if (r.DataBoundItem is SlotSummary ss &&
                            string.Equals(ss.SlotId, selectedDailySlotId, StringComparison.OrdinalIgnoreCase))
                        {
                            _gridRoomDaily.CurrentCell = r.Cells[0];
                            r.Selected = true;
                            break;
                        }
                    }
                }
            }
        }
        Console.WriteLine($"[REFRESH_SLOTS][DETAIL] Completed RefreshSlots at {DateTime.Now:HH:mm:ss.fff}");
    }

    private void GridRoomDaily_SelectionChanged(object? sender, EventArgs e)
    {
        if (_gridRoomDaily == null || _gridSlots == null)
            return;

        if (_gridRoomDaily.CurrentRow?.DataBoundItem is not SlotSummary daily)
            return;

        var roomId = daily.RoomId;
        var slotId = daily.SlotId;
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(slotId))
            return;

        if (_dtRoomFilterDate != null && _dtDate != null)
        {
            var desired = _dtRoomFilterDate.Value.Date;
            if (_dtDate.Value.Date != desired)
                _dtDate.Value = desired;
        }

        foreach (DataGridViewRow r in _gridSlots.Rows)
        {
            if (r.DataBoundItem is SlotSummary ss &&
                string.Equals(ss.RoomId, roomId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ss.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
            {
                _gridSlots.CurrentCell = r.Cells[0];
                r.Selected = true;
                break;
            }
        }
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
        if (_suppressSlotSelectionChanged)
            return;

        if (_gridSlots.CurrentRow?.DataBoundItem is SlotSummary ss)
        {
            if (IsRoomDisabledUi(ss.RoomId))
            {
                _suppressSlotSelectionChanged = true;
                try
                {
                    if (!string.IsNullOrWhiteSpace(_lastEnabledSlotRoomId) && !string.IsNullOrWhiteSpace(_lastEnabledSlotSlotId))
                    {
                        foreach (DataGridViewRow r in _gridSlots.Rows)
                        {
                            if (r.DataBoundItem is SlotSummary prev &&
                                string.Equals(prev.RoomId, _lastEnabledSlotRoomId, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(prev.SlotId, _lastEnabledSlotSlotId, StringComparison.OrdinalIgnoreCase))
                            {
                                _gridSlots.CurrentCell = r.Cells[0];
                                r.Selected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (DataGridViewRow r in _gridSlots.Rows)
                        {
                            if (r.DataBoundItem is SlotSummary prev && !IsRoomDisabledUi(prev.RoomId))
                            {
                                _gridSlots.CurrentCell = r.Cells[0];
                                r.Selected = true;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _suppressSlotSelectionChanged = false;
                }
                return;
            }

            _lastEnabledSlotRoomId = ss.RoomId;
            _lastEnabledSlotSlotId = ss.SlotId;
        }

        UpdateQueueViewForSelected();
        UpdateCheckinPanel();
    }
    private void UpdateCheckinPanel()
    {
        // Guard: if UI not fully initialized, skip
        if (_gridSlots == null || _lblSelectedSlot == null || _btnCheckIn == null || _lblBookingUser == null)
            return;

        _btnCheckIn.Enabled = false;
        _btnComplete.Enabled = false;

        if (_gridSlots.CurrentRow == null)
        {
            _lblSelectedSlot.Text = "Slot: (chưa chọn)";
            ClearBookingDetails();
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
            ClearBookingDetails();
            return;
        }

        _lblSelectedSlot.Text = $"Slot: {roomId} - {slotId} - {_dtDate.Value:yyyy-MM-dd}";

        // Gọi ServerState để lấy booking hiện tại của slot
        var date = _dtDate.Value.Date;
        var booking = _state.GetCurrentBookingForSlot(date, roomId, slotId);

        // Lấy trạng thái slot
        var dateKey = date.ToString("yyyy-MM-dd");
        var slotState = _state.GetSlotState(dateKey, roomId, slotId);
        if (slotState != null && slotState.IsEventLocked)
        {
            // Nếu là event lock, hiển thị thông tin FixedSession (nếu có)
            var fixedSession = _state.GetFixedSessionForSlot(date, roomId, slotId);
            if (fixedSession != null)
            {
                _lblBookingId.Text = $"FixedSessionId: {fixedSession.SessionId}";
                _lblBookingUserId.Text = $"Lecturer: {fixedSession.LecturerUserId}";
                _lblBookingRoomId.Text = $"RoomId: {fixedSession.RoomId}";
                _lblBookingDate.Text = $"Date: {dateKey}";
                _lblBookingSlotStartId.Text = $"SlotStartId: {fixedSession.SlotStartId}";
                _lblBookingSlotEndId.Text = $"SlotEndId: {fixedSession.SlotEndId}";
                _lblBookingIsRange.Text = $"IsRangeBooking: true (Fixed)";
                _lblBookingStatus.Text = $"Status: EVENT_LOCKED";
                _lblBookingPurpose.Text = $"Note: {fixedSession.Note}";
                _lblBookingCreatedAt.Text = $"CreatedAt: {fixedSession.CreatedAt:yyyy-MM-dd HH:mm:ss}";
                _lblBookingUpdatedAt.Text = $"UpdatedAt: {fixedSession.UpdatedAt:yyyy-MM-dd HH:mm:ss}";
                _lblBookingUser.Text = $"Fixed schedule: {fixedSession.SubjectCode} - {fixedSession.SubjectName} ({fixedSession.Class})";
                _btnCheckIn.Enabled = false;
                _btnComplete.Enabled = false;
                return;
            }
        }

        if (booking == null)
        {
            _lblBookingUser.Text = "User: (không có booking)";
            ClearBookingDetails();
            return;
        }

        // Hiển thị tất cả thông tin booking
        _lblBookingId.Text = $"BookingId: {booking.BookingId}";
        _lblBookingUserId.Text = $"UserId: {booking.UserId}";
        _lblBookingRoomId.Text = $"RoomId: {booking.RoomId}";
        _lblBookingDate.Text = $"Date: {booking.Date}";
        _lblBookingSlotStartId.Text = $"SlotStartId: {booking.SlotStartId}";
        _lblBookingSlotEndId.Text = $"SlotEndId: {booking.SlotEndId}";
        _lblBookingIsRange.Text = $"IsRangeBooking: {booking.IsRange}";
        _lblBookingStatus.Text = $"Status: {booking.Status}";
        _lblBookingPurpose.Text = $"Purpose: {booking.Purpose}";
        _lblBookingCreatedAt.Text = $"CreatedAt: {booking.CreatedAt:yyyy-MM-dd HH:mm:ss}";
        _lblBookingUpdatedAt.Text = $"UpdatedAt: {booking.UpdatedAt:yyyy-MM-dd HH:mm:ss}";

        _lblBookingUser.Text = $"User: {booking.UserId} - {booking.FullName} ({booking.UserType})";

        // Enable nút theo trạng thái
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

    private void ClearBookingDetails()
    {
        if (_lblBookingId != null) _lblBookingId.Text = "BookingId: -";
        if (_lblBookingUserId != null) _lblBookingUserId.Text = "UserId: -";
        if (_lblBookingRoomId != null) _lblBookingRoomId.Text = "RoomId: -";
        if (_lblBookingDate != null) _lblBookingDate.Text = "Date: -";
        if (_lblBookingSlotStartId != null) _lblBookingSlotStartId.Text = "SlotStartId: -";
        if (_lblBookingSlotEndId != null) _lblBookingSlotEndId.Text = "SlotEndId: -";
        if (_lblBookingIsRange != null) _lblBookingIsRange.Text = "IsRangeBooking: -";
        if (_lblBookingStatus != null) _lblBookingStatus.Text = "Status: -";
        if (_lblBookingPurpose != null) _lblBookingPurpose.Text = "Purpose: -";
        if (_lblBookingCreatedAt != null) _lblBookingCreatedAt.Text = "CreatedAt: -";
        if (_lblBookingUpdatedAt != null) _lblBookingUpdatedAt.Text = "UpdatedAt: -";
        if (_lblBookingUser != null) _lblBookingUser.Text = "User: -";
    }

    private void LstQueue_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lstQueue.SelectedIndex < 0)
        {
            ClearQueueUserDetails();
            return;
        }

        var selectedText = _lstQueue.SelectedItem?.ToString();
        var userId = ExtractUserIdFromQueueItem(selectedText);

        if (string.IsNullOrWhiteSpace(userId))
        {
            ClearQueueUserDetails();
            return;
        }

        if (_state.UsersInfo.TryGetValue(userId, out var userInfo))
        {
            _lblQueueUserUserId.Text = $"UserId: {userInfo.UserId}";
            _lblQueueUserUserType.Text = $"UserType: {userInfo.UserType}";
            _lblQueueUserFullName.Text = $"FullName: {userInfo.FullName}";
            _lblQueueUserStudentId.Text = $"StudentId: {userInfo.StudentId ?? "-"}";
            _lblQueueUserClass.Text = $"Class: {userInfo.Class ?? "-"}";
            _lblQueueUserDepartment.Text = $"Department: {userInfo.Department ?? "-"}";
            _lblQueueUserLecturerId.Text = $"LecturerId: {userInfo.LecturerId ?? "-"}";
            _lblQueueUserFaculty.Text = $"Faculty: {userInfo.Faculty ?? "-"}";
            _lblQueueUserEmail.Text = $"Email: {userInfo.Email ?? "-"}";
            _lblQueueUserPhone.Text = $"Phone: {userInfo.Phone ?? "-"}";
            _lblQueueUserIsActive.Text = $"IsActive: {userInfo.IsActive}";
        }
        else
        {
            // Debug nhanh: bạn có thể tạm show cái userId đã parse
            _lblQueueUserUserId.Text = $"UserId: {userId} (NOT FOUND)";
            ClearQueueUserDetailsExceptUserId();
        }
    }
    private string ExtractUserIdFromQueueItem(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var t = text.Trim();

        // bỏ các dòng không phải user
        if (t.Equals("Queue empty", StringComparison.OrdinalIgnoreCase)) return "";
        if (t.Equals("No selection", StringComparison.OrdinalIgnoreCase)) return "";

        // 1) bỏ prefix "1. "
        int dot = t.IndexOf('.');
        if (dot >= 0 && dot <= 3) // "1.", "12.", "999."
        {
            // nếu trước dấu '.' toàn số thì strip
            var prefix = t.Substring(0, dot).Trim();
            if (prefix.All(char.IsDigit))
                t = t.Substring(dot + 1).Trim();
        }

        // 2) nếu có " - " thì lấy trước " - "
        int dash = t.IndexOf(" - ");
        if (dash > 0) t = t.Substring(0, dash).Trim();

        // 3) nếu có suffix "(RANGE ...)" thì bỏ
        int paren = t.IndexOf(" (", StringComparison.Ordinal);
        if (paren > 0) t = t.Substring(0, paren).Trim();

        // 4) nếu còn dư token, lấy token đầu tiên
        int space = t.IndexOf(' ');
        if (space > 0) t = t.Substring(0, space).Trim();

        return t;
    }
    private void ClearQueueUserDetails()
    {
        if (_lblQueueUserUserId != null) _lblQueueUserUserId.Text = "UserId: -";
        if (_lblQueueUserUserType != null) _lblQueueUserUserType.Text = "UserType: -";
        if (_lblQueueUserFullName != null) _lblQueueUserFullName.Text = "FullName: -";
        if (_lblQueueUserStudentId != null) _lblQueueUserStudentId.Text = "StudentId: -";
        if (_lblQueueUserClass != null) _lblQueueUserClass.Text = "Class: -";
        if (_lblQueueUserDepartment != null) _lblQueueUserDepartment.Text = "Department: -";
        if (_lblQueueUserLecturerId != null) _lblQueueUserLecturerId.Text = "LecturerId: -";
        if (_lblQueueUserFaculty != null) _lblQueueUserFaculty.Text = "Faculty: -";
        if (_lblQueueUserEmail != null) _lblQueueUserEmail.Text = "Email: -";
        if (_lblQueueUserPhone != null) _lblQueueUserPhone.Text = "Phone: -";
        if (_lblQueueUserIsActive != null) _lblQueueUserIsActive.Text = "IsActive: -";
    }
    private void ClearQueueUserDetailsExceptUserId()
    {
        if (_lblQueueUserUserType != null) _lblQueueUserUserType.Text = "UserType: -";
        if (_lblQueueUserFullName != null) _lblQueueUserFullName.Text = "FullName: -";
        if (_lblQueueUserStudentId != null) _lblQueueUserStudentId.Text = "StudentId: -";
        if (_lblQueueUserClass != null) _lblQueueUserClass.Text = "Class: -";
        if (_lblQueueUserDepartment != null) _lblQueueUserDepartment.Text = "Department: -";
        if (_lblQueueUserLecturerId != null) _lblQueueUserLecturerId.Text = "LecturerId: -";
        if (_lblQueueUserFaculty != null) _lblQueueUserFaculty.Text = "Faculty: -";
        if (_lblQueueUserEmail != null) _lblQueueUserEmail.Text = "Email: -";
        if (_lblQueueUserPhone != null) _lblQueueUserPhone.Text = "Phone: -";
        if (_lblQueueUserIsActive != null) _lblQueueUserIsActive.Text = "IsActive: -";
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

        targetUserId = targetUserId.ToUpperInvariant();
        _txtForceUserId.Text = targetUserId;

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
        var roomId = _cbForceRoom.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(roomId) && _gridSlots.CurrentRow != null)
        {
            var row = _gridSlots.CurrentRow;
            roomId = row.Cells["RoomId"].Value?.ToString();
        }
        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Hãy chọn phòng cho range.",
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

        targetUserId = targetUserId.ToUpperInvariant();
        _txtForceUserId.Text = targetUserId;

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
        var roomId = _cbForceRoom.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(roomId) && _gridSlots.CurrentRow != null)
        {
            var row = _gridSlots.CurrentRow;
            roomId = row.Cells["RoomId"].Value?.ToString();
        }
        if (string.IsNullOrEmpty(roomId))
        {
            MessageBox.Show(this, "Hãy chọn phòng cho range.",
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
        if (_gridSlots == null || _lblQueueTitle == null || _lstQueue == null)
            return;

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

        // ✅ FIX: lấy queue đúng ngày của row đang chọn
        var clients = _state.GetQueueClients(summary.Date, roomId, slotId);

        _lstQueue.Items.Clear();
        if (clients.Count == 0) _lstQueue.Items.Add("Queue empty");
        else for (int i = 0; i < clients.Count; i++) _lstQueue.Items.Add($"{i + 1}. {clients[i]}");
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

    private void RefreshFixedSchedulesGrid()
    {
        if (_gridFixedSchedules == null) return;

        var allSessions = _state.GetAllFixedSessions();

        // Group sessions by SubjectCode+RoomId+SlotStartId+SlotEndId để tránh duplicate
        // Mỗi group chỉ hiển thị 1 row với DateFrom = min date, DateTo = max date
        var grouped = allSessions
            .GroupBy(s => new { s.SubjectCode, s.SubjectName, s.Class, s.RoomId, s.SlotStartId, s.SlotEndId })
            .Select(g => new
            {
                SubjectCode = g.Key.SubjectCode,
                SubjectName = g.Key.SubjectName,
                Class = g.Key.Class,
                RoomId = g.Key.RoomId,
                SlotRange = $"{g.Key.SlotStartId}-{g.Key.SlotEndId}",
                DateFrom = g.Min(s => s.DateFrom),
                DateTo = g.Max(s => s.DateTo),
                SessionIds = g.Select(s => s.SessionId).ToList() // Lưu tất cả SessionIds để xoá
            })
            .ToList();

        _gridFixedSchedules.AutoGenerateColumns = false;
        _gridFixedSchedules.Columns.Clear();

        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "SubjectCode",
            HeaderText = "Subject",
            Width = 70
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "SubjectName",
            HeaderText = "Name",
            Width = 100
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Class",
            HeaderText = "Class",
            Width = 60
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "RoomId",
            HeaderText = "Room",
            Width = 50
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "SlotRange",
            HeaderText = "Slots",
            Width = 60
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DateFrom",
            HeaderText = "From",
            Width = 80
        });
        _gridFixedSchedules.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DateTo",
            HeaderText = "To",
            Width = 80
        });

        _gridFixedSchedules.DataSource = grouped;
        // Log chỉ khi _txtLog đã được khởi tạo
        if (_txtLog != null)
            Log($"[UI] Loaded {grouped.Count} unique fixed schedules (from {allSessions.Count} sessions).");
    }

    private void BtnDeleteFixedSchedule_Click(object? sender, EventArgs e)
    {
        if (_gridFixedSchedules.CurrentRow == null)
        {
            MessageBox.Show(this, "Vui lòng chọn Fixed Schedule cần xoá.", "Delete Fixed Schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // DataBoundItem là anonymous type với SessionIds (List<Guid>)
        var item = _gridFixedSchedules.CurrentRow.DataBoundItem;
        if (item == null) return;

        // Dùng reflection để lấy properties từ anonymous type
        var type = item.GetType();
        var subjectCode = type.GetProperty("SubjectCode")?.GetValue(item)?.ToString() ?? "";
        var subjectName = type.GetProperty("SubjectName")?.GetValue(item)?.ToString() ?? "";
        var className = type.GetProperty("Class")?.GetValue(item)?.ToString() ?? "";
        var roomId = type.GetProperty("RoomId")?.GetValue(item)?.ToString() ?? "";
        var slotRange = type.GetProperty("SlotRange")?.GetValue(item)?.ToString() ?? "";
        var dateFrom = type.GetProperty("DateFrom")?.GetValue(item)?.ToString() ?? "";
        var dateTo = type.GetProperty("DateTo")?.GetValue(item)?.ToString() ?? "";
        var sessionIds = type.GetProperty("SessionIds")?.GetValue(item) as List<Guid>;

        if (sessionIds == null || sessionIds.Count == 0)
        {
            MessageBox.Show(this, "Không tìm thấy SessionIds.", "Delete Fixed Schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Bạn chắc chắn muốn xoá Fixed Schedule:\n" +
            $"Subject: {subjectCode} - {subjectName}\n" +
            $"Class: {className}\n" +
            $"Room: {roomId}\n" +
            $"Slots: {slotRange}\n" +
            $"Date: {dateFrom} to {dateTo}\n" +
            $"Total sessions: {sessionIds.Count}\n\n" +
            $"Tất cả slots đã lock sẽ được unlock.",
            "Xác nhận xoá Fixed Schedule",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        var log = new StringWriter();
        int deletedCount = 0;
        foreach (var sessionId in sessionIds)
        {
            if (_state.DeleteFixedSchedule(sessionId, log, out var error))
            {
                deletedCount++;
            }
            else
            {
                Log($"[ERROR] Failed to delete session {sessionId}: {error}");
            }
        }

        Log(log.ToString());
        MessageBox.Show(this, $"Đã xoá {deletedCount}/{sessionIds.Count} Fixed Schedule sessions và unlock các slots.", "Delete Fixed Schedule",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshFixedSchedulesGrid();
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
        // Get controls from Tag
        var tagArr = (_btnFixedApply.Tag as object[]);
        var cbFixedClass = tagArr?[0] as ComboBox;
        var cbFixedLecturer = tagArr?[1] as ComboBox;
        var lbFixedStudents = tagArr?[2] as ListBox;

        var className = cbFixedClass?.SelectedItem?.ToString() ?? "";
        var lecturerInfo = cbFixedLecturer?.SelectedItem?.ToString() ?? "";
        var lecturerId = lecturerInfo.Split(' ').FirstOrDefault() ?? "";
        var studentIds = new List<string>();
        if (lbFixedStudents != null)
        {
            foreach (var item in lbFixedStudents.SelectedItems)
            {
                var id = item.ToString()?.Split(' ').FirstOrDefault();
                if (!string.IsNullOrEmpty(id)) studentIds.Add(id);
            }
        }

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

        // TODO: Update CreateFixedWeeklyClassSchedule to accept lecturerId and studentIds
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
                out var error,
                lecturerId,
                studentIds))
        {
            MessageBox.Show(this, error, "Tạo lịch cố định thất bại",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // ✅ UI sẽ tự động refresh qua event FixedScheduleCreated
        // Note: PushMyFixedSchedule đã được gọi trong CreateFixedWeeklyClassSchedule
        
        MessageBox.Show(this, "Đã tạo lịch cố định cho môn học.", "Fixed schedule",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }


    private class UiLogger : TextWriter
    {
        private readonly Form1 _form;
        public UiLogger(Form1 form) => _form = form;
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value)
        {
            if (value != null)
            {
                _form.Log(value);
                Console.WriteLine(value); // Ghi ra console để debug
            }
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
        logger.WriteLine("FORM CLOSING CALLED!");
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
        var userId = (_txtUserId.Text ?? "").Trim().ToUpperInvariant();
        var fullName = _txtUserFullName.Text.Trim();
        var userType = _cbUserType.SelectedItem?.ToString() ?? "";
        var email = _txtUserEmail.Text.Trim();
        var phone = _txtUserPhone.Text.Trim();
        var studentId = (_txtUserStudentId.Text ?? "").Trim().ToUpperInvariant();
        var lecturerId = (_txtUserLecturerId.Text ?? "").Trim().ToUpperInvariant();
        var facultySel = _cbUserFaculty.SelectedItem?.ToString() ?? "";
        var className = (_txtUserClass.Text ?? "").Trim().ToUpperInvariant();

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
            Class = className,
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
        var userId = (_txtUserId.Text ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(userId))
        {
            MessageBox.Show("UserId is required.");
            return;
        }

        var fullName = _txtUserFullName.Text.Trim();
        var userType = _cbUserType.SelectedItem?.ToString() ?? "";
        var email = _txtUserEmail.Text.Trim();
        var phone = _txtUserPhone.Text.Trim();
        var studentId = (_txtUserStudentId.Text ?? "").Trim().ToUpperInvariant();
        var lecturerId = (_txtUserLecturerId.Text ?? "").Trim().ToUpperInvariant();
        var facultySel = _cbUserFaculty.SelectedItem?.ToString() ?? "";
        var isActive = _chkUserActive.Checked;
        var className = (_txtUserClass.Text ?? "").Trim().ToUpperInvariant();

        var updatedUser = new UserInfo
        {
            UserId = userId,
            FullName = fullName,
            UserType = userType,
            Email = email,
            Phone = phone,
            StudentId = studentId,
            LecturerId = lecturerId,
            Class = className,
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

        var desiredDate = _dtRoomFilterDate.Value.Date;
        if (_dtDate.Value.Date != desiredDate)
        {
            _dtDate.Value = desiredDate;
        }

        var roomDaily = _gridSlotsBinding
            .Where(s => string.Equals(s.RoomId, roomId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s =>
            {
                var t = (s.SlotId ?? "").Trim().ToUpperInvariant();
                if (t.StartsWith("S") && int.TryParse(t.Substring(1), out var idx))
                    return idx;
                return int.MaxValue;
            })
            .ToList();

        _gridRoomDaily.AutoGenerateColumns = true;
        _gridRoomDaily.DataSource = null;
        _gridRoomDaily.DataSource = roomDaily;

        if (_gridRoomDaily.Rows.Count > 0 && _gridRoomDaily.CurrentRow == null)
            _gridRoomDaily.Rows[0].Selected = true;
    }

    // ===== HELPER: Tạo form con hiển thị Slots & Tabs =====
    /// <summary>
    /// Tạo một form con chứa BuildLeftTabs (Slot view) và BuildRightTabs (Management tabs)
    /// Có thể phóng to, thu nhỏ, hoặc đặt ở bất kỳ vị trí nào.
    /// </summary>
    public void ShowSlotsAndTabsWindow(string title = "Slots & Management", int width = 1200, int height = 750)
    {
        var form = new Form
        {
            Text = title,
            Width = width,
            Height = height,
            StartPosition = FormStartPosition.CenterParent,
            ShowIcon = false
        };

        // Tạo layout giống như form chính
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 450,
            BorderStyle = BorderStyle.Fixed3D
        };
        form.Controls.Add(mainSplit);

        // Dùng các hàm build existing (BuildLeftTabs/BuildRightTabs đã tách sẵn)
        // Tạm thời: tái sử dụng lại các control của form hiện tại
        // Trong thực tế, bạn có thể:
        // 1. Tách BuildLeftTabs/BuildRightTabs thành method không phụ thuộc field
        // 2. Truyền Panel vào thay vì _mainSplit
        // Ví dụ:
        //   BuildLeftTabsIntoPanel(mainSplit.Panel1, serverState);
        //   BuildRightTabsIntoPanel(mainSplit.Panel2, serverState);

        form.Show(this);
    }

}

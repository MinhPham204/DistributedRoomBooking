// // ====== FIXED SCHEDULE DATA ======

// //bookingclient/mainclientform.cs
// using System;
// using System.Drawing;
// using System.Windows.Forms;
// using System.Net.Sockets;
// using System.Text;
// using System.Threading.Tasks;
// using System.Drawing.Drawing2D;
// using System.IO;
// using System.Collections.Generic;
// using System.Windows.Forms;
// using System.ComponentModel;
// using System.Linq;

// // ĐẶT ALIAS RÕ RÀNG ĐỂ HẾT LỖI AMBIGUOUS TIMER
// using WinFormsTimer = System.Windows.Forms.Timer;

// namespace BookingClient
// {
//     public class MainClientForm : Form
//     {
//         private readonly LoginForm _loginForm;
//         private bool _isLoggingOut = false;
//         private bool _isReadingRooms = false;
//         private string _pendingRoomsJson = "";
//         private bool _isReadingHome = false;
//         private List<string> _homeScheduleLines = new List<string>();
//         private List<string> _homeNotificationLines = new List<string>();
//         private bool _isReadingSlotConfig = false;
//         private List<(string slotId, string start, string end)> _slotConfigTemp =
//             new List<(string, string, string)>();
//         //TCP Conection
//         private TcpClient? _tcp;
//         private NetworkStream? _stream;
//         private StreamWriter? _writer;
//         private StreamReader? _reader;

//         // Header
//         private Panel _panelHeader = null!;
//         private PictureBox _picAvatar = null!;
//         private Label _lblNameWithType = null!;
//         private Label _lblSubInfo = null!;
//         private Label _lblToday = null!;
//         private Button _btnHeaderCheckConnect = null!;
//         private Panel _pnlHeaderConnectDot = null!;
//         private Label _lblHeaderConnectText = null!;
//         private Label _lblRunningTime = null!;
//         private WinFormsTimer _timerClock = null!;
//         private WinFormsTimer _timerTimeSync = null!;   // <<< thêm dòng này

//         private TableLayoutPanel _rootLayout = null!;
//         private Panel _panelSidebar = null!;
//         private Panel _panelContentHost = null!;
//         private Panel _navHome = null!;
//         private Panel _navBooking = null!;
//         private Panel _navSchedule = null!;
//         private Panel _navLog = null!;
//         private Panel _navAccount = null!;
//         private Panel _profileCard = null!;
//         private bool _debugLogVisible = false;

//         // Sub-header (cấu hình ca)
//         private Panel _panelSubHeader = null!;
//         private Label _lblSlotConfig = null!;
//         private Button _btnSlotConfigHelp = null!;

//         // Tabs
//         private TabControl _tabMain = null!;
//         private TabPage _tabHome = null!;
//         private TabPage _tabBooking = null!;
//         private TabPage _tabSchedule = null!;
//         private TabPage _tabNotifications = null!;
//         private TabPage _tabLog = null!;
//         private TabPage _tabAccount = null!;

//         // ======= Controls cho tab Trang chủ =======
//         private GroupBox _grpTodaySchedule = null!;
//         private DataGridView _gridTodaySchedule = null!;
//         private GroupBox _grpLatestNotifications = null!;
//         private ListBox _lstLatestNotifications = null!;
//         private Button _btnGoBookingTab = null!;
//         private Button _btnGoMyWeekSchedule = null!;

//         // ======= Controls cho tab Đặt phòng (UI) =======
//         // A. Filter + list phòng
//         private GroupBox _grpSearchRooms = null!;
//         private DateTimePicker _dtBookingDate = null!;
//         private ComboBox _cbBookingFromSlot = null!;
//         private ComboBox _cbBookingToSlot = null!;
//         private NumericUpDown _numMinCapacity = null!;
//         private CheckBox _chkNeedProjector = null!;
//         private CheckBox _chkNeedPC = null!;
//         private CheckBox _chkNeedAC = null!;
//         private ComboBox _cbBuilding = null!;
//         private CheckBox _chkNeedMic = null!;
//         private Button _btnSearchRooms = null!;
//         private DataGridView _gridRooms = null!;
//         private FlowLayoutPanel _flpRoomCards = null!;

//         private GroupBox _grpRoomSlots = null!;
//         private Panel _pnlBookingWizardHeader = null!;
//         private Label _lblBookingStep1 = null!;
//         private Label _lblBookingStep2 = null!;
//         private Label _lblBookingStep3 = null!;
//         private bool _suppressRoomSelectionHandler = false;
//         private string? _lastEnabledRoomId;
//         private readonly Dictionary<string, Panel> _roomCardById = new(StringComparer.OrdinalIgnoreCase);
//         private string? _selectedRoomCardId;

//         // B. Gửi request
//         private GroupBox _grpRequest = null!;
//         private ComboBox _cbReqRoom = null!;
//         private DateTimePicker _dtReqDate = null!;
//         private ComboBox _cbReqSlotSingle = null!;
//         private ComboBox _cbReqSlotFrom = null!;
//         private ComboBox _cbReqSlotTo = null!;
//         private TextBox _txtPurpose = null!;
//         private Label _lblSlotTimeRange = null!;
//         private Button _btnReqSingle = null!;
//         private Button _btnReqRange = null!;
//         private Button _btnReleaseSingle = null!;
//         private Button _btnReleaseRange = null!;
//         private Label _lblRequestStatus = null!;
//         private GroupBox _grpClientLog = null!;
//         private TextBox _txtClientLog = null!;

//         // ======= Tab Lịch của tôi =======
//         private ComboBox _cbScheduleWeek = null!;
//         private DataGridView _gridWeekView = null!;
//         private Button _btnExportSchedule = null!;
//         private Button _btnBackHomeFromSchedule = null!;
//         private MonthCalendar _calSchedule = null!;
//         private ToolTip _ttSchedule = null!;
//         private int _scheduleTooltipRow = -1;
//         private int _scheduleTooltipCol = -1;

//         // ======= Tab Thông báo =======
//         private DataGridView _gridNotifications = null!;
//         private Button _btnMarkAllRead = null!;
//         private ComboBox _cbFilterType = null!;
//         private DateTimePicker _dtNotiFrom = null!;
//         private DateTimePicker _dtNotiTo = null!;

//         // ======= Tab Tài khoản =======
//         private TextBox _txtAccFullName = null!;
//         private TextBox _txtAccStudentLecturerId = null!;
//         private TextBox _txtAccClassFaculty = null!;
//         private TextBox _txtAccDepartment = null!;
//         private TextBox _txtAccEmail = null!;
//         private TextBox _txtAccPhone = null!;
//         private TextBox _txtOldPassword = null!;
//         private TextBox _txtNewPassword = null!;
//         private TextBox _txtConfirmPassword = null!;
//         private Button _btnUpdateContact = null!;
//         private Button _btnChangePassword = null!;
//         private Button _btnLogout = null!;

//         // Check connect trong tab Account
//         private Button _btnAccCheckConnect = null!;
//         private Panel _pnlAccConnectDot = null!;
//         private Label _lblAccConnectText = null!;
//         private Button _btnGoAccountTab = null!;
//         private List<FixedScheduleRow> _myFixedSchedules = new();

//         private class FixedScheduleRow
//         {
//             public Guid SessionId { get; set; }
//             public string SubjectCode { get; set; } = "";
//             public string SubjectName { get; set; } = "";
//             public string Class { get; set; } = "";
//             public string LecturerUserId { get; set; } = "";
//             public string RoomId { get; set; } = "";
//             public string DayOfWeek { get; set; } = "";
//             public string SlotStartId { get; set; } = "";
//             public string SlotEndId { get; set; } = "";
//             public string DateFrom { get; set; } = "";
//             public string DateTo { get; set; } = "";
//             public string Note { get; set; } = "";
//         }

//         private class WeekItem
//         {
//             public DateTime Monday { get; set; }
//             public string Label { get; set; } = "";
//         }

//         // ======= Tab Đặt phòng =======
//         private Button _btnBackToHome = null!;   // nút quay về tab Trang chủ
//                                                  // Demo dữ liệu lịch cá nhân
//         private class MyScheduleItem
//         {
//             public DateTime Date { get; set; }
//             public int Slot { get; set; }              // 1..14
//             public string TimeRange { get; set; } = "";
//             public string RoomId { get; set; } = "";
//             public string Subject { get; set; } = "";  // Môn / lý do
//             public string Status { get; set; } = "";   // APPROVED / IN_USE / COMPLETED / NO_SHOW
//             public string Note { get; set; } = "";
//         }
//         private class RoomSearchRow
//         {
//             public string RoomId { get; set; } = "";
//             public string Building { get; set; } = "";
//             public int Capacity { get; set; }
//             public bool HasProjector { get; set; }
//             public bool HasPC { get; set; }
//             public bool HasAC { get; set; }
//             public bool HasMic { get; set; }
//             public string Status { get; set; } = "ACTIVE";
//         }
//         public class RoomInfo
//         {
//             public string? RoomId { get; set; }
//             public string? Building { get; set; }
//             public int Capacity { get; set; }
//             public bool HasProjector { get; set; }
//             public bool HasPC { get; set; }
//             public bool HasAirConditioner { get; set; }
//             public bool HasMic { get; set; }
//             public string? Status { get; set; }
//         }

//         // ====== MODEL PHỤ CHO TAB ĐẶT PHÒNG ======
//         private class RoomSlotRow
//         {
//             public bool Selected { get; set; }          // tick để request
//             public string SlotId { get; set; } = "";    // S1..S14
//             public string TimeRange { get; set; } = ""; // 07:00-08:00
//             public string Status { get; set; } = "";    // FREE / BUSY
//             public string HolderName { get; set; } = ""; // tên người giữ
//             public string Purpose { get; set; } = "";
//             public string UserId { get; set; } = "";     // để biết có phải mình không
//             public string BookingStatus { get; set; } = ""; // APPROVED / IN_USE / WAITING / QUEUED...

//         }

//         private class MyBookingRow
//         {
//             public Guid BookingId { get; set; }
//             public string Date { get; set; } = "";        // yyyy-MM-dd
//             public string RoomId { get; set; } = "";
//             public string TimeRange { get; set; } = "";
//             public string SlotStartId { get; set; } = "";
//             public string SlotEndId { get; set; } = "";
//             public string Status { get; set; } = "";
//             public string Purpose { get; set; } = "";

//             public string CreatedAt { get; set; } = "";
//             public string CheckinDeadline { get; set; } = "";
//             public string CheckinTime { get; set; } = "";
//             public string UpdatedAt { get; set; } = "";
//         }

//         // ====== FIELD CHO TAB ĐẶT PHÒNG MỚI ======
//         private DataGridView _gridRoomSlots = null!;
//         private DataGridView _gridMyBookings = null!;
//         private Button _btnRequest = null!;
//         private Button _btnReleaseBooking = null!;
//         private bool _isReadingRoomSlots = false;
//         private readonly List<string> _roomSlotsBuffer = new();

//         private bool _isReadingMyBookings = false;
//         private readonly List<string> _myBookingsBuffer = new();
        
//         private bool _isReadingFixedSessions = false;
//         private readonly List<string> _fixedSessionsBuffer = new();
//         private readonly BindingSource _bsRoomSlots = new();
//         private readonly BindingList<MyBookingRow> _myBookings = new();
//         private readonly BindingSource _bsMyBookings = new();


//         private BindingList<RoomSlotRow> _currentRoomSlots = new();
//         private Dictionary<string, int> _slotIndexById = new(); // SlotId -> index

//         // private readonly List<MyBookingRow> _myBookings = new();


//         private readonly List<RoomSearchRow> _allRoomsForSearch = new();

//         private static string NormalizeRoomStatus(string? status)
//         {
//             var s = (status ?? "").Trim().ToUpperInvariant();
//             if (string.Equals(s, "DISABLED", StringComparison.OrdinalIgnoreCase))
//                 return "DISABLED";
//             return "ACTIVE";
//         }

//         private bool IsRoomDisabled(string? roomId)
//         {
//             roomId = (roomId ?? "").Trim().ToUpperInvariant();
//             if (string.IsNullOrWhiteSpace(roomId))
//                 return false;

//             var r = _allRoomsForSearch.FirstOrDefault(x => string.Equals(x.RoomId, roomId, StringComparison.OrdinalIgnoreCase));
//             if (r == null) return false;
//             return string.Equals(NormalizeRoomStatus(r.Status), "DISABLED", StringComparison.OrdinalIgnoreCase);
//         }

//         private void RefreshBuildingComboFromRooms()
//         {
//             if (_cbBuilding == null) return;

//             var prev = _cbBuilding.SelectedItem?.ToString();
//             _cbBuilding.Items.Clear();
//             _cbBuilding.Items.Add("ALL");

//             var buildings = _allRoomsForSearch
//                 .Select(r => (r.Building ?? "").Trim())
//                 .Where(b => !string.IsNullOrWhiteSpace(b))
//                 .Distinct(StringComparer.OrdinalIgnoreCase)
//                 .OrderBy(b => b)
//                 .ToList();

//             foreach (var b in buildings)
//                 _cbBuilding.Items.Add(b);

//             if (!string.IsNullOrWhiteSpace(prev) && _cbBuilding.Items.Contains(prev))
//                 _cbBuilding.SelectedItem = prev;
//             else
//                 _cbBuilding.SelectedIndex = 0;
//         }

//         private void RefreshReqRoomComboFromRooms()
//         {
//             if (_cbReqRoom == null) return;

//             var prev = _cbReqRoom.SelectedItem?.ToString();
//             _cbReqRoom.Items.Clear();

//             foreach (var r in _allRoomsForSearch.OrderBy(x => x.RoomId))
//                 _cbReqRoom.Items.Add(r.RoomId);

//             if (!string.IsNullOrWhiteSpace(prev) && _cbReqRoom.Items.Contains(prev) && !IsRoomDisabled(prev))
//                 _cbReqRoom.SelectedItem = prev;
//             else
//             {
//                 foreach (var item in _cbReqRoom.Items)
//                 {
//                     var id = item?.ToString();
//                     if (!string.IsNullOrWhiteSpace(id) && !IsRoomDisabled(id))
//                     {
//                         _cbReqRoom.SelectedItem = id;
//                         break;
//                     }
//                 }
//             }
//         }

//         private void ApplyRoomsGridDisabledStyle()
//         {
//             if (_gridRooms == null) return;

//             foreach (DataGridViewRow row in _gridRooms.Rows)
//             {
//                 if (row.DataBoundItem is not RoomSearchRow r) continue;
//                 bool disabled = string.Equals(NormalizeRoomStatus(r.Status), "DISABLED", StringComparison.OrdinalIgnoreCase);
//                 row.DefaultCellStyle.ForeColor = disabled ? Color.FromArgb(156, 163, 175) : Color.Black;
//                 row.DefaultCellStyle.BackColor = disabled ? Color.FromArgb(243, 244, 246) : Color.White;
//                 row.DefaultCellStyle.SelectionBackColor = disabled ? Color.FromArgb(229, 231, 235) : _gridRooms.DefaultCellStyle.SelectionBackColor;
//                 row.DefaultCellStyle.SelectionForeColor = disabled ? Color.FromArgb(107, 114, 128) : _gridRooms.DefaultCellStyle.SelectionForeColor;
//             }
//         }

//         private readonly Dictionary<string, string> _slotTimeLookup = new();

//         private readonly DemoUserInfo _currentUser;
//         private readonly string _serverIp;
//         private TimeSpan _serverTimeOffset = TimeSpan.Zero;
//         private System.Windows.Forms.Timer? _autoRefreshTimer;
//         private HashSet<string>? _pendingSelectedSlotIds;
//         private bool _refreshing;
//         private bool _roomSlotsRequestInFlight = false;
//         private string? _lastRoomSlotsKey = null; // roomId|date
//         private string? _subRoomId;
//         private string? _subDateKey;
//         private bool _scheduleReloadInFlight = false; // Prevent concurrent schedule reloads

//         private string? _currentSubscribedRoomId;
//         private string? _currentSubscribedDateKey; // yyyy-MM-dd
//         private DateTime _lastSocketActivity = DateTime.MinValue;
//         private volatile bool _socketClosed = false;
//         public MainClientForm(DemoUserInfo currentUser, string serverIp, LoginForm loginForm)
//         {
//             _currentUser = currentUser;
//             _serverIp = serverIp;
//             _loginForm = loginForm;

//             InitializeComponent(); // trống cũng được, nhưng cứ giữ cho chuẩn WinForms
//             SetupUi();

//             this.FormClosing += MainClientForm_FormClosing;

//             // ⭐ MỞ KẾT NỐI TCP CHÍNH THỨC
//             this.Shown += async (s, e) =>
//             {
//                 try
//                 {
//                     _tcp = new TcpClient();
//                     await _tcp.ConnectAsync(_serverIp, 5000);
//                     // ✅ M6: bật keepalive ngay sau connect
//                     EnableTcpKeepAlive(_tcp.Client);
//                     _stream = _tcp.GetStream();
//                     _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
//                     _reader = new StreamReader(_stream, Encoding.UTF8);

//                     AppendClientLog("[INFO] Connected persistent TCP to server.");

//                     // ⭐⭐ LOGIN lại trên kết nối persistent ⭐⭐
//                     var loginCmd = $"LOGIN|{_currentUser.UserId}|{_currentUser.Password}";
//                     AppendClientLog("[SEND] " + loginCmd);
//                     await _writer.WriteLineAsync(loginCmd);

//                     var loginResp = await _reader.ReadLineAsync();
//                     if (loginResp == null || !loginResp.StartsWith("LOGIN_OK|"))
//                     {
//                         AppendClientLog("[ERROR] LOGIN on main TCP failed: " + (loginResp ?? "NULL"));
//                         MessageBox.Show("Không đăng nhập được trên kết nối chính.\n" + (loginResp ?? "No response"));
//                         Close();
//                         return;
//                     }

//                     AppendClientLog("[INFO] LOGIN on main TCP OK.");

//                     // ✅ M6: đánh dấu có activity ngay khi login OK
//                     _lastSocketActivity = DateTime.UtcNow;
//                     _socketClosed = false;
//                     // ⭐ M2 – SUB booking của tôi (persistent, chỉ 1 lần)
//                     await _writer.WriteLineAsync($"SUB_MY_BOOKINGS|{_currentUser.UserId}");
//                     AppendClientLog("[SEND] SUB_MY_BOOKINGS|" + _currentUser.UserId);
//                     await _writer.WriteLineAsync($"SUB_HOME|{_currentUser.UserId}");
//                     AppendClientLog("[SEND] SUB_HOME|" + _currentUser.UserId);

//                     // nếu bạn đã làm server SUB_ROOMS / SUB_SLOT_CONFIG thì bật luôn
//                     await _writer.WriteLineAsync("SUB_ROOMS");
//                     AppendClientLog("[SEND] SUB_ROOMS");

//                     await _writer.WriteLineAsync("SUB_SLOT_CONFIG");
//                     AppendClientLog("[SEND] SUB_SLOT_CONFIG");

//                 }
//                 catch (Exception ex)
//                 {
//                     AppendClientLog("[ERROR] Cannot connect TCP: " + ex.Message);
//                     MessageBox.Show("Không thể kết nối server.\n" + ex.Message);
//                     return;
//                 }

//                 StartBackgroundListen();

//                 // ⭐ SAU KHI CÓ TCP → GỌI CHECK CONNECT
//                 await HeaderCheckConnectAsync();

//                 // ⭐ LÚC NÀY GỌI LOAD DATA ĐÃ AN TOÀN
//                 await InitHeaderTimeAsync();
//                 await LoadSlotConfigFromServerAsync();
//                 await LoadHomeFromServerAsync();
//                 await LoadFixedSchedulesFromServerAsync();
//                 await ReloadScheduleFromServerAsync();
//                 await LoadRoomsFromServerAsync();
//                 await ReloadMyBookingsAsync();
//             };
//         }

//         private void InitializeComponent()
//         {
//             // Không dùng Designer, nên để trống
//         }

//         private void SetupUi()
//         {
//             Text = "Client - Room Booking";
//             Width = 1000;
//             Height = 700;
//             StartPosition = FormStartPosition.CenterScreen;
//             DoubleBuffered = true;
//             KeyPreview = true;
//             KeyDown += (s, e) => HandleGlobalHotkeys(e);

//             _rootLayout = new TableLayoutPanel
//             {
//                 Dock = DockStyle.Fill,
//                 ColumnCount = 2,
//                 RowCount = 2,
//                 BackColor = Color.White
//             };
//             _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
//             _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
//             _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
//             _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
//             Controls.Add(_rootLayout);

//             _panelSidebar = new Panel
//             {
//                 Dock = DockStyle.Fill,
//                 BackColor = Color.FromArgb(20, 20, 24)
//             };
//             _rootLayout.Controls.Add(_panelSidebar, 0, 1);

//             _panelContentHost = new Panel
//             {
//                 Dock = DockStyle.Fill,
//                 BackColor = Color.White
//             };
//             _rootLayout.Controls.Add(_panelContentHost, 1, 1);

//             // ====== HEADER ======
//             _panelHeader = new Panel
//             {
//                 Dock = DockStyle.Top,
//                 Height = 80,
//                 BackColor = Color.WhiteSmoke
//             };
//             _rootLayout.Controls.Add(_panelHeader, 0, 0);
//             _rootLayout.SetColumnSpan(_panelHeader, 2);

//             _picAvatar = new PictureBox
//             {
//                 Left = 10,
//                 Top = 10,
//                 Width = 60,
//                 Height = 60,
//                 BorderStyle = BorderStyle.FixedSingle,
//                 SizeMode = PictureBoxSizeMode.StretchImage
//             };
//             // Bạn có thể load 1 icon default ở đây nếu thích
//             _panelHeader.Controls.Add(_picAvatar);
//             _picAvatar.SizeMode = PictureBoxSizeMode.StretchImage; // sau này nếu bạn có icon/ảnh

//             _picAvatar.Paint += (s, e) =>
//             {
//                 var gp = new GraphicsPath();
//                 gp.AddEllipse(0, 0, _picAvatar.Width - 1, _picAvatar.Height - 1);
//                 _picAvatar.Region = new Region(gp);
//             };

//             // Tên + loại user (giữ nguyên)
//             _lblNameWithType = new Label
//             {
//                 Left = 80,
//                 Top = 10,
//                 Width = 400,
//                 Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold)
//             };
//             _panelHeader.Controls.Add(_lblNameWithType);
//             _lblNameWithType.Text = $"{_currentUser.FullName} ({_currentUser.UserType})";

//             // Dòng phụ: StudentId/Class/Department hoặc LecturerId/Faculty + Email/Phone
//             _lblSubInfo = new Label
//             {
//                 Left = 80,
//                 Top = 35,
//                 Width = 700,          // tăng rộng chút cho thoải mái
//                 AutoSize = false
//             };
//             _panelHeader.Controls.Add(_lblSubInfo);

//             string subText;
//             if (_currentUser.UserType == "Student")
//             {
//                 subText =
//                     $"StudentId: {_currentUser.StudentId} - Class: {_currentUser.Class} - Department: {_currentUser.Department}";
//             }
//             else if (_currentUser.UserType == "Lecturer")
//             {
//                 subText =
//                     $"LecturerId: {_currentUser.LecturerId} - Faculty: {_currentUser.Faculty}";
//             }
//             else
//             {
//                 // Staff
//                 subText = $"Staff - Department: {_currentUser.Department}";
//             }

//             // Thông tin liên hệ chung
//             var contactParts = new List<string>();
//             if (!string.IsNullOrWhiteSpace(_currentUser.Email))
//                 contactParts.Add($"Email: {_currentUser.Email}");
//             if (!string.IsNullOrWhiteSpace(_currentUser.Phone))
//                 contactParts.Add($"Phone: {_currentUser.Phone}");

//             if (contactParts.Count > 0)
//             {
//                 subText += " | " + string.Join(" - ", contactParts);
//             }

//             _lblSubInfo.Text = subText;


//             // _lblSubInfo = new Label
//             // {
//             //     Left = 80,
//             //     Top = 35,
//             //     Width = 400,
//             //     Height = 20,
//             //     Text = subText
//             // };
//             // _panelHeader.Controls.Add(_lblSubInfo);

//             _lblToday = new Label
//             {
//                 AutoSize = false,
//                 Width = 260,
//                 Height = 20,
//                 TextAlign = ContentAlignment.MiddleRight,
//                 Top = 10,
//                 Anchor = AnchorStyles.Top | AnchorStyles.Right,
//                 Text = "Date --/--/---- – --"   // placeholder, lát nữa InitHeaderTimeAsync sẽ set lại

//             };
//             // Left = Right - Width (sau khi form load thì Anchor sẽ tự co)
//             _lblToday.Left = ClientSize.Width - _lblToday.Width - 20;
//             _panelHeader.Controls.Add(_lblToday);
//             // UpdateTodayLabel(DateTime.Now);

//             _btnHeaderCheckConnect = new Button
//             {
//                 Text = "Check connect",
//                 Width = 110,
//                 Height = 25,
//                 Top = 30,
//                 Anchor = AnchorStyles.Top | AnchorStyles.Right
//             };
//             _btnHeaderCheckConnect.Left = ClientSize.Width - 180;
//             _btnHeaderCheckConnect.Click += async (s, e) =>
//             {
//                 await HeaderCheckConnectAsync();
//             };
//             _panelHeader.Controls.Add(_btnHeaderCheckConnect);

//             // ===== Nút ĐĂNG XUẤT trên header =====
//             _btnLogout = new Button
//             {
//                 Text = "Đăng xuất",
//                 Width = 90,
//                 Height = 25,
//                 // Vị trí cố định
//                 Left = 810,
//                 Top = 50,
//                 Anchor = AnchorStyles.Top   // nếu không muốn nó chạy theo chiều ngang thì bỏ Right đi
//             };

//             _btnLogout.Click += (s, e) =>
//             {
//                 _isLoggingOut = true;      // báo cho FormClosing biết đây là logout, không phải thoát app
//                 _loginForm.Show();         // hiện lại form login cũ
//                 this.Close();              // đóng MainClientForm
//             };

//             _panelHeader.Controls.Add(_btnLogout);


//             _pnlHeaderConnectDot = new Panel
//             {
//                 Width = 14,
//                 Height = 14,
//                 Top = 33,
//                 Anchor = AnchorStyles.Top | AnchorStyles.Right,
//                 BackColor = Color.Red,
//                 BorderStyle = BorderStyle.FixedSingle
//             };
//             _pnlHeaderConnectDot.Left = ClientSize.Width - 65;
//             _panelHeader.Controls.Add(_pnlHeaderConnectDot);

//             _lblHeaderConnectText = new Label
//             {
//                 Width = 60,
//                 Height = 20,
//                 Top = 33,
//                 Anchor = AnchorStyles.Top | AnchorStyles.Right,
//                 Text = "Lost",
//                 ForeColor = Color.Red
//             };
//             _lblHeaderConnectText.Left = ClientSize.Width - 50;
//             _panelHeader.Controls.Add(_lblHeaderConnectText);

//             _lblRunningTime = new Label
//             {
//                 Width = 200,
//                 Height = 20,
//                 Top = 58,
//                 Anchor = AnchorStyles.Top | AnchorStyles.Right,
//                 Text = "Time: --:--:--"
//             };
//             _lblRunningTime.Left = ClientSize.Width - 280;
//             _panelHeader.Controls.Add(_lblRunningTime);

//             // === FIX: dùng WinFormsTimer chứ không dùng Timer mơ hồ ===
//             _timerClock = new WinFormsTimer { Interval = 1000 };
//             _timerClock.Tick += (s, e) =>
//             {
//                 var now = DateTime.Now + _serverTimeOffset;
//                 _lblRunningTime.Text = "Time: " + now.ToString("HH:mm:ss");
//                 UpdateTodayLabel(now); // <-- thêm dòng này

//             };
//             _timerClock.Start();
//             // ====== SUB HEADER: cấu hình ca ======
//             _panelSubHeader = new Panel
//             {
//                 Dock = DockStyle.Top,
//                 Height = 35,
//                 BackColor = Color.Gainsboro
//             };
//             _panelContentHost.Controls.Add(_panelSubHeader);

//             _lblSlotConfig = new Label
//             {
//                 Left = 10,
//                 Top = 8,
//                 Width = 800,
//                 Text = "Cấu hình ca: Ca 1: 07:00–08:00 | Ca 2: 08:00–09:00 | Ca 3: 09:00–10:00 | ..."
//             };
//             _panelSubHeader.Controls.Add(_lblSlotConfig);

//             _btnSlotConfigHelp = new Button
//             {
//                 Text = "?",
//                 Width = 24,
//                 Height = 24,
//                 Top = 5,
//                 Left = 820
//             };
//             _btnSlotConfigHelp.Click += (s, e) =>
//             {
//                 MessageBox.Show("Chi tiết cấu hình ca (demo).", "Cấu hình ca");
//             };
//             _panelSubHeader.Controls.Add(_btnSlotConfigHelp);

//             // ====== TAB CONTROL ======
//             _tabMain = new TabControl
//             {
//                 Dock = DockStyle.Fill
//             };
//             _tabMain.Appearance = TabAppearance.FlatButtons;
//             _tabMain.ItemSize = new Size(0, 1);
//             _tabMain.SizeMode = TabSizeMode.Fixed;
//             _panelContentHost.Controls.Add(_tabMain);

//             _tabHome = new TabPage("Trang chủ");
//             _tabBooking = new TabPage("Đặt phòng");
//             _tabSchedule = new TabPage("Lịch của tôi");
//             _tabNotifications = new TabPage("Thông báo");
//             _tabLog = new TabPage("Log");
//             _tabAccount = new TabPage("Tài khoản");

//             _tabMain.TabPages.AddRange(new[]
//             {
//                 _tabHome, _tabBooking, _tabSchedule, _tabNotifications, _tabLog, _tabAccount
//             });

//             _navHome = CreateSidebarItem("\uE80F", "Trang chủ", () => _tabMain.SelectedTab = _tabHome);
//             _navBooking = CreateSidebarItem("\uE8C7", "Đặt phòng", () => _tabMain.SelectedTab = _tabBooking);
//             _navSchedule = CreateSidebarItem("\uE787", "Lịch biểu", () => _tabMain.SelectedTab = _tabSchedule);
//             _navLog = CreateSidebarItem("\uE8A5", "Log", () => _tabMain.SelectedTab = _tabLog);
//             _navAccount = CreateSidebarItem("\uE77B", "Tài khoản", () => _tabMain.SelectedTab = _tabAccount);

//             _navHome.Top = 30;
//             _navBooking.Top = _navHome.Bottom + 6;
//             _navSchedule.Top = _navBooking.Bottom + 6;
//             _navLog.Top = _navSchedule.Bottom + 6;
//             _navAccount.Top = _navLog.Bottom + 6;

//             _panelSidebar.Controls.Add(_navHome);
//             _panelSidebar.Controls.Add(_navBooking);
//             _panelSidebar.Controls.Add(_navSchedule);
//             _panelSidebar.Controls.Add(_navLog);
//             _panelSidebar.Controls.Add(_navAccount);
//             SetSidebarActive(_navHome);

//             _tabMain.SelectedIndexChanged += (s, e) =>
//             {
//                 if (_tabMain.SelectedTab == _tabHome) SetSidebarActive(_navHome);
//                 else if (_tabMain.SelectedTab == _tabBooking) SetSidebarActive(_navBooking);
//                 else if (_tabMain.SelectedTab == _tabSchedule) SetSidebarActive(_navSchedule);
//                 else if (_tabMain.SelectedTab == _tabLog) SetSidebarActive(_navLog);
//                 else if (_tabMain.SelectedTab == _tabAccount) SetSidebarActive(_navAccount);
//             };

//             // this.Shown += async (s, e) =>
//             // {
//             //     await InitHeaderTimeAsync();   // anh đã có cho header
//             //     await LoadSlotConfigFromServerAsync(); // NEW: load giờ ca từ server
//             //     await LoadHomeFromServerAsync(); // load dữ liệu thật cho Trang chủ
//             //     await ReloadScheduleFromServerAsync();   // phần lịch sẽ thêm ở dưới
//             //     await LoadRoomsFromServerAsync();

//             // };
//             BuildHomeTabUi();
//             BuildBookingTabUi();
//             BuildScheduleTabUi();
//             BuildNotificationsTabUi();
//             BuildAccountTabUi();
//             // Khởi tạo thời gian header theo server
//             // _ = InitHeaderTimeAsync();
//         }
//         private Task<bool> HeaderCheckConnectAsync()
//         {
//             bool ok =
//                 _writer != null &&
//                 _tcp != null &&
//                 _tcp.Connected &&
//                 !_socketClosed &&
//                 (DateTime.UtcNow - _lastSocketActivity) < TimeSpan.FromMinutes(2);

//             // update UI dot/text giống bạn đang làm
//             return Task.FromResult(ok);
//         }

//         private async Task<bool> RequestServerNowAsync()
//         {
//             try
//             {
//                 if (_writer == null)
//                     return false;

//                 await _writer.WriteLineAsync("GET_NOW");
//                 return true;
//             }
//             catch
//             {
//                 return false;
//             }
//         }

//         private void StartBackgroundListen()
//         {
//             _ = Task.Run(async () =>
//             {
//                 try
//                 {
//                     while (true)
//                     {
//                         string? line = await _reader!.ReadLineAsync();
//                         if (line == null)
//                         {
//                             _socketClosed = true;
//                             break;
//                         }

//                         _lastSocketActivity = DateTime.UtcNow;

//                         if (!IsDisposed && IsHandleCreated)
//                             BeginInvoke(new Action(() => HandleServerMessage(line)));
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     _socketClosed = true;
//                     AppendClientLog("[ERROR] BackgroundListen: " + ex.Message);
//                 }
//             });
//         }

//         private string GetVietnameseWeekday(DayOfWeek dow)
//         {
//             switch (dow)
//             {
//                 case DayOfWeek.Monday: return "Thứ 2";
//                 case DayOfWeek.Tuesday: return "Thứ 3";
//                 case DayOfWeek.Wednesday: return "Thứ 4";
//                 case DayOfWeek.Thursday: return "Thứ 5";
//                 case DayOfWeek.Friday: return "Thứ 6";
//                 case DayOfWeek.Saturday: return "Thứ 7";
//                 case DayOfWeek.Sunday: return "Chủ nhật";
//                 default: return "";
//             }
//         }

//         private void UpdateTodayLabel(DateTime now)
//         {
//             var thu = GetVietnameseWeekday(now.DayOfWeek);
//             _lblToday.Text = $"Date {now:dd/MM/yyyy} – {thu}";
//         }
//         private async Task InitHeaderTimeAsync()
//         {
//             await RequestServerNowAsync();   // 1 lần duy nhất lúc init
//             UpdateTodayLabel(DateTime.Now + _serverTimeOffset); // tạm
//         }

//         // ================== 2.3. TAB TRANG CHỦ ==================
//         private void BuildHomeTabUi()
//         {
//             _tabHome.Controls.Clear();
//             _tabHome.BackColor = Color.White;

//             var root = new TableLayoutPanel
//             {
//                 Dock = DockStyle.Fill,
//                 ColumnCount = 2,
//                 RowCount = 2,
//                 Padding = new Padding(10),
//                 BackColor = Color.White
//             };
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
//             root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
//             root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
//             _tabHome.Controls.Add(root);

//             // ===== Trái: Lịch hôm nay =====
//             _grpTodaySchedule = new GroupBox
//             {
//                 Text = "Lịch hôm nay của bạn",
//                 Dock = DockStyle.Fill,
//                 Padding = new Padding(10)
//             };
//             root.Controls.Add(_grpTodaySchedule, 0, 0);

//             _gridTodaySchedule = new DataGridView
//             {
//                 Dock = DockStyle.Fill,
//                 ReadOnly = true,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                 SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                 MultiSelect = false,
//                 RowHeadersVisible = false,
//                 BorderStyle = BorderStyle.None,
//                 CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
//                 GridColor = Color.FromArgb(235, 235, 235),
//                 BackgroundColor = Color.White,
//                 EnableHeadersVisualStyles = false
//             };
//             _grpTodaySchedule.Controls.Add(_gridTodaySchedule);
//             _gridTodaySchedule.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
//             _gridTodaySchedule.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
//             _gridTodaySchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
//             _gridTodaySchedule.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
//             _gridTodaySchedule.DefaultCellStyle.SelectionBackColor = Color.FromArgb(229, 231, 235);
//             _gridTodaySchedule.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
//             _gridTodaySchedule.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
//             _gridTodaySchedule.RowTemplate.Height = 44;
//             _gridTodaySchedule.ColumnHeadersHeight = 36;
//             _gridTodaySchedule.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
//             _gridTodaySchedule.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
//             _gridTodaySchedule.CellPainting -= GridTodaySchedule_CellPainting;
//             _gridTodaySchedule.CellPainting += GridTodaySchedule_CellPainting;
//             _gridTodaySchedule.RowPostPaint -= GridTodaySchedule_RowPostPaint;
//             _gridTodaySchedule.RowPostPaint += GridTodaySchedule_RowPostPaint;

//             // Định nghĩa cột: Giờ, Phòng, Môn, Người dạy/người đặt, Trạng thái, Ghi chú
//             _gridTodaySchedule.Columns.Clear();
//             _gridTodaySchedule.Columns.Add("TimeRange", "Giờ");
//             _gridTodaySchedule.Columns.Add("RoomId", "Phòng");
//             _gridTodaySchedule.Columns.Add("Subject", "Môn / Mục đích");
//             _gridTodaySchedule.Columns.Add("Owner", "Giảng viên / Người đặt");
//             _gridTodaySchedule.Columns.Add("Status", "Trạng thái");
//             _gridTodaySchedule.Columns.Add("Note", "Ghi chú");

//             _gridTodaySchedule.Columns["TimeRange"].FillWeight = 18f;
//             _gridTodaySchedule.Columns["RoomId"].FillWeight = 10f;
//             _gridTodaySchedule.Columns["Subject"].FillWeight = 24f;
//             _gridTodaySchedule.Columns["Owner"].FillWeight = 20f;
//             _gridTodaySchedule.Columns["Status"].FillWeight = 12f;
//             _gridTodaySchedule.Columns["Note"].FillWeight = 16f;

//             _gridTodaySchedule.Columns["TimeRange"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
//             _gridTodaySchedule.Columns["TimeRange"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
//             _gridTodaySchedule.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
//             _gridTodaySchedule.Columns["Status"].DefaultCellStyle.ForeColor = Color.Transparent;

//             // ===== Phải: Thông báo mới =====
//             _grpLatestNotifications = new GroupBox
//             {
//                 Text = "Thông báo mới",
//                 Dock = DockStyle.Fill,
//                 Padding = new Padding(10)
//             };
//             root.Controls.Add(_grpLatestNotifications, 1, 0);

//             _lstLatestNotifications = new ListBox
//             {
//                 Dock = DockStyle.Fill
//             };
//             _grpLatestNotifications.Controls.Add(_lstLatestNotifications);
//             _lstLatestNotifications.BorderStyle = BorderStyle.None;
//             _lstLatestNotifications.BackColor = Color.White;
//             _lstLatestNotifications.ForeColor = Color.FromArgb(17, 24, 39);
//             _lstLatestNotifications.Font = new Font("Segoe UI", 10, FontStyle.Regular);
//             _lstLatestNotifications.IntegralHeight = false;
//             _lstLatestNotifications.DrawMode = DrawMode.OwnerDrawFixed;
//             _lstLatestNotifications.ItemHeight = 72;
//             _lstLatestNotifications.DrawItem += DrawHomeNotificationItem;

//             // ===== Nút nhanh =====
//             var pnlActions = new FlowLayoutPanel
//             {
//                 Dock = DockStyle.Fill,
//                 FlowDirection = FlowDirection.LeftToRight,
//                 WrapContents = false,
//                 Padding = new Padding(0, 8, 0, 0),
//                 BackColor = Color.White
//             };
//             root.Controls.Add(pnlActions, 0, 1);
//             root.SetColumnSpan(pnlActions, 2);

//             _btnGoBookingTab = new Button
//             {
//                 Text = "Đặt phòng ngay",
//                 Width = 160,
//                 Height = 36,
//                 FlatStyle = FlatStyle.Flat,
//                 BackColor = Color.FromArgb(37, 99, 235),
//                 ForeColor = Color.White
//             };
//             _btnGoBookingTab.FlatAppearance.BorderSize = 0;
//             _btnGoBookingTab.Click += (s, e) =>
//             {
//                 _tabMain.SelectedTab = _tabBooking;
//             };
//             // pnlActions.Controls.Add(_btnGoBookingTab);

//             _btnGoMyWeekSchedule = new Button
//             {
//                 Text = "Xem lịch tuần này",
//                 Width = 170,
//                 Height = 36,
//                 FlatStyle = FlatStyle.Flat,
//                 BackColor = Color.FromArgb(243, 244, 246),
//                 ForeColor = Color.FromArgb(17, 24, 39)
//             };
//             _btnGoMyWeekSchedule.FlatAppearance.BorderSize = 1;
//             _btnGoMyWeekSchedule.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
//             _btnGoMyWeekSchedule.Click += (s, e) =>
//             {
//                 _tabMain.SelectedTab = _tabSchedule;
//                 // Week view luôn được hiển thị (không còn Day view)
//                 // Load lịch khi chuyển đến tab Schedule
//                 ReloadScheduleForSelectedDate();
//             };
//             // pnlActions.Controls.Add(_btnGoMyWeekSchedule);

//             // Nút sang tab Tài khoản
//             _btnGoAccountTab = new Button
//             {
//                 Text = "Tài khoản của tôi",
//                 Width = 170,
//                 Height = 36,
//                 FlatStyle = FlatStyle.Flat,
//                 BackColor = Color.FromArgb(243, 244, 246),
//                 ForeColor = Color.FromArgb(17, 24, 39)
//             };
//             _btnGoAccountTab.FlatAppearance.BorderSize = 1;
//             _btnGoAccountTab.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
//             _btnGoAccountTab.Click += (s, e) =>
//             {
//                 _tabMain.SelectedTab = _tabAccount;
//             };
//             // pnlActions.Controls.Add(_btnGoAccountTab);

//             // Tạm thởi load dữ liệu demo, sau này thay bằng dữ liệu thật từ server
//         }

//         private void DrawHomeNotificationItem(object? sender, DrawItemEventArgs e)
//         {
//             if (e.Index < 0) return;
//             if (sender is not ListBox lb) return;
//             var raw = lb.Items[e.Index]?.ToString() ?? string.Empty;

//             e.DrawBackground();

//             var g = e.Graphics;
//             g.SmoothingMode = SmoothingMode.AntiAlias;

//             var pad = 8;
//             var rect = new Rectangle(e.Bounds.Left + pad, e.Bounds.Top + pad, e.Bounds.Width - pad * 2, e.Bounds.Height - pad * 2);
//             var iconRect = new Rectangle(rect.Left + 10, rect.Top + 10, 36, 36);
//             var textRect = new Rectangle(iconRect.Right + 10, rect.Top + 8, rect.Width - (iconRect.Width + 30), rect.Height - 16);

//             Color accent = Color.FromArgb(107, 114, 128);
//             var lower = raw.ToLowerInvariant();
//             if (lower.Contains("grant")) accent = Color.FromArgb(22, 163, 74);
//             else if (lower.Contains("check-in") || lower.Contains("checkin")) accent = Color.FromArgb(37, 99, 235);
//             else if (lower.Contains("hủy") || lower.Contains("cancel") || lower.Contains("no_show") || lower.Contains("no show")) accent = Color.FromArgb(220, 38, 38);
//             else if (lower.Contains("reminder") || lower.Contains("nhắc")) accent = Color.FromArgb(245, 158, 11);
//             else if (lower.Contains("change") || lower.Contains("chuyển") || lower.Contains("đổi")) accent = Color.FromArgb(14, 165, 233);

//             var glyph = "\uE946";
//             if (lower.Contains("grant")) glyph = "\uE73E";
//             else if (lower.Contains("check-in") || lower.Contains("checkin")) glyph = "\uE930";
//             else if (lower.Contains("hủy") || lower.Contains("cancel") || lower.Contains("no_show") || lower.Contains("no show")) glyph = "\uE711";
//             else if (lower.Contains("reminder") || lower.Contains("nhắc")) glyph = "\uE823";
//             else if (lower.Contains("change") || lower.Contains("chuyển") || lower.Contains("đổi")) glyph = "\uE8AC";

//             using (var bg = new SolidBrush(Color.White))
//             using (var border = new Pen(Color.FromArgb(229, 231, 235)))
//             using (var accentBrush = new SolidBrush(accent))
//             {
//                 var gp = new GraphicsPath();
//                 var r = rect;
//                 int radius = 10;
//                 gp.AddArc(r.Left, r.Top, radius, radius, 180, 90);
//                 gp.AddArc(r.Right - radius, r.Top, radius, radius, 270, 90);
//                 gp.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
//                 gp.AddArc(r.Left, r.Bottom - radius, radius, radius, 90, 90);
//                 gp.CloseFigure();

//                 g.FillPath(bg, gp);
//                 g.DrawPath(border, gp);
//                 g.FillEllipse(accentBrush, iconRect);
//             }

//             using (var iconFont = new Font("Segoe MDL2 Assets", 16f, FontStyle.Regular))
//             {
//                 TextRenderer.DrawText(
//                     g,
//                     glyph,
//                     iconFont,
//                     iconRect,
//                     Color.White,
//                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
//                 );
//             }

//             var title = raw;
//             var idxDot = raw.IndexOf('.');
//             if (idxDot > 0 && idxDot <= 60) title = raw.Substring(0, idxDot + 1);
//             if (title.Length > 80) title = title.Substring(0, 77) + "...";

//             var body = raw;
//             if (!string.IsNullOrWhiteSpace(title) && body.StartsWith(title, StringComparison.Ordinal))
//             {
//                 body = body.Substring(title.Length).TrimStart();
//             }

//             string timeText = string.Empty;
//             var m = System.Text.RegularExpressions.Regex.Match(raw, "\\b\\d{1,2}:\\d{2}\\b");
//             if (m.Success) timeText = m.Value;
//             else
//             {
//                 var d = System.Text.RegularExpressions.Regex.Match(raw, "\\b\\d{1,2}/\\d{1,2}(/\\d{2,4})?\\b");
//                 if (d.Success) timeText = d.Value;
//             }

//             var titleFont = new Font("Segoe UI", 9.8f, FontStyle.Bold);
//             var bodyFont = new Font("Segoe UI", 9f, FontStyle.Regular);
//             var metaFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

//             var titleRect = new Rectangle(textRect.Left, textRect.Top, textRect.Width, 22);
//             var bodyRect = new Rectangle(textRect.Left, textRect.Top + 22, textRect.Width, textRect.Height - 22);
//             TextRenderer.DrawText(g, title, titleFont, titleRect, Color.FromArgb(17, 24, 39), TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
//             TextRenderer.DrawText(g, body, bodyFont, bodyRect, Color.FromArgb(75, 85, 99), TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

//             if (!string.IsNullOrWhiteSpace(timeText))
//             {
//                 var timeRect = new Rectangle(rect.Right - 110, rect.Top + 8, 100, 20);
//                 TextRenderer.DrawText(g, timeText, metaFont, timeRect, Color.FromArgb(107, 114, 128), TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
//             }

//             e.DrawFocusRectangle();
//         }

//         private void GridTodaySchedule_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
//         {
//             if (sender is not DataGridView grid) return;
//             if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
//             var col = grid.Columns[e.ColumnIndex].Name;

//             if (col == "Status")
//             {
//                 var raw = (e.FormattedValue?.ToString() ?? string.Empty).Trim();
//                 var status = raw.ToUpperInvariant();
//                 (Color bg, Color fg, Color border) = GetStatusBadgeColors(status);

//                 e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

//                 var g = e.Graphics;
//                 g.SmoothingMode = SmoothingMode.AntiAlias;

//                 var padX = 8;
//                 var padY = 6;
//                 var badgeRect = new Rectangle(
//                     e.CellBounds.Left + padX,
//                     e.CellBounds.Top + padY,
//                     e.CellBounds.Width - padX * 2,
//                     e.CellBounds.Height - padY * 2
//                 );

//                 int radius = Math.Min(14, badgeRect.Height);
//                 using (var b = new SolidBrush(bg))
//                 using (var p = new Pen(border))
//                 {
//                     var gp = new GraphicsPath();
//                     gp.AddArc(badgeRect.Left, badgeRect.Top, radius, radius, 180, 90);
//                     gp.AddArc(badgeRect.Right - radius, badgeRect.Top, radius, radius, 270, 90);
//                     gp.AddArc(badgeRect.Right - radius, badgeRect.Bottom - radius, radius, radius, 0, 90);
//                     gp.AddArc(badgeRect.Left, badgeRect.Bottom - radius, radius, radius, 90, 90);
//                     gp.CloseFigure();

//                     g.FillPath(b, gp);
//                     g.DrawPath(p, gp);
//                 }

//                 using (var f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
//                 {
//                     TextRenderer.DrawText(
//                         g,
//                         raw,
//                         f,
//                         badgeRect,
//                         fg,
//                         TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
//                     );
//                 }

//                 e.Handled = true;
//                 return;
//             }

//             if (col == "TimeRange")
//             {
//                 var raw = (e.FormattedValue?.ToString() ?? string.Empty).Trim();
//                 if (string.IsNullOrWhiteSpace(raw)) return;

//                 string line1 = raw;
//                 string line2 = string.Empty;

//                 int open = raw.IndexOf('(');
//                 int close = raw.IndexOf(')');
//                 if (open > 0 && close > open)
//                 {
//                     line1 = raw.Substring(0, open).Trim();
//                     line2 = raw.Substring(open + 1, close - open - 1).Trim();
//                 }
//                 else
//                 {
//                     var m = System.Text.RegularExpressions.Regex.Match(raw, "\\b\\d{1,2}:\\d{2}\\s*[–-]\\s*\\d{1,2}:\\d{2}\\b");
//                     if (m.Success)
//                     {
//                         line2 = m.Value.Replace("-", "–").Replace(" ", "");
//                         line1 = raw.Replace(m.Value, "").Trim();
//                         if (string.IsNullOrWhiteSpace(line1)) line1 = raw;
//                     }
//                 }

//                 e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

//                 var contentRect = Rectangle.Inflate(e.CellBounds, -10, -6);
//                 var line1Rect = new Rectangle(contentRect.Left, contentRect.Top, contentRect.Width, 20);
//                 var line2Rect = new Rectangle(contentRect.Left, contentRect.Top + 20, contentRect.Width, contentRect.Height - 20);

//                 using (var f1 = new Font("Segoe UI", 9.5f, FontStyle.Bold))
//                 using (var f2 = new Font("Segoe UI", 8.5f, FontStyle.Regular))
//                 {
//                     TextRenderer.DrawText(e.Graphics, line1, f1, line1Rect, Color.FromArgb(17, 24, 39), TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
//                     if (!string.IsNullOrWhiteSpace(line2))
//                     {
//                         TextRenderer.DrawText(e.Graphics, line2, f2, line2Rect, Color.FromArgb(107, 114, 128), TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
//                     }
//                 }

//                 e.Handled = true;
//                 return;
//             }
//         }

//         private void GridTodaySchedule_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
//         {
//             if (sender is not DataGridView grid) return;
//             if (e.RowIndex < 0) return;
//             if (!grid.Columns.Contains("Status")) return;

//             var statusRaw = grid.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? string.Empty;
//             var status = statusRaw.Trim().ToUpperInvariant();
//             (Color bg, Color fg, Color border) = GetStatusBadgeColors(status);

//             var stripeRect = new Rectangle(e.RowBounds.Left + 1, e.RowBounds.Top + 1, 4, e.RowBounds.Height - 2);
//             using (var b = new SolidBrush(fg))
//             {
//                 e.Graphics.FillRectangle(b, stripeRect);
//             }
//         }

//         private (Color bg, Color fg, Color border) GetStatusBadgeColors(string status)
//         {
//             if (status == "APPROVED") return (Color.FromArgb(220, 252, 231), Color.FromArgb(22, 163, 74), Color.FromArgb(187, 247, 208));
//             if (status == "IN_USE") return (Color.FromArgb(219, 234, 254), Color.FromArgb(37, 99, 235), Color.FromArgb(191, 219, 254));
//             if (status == "COMPLETED") return (Color.FromArgb(243, 244, 246), Color.FromArgb(75, 85, 99), Color.FromArgb(229, 231, 235));
//             if (status == "NO_SHOW" || status == "NO SHOW") return (Color.FromArgb(254, 226, 226), Color.FromArgb(220, 38, 38), Color.FromArgb(254, 202, 202));
//             if (status == "FIXED") return (Color.FromArgb(254, 249, 195), Color.FromArgb(202, 138, 4), Color.FromArgb(253, 230, 138));
//             if (status == "HỌC" || status == "HOC") return (Color.FromArgb(237, 233, 254), Color.FromArgb(109, 40, 217), Color.FromArgb(221, 214, 254));
//             return (Color.FromArgb(243, 244, 246), Color.FromArgb(55, 65, 81), Color.FromArgb(229, 231, 235));
//         }
//         private void MainClientForm_FormClosing(object? sender, FormClosingEventArgs e)
//         {
//             if (_isLoggingOut)
//             {
//                 // Logout chủ động: chỉ đóng MainClientForm, quay về LoginForm (đã Show ở nút Logout)
//                 return;
//             }

//             // User bấm X đóng app => thoát toàn bộ
//             Application.Exit();
//         }

//         /// <summary>
//         /// Tạm thởi: dữ liệu demo cho tab Trang chủ.
//         /// Sau này bạn sẽ replace bằng call server để lấy:
//         /// - Lịch cố định của SV / GV
//         /// - Booking của user hôm nay
//         /// - Thông báo mới từ server
//         /// </summary>
//         private void LoadHomeDemoData()
//         {
//             // Xóa dữ liệu cũ
//             _gridTodaySchedule.Rows.Clear();
//             _lstLatestNotifications.Items.Clear();

//             // Ví dụ: nếu là Student thì lịch hôm nay là các môn học
//             if (_currentUser.UserType == "Student")
//             {
//                 _gridTodaySchedule.Rows.Add("Ca 1 (07:00–08:00)", "A08", "CTDL & GT", "GV Nguyễn Văn B", "Học", "");
//                 _gridTodaySchedule.Rows.Add("Ca 3 (09:00–10:00)", "A16", "Lập trình .NET", "GV Trần Thị C", "Học", "");
//             }
//             else
//             {
//                 // Ví dụ: nếu là Lecturer hoặc Staff -> lịch theo booking phòng
//                 _gridTodaySchedule.Rows.Add("Ca 2 (08:00–09:00)", "A08", "Họp nhóm đồ án", _currentUser.FullName, "APPROVED", "Book phòng thảo luận");
//                 _gridTodaySchedule.Rows.Add("Ca 5 (13:00–14:00)", "A16", "Seminar Khoa CNTT", _currentUser.FullName, "APPROVED", "");
//             }

//             // Thông báo mới (demo)
//             _lstLatestNotifications.Items.Add("Phòng A08 ca 3 ngày 05/12 đã được grant. Vui lòng check-in trước 07:15.");
//             _lstLatestNotifications.Items.Add("Phòng A08 ca 5 chuyển sang A16 do hội thảo Khoa CNTT.");
//             _lstLatestNotifications.Items.Add("Booking ca 2 phòng B03 của bạn đã bị hủy do bảo trì phòng.");
//         }

//         private async Task LoadHomeFromServerAsync()
//         {
//             if (_writer == null)
//             {
//                 MessageBox.Show("TCP chưa sẵn sàng.");
//                 return;
//             }

//             await _writer.WriteLineAsync($"GET_HOME_DATA|{_currentUser.UserId}");
//             // Không đọc gì ở đây — server sẽ push vào HandleServerMessage()
//         }



//         // ================== 2.4. TAB ĐẶT PHÒNG ==================
//         private void BuildBookingTabUi()
//         {
//             _tabBooking.Controls.Clear();
//             _tabBooking.AutoScroll = true;
//             _tabBooking.BackColor = Color.White;

//             // ================= A. FILTER + LIST PHÒNG =================
//             _pnlBookingWizardHeader = new Panel
//             {
//                 Left = 10,
//                 Top = 10,
//                 Width = 950,
//                 Height = 60,
//                 BackColor = Color.White
//             };
//             _tabBooking.Controls.Add(_pnlBookingWizardHeader);

//             _lblBookingStep1 = new Label
//             {
//                 Left = 0,
//                 Top = 10,
//                 Width = 260,
//                 Height = 40,
//                 Text = "1. Bộ lọc",
//                 Font = new Font("Segoe UI", 11f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(17, 24, 39)
//             };
//         //  _pnlBookingWizardHeader.Controls.Add(_lblBookingStep1);

//             _lblBookingStep2 = new Label
//             {
//                 Left = 270,
//                 Top = 10,
//                 Width = 320,
//                 Height = 40,
//                 Text = "2. Chọn phòng",
//                 Font = new Font("Segoe UI", 11f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(107, 114, 128)
//             };
//             // _pnlBookingWizardHeader.Controls.Add(_lblBookingStep2);

//             _lblBookingStep3 = new Label
//             {
//                 Left = 600,
//                 Top = 10,
//                 Width = 340,
//                 Height = 40,
//                 Text = "3. Chọn slot",
//                 Font = new Font("Segoe UI", 11f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(107, 114, 128)
//             };
//             // _pnlBookingWizardHeader.Controls.Add(_lblBookingStep3);

//             _grpSearchRooms = new GroupBox
//             {
//                 Text = "Danh sách phòng",
//                 Left = 10,
//                 Top = 90,
//                 Width = 950,
//                 Height = 140
//             };
//             _tabBooking.Controls.Add(_grpSearchRooms);

//             // Ngày
//             var lblDate = new Label
//             {
//                 Text = "Ngày:",
//                 Left = 10,
//                 Top = 70,
//                 Width = 50
//             };
//             _grpSearchRooms.Controls.Add(lblDate);

//             _dtBookingDate = new DateTimePicker
//             {
//                 Left = 70,
//                 Top = 70,
//                 Width = 130,
//                 Format = DateTimePickerFormat.Custom,
//                 CustomFormat = "dd/MM/yyyy"
//             };
//             _grpSearchRooms.Controls.Add(_dtBookingDate);

//             // Building
//             var lblBuilding = new Label
//             {
//                 Text = "Tòa nhà:",
//                 Left = 220,
//                 Top = 70,
//                 Width = 60
//             };
//             _grpSearchRooms.Controls.Add(lblBuilding);

//             _cbBuilding = new ComboBox
//             {
//                 Left = 290,
//                 Top = 70,
//                 Width = 200,
//                 DropDownStyle = ComboBoxStyle.DropDownList
//             };
//             _grpSearchRooms.Controls.Add(_cbBuilding);
//             _cbBuilding.Items.Clear();
//             _cbBuilding.Items.Add("ALL");
//             _cbBuilding.SelectedIndex = 0;

//             // Sức chứa
//             var lblCapacity = new Label
//             {
//                 Text = "Sức chứa ≥",
//                 Left = 520,
//                 Top = 70,
//                 Width = 80
//             };
//             _grpSearchRooms.Controls.Add(lblCapacity);

//             _numMinCapacity = new NumericUpDown
//             {
//                 Left = 600,
//                 Top = 70,
//                 Width = 80,
//                 Minimum = 0,
//                 Maximum = 500,
//                 Value = 0
//             };
//             _grpSearchRooms.Controls.Add(_numMinCapacity);

//             // Nút load phòng
//             _btnSearchRooms = new Button
//             {
//                 Text = "Load phòng",
//                 Left = 720,
//                 Top = 90,
//                 Width = 200,
//                 Height = 30
//             };
//             _grpSearchRooms.Controls.Add(_btnSearchRooms);

//             // ===== Hàng 2: checkbox filter theo tiện nghi =====
//             _chkNeedProjector = new CheckBox
//             {
//                 Text = "Cần máy chiếu",
//                 Left = 10,
//                 Top = 90,
//                 Width = 130
//             };
//             _grpSearchRooms.Controls.Add(_chkNeedProjector);
//             _chkNeedPC = new CheckBox
//             {
//                 Text = "Cần PC",
//                 Left = 160,
//                 Top = 90,
//                 Width = 100
//             };
//             _grpSearchRooms.Controls.Add(_chkNeedPC);
//             _chkNeedAC = new CheckBox
//             {
//                 Text = "Cần điều hòa",
//                 Left = 270,
//                 Top = 90,
//                 Width = 120
//             };
//             _grpSearchRooms.Controls.Add(_chkNeedAC);
//             _chkNeedMic = new CheckBox
//             {
//                 Text = "Cần micro",
//                 Left = 410,
//                 Top = 90,
//                 Width = 120
//             };
//             _grpSearchRooms.Controls.Add(_chkNeedMic);

//             // Sự kiện filter: chỉ gọi ApplyRoomFilter (không gửi lệnh mới lên server)
//             _cbBuilding.SelectedIndexChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };
//             _numMinCapacity.ValueChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };
//             _chkNeedProjector.CheckedChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };
//             _chkNeedPC.CheckedChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };
//             _chkNeedAC.CheckedChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };
//             _chkNeedMic.CheckedChanged += (s, e) => { SetBookingWizardStep(1); ApplyRoomFilter(); };

//             _btnSearchRooms.Click += async (s, e) =>
//             {
//                 SetBookingWizardStep(2);
//                 await LoadRoomsFromServerAsync();
//                 // ApplyRoomFilter();
//             };

//             // ===== Grid phòng =====
//             _gridRooms = new DataGridView
//             {
//                 Left = 10,
//                 Top = _grpSearchRooms.Bottom + 5,
//                 Width = 950,
//                 Height = 220,
//                 ReadOnly = true,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                 MultiSelect = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                 AutoGenerateColumns = false
//             };
//             _tabBooking.Controls.Add(_gridRooms);
//             _gridRooms.Visible = false;

//             _gridRooms.Columns.Clear();
//             _gridRooms.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "RoomId",
//                 HeaderText = "Phòng",
//                 DataPropertyName = "RoomId"
//             });
//             _gridRooms.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Building",
//                 HeaderText = "Tòa nhà",
//                 DataPropertyName = "Building"
//             });
//             _gridRooms.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Capacity",
//                 HeaderText = "Sức chứa",
//                 DataPropertyName = "Capacity"
//             });
//             _gridRooms.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Status",
//                 HeaderText = "Trạng thái",
//                 DataPropertyName = "Status"
//             });

//             _gridRooms.SelectionChanged += async (s, e) =>
//             {
//                 if (_suppressRoomSelectionHandler) return;
//                 if (_gridRooms.CurrentRow == null) return;

//                 var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//                 if (string.IsNullOrEmpty(roomId)) return;

//                 if (IsRoomDisabled(roomId))
//                 {
//                     _suppressRoomSelectionHandler = true;
//                     try
//                     {
//                         if (!string.IsNullOrWhiteSpace(_lastEnabledRoomId))
//                             SelectRoomInGridByRoomId(_lastEnabledRoomId);
//                         else
//                         {
//                             foreach (DataGridViewRow r in _gridRooms.Rows)
//                             {
//                                 if (r.DataBoundItem is RoomSearchRow rs && !IsRoomDisabled(rs.RoomId))
//                                 {
//                                     _gridRooms.CurrentCell = r.Cells["RoomId"];
//                                     r.Selected = true;
//                                     break;
//                                 }
//                             }
//                         }
//                     }
//                     finally
//                     {
//                         _suppressRoomSelectionHandler = false;
//                     }
//                     return;
//                 }

//                 _lastEnabledRoomId = roomId;

//                 var date = _dtBookingDate.Value;

//                 // ⭐ M2 – SUB theo màn hình
//                 await UpdateRoomSlotsSubscriptionAsync(roomId, date);

//                 await ReloadSlotsForSelectedRoomAsync();
//             };

//             _flpRoomCards = new FlowLayoutPanel
//             {
//                 Left = 10,
//                 Top = _grpSearchRooms.Bottom + 5,
//                 Width = 950,
//                 Height = 220,
//                 AutoScroll = true,
//                 WrapContents = true,
//                 FlowDirection = FlowDirection.LeftToRight,
//                 BackColor = Color.White,
//                 Padding = new Padding(6)
//             };
//             _tabBooking.Controls.Add(_flpRoomCards);

//             _dtBookingDate.ValueChanged += async (s, e) =>
//             {
//                 if (_gridRooms.CurrentRow == null) return;

//                 var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//                 if (string.IsNullOrEmpty(roomId)) return;

//                 await UpdateRoomSlotsSubscriptionAsync(roomId, _dtBookingDate.Value);
//             };

//             // ================= B. SLOT LIST CỦA PHÒNG =================
//             _grpRoomSlots = new GroupBox
//             {
//                 Text = "Slot trong ngày của phòng",
//                 Left = _gridRooms.Right + 10,
//                 Top = _grpSearchRooms.Bottom + 5,
//                 Width = 510,
//                 Height = 220
//             };
//             _tabBooking.Controls.Add(_grpRoomSlots);
//             _grpRoomSlots.Visible = false;

//             _gridRoomSlots = new DataGridView
//             {
//                 Dock = DockStyle.Fill,
//                 ReadOnly = false,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                 MultiSelect = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                 AutoGenerateColumns = false
//             };
//             _grpRoomSlots.Controls.Add(_gridRoomSlots);
//             _gridRoomSlots.CurrentCellDirtyStateChanged += (s, e) =>
//             {
//                 if (_gridRoomSlots.IsCurrentCellDirty)
//                 {
//                     _gridRoomSlots.EndEdit();
//                     _gridRoomSlots.CommitEdit(DataGridViewDataErrorContexts.Commit);
//                 }
//             };

//             _gridRoomSlots.Columns.Clear();
//             _gridRoomSlots.Columns.Add(new DataGridViewCheckBoxColumn
//             {
//                 Name = "Selected",
//                 HeaderText = "",
//                 Width = 40,
//                 DataPropertyName = "Selected"
//             });
//             _gridRoomSlots.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "SlotId",
//                 HeaderText = "Slot",
//                 DataPropertyName = "SlotId"
//             });
//             _gridRoomSlots.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "TimeRange",
//                 HeaderText = "Giờ",
//                 DataPropertyName = "TimeRange"
//             });
//             _gridRoomSlots.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Status",
//                 HeaderText = "Trạng thái",
//                 DataPropertyName = "Status"
//             });
//             _gridRoomSlots.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "HolderName",
//                 HeaderText = "Người giữ",
//                 DataPropertyName = "HolderName"
//             });
//             _gridRoomSlots.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Purpose",
//                 HeaderText = "Mục đích",
//                 DataPropertyName = "Purpose"
//             });
//             _bsRoomSlots.DataSource = _currentRoomSlots;
//             _gridRoomSlots.DataSource = _bsRoomSlots;

//             // ================= C. BOOKING CỦA TÔI + NÚT REQUEST / RELEASE =================
//             var grpMyBookings = new GroupBox
//             {
//                 Text = "Booking của tôi",
//                 Left = 10,
//                 Top = _gridRooms.Bottom + 10,
//                 Width = 950,
//                 Height = 230
//             };
//             _tabBooking.Controls.Add(grpMyBookings);

//             _gridMyBookings = new DataGridView
//             {
//                 Left = 10,
//                 Top = 20,
//                 Width = 720,
//                 Height = 220,
//                 ReadOnly = true,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                 MultiSelect = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                 AutoGenerateColumns = false,
//                 ScrollBars = ScrollBars.Both
//             };
//             grpMyBookings.Controls.Add(_gridMyBookings);

//             _gridMyBookings.Columns.Clear();
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Date",
//                 HeaderText = "Ngày",
//                 DataPropertyName = "Date"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "RoomId",
//                 HeaderText = "Phòng",
//                 DataPropertyName = "RoomId"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "TimeRange",
//                 HeaderText = "Giờ",
//                 DataPropertyName = "TimeRange"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Status",
//                 HeaderText = "Trạng thái",
//                 DataPropertyName = "Status"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "Purpose",
//                 HeaderText = "Mục đích",
//                 DataPropertyName = "Purpose"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "CreatedAt",
//                 HeaderText = "Tạo lúc",
//                 DataPropertyName = "CreatedAt"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "CheckinDeadline",
//                 HeaderText = "Hạn check-in",
//                 DataPropertyName = "CheckinDeadline"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "CheckinTime",
//                 HeaderText = "Check-in lúc",
//                 DataPropertyName = "CheckinTime"
//             });
//             _gridMyBookings.Columns.Add(new DataGridViewTextBoxColumn
//             {
//                 Name = "UpdatedAt",
//                 HeaderText = "Cập nhật",
//                 DataPropertyName = "UpdatedAt"
//             });

//             _bsMyBookings.DataSource = _myBookings;
//             _gridMyBookings.DataSource = _bsMyBookings;

//             // Làm nổi bật lịch cố định (Status == "FIXED")
//             _gridMyBookings.CellFormatting += (s, e) =>
//             {
//                 if (_gridMyBookings.Columns[e.ColumnIndex].Name == "Status")
//                 {
//                     var row = _gridMyBookings.Rows[e.RowIndex].DataBoundItem as MyBookingRow;
//                     if (row != null && row.Status == "FIXED")
//                     {
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.Font = new Font(_gridMyBookings.Font, FontStyle.Bold);
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkOrange;
//                     }
//                     else
//                     {
//                         // Reset nếu không phải fixed
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.Font = _gridMyBookings.Font;
//                         _gridMyBookings.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Black;
//                     }
//                 }
//             };

//             // Text lý do
//             var lblPurpose = new Label
//             {
//                 Text = "Lý do:",
//                 Left = 740,
//                 Top = 29,
//                 Width = 50
//             };
//             grpMyBookings.Controls.Add(lblPurpose);

//             _txtPurpose = new TextBox
//             {
//                 Left = 740,
//                 Top = 50,
//                 Width = 190,
//                 Height = 80,
//                 Multiline = true
//             };
//             grpMyBookings.Controls.Add(_txtPurpose);

//             // Nút REQUEST
//             _btnRequest = new Button
//             {
//                 Text = "REQUEST",
//                 Left = 740,
//                 Top = 140,
//                 Width = 90,
//                 Height = 30
//             };
//             // grpMyBookings.Controls.Add(_btnRequest);
//             _btnRequest.Click += BtnRequest_Click;

//             // Nút RELEASE booking
//             _btnReleaseBooking = new Button
//             {
//                 Text = "RELEASE",
//                 Left = 840,
//                 Top = 140,
//                 Width = 90,
//                 Height = 30
//             };
//             grpMyBookings.Controls.Add(_btnReleaseBooking);
//             _btnReleaseBooking.Click += BtnReleaseBooking_Click;

//             // Label status
//             _lblRequestStatus = new Label
//             {
//                 Text = "Chưa có yêu cầu.",
//                 Left = 10,
//                 Top = grpMyBookings.Bottom + 5,
//                 Width = 800,
//                 ForeColor = Color.DarkGray,
//                 Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
//             };
//             _tabBooking.Controls.Add(_lblRequestStatus);

//             // Client log
//             _grpClientLog = new GroupBox
//             {
//                 Text = "Client log (debug)",
//                 Dock = DockStyle.Fill
//             };
//             _tabLog.Controls.Add(_grpClientLog);

//             _txtClientLog = new TextBox
//             {
//                 Dock = DockStyle.Fill,
//                 Multiline = true,
//                 ScrollBars = ScrollBars.Vertical
//             };
//             _grpClientLog.Controls.Add(_txtClientLog);

//             // Nút Về Trang chủ
//             _btnBackToHome = new Button
//             {
//                 Text = "← Về Trang chủ",
//                 Width = 120,
//                 Height = 30,
//                 Left = 840,
//                 Top = _grpClientLog.Bottom + 5
//             };
//             // _tabBooking.Controls.Add(_btnBackToHome);

//             _btnBackToHome.Click += (s, e) =>
//             {
//                 _tabMain.SelectedTab = _tabHome;
//             };

//             // InitBookingTabData();      // dùng lại init cũ nhưng chỉ lấy SlotConfig + Building
//             SetBookingWizardStep(1);
//         }

//         private void SetBookingWizardStep(int step)
//         {
//             if (_lblBookingStep1 == null || _lblBookingStep2 == null || _lblBookingStep3 == null) return;

//             var active = Color.FromArgb(17, 24, 39);
//             var inactive = Color.FromArgb(107, 114, 128);

//             _lblBookingStep1.ForeColor = step == 1 ? active : inactive;
//             _lblBookingStep2.ForeColor = step == 2 ? active : inactive;
//             _lblBookingStep3.ForeColor = step == 3 ? active : inactive;
//         }

//         private void RenderRoomCards(IEnumerable<RoomSearchRow> rooms)
//         {
//             if (_flpRoomCards == null) return;

//             _flpRoomCards.SuspendLayout();
//             try
//             {
//                 _flpRoomCards.Controls.Clear();
//                 _roomCardById.Clear();

//                 var list = rooms.ToList();
//                 if (list.Count == 0)
//                 {
//                     var empty = new Label
//                     {
//                         AutoSize = false,
//                         Width = _flpRoomCards.ClientSize.Width - 24,
//                         Height = 80,
//                         Text = "Không có phòng phù hợp bộ lọc hiện tại.",
//                         TextAlign = ContentAlignment.MiddleCenter,
//                         ForeColor = Color.FromArgb(107, 114, 128),
//                         Font = new Font("Segoe UI", 10f, FontStyle.Italic),
//                         Margin = new Padding(8)
//                     };
//                     _flpRoomCards.Controls.Add(empty);
//                     _selectedRoomCardId = null;
//                     return;
//                 }

//                 foreach (var r in list)
//                 {
//                     var card = CreateRoomCard(r);
//                     _flpRoomCards.Controls.Add(card);
//                 }

//                 if (!string.IsNullOrWhiteSpace(_selectedRoomCardId) && _roomCardById.TryGetValue(_selectedRoomCardId, out var selectedCard))
//                 {
//                     ApplyRoomCardSelectedStyle(selectedCard, true);
//                 }
//             }
//             finally
//             {
//                 _flpRoomCards.ResumeLayout();
//             }
//         }

//         private Control CreateRoomCard(RoomSearchRow r)
//         {
//             var card = new Panel
//             {
//                 Width = 220,
//                 Height = 110,
//                 BackColor = Color.White,
//                 Margin = new Padding(8),
//                 Cursor = Cursors.Hand,
//                 BorderStyle = BorderStyle.FixedSingle,
//                 Tag = r.RoomId
//             };

//             _roomCardById[r.RoomId] = card;

//             var lblRoom = new Label
//             {
//                 Left = 12,
//                 Top = 12,
//                 Width = 180,
//                 Height = 22,
//                 Text = r.RoomId,
//                 Font = new Font("Segoe UI", 12f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(17, 24, 39)
//             };
//             card.Controls.Add(lblRoom);

//             var lblMeta = new Label
//             {
//                 Left = 12,
//                 Top = 38,
//                 Width = 200,
//                 Height = 18,
//                 Text = $"{r.Building} • {r.Capacity}",
//                 Font = new Font("Segoe UI", 9f, FontStyle.Regular),
//                 ForeColor = Color.FromArgb(107, 114, 128)
//             };
//             card.Controls.Add(lblMeta);

//             var statusText = (r.Status ?? string.Empty).Trim();
//             if (string.IsNullOrWhiteSpace(statusText)) statusText = "UNKNOWN";

//             var lblStatus = new Label
//             {
//                 Left = 12,
//                 Top = 62,
//                 Width = 200,
//                 Height = 18,
//                 Text = statusText,
//                 Font = new Font("Segoe UI", 9f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(55, 65, 81)
//             };
//             card.Controls.Add(lblStatus);

//             var lblAmenities = new Label
//             {
//                 Left = 12,
//                 Top = 84,
//                 Width = 200,
//                 Height = 18,
//                 Text = BuildAmenitiesText(r),
//                 Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
//                 ForeColor = Color.FromArgb(107, 114, 128)
//             };
//             card.Controls.Add(lblAmenities);

//             EventHandler click = async (s, e) =>
//             {
//                 if (string.Equals(NormalizeRoomStatus(r.Status), "DISABLED", StringComparison.OrdinalIgnoreCase))
//                     return;
//                 SetSelectedRoomCard(r.RoomId);
//                 SetBookingWizardStep(3);
//                 await OpenSlotPickerForRoomAsync(r.RoomId);
//                 SetBookingWizardStep(2);
//             };

//             card.Click += click;
//             lblRoom.Click += click;
//             lblMeta.Click += click;
//             lblStatus.Click += click;
//             lblAmenities.Click += click;

//             if (string.Equals(NormalizeRoomStatus(r.Status), "DISABLED", StringComparison.OrdinalIgnoreCase))
//             {
//                 card.BackColor = Color.FromArgb(243, 244, 246);
//                 card.Cursor = Cursors.No;
//                 lblRoom.ForeColor = Color.FromArgb(156, 163, 175);
//                 lblStatus.ForeColor = Color.FromArgb(156, 163, 175);
//             }

//             return card;
//         }

//         private void SetSelectedRoomCard(string roomId)
//         {
//             if (string.IsNullOrWhiteSpace(roomId)) return;

//             if (!string.IsNullOrWhiteSpace(_selectedRoomCardId) && _roomCardById.TryGetValue(_selectedRoomCardId, out var oldCard))
//             {
//                 ApplyRoomCardSelectedStyle(oldCard, false);
//             }

//             _selectedRoomCardId = roomId;
//             if (_roomCardById.TryGetValue(roomId, out var newCard))
//             {
//                 ApplyRoomCardSelectedStyle(newCard, true);
//             }
//         }

//         private void ApplyRoomCardSelectedStyle(Panel card, bool selected)
//         {
//             if (selected)
//             {
//                 card.BackColor = Color.FromArgb(239, 246, 255);
//                 card.BorderStyle = BorderStyle.FixedSingle;
//             }
//             else
//             {
//                 card.BackColor = Color.White;
//                 card.BorderStyle = BorderStyle.FixedSingle;
//             }
//         }

//         private string BuildAmenitiesText(RoomSearchRow r)
//         {
//             var parts = new List<string>();
//             if (r.HasProjector) parts.Add("Projector");
//             if (r.HasPC) parts.Add("PC");
//             if (r.HasAC) parts.Add("AC");
//             if (r.HasMic) parts.Add("Mic");
//             if (parts.Count == 0) return "(Không có tiện nghi)";
//             return string.Join(" • ", parts);
//         }

//         private void SelectRoomInGridByRoomId(string roomId)
//         {
//             if (_gridRooms == null) return;

//             foreach (DataGridViewRow row in _gridRooms.Rows)
//             {
//                 var id = row.Cells["RoomId"].Value?.ToString();
//                 if (!string.Equals(id, roomId, StringComparison.OrdinalIgnoreCase)) continue;

//                 _gridRooms.ClearSelection();
//                 row.Selected = true;
//                 _gridRooms.CurrentCell = row.Cells["RoomId"];
//                 return;
//             }
//         }

//         private async Task OpenSlotPickerForRoomAsync(string roomId)
//         {
//             if (_grpRoomSlots == null || _dtBookingDate == null) return;

//             if (IsRoomDisabled(roomId))
//                 return;

//             _suppressRoomSelectionHandler = true;
//             try
//             {
//                 SelectRoomInGridByRoomId(roomId);
//                 await UpdateRoomSlotsSubscriptionAsync(roomId, _dtBookingDate.Value);
//                 await ReloadSlotsForSelectedRoomAsync();
//             }
//             finally
//             {
//                 _suppressRoomSelectionHandler = false;
//             }

//             var oldParent = _grpRoomSlots.Parent;
//             var oldBounds = _grpRoomSlots.Bounds;
//             var oldDock = _grpRoomSlots.Dock;
//             var oldVisible = _grpRoomSlots.Visible;

//             using (var dlg = new Form())
//             {
//                 dlg.Text = $"Chọn slot - {roomId}";
//                 dlg.StartPosition = FormStartPosition.CenterParent;
//                 dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
//                 dlg.MinimizeBox = false;
//                 dlg.MaximizeBox = false;
//                 dlg.ShowInTaskbar = false;
//                 dlg.BackColor = Color.White;
//                 dlg.Width = 820;
//                 dlg.Height = 560;

//                 var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.White };
//                 dlg.Controls.Add(header);

//                 var title = new Label
//                 {
//                     Left = 16,
//                     Top = 12,
//                     Width = 760,
//                     Height = 28,
//                     Text = $"Chọn slot cho phòng {roomId} ({_dtBookingDate.Value:dd/MM/yyyy})",
//                     Font = new Font("Segoe UI", 12f, FontStyle.Bold),
//                     ForeColor = Color.FromArgb(17, 24, 39)
//                 };
//                 header.Controls.Add(title);

//                 var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
//                 dlg.Controls.Add(footer);

//                 var lblPurpose = new Label
//                 {
//                     Left = 16,
//                     Top = 10,
//                     Width = 44,
//                     Height = 34,
//                     Text = "Lý do",
//                     TextAlign = ContentAlignment.MiddleLeft
//                 };
//                 footer.Controls.Add(lblPurpose);

//                 var txtPurpose = new TextBox
//                 {
//                     Left = 64,
//                     Top = 12,
//                     Width = 430,
//                     Height = 28
//                 };
//                 txtPurpose.Text = _txtPurpose?.Text ?? string.Empty;
//                 footer.Controls.Add(txtPurpose);

//                 var btnRequest = new Button
//                 {
//                     Text = "Gửi REQUEST",
//                     Width = 130,
//                     Height = 34,
//                     Left = 510,
//                     Top = 10
//                 };
//                 footer.Controls.Add(btnRequest);

//                 var btnClose = new Button
//                 {
//                     Text = "Đóng",
//                     Width = 120,
//                     Height = 34,
//                     Left = 650,
//                     Top = 10,
//                     Anchor = AnchorStyles.Right | AnchorStyles.Top
//                 };
//                 btnClose.Click += (s, e) => dlg.Close();
//                 footer.Controls.Add(btnClose);

//                 btnRequest.Click += async (s, e) =>
//                 {
//                     if (_txtPurpose != null)
//                         _txtPurpose.Text = txtPurpose.Text;

//                     bool sent = await TrySendBookingRequestAsync();
//                     if (sent)
//                         dlg.Close();
//                 };

//                 _grpRoomSlots.Parent = dlg;
//                 _grpRoomSlots.Dock = DockStyle.Fill;
//                 _grpRoomSlots.Visible = true;
//                 dlg.Controls.SetChildIndex(_grpRoomSlots, 1);

//                 dlg.ShowDialog(this);

//                 _grpRoomSlots.Parent = oldParent;
//                 _grpRoomSlots.Dock = oldDock;
//                 _grpRoomSlots.Bounds = oldBounds;
//                 _grpRoomSlots.Visible = oldVisible;
//             }
//         }

//         private async Task<bool> TrySendBookingRequestAsync()
//         {
//             try
//             {
//                 if (_currentUser == null)
//                 {
//                     MessageBox.Show("Chưa đăng nhập.");
//                     return false;
//                 }
//                 if (_writer == null)
//                 {
//                     MessageBox.Show("Chưa kết nối server.");
//                     return false;
//                 }
//                 if (_gridRooms.CurrentRow == null)
//                 {
//                     MessageBox.Show("Vui lòng chọn phòng.");
//                     return false;
//                 }

//                 var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//                 if (string.IsNullOrWhiteSpace(roomId))
//                 {
//                     MessageBox.Show("Phòng không hợp lệ.");
//                     return false;
//                 }

//                 if (IsRoomDisabled(roomId))
//                 {
//                     MessageBox.Show("Phòng đang DISABLED, không thể request.");
//                     return false;
//                 }

//                 var purpose = _txtPurpose.Text.Trim();
//                 if (string.IsNullOrWhiteSpace(purpose))
//                 {
//                     MessageBox.Show("Vui lòng nhập lý do mượn phòng.");
//                     return false;
//                 }

//                 var selected = _currentRoomSlots
//                     .Where(r => r.Selected)
//                     .ToList();

//                 if (selected.Count == 0)
//                 {
//                     MessageBox.Show("Vui lòng tick ít nhất 1 slot để request.");
//                     return false;
//                 }

//                 var dateKey = _dtBookingDate.Value.ToString("yyyy-MM-dd");

//                 foreach (var s in selected)
//                 {
//                     int slotIdx = ParseSlotIndexSafe(s.SlotId);
//                     if (slotIdx <= 0) continue;

//                     bool overlapWithGrantedBooking = _myBookings.Any(b =>
//                     {
//                         if (!IsGrantedBookingStatus(b.Status)) return false;
//                         if (!string.Equals(b.Date, dateKey, StringComparison.OrdinalIgnoreCase))
//                             return false;
//                         int start = ParseSlotIndexSafe(b.SlotStartId);
//                         int end = ParseSlotIndexSafe(b.SlotEndId);
//                         if (start <= 0 || end < start) return false;
//                         return slotIdx >= start && slotIdx <= end;
//                     });

//                     if (overlapWithGrantedBooking)
//                     {
//                         MessageBox.Show(
//                             $"Slot {s.SlotId} trùng với một booking đã được GRANT của bạn.\nKhông được request chồng (kể cả single/range).",
//                             "Request",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Warning
//                         );
//                         return false;
//                     }
//                 }

//                 var ordered = selected
//                     .Select(s => new { Row = s, Index = ParseSlotIndexSafe(s.SlotId) })
//                     .Where(x => x.Index > 0)
//                     .OrderBy(x => x.Index)
//                     .ToList();

//                 if (ordered.Count == 0)
//                 {
//                     MessageBox.Show("Slot không hợp lệ.");
//                     return false;
//                 }

//                 bool isContinuous = true;
//                 for (int i = 1; i < ordered.Count; i++)
//                 {
//                     if (ordered[i].Index != ordered[i - 1].Index + 1)
//                     {
//                         isContinuous = false;
//                         break;
//                     }
//                 }

//                 string slotFrom = ordered.First().Row.SlotId;
//                 string slotTo = ordered.Last().Row.SlotId;

//                 if (!isContinuous && ordered.Count > 1)
//                 {
//                     MessageBox.Show("Các slot được chọn phải liên tục (S3, S4, S5...).");
//                     return false;
//                 }

//                 string safePurpose = purpose
//                     .Replace("|", "/")
//                     .Replace("\r", " ")
//                     .Replace("\n", " ");

//                 string cmd;
//                 if (ordered.Count == 1)
//                 {
//                     cmd = $"REQUEST|{_currentUser.UserId}|{roomId}|{slotFrom}|{dateKey}|{safePurpose}";
//                 }
//                 else
//                 {
//                     cmd = $"REQUEST_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}|{dateKey}|{safePurpose}";
//                 }

//                 AppendClientLog("[SEND] " + cmd);
//                 await _writer.WriteLineAsync(cmd);

//                 _lblRequestStatus.ForeColor = Color.Blue;
//                 _lblRequestStatus.Text = $"Đang gửi REQUEST cho {roomId}: {slotFrom} → {slotTo}";

//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] BtnRequest_Click: " + ex.Message);
//                 MessageBox.Show("Request lỗi: " + ex.Message);
//                 return false;
//             }
//         }
//         private async Task ReloadSlotsForSelectedRoomAsync()
//         {
//             if (_roomSlotsRequestInFlight) return;
//             if (_gridRooms.CurrentRow == null) return;

//             var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//             if (string.IsNullOrWhiteSpace(roomId)) return;

//             var date = _dtBookingDate.Value.Date.ToString("yyyy-MM-dd");
//             var key = roomId + "|" + date;

//             if (_writer == null) return;

//             // ✅ CHỐNG CHỒNG REQUEST
//             if (_roomSlotsRequestInFlight && _lastRoomSlotsKey == key)
//                 return;

//             _roomSlotsRequestInFlight = true;
//             _lastRoomSlotsKey = key;

//             string cmd = $"GET_ROOM_DAILY_SLOTS|{roomId}|{date}";
//             AppendClientLog("[SEND] " + cmd);
//             await _writer.WriteLineAsync(cmd);
//         }

//         private async Task ReloadMyBookingsAsync()
//         {
//             if (_currentUser == null)
//                 return;

//             if (_writer == null)
//             {
//                 MessageBox.Show("TCP chưa sẵn sàng.");
//                 return;
//             }

//             string cmd = $"GET_MY_BOOKINGS|{_currentUser.UserId}";
//             AppendClientLog("[SEND] " + cmd);
//             await _writer.WriteLineAsync(cmd);
//         }

//         private async Task LoadFixedSchedulesFromServerAsync()
//         {
//             if (_currentUser == null)
//                 return;

//             if (_writer == null)
//             {
//                 AppendClientLog("[WARN] LoadFixedSchedulesFromServerAsync: TCP chưa sẵn sàng");
//                 return;
//             }

//             // Lấy fixed schedules cho 6 tháng tới
//             var fromDate = DateTime.Today.ToString("yyyy-MM-dd");
//             var toDate = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");
            
//             string cmd = $"GET_FIXED_SESSIONS|{_currentUser.UserId}|{fromDate}|{toDate}";
//             AppendClientLog("[SEND] " + cmd);
//             await _writer.WriteLineAsync(cmd);
//         }

//         private void ApplyRoomFilter()
//         {
//             var filtered = new List<RoomSearchRow>();

//             foreach (var r in _allRoomsForSearch)
//             {
//                 if (_chkNeedProjector.Checked && !r.HasProjector) continue;
//                 if (_chkNeedPC.Checked && !r.HasPC) continue;
//                 if (_chkNeedAC.Checked && !r.HasAC) continue;
//                 if (_chkNeedMic.Checked && !r.HasMic) continue;

//                 if (_numMinCapacity.Value > 0 && r.Capacity < (int)_numMinCapacity.Value)
//                     continue;

//                 if (_cbBuilding.SelectedIndex > 0)
//                 {
//                     var selectedBuilding = _cbBuilding.SelectedItem?.ToString();
//                     if (selectedBuilding != null && !string.Equals(r.Building, selectedBuilding, StringComparison.OrdinalIgnoreCase))
//                         continue;
//                 }

//                 filtered.Add(r);
//             }

//             _gridRooms.DataSource = null;
//             _gridRooms.DataSource = filtered;
//             ApplyRoomsGridDisabledStyle();

//             if (_flpRoomCards != null)
//             {
//                 RenderRoomCards(filtered);
//                 if (filtered.Count > 0) SetBookingWizardStep(2);
//             }
//         }

//         private async Task LoadRoomsFromServerAsync()
//         {
//             if (_writer == null)
//             {
//                 MessageBox.Show("Không thể load phòng: TCP chưa sẵn sàng.");
//                 return;
//             }

//             await _writer.WriteLineAsync("GET_ROOMS");
//             // KHÔNG đọc stream tại đây – kết quả sẽ đi vào HandleServerMessage()
//         }

//         private void UpdateRoomsGridOnUi(List<RoomInfo>? rooms)
//         {
//             if (rooms == null) return;

//             _allRoomsForSearch.Clear();

//             foreach (var r in rooms)
//             {
//                 _allRoomsForSearch.Add(new RoomSearchRow
//                 {
//                     RoomId = r.RoomId!,
//                     Building = r.Building!,
//                     Capacity = r.Capacity,
//                     HasProjector = r.HasProjector,
//                     HasPC = r.HasPC,
//                     HasAC = r.HasAirConditioner,
//                     HasMic = r.HasMic,
//                     Status = NormalizeRoomStatus(r.Status)
//                 });
//             }

//             RefreshBuildingComboFromRooms();
//             RefreshReqRoomComboFromRooms();

//             // _gridRooms.DataSource = null;
//             // _gridRooms.DataSource = _allRoomsForSearch;

//             ApplyRoomFilter();

//         }

//         private void InitBookingTabData()
//         {
//             // Slot combobox: S1..S14
//             var slotIds = Enumerable.Range(1, 14)
//                 .Select(i => $"S{i}")
//                 .ToList();

//             _cbBookingFromSlot.Items.Clear();
//             _cbBookingToSlot.Items.Clear();
//             _cbReqSlotSingle.Items.Clear();
//             _cbReqSlotFrom.Items.Clear();
//             _cbReqSlotTo.Items.Clear();

//             foreach (var s in slotIds)
//             {
//                 _cbBookingFromSlot.Items.Add(s);
//                 _cbBookingToSlot.Items.Add(s);
//                 _cbReqSlotSingle.Items.Add(s);
//                 _cbReqSlotFrom.Items.Add(s);
//                 _cbReqSlotTo.Items.Add(s);
//             }

//             if (_cbBookingFromSlot.Items.Count > 0)
//                 _cbBookingFromSlot.SelectedIndex = 0;
//             if (_cbBookingToSlot.Items.Count > 0)
//                 _cbBookingToSlot.SelectedIndex = _cbBookingToSlot.Items.Count - 1;
//             if (_cbReqSlotSingle.Items.Count > 0)
//                 _cbReqSlotSingle.SelectedIndex = 2; // ví dụ default S3
//             if (_cbReqSlotFrom.Items.Count > 0)
//                 _cbReqSlotFrom.SelectedIndex = 2;
//             if (_cbReqSlotTo.Items.Count > 0)
//                 _cbReqSlotTo.SelectedIndex = 4;

//             // Ngày default = hôm nay theo server
//             var now = DateTime.Now + _serverTimeOffset;
//             _dtBookingDate.Value = now.Date;
//             _dtReqDate.Value = now.Date;

//             if (_cbBuilding != null)
//             {
//                 _cbBuilding.Items.Clear();
//                 _cbBuilding.Items.Add("ALL");
//                 _cbBuilding.SelectedIndex = 0;
//             }

//             RefreshReqRoomComboFromRooms();

//             // Event đổi slot → cập nhật label thởi gian ca
//             _cbReqSlotSingle.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();
//             _cbReqSlotFrom.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();
//             _cbReqSlotTo.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();

//             UpdateSlotTimeLabel();
//         }

//         private void UpdateSlotTimeLabel()
//         {
//             // Nếu chưa làm UI cho phần request thì mấy control này vẫn null,
//             // ta bỏ qua cho an toàn.
//             if (_lblSlotTimeRange == null ||
//                 _cbReqSlotSingle == null ||
//                 _cbReqSlotFrom == null ||
//                 _cbReqSlotTo == null)
//             {
//                 return;
//             }
//             string GetTime(string slotId)
//             {
//                 return _slotTimeLookup.TryGetValue(slotId, out var t) ? t : "?";
//             }

//             var single = _cbReqSlotSingle.SelectedItem?.ToString();
//             var from = _cbReqSlotFrom.SelectedItem?.ToString();
//             var to = _cbReqSlotTo.SelectedItem?.ToString();

//             if (!string.IsNullOrEmpty(single))
//             {
//                 _lblSlotTimeRange.Text = $"Thời gian ca (single): {single} – {GetTime(single)}";
//             }

//             if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
//             {
//                 int sIdx = ParseSlotIndexSafe(from);
//                 int eIdx = ParseSlotIndexSafe(to);
//                 if (eIdx < sIdx)
//                 {
//                     _lblSlotTimeRange.Text += " | Range ca không hợp lệ (End < Start)";
//                 }
//                 else
//                 {
//                     var startTime = GetTime(from);
//                     var endTime = GetTime(to);
//                     _lblSlotTimeRange.Text += $" | Range ca: {from}–{to} ({startTime} → {endTime})";
//                 }
//             }
//         }

//         private int ParseSlotIndexSafe(string? slotId)
//         {
//             if (string.IsNullOrWhiteSpace(slotId)) return -1;
//             if (slotId.StartsWith("S") && int.TryParse(slotId.Substring(1), out int idx))
//                 return idx;
//             return -1;
//         }

//         // ================== 2.5. TAB "LỊCH CỦA TÔI" ==================
//         private void BuildScheduleTabUi()
//         {
//             _tabSchedule.Controls.Clear();
//             _tabSchedule.BackColor = Color.White;

//             if (_ttSchedule == null)
//             {
//                 _ttSchedule = new ToolTip
//                 {
//                     AutoPopDelay = 8000,
//                     InitialDelay = 250,
//                     ReshowDelay = 100,
//                     ShowAlways = true
//                 };
//             }

//             var root = new TableLayoutPanel
//             {
//                 Dock = DockStyle.Fill,
//                 BackColor = Color.White,
//                 ColumnCount = 2,
//                 RowCount = 2,
//                 Padding = new Padding(10)
//             };
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
//             root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
//             root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
//             _tabSchedule.Controls.Add(root);

//             var topBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//             root.Controls.Add(topBar, 0, 0);
//             root.SetColumnSpan(topBar, 2);

//             // ==== Thanh trên: Chọn tuần + Export + Back ====
//             var lblWeek = new Label
//             {
//                 Left = 0,
//                 Top = 18,
//                 Width = 90,
//                 Text = "Chọn tuần:"
//             };
//             topBar.Controls.Add(lblWeek);

//             // ComboBox chọn tuần (T2-CN)
//             var cbWeek = new ComboBox
//             {
//                 Left = 95,
//                 Top = 14,
//                 Width = 320,
//                 DropDownStyle = ComboBoxStyle.DropDownList
//             };
            
//             // Tạo danh sách tuần: 4 tuần trước, tuần hiện tại, 8 tuần sau
//             var now = DateTime.Now + _serverTimeOffset;
//             var currentMonday = GetMondayOfWeek(now.Date);
            
//             for (int i = -4; i <= 8; i++)
//             {
//                 var monday = currentMonday.AddDays(i * 7);
//                 var sunday = monday.AddDays(6);
//                 var label = $"Tuần {monday:dd/MM} - {sunday:dd/MM/yyyy}";
//                 cbWeek.Items.Add(new WeekItem { Monday = monday, Label = label });
                
//                 if (i == 0) // Tuần hiện tại
//                     cbWeek.SelectedIndex = cbWeek.Items.Count - 1;
//             }
            
//             cbWeek.DisplayMember = "Label";
//             topBar.Controls.Add(cbWeek);
            
//             // Lưu reference để dùng sau
//             _cbScheduleWeek = cbWeek;

//             _btnExportSchedule = new Button
//             {
//                 Text = "Xuất file (PDF/Excel)",
//                 Left = 430,
//                 Top = 14,
//                 Width = 150
//             };
//             topBar.Controls.Add(_btnExportSchedule);

//             // Nút quay lại Trang chủ
//             _btnBackHomeFromSchedule = new Button
//             {
//                 Text = "Về Trang chủ",
//                 Left = 590,
//                 Top = 14,
//                 Width = 120
//             };
//             _btnBackHomeFromSchedule.Click += (s, e) =>
//             {
//                 _tabMain.SelectedTab = _tabHome;
//             };
//             // topBar.Controls.Add(_btnBackHomeFromSchedule);

//             var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//             root.Controls.Add(leftPanel, 0, 1);

//             _calSchedule = new MonthCalendar
//             {
//                 MaxSelectionCount = 1
//             };
//             leftPanel.Controls.Add(_calSchedule);

//             var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//             root.Controls.Add(rightPanel, 1, 1);

//             // ==== Week View: 7 cột (T2–CN) x 14 dòng (ca1–14) ====
//             _gridWeekView = new DataGridView
//             {
//                 Dock = DockStyle.Fill,
//                 ReadOnly = true,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                 SelectionMode = DataGridViewSelectionMode.CellSelect,
//                 MultiSelect = false,
//                 RowHeadersVisible = true,   // hiển thị "Ca 1..14" ở row header
//                 ShowCellToolTips = false,
//                 BorderStyle = BorderStyle.None,
//                 CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
//                 GridColor = Color.FromArgb(235, 235, 235),
//                 BackgroundColor = Color.White,
//                 EnableHeadersVisualStyles = false
//             };
//             rightPanel.Controls.Add(_gridWeekView);

//             _gridWeekView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
//             _gridWeekView.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
//             _gridWeekView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
//             _gridWeekView.ColumnHeadersHeight = 48;
//             _gridWeekView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
//             _gridWeekView.DefaultCellStyle.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);
//             _gridWeekView.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
//             _gridWeekView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(229, 231, 235);
//             _gridWeekView.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
//             _gridWeekView.RowTemplate.Height = 54;
//             _gridWeekView.RowHeadersWidth = 110;
//             _gridWeekView.RowHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
//             _gridWeekView.RowHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
//             _gridWeekView.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
//             _gridWeekView.RowHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

//             // Cột T2..CN (7 cột đúng yêu cầu)
//             _gridWeekView.Columns.Clear();
//             _gridWeekView.Columns.Add("Mon", "T2");
//             _gridWeekView.Columns.Add("Tue", "T3");
//             _gridWeekView.Columns.Add("Wed", "T4");
//             _gridWeekView.Columns.Add("Thu", "T5");
//             _gridWeekView.Columns.Add("Fri", "T6");
//             _gridWeekView.Columns.Add("Sat", "T7");
//             _gridWeekView.Columns.Add("Sun", "CN");

//             // Tạo 14 dòng (Ca 1..14), hiển thị ở RowHeader
//             _gridWeekView.Rows.Clear();
//             for (int slot = 1; slot <= 14; slot++)
//             {
//                 int rowIndex = _gridWeekView.Rows.Add();
//                 _gridWeekView.Rows[rowIndex].HeaderCell.Value = $"Ca {slot}\n{GetTimeRangeForSlot(slot)}";
//             }

//             // ==== Gắn event ====
//             cbWeek.SelectedIndexChanged += (s, e) =>
//             {
//                 if (_cbScheduleWeek?.SelectedItem is WeekItem wi)
//                 {
//                     _calSchedule?.SetDate(wi.Monday);
//                 }
//                 ReloadScheduleForSelectedDate();
//             };

//             _calSchedule.DateChanged += (s, e) =>
//             {
//                 if (_cbScheduleWeek == null) return;
//                 var monday = GetMondayOfWeek(e.Start.Date);
//                 EnsureAndSelectWeek(monday);
//             };

//             _btnExportSchedule.Click += (s, e) =>
//             {
//                 ExportCurrentSchedule();
//             };

//             // Hiển thị Week View
//             _gridWeekView.Visible = true;

//             _gridWeekView.CellMouseEnter -= GridWeekView_CellMouseEnter;
//             _gridWeekView.CellMouseEnter += GridWeekView_CellMouseEnter;
//             _gridWeekView.CellMouseLeave -= GridWeekView_CellMouseLeave;
//             _gridWeekView.CellMouseLeave += GridWeekView_CellMouseLeave;

//             // KHÔNG load ngay khi build UI vì TCP chưa sẵn sàng
//             // Sẽ load khi user chọn tuần hoặc khi tab được activate
//         }

//         private void EnsureAndSelectWeek(DateTime monday)
//         {
//             if (_cbScheduleWeek == null) return;

//             for (int i = 0; i < _cbScheduleWeek.Items.Count; i++)
//             {
//                 if (_cbScheduleWeek.Items[i] is WeekItem wi && wi.Monday.Date == monday.Date)
//                 {
//                     _cbScheduleWeek.SelectedIndex = i;
//                     return;
//                 }
//             }

//             var sunday = monday.AddDays(6);
//             var label = $"Tuần {monday:dd/MM} - {sunday:dd/MM/yyyy}";
//             _cbScheduleWeek.Items.Add(new WeekItem { Monday = monday, Label = label });
//             _cbScheduleWeek.SelectedIndex = _cbScheduleWeek.Items.Count - 1;
//         }

//         private void GridWeekView_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
//         {
//             if (sender is not DataGridView grid) return;
//             if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

//             if (_scheduleTooltipRow == e.RowIndex && _scheduleTooltipCol == e.ColumnIndex)
//                 return;

//             _scheduleTooltipRow = e.RowIndex;
//             _scheduleTooltipCol = e.ColumnIndex;

//             var text = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText;
//             if (string.IsNullOrWhiteSpace(text))
//             {
//                 _ttSchedule.Hide(grid);
//                 return;
//             }

//             var rect = grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
//             _ttSchedule.Show(text, grid, rect.Left + 8, rect.Bottom + 8, 7000);
//         }

//         private void GridWeekView_CellMouseLeave(object? sender, DataGridViewCellEventArgs e)
//         {
//             if (sender is not DataGridView grid) return;
//             _scheduleTooltipRow = -1;
//             _scheduleTooltipCol = -1;
//             _ttSchedule.Hide(grid);
//         }
//         private void ReloadScheduleForSelectedDate()
//         {
//             // fire-and-forget gọi hàm async
//             _ = ReloadScheduleFromServerAsync();
//         }


//         // Tính thứ 2 của tuần chứa ngày d
//         private DateTime GetMondayOfWeek(DateTime d)
//         {
//             // DayOfWeek: Monday=1, Sunday=0
//             int diff = (int)d.DayOfWeek - (int)DayOfWeek.Monday;
//             if (diff < 0) diff += 7;
//             return d.Date.AddDays(-diff);
//         }

//         // Tạo time range cho ca 
//         private string GetTimeRangeForSlot(int slot)
//         {
//             var slotId = $"S{slot}";
//             if (_slotTimeLookup.TryGetValue(slotId, out var range))
//                 return range;

//             // fallback nếu vì lý do gì đó chưa có config
//             var start = new TimeSpan(7 + (slot - 1), 0, 0);
//             var end = start.Add(TimeSpan.FromHours(1));
//             return $"{start:hh\\:mm}–{end:hh\\:mm}";
//         }

//         // Load lại dữ liệu khi chọn ngày
//         private async Task ReloadScheduleFromServerAsync()
//         {
//             // Prevent concurrent reloads
//             if (_scheduleReloadInFlight)
//             {
//                 AppendClientLog("[SCHEDULE] Reload already in progress, skipping...");
//                 return;
//             }

//             try
//             {
//                 _scheduleReloadInFlight = true;
                
//                 if (_cbScheduleWeek == null || _cbScheduleWeek.SelectedItem == null)
//                 {
//                     AppendClientLog("[SCHEDULE] ComboBox chưa sẵn sàng");
//                     return;
//                 }

//                 var weekItem = (WeekItem)_cbScheduleWeek.SelectedItem;
//                 var monday = weekItem.Monday;
//                 var sunday = monday.AddDays(6);
                
//                 AppendClientLog($"[SCHEDULE] Loading từ {monday:yyyy-MM-dd} đến {sunday:yyyy-MM-dd}");
//                 var weekItems = await LoadMyScheduleFromServerAsync(monday, sunday);
//                 AppendClientLog($"[SCHEDULE] Nhận được {weekItems.Count} items từ server");
//                 FillWeekView(monday, weekItems);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show(
//                     "Không lấy được lịch từ server: " + ex.Message,
//                     "Schedule",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Error);

//                 _gridWeekView.Rows.Clear();

//                 for (int slot = 1; slot <= 14; slot++)
//                 {
//                     int rowIndex = _gridWeekView.Rows.Add();
//                     _gridWeekView.Rows[rowIndex].HeaderCell.Value = "Ca " + slot;
//                 }
//             }
//             finally
//             {
//                 _scheduleReloadInFlight = false;
//             }
//         }



//         // Lấy thứ 2 của tuần chứa ngày d
//         // private DateTime GetMondayOfWeek(DateTime d)
//         // {
//         //     int diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
//         //     return d.Date.AddDays(-diff);
//         // }

//         // Lấy lịch từ _myBookings thay vì gọi server (tránh conflict stream)
//         private async Task<List<MyScheduleItem>> LoadMyScheduleFromServerAsync(DateTime fromDate, DateTime toDate)
//         {
//             var result = new List<MyScheduleItem>();

//             try
//             {
//                 await Task.Delay(100); // Đợi một chút để UI update

//                 AppendClientLog($"[SCHEDULE] Loading from _myBookings (count={_myBookings.Count}) and _myFixedSchedules (count={_myFixedSchedules.Count})");

//                 // 1. Load từ _myBookings
//                 foreach (var booking in _myBookings)
//                 {
//                     if (!DateTime.TryParse(booking.Date, out var bookingDate))
//                         continue;

//                     if (bookingDate < fromDate || bookingDate > toDate)
//                         continue;

//                     // Chỉ hiển thị APPROVED, IN_USE, COMPLETED, FIXED (bỏ QUEUED, CANCELLED, NO_SHOW)
//                     if (!string.Equals(booking.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
//                         !string.Equals(booking.Status, "IN_USE", StringComparison.OrdinalIgnoreCase) &&
//                         !string.Equals(booking.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) &&
//                         !string.Equals(booking.Status, "FIXED", StringComparison.OrdinalIgnoreCase))
//                     {
//                         continue;
//                     }

//                     // Fix: Hiển thị tất cả slots trong range booking
//                     int slotStart = ParseSlotIndexSafe(booking.SlotStartId);
//                     int slotEnd = ParseSlotIndexSafe(booking.SlotEndId);
                    
//                     if (slotStart <= 0) continue;
//                     if (slotEnd <= 0) slotEnd = slotStart; // Single slot nếu SlotEndId không hợp lệ

//                     // Thêm tất cả slots từ start đến end
//                     for (int slotIndex = slotStart; slotIndex <= slotEnd; slotIndex++)
//                     {
//                         result.Add(new MyScheduleItem
//                         {
//                             Date = bookingDate,
//                             Slot = slotIndex,
//                             RoomId = booking.RoomId,
//                             TimeRange = booking.TimeRange,
//                             Status = booking.Status,
//                             Subject = booking.Purpose,
//                             Note = ""
//                         });
//                     }

//                     AppendClientLog($"[SCHEDULE] Added from _myBookings: Date={bookingDate:yyyy-MM-dd}, Slots={slotStart}-{slotEnd}, Room={booking.RoomId}, Status={booking.Status}");
//                 }

//                 // 2. Load từ _myFixedSchedules
//                 foreach (var fixedSchedule in _myFixedSchedules)
//                 {
//                     // Parse DateFrom và DateTo
//                     if (!DateTime.TryParse(fixedSchedule.DateFrom, out var dateFrom))
//                         continue;
//                     if (!DateTime.TryParse(fixedSchedule.DateTo, out var dateTo))
//                         continue;

//                     // Parse DayOfWeek
//                     if (!Enum.TryParse<DayOfWeek>(fixedSchedule.DayOfWeek, out var dow))
//                         continue;

//                     // Parse slot range
//                     int slotStart = ParseSlotIndexSafe(fixedSchedule.SlotStartId);
//                     int slotEnd = ParseSlotIndexSafe(fixedSchedule.SlotEndId);
//                     if (slotStart <= 0) continue;
//                     if (slotEnd <= 0) slotEnd = slotStart;

//                     // Tìm tất cả các ngày trong khoảng fromDate-toDate có DayOfWeek khớp
//                     for (var date = fromDate; date <= toDate; date = date.AddDays(1))
//                     {
//                         if (date.DayOfWeek == dow && date >= dateFrom && date <= dateTo)
//                         {
//                             // Thêm tất cả slots trong range
//                             for (int slotIndex = slotStart; slotIndex <= slotEnd; slotIndex++)
//                             {
//                                 result.Add(new MyScheduleItem
//                                 {
//                                     Date = date,
//                                     Slot = slotIndex,
//                                     RoomId = fixedSchedule.RoomId,
//                                     TimeRange = "", // Fixed schedule không có TimeRange
//                                     Status = "FIXED",
//                                     Subject = $"{fixedSchedule.SubjectCode} - {fixedSchedule.SubjectName}",
//                                     Note = fixedSchedule.Note
//                                 });
//                             }

//                             AppendClientLog($"[SCHEDULE] Added from _myFixedSchedules: Date={date:yyyy-MM-dd}, Slots={slotStart}-{slotEnd}, Room={fixedSchedule.RoomId}, Subject={fixedSchedule.SubjectCode}");
//                         }
//                     }
//                 }

//                 AppendClientLog($"[SCHEDULE] Loaded {result.Count} items total.");
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] LoadMyScheduleFromServerAsync: " + ex.Message);
//             }

//             return result;
//         }


//         private void FillWeekView(DateTime selectedDate, List<MyScheduleItem> weekItems)
//         {
//             if (InvokeRequired)
//             {
//                 BeginInvoke(new Action(() => FillWeekView(selectedDate, weekItems)));
//                 return;
//             }

//             try
//             {
//                 var monday = GetMondayOfWeek(selectedDate);
//                 AppendClientLog($"[SCHEDULE] FillWeekView: monday={monday:yyyy-MM-dd}, items={weekItems.Count}");
                
//                 // Suspend layout để tránh lỗi auto-filled column resizing
//                 _gridWeekView.SuspendLayout();
//                 _gridWeekView.Rows.Clear();

//                 if (_gridWeekView.Columns.Count >= 7)
//                 {
//                     _gridWeekView.Columns[0].HeaderText = $"T2\n{monday:dd/MM}";
//                     _gridWeekView.Columns[1].HeaderText = $"T3\n{monday.AddDays(1):dd/MM}";
//                     _gridWeekView.Columns[2].HeaderText = $"T4\n{monday.AddDays(2):dd/MM}";
//                     _gridWeekView.Columns[3].HeaderText = $"T5\n{monday.AddDays(3):dd/MM}";
//                     _gridWeekView.Columns[4].HeaderText = $"T6\n{monday.AddDays(4):dd/MM}";
//                     _gridWeekView.Columns[5].HeaderText = $"T7\n{monday.AddDays(5):dd/MM}";
//                     _gridWeekView.Columns[6].HeaderText = $"CN\n{monday.AddDays(6):dd/MM}";
//                 }
                
//                 for (int slot = 1; slot <= 14; slot++)
//                 {
//                     int rowIndex = _gridWeekView.Rows.Add();
//                     _gridWeekView.Rows[rowIndex].HeaderCell.Value = $"Ca {slot}\n{GetTimeRangeForSlot(slot)}";
//                 }

//                 foreach (DataGridViewRow r in _gridWeekView.Rows)
//                 {
//                     foreach (DataGridViewCell c in r.Cells)
//                     {
//                         c.Value = null;
//                         c.ToolTipText = string.Empty;
//                         c.Style.BackColor = Color.White;
//                         c.Style.ForeColor = Color.FromArgb(17, 24, 39);
//                     }
//                 }
                
//                 foreach (var item in weekItems)
//                 {
//                     int dayOffset = (item.Date.Date - monday).Days;
//                     AppendClientLog($"[SCHEDULE] Item: Date={item.Date:yyyy-MM-dd}, Slot={item.Slot}, Room={item.RoomId}, dayOffset={dayOffset}");
                    
//                     if (dayOffset < 0 || dayOffset > 6)
//                     {
//                         AppendClientLog($"[SCHEDULE] Skip: dayOffset out of range");
//                         continue;
//                     }
                    
//                     // Kiểm tra columns count
//                     if (dayOffset >= _gridWeekView.Columns.Count)
//                     {
//                         AppendClientLog($"[SCHEDULE] Skip: dayOffset >= columns count ({dayOffset} >= {_gridWeekView.Columns.Count})");
//                         continue;
//                     }
                    
//                     int rowIndex = item.Slot - 1;
//                     if (rowIndex < 0 || rowIndex >= _gridWeekView.Rows.Count)
//                     {
//                         AppendClientLog($"[SCHEDULE] Skip: rowIndex out of range ({rowIndex})");
//                         continue;
//                     }
                    
//                     var cell = _gridWeekView.Rows[rowIndex].Cells[dayOffset];
//                     cell.Value = $"{item.RoomId}\n{item.Subject}";
//                     cell.ToolTipText =
//                         $"Phòng: {item.RoomId}\n" +
//                         $"Môn/Lý do: {item.Subject}\n" +
//                         $"Trạng thái: {item.Status}\n" +
//                         $"Ghi chú: {item.Note}";
                    
//                     var st = (item.Status ?? string.Empty).Trim().ToUpperInvariant();
//                     var badge = GetStatusBadgeColors(st);
//                     cell.Style.BackColor = badge.bg;
//                     cell.Style.ForeColor = Color.FromArgb(17, 24, 39);
//                     cell.Style.Alignment = DataGridViewContentAlignment.TopLeft;
//                     cell.Style.Padding = new Padding(6, 6, 6, 6);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog($"[ERROR] FillWeekView: {ex.Message}");
//             }
//             finally
//             {
//                 _gridWeekView.ResumeLayout();
//             }
//         }
//         // Export Week View
//         private void ExportCurrentSchedule()
//         {
//             if (_cbScheduleWeek == null || _cbScheduleWeek.SelectedItem == null)
//                 return;

//             var weekItem = (WeekItem)_cbScheduleWeek.SelectedItem;
//             var monday = weekItem.Monday;

//             using (var dlg = new SaveFileDialog())
//             {
//                 dlg.Title = "Export my schedule";
//                 dlg.Filter = "Excel CSV (*.csv)|*.csv";
//                 dlg.FileName = $"{_currentUser.UserId}_Schedule_Week_{monday:yyyyMMdd}";

//                 if (dlg.ShowDialog() != DialogResult.OK)
//                     return;

//                 var filePath = dlg.FileName;

//                 try
//                 {
//                     ExportWeekScheduleToFile(filePath);

//                     MessageBox.Show("Export lịch thành công.", "Export",
//                         MessageBoxButtons.OK, MessageBoxIcon.Information);
//                 }
//                 catch (Exception ex)
//                 {
//                     MessageBox.Show("Export lỗi: " + ex.Message, "Export",
//                         MessageBoxButtons.OK, MessageBoxIcon.Error);
//                 }
//             }
//         }

//         // Export Week View (chỉ CSV để Excel mở dạng bảng T2..CN x Ca)
//         private void ExportWeekScheduleToFile(string filePath)
//         {
//             if (InvokeRequired)
//             {
//                 Invoke(new Action(() => ExportWeekScheduleToFile(filePath)));
//                 return;
//             }

//             using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
//             {
//                 // Header: Ca,T2,T3,...,CN
//                 writer.WriteLine("Ca,T2,T3,T4,T5,T6,T7,CN");

//                 for (int rowIdx = 0; rowIdx < _gridWeekView.Rows.Count; rowIdx++)
//                 {
//                     var row = _gridWeekView.Rows[rowIdx];
//                     if (row.IsNewRow) continue;

//                     var ca = row.HeaderCell.Value?.ToString() ?? "";

//                     var values = new List<string> { ca };

//                     for (int col = 0; col < _gridWeekView.Columns.Count; col++)
//                     {
//                         var cell = row.Cells[col];
//                         var room = cell.Value?.ToString() ?? "";
//                         values.Add(room);
//                     }

//                     writer.WriteLine(string.Join(",", values));
//                 }
//             }
//         }



//         // ================== 2.6. TAB THÔNG BÁO ==================
//         private void BuildNotificationsTabUi()
//         {
//             var lblType = new Label { Left = 10, Top = 15, Width = 60, Text = "Loại:" };
//             _cbFilterType = new ComboBox
//             {
//                 Left = 70,
//                 Top = 12,
//                 Width = 150,
//                 DropDownStyle = ComboBoxStyle.DropDownList
//             };
            
//             _cbFilterType.Items.AddRange(new object[]
//             {
//                 "Tất cả", "Grant", "ChangeRoom", "NoShow", "Reminder"
//             });
//             _cbFilterType.SelectedIndex = 0;

//             var lblFrom = new Label { Left = 240, Top = 15, Width = 40, Text = "Từ:" };
//             _dtNotiFrom = new DateTimePicker
//             {
//                 Left = 280,
//                 Top = 12,
//                 Width = 120,
//                 Format = DateTimePickerFormat.Custom,
//                 CustomFormat = "dd/MM/yyyy"
//             };
//             var lblTo = new Label { Left = 410, Top = 15, Width = 30, Text = "Đến:" };
//             _dtNotiTo = new DateTimePicker
//             {
//                 Left = 440,
//                 Top = 12,
//                 Width = 120,
//                 Format = DateTimePickerFormat.Custom,
//                 CustomFormat = "dd/MM/yyyy"
//             };

//             _btnMarkAllRead = new Button
//             {
//                 Text = "Đánh dấu đã đọc tất cả",
//                 Left = 580,
//                 Top = 10,
//                 Width = 180
//             };

//             _tabNotifications.Controls.AddRange(new Control[]
//             {
//                 lblType, _cbFilterType,
//                 lblFrom, _dtNotiFrom,
//                 lblTo, _dtNotiTo,
//                 _btnMarkAllRead
//             });

//             _gridNotifications = new DataGridView
//             {
//                 Left = 10,
//                 Top = 45,
//                 Width = 950,
//                 Height = 500,
//                 ReadOnly = true,
//                 AllowUserToAddRows = false,
//                 AllowUserToDeleteRows = false,
//                 AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
//             };
//             _tabNotifications.Controls.Add(_gridNotifications);
//         }

//         // ================== 2.7. TAB TÀI KHOẢN ==================
//         private void BuildAccountTabUi()
//         {
//             _tabAccount.Controls.Clear();
//             _tabAccount.BackColor = Color.White;

//             var root = new TableLayoutPanel
//             {
//                 Dock = DockStyle.Fill,
//                 BackColor = Color.White,
//                 ColumnCount = 2,
//                 RowCount = 1,
//                 Padding = new Padding(12)
//             };
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340f));
//             root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
//             _tabAccount.Controls.Add(root);

//             var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//             var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//             root.Controls.Add(left, 0, 0);
//             root.Controls.Add(right, 1, 0);

//             var profileCard = new Panel
//             {
//                 Dock = DockStyle.Top,
//                 Height = 320,
//                 BackColor = Color.White,
//                 Padding = new Padding(14),
//                 BorderStyle = BorderStyle.FixedSingle
//             };
//             left.Controls.Add(profileCard);

//             var avatar = new Panel
//             {
//                 Left = 14,
//                 Top = 14,
//                 Width = 72,
//                 Height = 72,
//                 BackColor = Color.FromArgb(37, 99, 235)
//             };
//             profileCard.Controls.Add(avatar);

//             var lblAvatar = new Label
//             {
//                 Dock = DockStyle.Fill,
//                 TextAlign = ContentAlignment.MiddleCenter,
//                 Font = new Font("Segoe UI", 18f, FontStyle.Bold),
//                 ForeColor = Color.White
//             };
//             avatar.Controls.Add(lblAvatar);

//             var lblNameTitle = new Label
//             {
//                 Left = avatar.Right + 12,
//                 Top = 18,
//                 Width = 220,
//                 Height = 28,
//                 Font = new Font("Segoe UI", 12f, FontStyle.Bold),
//                 ForeColor = Color.FromArgb(17, 24, 39)
//             };
//             profileCard.Controls.Add(lblNameTitle);

//             var lblSub = new Label
//             {
//                 Left = avatar.Right + 12,
//                 Top = 46,
//                 Width = 220,
//                 Height = 20,
//                 Font = new Font("Segoe UI", 9f, FontStyle.Regular),
//                 ForeColor = Color.FromArgb(107, 114, 128)
//             };
//             profileCard.Controls.Add(lblSub);

//             var y = 102;
//             int labelW = 110;
//             int fieldW = 180;

//             var lblId = new Label { Left = 14, Top = y, Width = labelW, Text = "MSSV/Mã GV:" };
//             _txtAccStudentLecturerId = new TextBox { Left = 14 + labelW, Top = y - 3, Width = fieldW, ReadOnly = true };
//             profileCard.Controls.Add(lblId);
//             profileCard.Controls.Add(_txtAccStudentLecturerId);

//             y += 34;
//             var lblClassFac = new Label { Left = 14, Top = y, Width = labelW, Text = "Lớp / Khoa:" };
//             _txtAccClassFaculty = new TextBox { Left = 14 + labelW, Top = y - 3, Width = fieldW, ReadOnly = true };
//             profileCard.Controls.Add(lblClassFac);
//             profileCard.Controls.Add(_txtAccClassFaculty);

//             y += 34;
//             var lblDept = new Label { Left = 14, Top = y, Width = labelW, Text = "Bộ môn / Khoa:" };
//             _txtAccDepartment = new TextBox { Left = 14 + labelW, Top = y - 3, Width = fieldW, ReadOnly = true };
//             profileCard.Controls.Add(lblDept);
//             profileCard.Controls.Add(_txtAccDepartment);

//             y += 44;
//             _btnAccCheckConnect = new Button
//             {
//                 Text = "Check connect",
//                 Left = 14,
//                 Top = y,
//                 Width = 120,
//                 Height = 30
//             };
//             profileCard.Controls.Add(_btnAccCheckConnect);

//             _pnlAccConnectDot = new Panel
//             {
//                 Left = _btnAccCheckConnect.Right + 10,
//                 Top = y + 8,
//                 Width = 14,
//                 Height = 14,
//                 BackColor = Color.Red,
//                 BorderStyle = BorderStyle.FixedSingle
//             };
//             profileCard.Controls.Add(_pnlAccConnectDot);

//             _lblAccConnectText = new Label
//             {
//                 Left = _pnlAccConnectDot.Right + 6,
//                 Top = y + 6,
//                 Width = 120,
//                 Height = 18,
//                 Text = "Lost",
//                 ForeColor = Color.Red
//             };
//             profileCard.Controls.Add(_lblAccConnectText);

//             var grpContact = new GroupBox
//             {
//                 Text = "Liên hệ",
//                 Dock = DockStyle.Top,
//                 Height = 170,
//                 Padding = new Padding(12)
//             };
//             right.Controls.Add(grpContact);

//             var lblEmail = new Label { Left = 14, Top = 34, Width = 90, Text = "Email:" };
//             _txtAccEmail = new TextBox { Left = 110, Top = 30, Width = 360 };
//             var lblPhone = new Label { Left = 14, Top = 70, Width = 90, Text = "Phone:" };
//             _txtAccPhone = new TextBox { Left = 110, Top = 66, Width = 360 };
//             _btnUpdateContact = new Button { Text = "Lưu liên hệ", Left = 110, Top = 106, Width = 120, Height = 30 };
//             grpContact.Controls.Add(lblEmail);
//             grpContact.Controls.Add(_txtAccEmail);
//             grpContact.Controls.Add(lblPhone);
//             grpContact.Controls.Add(_txtAccPhone);
//             grpContact.Controls.Add(_btnUpdateContact);

//             var grpPwd = new GroupBox
//             {
//                 Text = "Đổi mật khẩu",
//                 Dock = DockStyle.Top,
//                 Height = 220,
//                 Padding = new Padding(12)
//             };
//             right.Controls.Add(grpPwd);

//             var lblOldPwd = new Label { Left = 14, Top = 34, Width = 90, Text = "Mật khẩu cũ:" };
//             _txtOldPassword = new TextBox { Left = 110, Top = 30, Width = 360, UseSystemPasswordChar = true };
//             var lblNewPwd = new Label { Left = 14, Top = 70, Width = 90, Text = "Mật khẩu mới:" };
//             _txtNewPassword = new TextBox { Left = 110, Top = 66, Width = 360, UseSystemPasswordChar = true };
//             var lblConfirmPwd = new Label { Left = 14, Top = 106, Width = 90, Text = "Nhập lại:" };
//             _txtConfirmPassword = new TextBox { Left = 110, Top = 102, Width = 360, UseSystemPasswordChar = true };
//             _btnChangePassword = new Button { Text = "Đổi mật khẩu", Left = 110, Top = 146, Width = 120, Height = 30 };
//             grpPwd.Controls.Add(lblOldPwd);
//             grpPwd.Controls.Add(_txtOldPassword);
//             grpPwd.Controls.Add(lblNewPwd);
//             grpPwd.Controls.Add(_txtNewPassword);
//             grpPwd.Controls.Add(lblConfirmPwd);
//             grpPwd.Controls.Add(_txtConfirmPassword);
//             grpPwd.Controls.Add(_btnChangePassword);

//             _txtAccFullName = new TextBox { Visible = false, ReadOnly = true };
//             _tabAccount.Controls.Add(_txtAccFullName);

//             // ===== Fill info từ _currentUser =====
//             _txtAccFullName.Text = _currentUser.FullName;
//             lblAvatar.Text = GetInitials(_currentUser.FullName);
//             lblNameTitle.Text = _currentUser.FullName;
//             lblSub.Text = _currentUser.UserType;
//             _txtAccEmail.Text = _currentUser.Email ?? "";
//             _txtAccPhone.Text = _currentUser.Phone ?? "";

//             if (_currentUser.UserType == "Student")
//             {
//                 _txtAccStudentLecturerId.Text = _currentUser.StudentId;
//                 _txtAccClassFaculty.Text = _currentUser.Class;
//                 _txtAccDepartment.Text = _currentUser.Department;
//             }
//             else if (_currentUser.UserType == "Lecturer")
//             {
//                 _txtAccStudentLecturerId.Text = _currentUser.LecturerId;
//                 _txtAccClassFaculty.Text = _currentUser.Faculty;
//                 _txtAccDepartment.Text = "";
//             }
//             else
//             {
//                 _txtAccStudentLecturerId.Text = _currentUser.UserId;
//                 _txtAccClassFaculty.Text = _currentUser.Department;
//                 _txtAccDepartment.Text = "";
//             }

//             // ===== Gắn event handler =====
//             _btnAccCheckConnect.Click += async (s, e) =>
//             {
//                 await AccountCheckConnectAsync();
//             };

//             _btnUpdateContact.Click += async (s, e) =>
//             {
//                 await UpdateContactAsync();
//             };

//             _btnChangePassword.Click += async (s, e) =>
//             {
//                 await ChangePasswordAsync();
//             };
//         }

//         private string GetInitials(string? name)
//         {
//             if (string.IsNullOrWhiteSpace(name)) return "?";
//             var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//             if (parts.Length == 0) return "?";
//             if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
//             var a = parts[0].Substring(0, 1);
//             var b = parts[parts.Length - 1].Substring(0, 1);
//             return (a + b).ToUpperInvariant();
//         }

//         private void RefreshHeaderSubInfo()
//         {
//             if (_lblSubInfo == null) return;

//             string subText;
//             if (_currentUser.UserType == "Student")
//             {
//                 subText =
//                     $"StudentId: {_currentUser.StudentId} - Class: {_currentUser.Class} - Department: {_currentUser.Department}";
//             }
//             else if (_currentUser.UserType == "Lecturer")
//             {
//                 subText =
//                     $"LecturerId: {_currentUser.LecturerId} - Faculty: {_currentUser.Faculty}";
//             }
//             else
//             {
//                 subText = $"Staff - Department: {_currentUser.Department}";
//             }

//             var contactParts = new List<string>();
//             if (!string.IsNullOrWhiteSpace(_currentUser.Email))
//                 contactParts.Add($"Email: {_currentUser.Email}");
//             if (!string.IsNullOrWhiteSpace(_currentUser.Phone))
//                 contactParts.Add($"Phone: {_currentUser.Phone}");

//             if (contactParts.Count > 0)
//             {
//                 subText += " | " + string.Join(" - ", contactParts);
//             }

//             _lblSubInfo.Text = subText;
//         }
//         private async Task UpdateContactAsync()
//         {
//             var email = _txtAccEmail.Text.Trim();
//             var phone = _txtAccPhone.Text.Trim();

//             if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
//             {
//                 MessageBox.Show(
//                     "Email hoặc Phone phải có ít nhất một giá trị.",
//                     "Account",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Warning
//                 );
//                 return;
//             }

//             try
//             {
//                 if (_writer == null)
//                 {
//                     MessageBox.Show("TCP chưa sẵn sàng.", "Account",
//                         MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // ⭐ Gửi lên server — KHÔNG chờ phản hồi!
//                 string cmd = $"UPDATE_CONTACT|{_currentUser.UserId}|{email}|{phone}";
//                 await _writer.WriteLineAsync(cmd);

//                 // ❌ KHÔNG được MessageBox "Đang gửi..." 
//                 // Vì phản hồi có thể đến ngay lập tức và chặn UI khiến lỗi race condition.
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show("Lỗi khi cập nhật:\n" + ex.Message,
//                     "Account", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }

//         private async Task ChangePasswordAsync()
//         {
//             var oldPwd = _txtOldPassword.Text.Trim();
//             var newPwd = _txtNewPassword.Text.Trim();
//             var confirm = _txtConfirmPassword.Text.Trim();

//             // ==== Validate trước ====
//             if (string.IsNullOrWhiteSpace(oldPwd) ||
//                 string.IsNullOrWhiteSpace(newPwd) ||
//                 string.IsNullOrWhiteSpace(confirm))
//             {
//                 MessageBox.Show(
//                     "Vui lòng nhập đủ mật khẩu cũ, mật khẩu mới và xác nhận.",
//                     "Đổi mật khẩu",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Warning
//                 );
//                 return;
//             }

//             if (newPwd != confirm)
//             {
//                 MessageBox.Show(
//                     "Mật khẩu mới và xác nhận không khớp.",
//                     "Đổi mật khẩu",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Warning
//                 );
//                 return;
//             }

//             if (newPwd.Length < 6)
//             {
//                 MessageBox.Show(
//                     "Mật khẩu mới phải có ít nhất 6 ký tự.",
//                     "Đổi mật khẩu",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Warning
//                 );
//                 return;
//             }

//             try
//             {
//                 // ⭐ KHÔNG mở TCP mới — dùng persistent TCP
//                 if (_writer == null || _reader == null)
//                 {
//                     MessageBox.Show("Chưa sẵn sàng kết nối server.", "Đổi mật khẩu",
//                         MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // ⭐ Gửi lệnh
//                 string cmd = $"CHANGE_PASSWORD|{_currentUser.UserId}|{oldPwd}|{newPwd}";
//                 await _writer.WriteLineAsync(cmd);

//                 // ⭐ Đọc trả lời
//                 string? resp = await _reader.ReadLineAsync();
//                 if (resp == null)
//                 {
//                     MessageBox.Show("Không nhận được phản hồi từ server.", "Đổi mật khẩu",
//                         MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 if (resp == "OK")
//                 {
//                     MessageBox.Show(
//                         "Đổi mật khẩu thành công.",
//                         "Đổi mật khẩu",
//                         MessageBoxButtons.OK,
//                         MessageBoxIcon.Information
//                     );

//                     // Clear UI fields
//                     _txtOldPassword.Text = "";
//                     _txtNewPassword.Text = "";
//                     _txtConfirmPassword.Text = "";
//                     return;
//                 }

//                 if (resp.StartsWith("ERR|"))
//                 {
//                     MessageBox.Show(resp.Substring(4), "Đổi mật khẩu",
//                         MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                     return;
//                 }

//                 // ❌ Bắt trường hợp server trả về không đúng format
//                 MessageBox.Show(
//                     "Phản hồi không hợp lệ từ server: " + resp,
//                     "Đổi mật khẩu",
//                     MessageBoxButtons.OK,
//                     MessageBoxIcon.Error
//                 );
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show("Lỗi khi đổi mật khẩu:\n" + ex.Message,
//                     "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }

//         private async Task AccountCheckConnectAsync()
//         {
//             var ok = await HeaderCheckConnectAsync();

//             if (ok)
//             {
//                 _pnlAccConnectDot.BackColor = Color.LimeGreen;
//                 _lblAccConnectText.Text = "Connected";
//                 _lblAccConnectText.ForeColor = Color.Green;
//             }
//             else
//             {
//                 _pnlAccConnectDot.BackColor = Color.Red;
//                 _lblAccConnectText.Text = "Lost";
//                 _lblAccConnectText.ForeColor = Color.Red;
//             }
//         }

//         /// <summary>
//         /// Double click 1 dòng → prefill group “Yêu cầu mượn phòng”
//         /// </summary>
//         private void PrefillRequestFromSelectedRoom()
//         {
//             if (_gridRooms.CurrentRow == null)
//                 return;

//             var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//             if (string.IsNullOrEmpty(roomId))
//                 return;

//             if (IsRoomDisabled(roomId))
//                 return;

//             if (_cbReqRoom == null)
//                 return;

//             // Chọn phòng tương ứng
//             if (_cbReqRoom.Items.Contains(roomId))
//                 _cbReqRoom.SelectedItem = roomId;

//             // Đồng bộ ngày và ca với phần filter trên
//             _dtReqDate.Value = _dtBookingDate.Value;

//             if (_cbBookingFromSlot.SelectedItem != null)
//                 _cbReqSlotFrom.SelectedItem = _cbBookingFromSlot.SelectedItem;

//             if (_cbBookingToSlot.SelectedItem != null)
//                 _cbReqSlotTo.SelectedItem = _cbBookingToSlot.SelectedItem;

//             // Single slot default là from
//             if (_cbReqSlotFrom.SelectedItem != null)
//                 _cbReqSlotSingle.SelectedItem = _cbReqSlotFrom.SelectedItem;

//             UpdateSlotTimeLabel();
//             AppendClientLog($"Prefill request from room {roomId}.");
//         }
//         private void ForceReloadAllUI()
//         {
//             try
//             {
//                 AppendClientLog("[INFO] Force reload all UI after fixed schedule update");
                
//                 // Reload tất cả components ngay lập tức
//                 _ = Task.Run(async () =>
//                 {
//                     try
//                     {
//                         // 1. Reload home
//                         await LoadHomeFromServerAsync();
                        
//                         // 2. Reload schedule nếu đang ở tab schedule
//                         if (_tabMain?.SelectedTab == _tabSchedule)
//                         {
//                             await ReloadScheduleFromServerAsync();
//                         }
                        
//                         // 3. Reload room slots nếu đang ở tab booking và đã chọn room
//                         if (_tabMain?.SelectedTab == _tabBooking && _gridRooms?.CurrentRow != null)
//                         {
//                             // Force reload bằng cách reset flag
//                             _roomSlotsRequestInFlight = false;
//                             await ReloadSlotsForSelectedRoomAsync();
//                         }
                        
//                         AppendClientLog("[INFO] Force reload completed");
//                     }
//                     catch (Exception ex)
//                     {
//                         AppendClientLog($"[ERROR] ForceReloadAllUI: {ex.Message}");
//                     }
//                 });
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog($"[ERROR] ForceReloadAllUI outer: {ex.Message}");
//             }
//         }

//         private void AppendClientLog(string message)
//         {
//             if (_txtClientLog == null) return;
//             if (_txtClientLog.InvokeRequired)
//             {
//                 _txtClientLog.Invoke(new Action(() => {
//                     _txtClientLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
//                 }));
//             }
//             else
//             {
//                 _txtClientLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
//             }
//         }

//         private void ApplySlotConfig()
//         {
//             // Clear lookup
//             _slotTimeLookup.Clear();

//             var listForHeader = new List<(int index, string id, string start, string end)>();

//             foreach (var item in _slotConfigTemp)
//             {
//                 string slotId = item.slotId;
//                 string start = item.start;
//                 string end = item.end;

//                 _slotTimeLookup[slotId] = $"{start}–{end}";

//                 int idx = ParseSlotIndexSafe(slotId);
//                 if (idx > 0)
//                     listForHeader.Add((idx, slotId, start, end));
//             }

//             // Cập nhật header UI
//             if (listForHeader.Count > 0 && _lblSlotConfig != null)
//             {
//                 var ordered = listForHeader.OrderBy(x => x.index).ToList();
//                 var partsText = ordered.Select(x => $"Ca {x.index}: {x.start}–{x.end}");
//                 _lblSlotConfig.Text = "Slot config: " + string.Join(" | ", partsText);
//             }

//             UpdateSlotTimeLabel();
//         }

//         private async void RequestSingleSlot()
//         {
//             var roomId = _cbReqRoom.SelectedItem?.ToString();
//             var slotId = _cbReqSlotSingle.SelectedItem?.ToString();
//             var purpose = _txtPurpose.Text.Trim();

//             if (string.IsNullOrWhiteSpace(roomId) ||
//                 string.IsNullOrWhiteSpace(slotId))
//             {
//                 MessageBox.Show("Vui lòng chọn phòng và ca.");
//                 return;
//             }

//             if (string.IsNullOrWhiteSpace(purpose))
//             {
//                 MessageBox.Show("Vui lòng nhập lý do mượn phòng.");
//                 return;
//             }

//             try
//             {
//                 if (_writer == null)
//                 {
//                     MessageBox.Show("Chưa kết nối server.");
//                     return;
//                 }

//                 var date = _dtReqDate.Value.Date;
//                 string msg = $"REQUEST|{_currentUser.UserId}|{roomId}|{slotId}|{date:yyyy-MM-dd}|{purpose}";
//                 AppendClientLog("[SEND] " + msg);

//                 await _writer.WriteLineAsync(msg);

//                 // ❗ KHÔNG đọc reader ở đây — response sẽ được HandleServerMessage xử lý
//                 _lblRequestStatus.ForeColor = Color.DarkBlue;
//                 _lblRequestStatus.Text = "Đang gửi request... chờ server phản hồi";

//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] RequestSingleSlot: " + ex.Message);
//                 MessageBox.Show("Request failed: " + ex.Message);
//             }
//         }

//         private async void RequestRangeSlot()
//         {
//             var roomId = _cbReqRoom.SelectedItem?.ToString();
//             var date = _dtReqDate.Value.Date;
//             var slotFrom = _cbReqSlotFrom.SelectedItem?.ToString();
//             var slotTo = _cbReqSlotTo.SelectedItem?.ToString();
//             var purpose = _txtPurpose.Text.Trim();

//             if (string.IsNullOrWhiteSpace(roomId) ||
//                 string.IsNullOrWhiteSpace(slotFrom) ||
//                 string.IsNullOrWhiteSpace(slotTo))
//             {
//                 MessageBox.Show("Vui lòng chọn phòng và range ca.", "Request range",
//                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             int sIdx = ParseSlotIndexSafe(slotFrom);
//             int eIdx = ParseSlotIndexSafe(slotTo);
//             if (eIdx < sIdx)
//             {
//                 MessageBox.Show("Slot kết thúc phải lớn hơn hoặc bằng slot bắt đầu.",
//                     "Request range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             if (string.IsNullOrWhiteSpace(purpose))
//             {
//                 MessageBox.Show("Vui lòng nhập lý do mượn phòng.", "Request range",
//                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             try
//             {
//                 // ⭐ Dùng persistent TCP – KHÔNG mở kết nối mới
//                 if (_writer == null || _reader == null)
//                 {
//                     MessageBox.Show("Chưa kết nối server.", "Request range",
//                         MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // ⭐ Gửi request
//                 string msg = $"REQUEST_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}|{date:yyyy-MM-dd}|{purpose}";
//                 AppendClientLog("[SEND] " + msg);
//                 await _writer.WriteLineAsync(msg);
//                 _lblRequestStatus.ForeColor = Color.Blue;
//                 _lblRequestStatus.Text = "Đang gửi yêu cầu range...";
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] RequestRangeSlot: " + ex.Message);
//                 MessageBox.Show("Request range failed: " + ex.Message,
//                     "Request range", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }

//         private void RenderHomeData()
//         {
//             // Clear UI
//             _gridTodaySchedule.Rows.Clear();
//             _lstLatestNotifications.Items.Clear();

//             // ---- Render Schedule ----
//             foreach (var line in _homeScheduleLines)
//             {
//                 var p = line.Split('|');
//                 if (p.Length >= 6)
//                 {
//                     int rowIdx = _gridTodaySchedule.Rows.Add(
//                         p[1], // time range
//                         p[2], // room
//                         p[3], // subject
//                         p[4], // teacher
//                         p[5], // status
//                         p.Length > 6 ? p[6] : ""
//                     );
//                     // Color code by status
//                     var status = p[5].Trim().ToUpperInvariant();
//                     var row = _gridTodaySchedule.Rows[rowIdx];
//                     if (status == "FIXED")
//                     {
//                         row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
//                         row.DefaultCellStyle.Font = new Font(_gridTodaySchedule.Font, FontStyle.Bold);
//                         row.DefaultCellStyle.ForeColor = Color.DarkOrange;
//                     }
//                     else if (status == "APPROVED")
//                     {
//                         row.DefaultCellStyle.BackColor = Color.LightGreen;
//                         row.DefaultCellStyle.Font = _gridTodaySchedule.Font;
//                         row.DefaultCellStyle.ForeColor = Color.DarkGreen;
//                     }
//                     else if (status == "IN_USE")
//                     {
//                         row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
//                         row.DefaultCellStyle.Font = _gridTodaySchedule.Font;
//                         row.DefaultCellStyle.ForeColor = Color.MidnightBlue;
//                     }
//                     else if (status == "COMPLETED")
//                     {
//                         row.DefaultCellStyle.BackColor = Color.Gainsboro;
//                         row.DefaultCellStyle.Font = _gridTodaySchedule.Font;
//                         row.DefaultCellStyle.ForeColor = Color.Gray;
//                     }
//                     else if (status == "NO_SHOW")
//                     {
//                         row.DefaultCellStyle.BackColor = Color.MistyRose;
//                         row.DefaultCellStyle.Font = _gridTodaySchedule.Font;
//                         row.DefaultCellStyle.ForeColor = Color.Red;
//                     }
//                     else
//                     {
//                         row.DefaultCellStyle.BackColor = Color.White;
//                         row.DefaultCellStyle.Font = _gridTodaySchedule.Font;
//                         row.DefaultCellStyle.ForeColor = Color.Black;
//                     }
//                 }
//             }

//             foreach (var n in _homeNotificationLines)
//             {
//                 if (!string.IsNullOrWhiteSpace(n))
//                     _lstLatestNotifications.Items.Add(n);
//             }
//             // Render thêm fixed schedule cho hôm nay (nếu chưa có trong bookings)
//             var today = DateTime.Now.Date;
//             foreach (var fs in _myFixedSchedules)
//             {
//                 if (DateTime.TryParse(fs.DateFrom, out var from) && DateTime.TryParse(fs.DateTo, out var to))
//                 {
//                     if (today < from || today > to) continue;
//                     var dow = today.DayOfWeek.ToString();
//                     if (!string.Equals(dow, fs.DayOfWeek, StringComparison.OrdinalIgnoreCase)) continue;
//                     // Kiểm tra đã có booking trùng slot/phòng chưa
//                     bool hasBooking = _homeScheduleLines.Any(l =>
//                         l.Contains(fs.RoomId) && l.Contains(fs.SlotStartId) && l.Contains(fs.SlotEndId));
//                     if (hasBooking) continue;
//                     int rowIdx = _gridTodaySchedule.Rows.Add(
//                         $"{fs.SlotStartId}-{fs.SlotEndId}",
//                         fs.RoomId,
//                         fs.SubjectName,
//                         fs.LecturerUserId,
//                         "FIXED",
//                         fs.Note
//                     );
//                     var row = _gridTodaySchedule.Rows[rowIdx];
//                     row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
//                     row.DefaultCellStyle.Font = new Font(_gridTodaySchedule.Font, FontStyle.Bold);
//                     row.DefaultCellStyle.ForeColor = Color.DarkOrange;
//                 }
//             }
//         }

//         private async void ReleaseSingleSlot()
//         {
//             var roomId = _cbReqRoom.SelectedItem?.ToString();
//             var slot = _cbReqSlotSingle.SelectedItem?.ToString();
//             var date = _dtReqDate.Value.Date;

//             if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(slot))
//             {
//                 MessageBox.Show("Vui lòng chọn phòng và ca.");
//                 return;
//             }

//             if (_writer == null)
//             {
//                 MessageBox.Show("Chưa kết nối server.");
//                 return;
//             }

//             string msg = $"RELEASE|{_currentUser.UserId}|{roomId}|{slot}|{date:yyyy-MM-dd}";
//             AppendClientLog("[SEND] " + msg);
//             await _writer.WriteLineAsync(msg);

//             // ⭐ Báo UI đang gửi
//             _lblRequestStatus.ForeColor = Color.Blue;
//             _lblRequestStatus.Text = "Đang gửi yêu cầu RELEASE...";
//         }

//         private async void ReleaseRangeSlot()
//         {
//             var roomId = _cbReqRoom.SelectedItem?.ToString();
//             var date = _dtReqDate.Value.Date;
//             var slotFrom = _cbReqSlotFrom.SelectedItem?.ToString();
//             var slotTo = _cbReqSlotTo.SelectedItem?.ToString();

//             if (string.IsNullOrWhiteSpace(roomId) ||
//                 string.IsNullOrWhiteSpace(slotFrom) ||
//                 string.IsNullOrWhiteSpace(slotTo))
//             {
//                 MessageBox.Show("Vui lòng chọn đầy đủ phòng và range ca.",
//                     "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             int sIdx = ParseSlotIndexSafe(slotFrom);
//             int eIdx = ParseSlotIndexSafe(slotTo);
//             if (eIdx < sIdx)
//             {
//                 MessageBox.Show("SlotEnd phải ≥ SlotStart.",
//                     "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             if (_currentUser == null)
//             {
//                 MessageBox.Show("Chưa có thông tin user.",
//                     "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             try
//             {
//                 // ⭐ Dùng persistent TCP
//                 if (_writer == null || _reader == null)
//                 {
//                     MessageBox.Show("Chưa kết nối server.",
//                         "Release range", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // ⭐ Gửi lệnh RELEASE_RANGE
//                 var msg = $"RELEASE_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}|{date:yyyy-MM-dd}";
//                 AppendClientLog("[SEND] " + msg);
//                 await _writer.WriteLineAsync(msg);

//                 // // ⭐ Nhận phản hồi
//                 // var resp = await _reader.ReadLineAsync();
//                 // if (resp == null)
//                 // {
//                 //     MessageBox.Show("Không nhận được phản hồi từ server.",
//                 //         "Release range", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                 //     return;
//                 // }

//                 // AppendClientLog("[RECV] " + resp);
//                 // var p = resp.Split('|');

//                 // // ===== RANGE RELEASED =====
//                 // if (p.Length >= 2 && p[0] == "INFO" && p[1] == "RANGE_RELEASED")
//                 // {
//                 //     _lblRequestStatus.ForeColor = Color.DarkGray;
//                 //     _lblRequestStatus.Text =
//                 //         $"Đã RELEASE RANGE: {roomId} – {slotFrom} → {slotTo} | {date:dd/MM/yyyy}.";

//                 //     return;
//                 // }

//                 // // ===== ERROR =====
//                 // if (p.Length >= 3 && p[0] == "INFO" && p[1] == "ERROR")
//                 // {
//                 //     _lblRequestStatus.ForeColor = Color.Red;
//                 //     _lblRequestStatus.Text = "Release RANGE lỗi: " + p[2];

//                 //     MessageBox.Show("Release RANGE lỗi: " + p[2],
//                 //         "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);

//                 //     return;
//                 // }

//                 // // ===== Unknown =====
//                 // _lblRequestStatus.ForeColor = Color.Black;
//                 // _lblRequestStatus.Text = "Response: " + resp;
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] ReleaseRangeSlot: " + ex.Message);
//                 MessageBox.Show("Release range failed: " + ex.Message,
//                     "Release range", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }

//         private async Task LoadSlotConfigFromServerAsync()
//         {
//             try
//             {
//                 if (_writer == null)
//                 {
//                     AppendClientLog("[ERROR] Writer null khi load slot config.");
//                     return;
//                 }

//                 await _writer.WriteLineAsync("GET_SLOT_CONFIG");
//                 AppendClientLog("[SEND] GET_SLOT_CONFIG");
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] LoadSlotConfigFromServerAsync: " + ex.Message);
//             }
//         }

//         private void HandleServerMessage(string line)



//         {
//             try
//             {
//                 AppendClientLog("[PUSH] " + line);
//                 //////////////////////////////////////
//                 if (line.StartsWith("PUSH_HOME_DATA_CHANGED|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 2 && _currentUser != null &&
//                         string.Equals(p[1], _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
//                     {
//                         _ = Task.Run(async () =>
//                         {
//                             try 
//                             { 
//                                 await LoadHomeFromServerAsync();
//                                 // Reload schedule tab nếu đang hiển thị
//                                 if (_tabMain != null && _tabSchedule != null && _tabMain.SelectedTab == _tabSchedule)
//                                 {
//                                     await ReloadScheduleFromServerAsync();
//                                 }
//                             }
//                             catch (Exception ex) { AppendClientLog("[ERROR] LoadHomeFromServerAsync: " + ex.Message); }
//                         });
//                     }
//                     return;
//                 }
//                 // ====== PUSH: MY FIXED SCHEDULE ======
//                 if (line.StartsWith("{\"type\":\"PUSH_MY_FIXED_SCHEDULE\""))
//                 {
//                     try
//                     {
//                         var obj = System.Text.Json.JsonDocument.Parse(line);
//                         var root = obj.RootElement;
//                         if (root.TryGetProperty("data", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
//                         {
//                             var list = new List<FixedScheduleRow>();
//                             foreach (var el in arr.EnumerateArray())
//                             {
//                                 var row = new FixedScheduleRow
//                                 {
//                                     SessionId = el.GetProperty("sessionId").GetGuid(),
//                                     SubjectCode = el.GetProperty("subjectCode").GetString() ?? "",
//                                     SubjectName = el.GetProperty("subjectName").GetString() ?? "",
//                                     Class = el.GetProperty("class").GetString() ?? "",
//                                     LecturerUserId = el.GetProperty("lecturerUserId").GetString() ?? "",
//                                     RoomId = el.GetProperty("roomId").GetString() ?? "",
//                                     DayOfWeek = el.GetProperty("dayOfWeek").GetString() ?? "",
//                                     SlotStartId = el.GetProperty("slotStartId").GetString() ?? "",
//                                     SlotEndId = el.GetProperty("slotEndId").GetString() ?? "",
//                                     DateFrom = el.GetProperty("dateFrom").GetString() ?? "",
//                                     DateTo = el.GetProperty("dateTo").GetString() ?? "",
//                                     Note = el.GetProperty("note").GetString() ?? ""
//                                 };
//                                 list.Add(row);
//                             }
//                             _myFixedSchedules = list;
//                             AppendClientLog($"[INFO] Đã nhận fixed schedule: {list.Count} mục");
                            
//                             // Force reload tất cả UI ngay lập tức - không dùng Task.Run để tránh delay
//                             if (InvokeRequired)
//                             {
//                                 Invoke(new Action(() => ForceReloadAllUI()));
//                             }
//                             else
//                             {
//                                 ForceReloadAllUI();
//                             }
//                         }
//                     }
//                     catch (Exception ex)
//                     {
//                         AppendClientLog("[ERROR] Parse PUSH_MY_FIXED_SCHEDULE: " + ex.Message);
//                     }
//                     return;
//                 }
//                 /////////////////////////////////////////////
//                 // ====== PUSH: BOOKING EVENT NOTIFICATIONS ======
//                 // Booking granted (event payload)
//                 if (line.StartsWith("GRANTED|") || line.StartsWith("GRANTED_FROM_QUEUE|") || line.StartsWith("GRANTED_RANGE|"))
//                 {
//                     // Format:
//                     // - GRANTED|BookingId|RoomId|SlotStartId|SlotEndId
//                     // - GRANTED_FROM_QUEUE|BookingId|RoomId|SlotStartId|SlotEndId
//                     // - GRANTED_RANGE|BookingId|RoomId|SlotStartId|SlotEndId
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         var kind = p[0];
//                         var roomId = p[2];
//                         var slotRange = p[3] == p[4] ? p[3] : $"{p[3]}-{p[4]}";

//                         string header = kind switch
//                         {
//                             "GRANTED_FROM_QUEUE" => "🎉 Booking của bạn đã được GRANT từ hàng chờ",
//                             "GRANTED_RANGE" => "🎉 RANGE booking của bạn đã được GRANT",
//                             _ => "🎉 Booking của bạn đã được GRANT"
//                         };

//                         if (_lstLatestNotifications != null && !_lstLatestNotifications.IsDisposed)
//                             _lstLatestNotifications.Items.Insert(0, $"{header}: {roomId} slots {slotRange}");

//                         _ = Task.Run(async () =>
//                         {
//                             try
//                             {
//                                 await LoadHomeFromServerAsync();
//                                 await ReloadMyBookingsAsync();
//                             }
//                             catch (Exception ex) { AppendClientLog($"[ERROR] Reload after {kind}: " + ex.Message); }
//                         });
//                     }
//                     return;
//                 }
//                 // Grant notification
//                 if (line.StartsWith("NOTIFY_GRANT|"))
//                 {
//                     // Format: NOTIFY_GRANT|RoomId|Slot|Date|Deadline
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         string msg = $"Phòng {p[1]} ca {p[2]} ngày {p[3]} đã được grant. Vui lòng check-in trước {p[4]}.";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }
//                 // Check-in notification
//                 if (line.StartsWith("NOTIFY_CHECKIN|"))
//                 {
//                     // Format: NOTIFY_CHECKIN|RoomId|Slot|Date|Time
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         string msg = $"Bạn đã check-in phòng {p[1]} ca {p[2]} ngày {p[3]} lúc {p[4]}.";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }
//                 // Complete notification
//                 if (line.StartsWith("NOTIFY_COMPLETE|"))
//                 {
//                     // Format: NOTIFY_COMPLETE|RoomId|Slot|Date|Time
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         string msg = $"Booking phòng {p[1]} ca {p[2]} ngày {p[3]} đã hoàn thành lúc {p[4]}.";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }
//                 // No-show notification
//                 if (line.StartsWith("NOTIFY_NO_SHOW|"))
//                 {
//                     // Format: NOTIFY_NO_SHOW|RoomId|SlotRange|Date|Deadline
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         string msg = $"⚠️ Bạn đã bị NO-SHOW: phòng {p[1]} ca {p[2]} ngày {p[3]} (quá deadline check-in {p[4]}).";
//                         _lstLatestNotifications.Items.Insert(0, msg);

//                         _ = Task.Run(async () =>
//                         {
//                             try
//                             {
//                                 await LoadHomeFromServerAsync();
//                                 await ReloadMyBookingsAsync();
//                             }
//                             catch (Exception ex) { AppendClientLog("[ERROR] Reload after NO_SHOW: " + ex.Message); }
//                         });
//                     }
//                     return;
//                 }
//                 // Fixed schedule notification
//                 if (line.StartsWith("NOTIFY_FIXED|"))
//                 {
//                     // Format: NOTIFY_FIXED|RoomId|Slot|Date|Subject
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         string msg = $"Fixed Schedule: {p[4]} in {p[1]}-{p[2]} on {p[3]}";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }

//                 if (line.StartsWith("NOTIFY_FIXED_CREATED|"))
//                 {
//                     // Format: NOTIFY_FIXED_CREATED|SubjectCode|SubjectName|RoomId|SlotStartId|SlotEndId|DateFrom|DateTo
//                     var p = line.Split('|');
//                     if (p.Length >= 8)
//                     {
//                         var slotRange = p[4] == p[5] ? p[4] : $"{p[4]}-{p[5]}";
//                         string msg = $"Bạn có lịch cố định mới: {p[0]} {p[1]} - {p[2]} | {p[3]} | {slotRange} | {p[6]} -> {p[7]}";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }

//                 if (line.StartsWith("NOTIFY_FIXED_DELETED|"))
//                 {
//                     // Format: NOTIFY_FIXED_DELETED|SubjectCode|SubjectName|RoomId|SlotStartId|SlotEndId|DateFrom|DateTo
//                     var p = line.Split('|');
//                     if (p.Length >= 8)
//                     {
//                         var slotRange = p[4] == p[5] ? p[4] : $"{p[4]}-{p[5]}";
//                         string msg = $"Lịch cố định đã bị xoá: {p[0]} {p[1]} - {p[2]} | {p[3]} | {slotRange} | {p[6]} -> {p[7]}";
//                         _lstLatestNotifications.Items.Insert(0, msg);
//                     }
//                     return;
//                 }
//                 // Force release notification
//                 if (line.StartsWith("FORCE_RELEASE|"))
//                 {
//                     // Format: FORCE_RELEASE|BookingId|RoomId|SlotStartId|SlotEndId
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         var slotRange = p[3] == p[4] ? p[3] : $"{p[3]}-{p[4]}";
//                         string msg = $"⚠️ Admin đã FORCE RELEASE booking của bạn: {p[2]} slots {slotRange}";
//                         _lstLatestNotifications.Items.Insert(0, msg);
                        
//                         // Reload home và bookings
//                         _ = Task.Run(async () =>
//                         {
//                             try
//                             {
//                                 await LoadHomeFromServerAsync();
//                                 await ReloadMyBookingsAsync();
//                             }
//                             catch (Exception ex) { AppendClientLog("[ERROR] Reload after FORCE_RELEASE: " + ex.Message); }
//                         });
//                     }
//                     return;
//                 }
//                 // Force grant notification
//                 if (line.StartsWith("FORCE_GRANT|"))
//                 {
//                     // Format: FORCE_GRANT|BookingId|RoomId|SlotStartId|SlotEndId
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         var slotRange = p[3] == p[4] ? p[3] : $"{p[3]}-{p[4]}";
//                         string msg = $"🎉 Admin đã FORCE GRANT cho bạn: {p[2]} slots {slotRange}";
//                         _lstLatestNotifications.Items.Insert(0, msg);
                        
//                         // Reload home và bookings
//                         _ = Task.Run(async () =>
//                         {
//                             try 
//                             { 
//                                 await LoadHomeFromServerAsync();
//                                 await ReloadMyBookingsAsync();
//                             }
//                             catch (Exception ex) { AppendClientLog("[ERROR] Reload after FORCE_GRANT: " + ex.Message); }
//                         });
//                     }
//                     return;
//                 }
//                 // ====== PUSH: MY_BOOKINGS changed ======
//                 if (line.StartsWith("PUSH_MY_BOOKINGS_CHANGED|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 2)
//                     {
//                         var userId = p[1];

//                         // chỉ reload nếu đúng user hiện tại
//                         if (string.Equals(userId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
//                         {
//                             AppendClientLog("[INFO] MY_BOOKINGS changed -> reload once");
//                             _ = Task.Run(async () =>
//                             {
//                                 try { await ReloadMyBookingsAsync(); }
//                                 catch (Exception ex) { AppendClientLog("[ERROR] ReloadMyBookingsAsync: " + ex.Message); }
//                             });
//                         }
//                     }
//                     return;
//                 }
//                 // ====== PUSH: MY_SCHEDULE changed ======
//                 if (line.StartsWith("PUSH_MY_SCHEDULE_CHANGED|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 2)
//                     {
//                         var userId = p[1];
//                         if (string.Equals(userId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
//                         {
//                             AppendClientLog("[INFO] MY_SCHEDULE changed -> reload bookings, fixed schedules and schedule tab");
//                             _ = Task.Run(async () =>
//                             {
//                                 try 
//                                 { 
//                                     // Reload bookings trước (để có virtual bookings từ fixed schedules)
//                                     await ReloadMyBookingsAsync();
//                                     // Reload fixed schedules
//                                     await LoadFixedSchedulesFromServerAsync();
//                                     // Sau đó reload schedule tab
//                                     await ReloadScheduleFromServerAsync(); 
//                                 }
//                                 catch (Exception ex) { AppendClientLog("[ERROR] ReloadScheduleFromServerAsync: " + ex.Message); }
//                             });
//                         }
//                     }
//                     return;
//                 }
//                 /////////////////////////////////////
//                 if (line == "PUSH_ROOMS_CHANGED")
//                 {
//                     _ = Task.Run(async () =>
//                     {
//                         try { await LoadRoomsFromServerAsync(); }
//                         catch (Exception ex) { AppendClientLog("[ERROR] LoadRoomsFromServerAsync: " + ex.Message); }
//                     }); return;
//                 }
//                 ////////////////////////////////////////////
//                 if (line == "PUSH_SLOT_CONFIG_CHANGED")
//                 {
//                     _ = Task.Run(async () =>
//                     {
//                         try { await LoadSlotConfigFromServerAsync(); }
//                         catch (Exception ex) { AppendClientLog("[ERROR] LoadSlotConfigFromServerAsync: " + ex.Message); }
//                     });
//                     return;
//                 }

//                 // ====== 1. HOME_DATA BEGIN ======
//                 if (line == "HOME_DATA_BEGIN")
//                 {
//                     _isReadingHome = true;
//                     _homeScheduleLines.Clear();
//                     _homeNotificationLines.Clear();
//                     return;
//                 }

//                 // ====== 2. Đang đọc HOME_DATA block ======
//                 if (_isReadingHome)
//                 {
//                     if (line == "HOME_DATA_END")
//                     {
//                         _isReadingHome = false;
//                         RenderHomeData();
//                         return;
//                     }

//                     if (line.StartsWith("SCHEDULE|"))
//                     {
//                         _homeScheduleLines.Add(line);
//                         return;
//                     }

//                     if (line.StartsWith("NOTI|"))
//                     {
//                         _homeNotificationLines.Add(line.Substring(5));
//                         return;
//                     }

//                     return;
//                 }

//                 // ====== 3. ROOMS BEGIN ======
//                 if (line == "ROOMS_BEGIN")
//                 {
//                     _isReadingRooms = true;
//                     _pendingRoomsJson = "";
//                     return;
//                 }




//                 // ====== 4. Đang đọc ROOMS block ======
//                 if (_isReadingRooms)
//                 {
//                     if (line == "ROOMS_END")
//                     {
//                         _isReadingRooms = false;

//                         try
//                         {
//                             var rooms = System.Text.Json.JsonSerializer.Deserialize<List<RoomInfo>>(_pendingRoomsJson);
//                             UpdateRoomsGridOnUi(rooms);
//                         }
//                         catch (Exception ex)
//                         {
//                             MessageBox.Show("Parse JSON Room lỗi: " + ex.Message);
//                         }

//                         return;
//                     }

//                     // Thêm dòng JSON
//                     _pendingRoomsJson += line;
//                     return;
//                 }
//                 // ===== SLOT CONFIG BEGIN =====
//                 if (line == "SLOT_CONFIG_BEGIN")
//                 {
//                     _isReadingSlotConfig = true;
//                     _slotConfigTemp.Clear();
//                     return;
//                 }
//                 if (_isReadingSlotConfig)
//                 {
//                     if (line == "END_SLOT_CONFIG")
//                     {
//                         _isReadingSlotConfig = false;
//                         ApplySlotConfig();   // <== HÀM UPDATE UI (bạn sẽ thêm bên dưới)
//                         return;
//                     }

//                     // Format: SLOT|S1|07:00|08:00
//                     var s = line.Split('|');
//                     if (s.Length == 4 && s[0] == "SLOT")
//                     {
//                         _slotConfigTemp.Add((s[1], s[2], s[3]));
//                     }
//                     return;
//                 }

//                 // ====== 6. GRANT SINGLE ======
//                 if (line.StartsWith("GRANT|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 3)
//                     {
//                         MessageBox.Show(
//                             $"Yêu cầu GRANT!\nPhòng {p[1]}, Ca {p[2]}",
//                             "GRANT",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Information
//                         );

//                         _lblRequestStatus.ForeColor = Color.Green;
//                         _lblRequestStatus.Text = $"GRANT: {p[1]} – {p[2]}";
//                     }
//                     return;
//                 }

//                 // ====== 7. GRANTED_FROM_QUEUE ======
//                 if (line.StartsWith("GRANTED_FROM_QUEUE|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 5)
//                     {
//                         MessageBox.Show(
//                             $"Bạn đã được GRANT từ QUEUE!\nPhòng {p[2]}, Ca {p[3]}–{p[4]}",
//                             "Queue Grant",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Information
//                         );

//                         _lblRequestStatus.ForeColor = Color.DarkGreen;
//                         _lblRequestStatus.Text = $"QUEUE GRANT: {p[2]} – {p[3]}-{p[4]}";
//                     }
//                     return;
//                 }
//                 // ====== 8.GRANT RANGE ======
//                 if (line.StartsWith("GRANT_RANGE|"))
//                 {
//                     var p = line.Split('|');
//                     // Format: GRANT_RANGE|RoomId|SlotFrom|SlotTo
//                     if (p.Length >= 4)
//                     {
//                         MessageBox.Show(
//                             $"Yêu cầu RANGE đã được GRANT!\nPhòng {p[1]}, Ca {p[2]}–{p[3]}",
//                             "GRANT RANGE",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Information
//                         );

//                         _lblRequestStatus.ForeColor = Color.DarkGreen;
//                         _lblRequestStatus.Text =
//                             $"GRANT RANGE: {p[1]} – {p[2]}-{p[3]}";
//                     }
//                     return;
//                 }
//                 // ====== 9.RANGE QUEUED ======
//                 if (line.StartsWith("INFO|QUEUED_RANGE|"))
//                 {
//                     var p = line.Split('|');
//                     // Format: INFO|QUEUED_RANGE|position
//                     if (p.Length >= 3)
//                     {
//                         _lblRequestStatus.ForeColor = Color.DarkOrange;
//                         _lblRequestStatus.Text = $"Đang chờ RANGE (vị trí {p[2]})";
//                     }
//                     return;
//                 }
//                 // ====== RANGE WAITING ======
//                 if (line.StartsWith("RANGE_WAITING|"))
//                 {
//                     var p = line.Split('|');
//                     if (p.Length >= 2)
//                     {
//                         _lblRequestStatus.ForeColor = Color.DarkOrange;
//                         _lblRequestStatus.Text = $"Đang chờ RANGE (vị trí {p[1]})";

//                         AppendClientLog($"[WAIT] RANGE đang ở hàng đợi vị trí {p[1]}");
//                     }
//                     return;
//                 }

//                 // ====== 10.RANGE ERROR ======
//                 if (line.StartsWith("INFO|RANGE_ERROR|"))
//                 {
//                     string msg = line.Substring("INFO|RANGE_ERROR|".Length);

//                     _lblRequestStatus.ForeColor = Color.Red;
//                     _lblRequestStatus.Text = "Lỗi RANGE: " + msg;

//                     MessageBox.Show(
//                         msg,
//                         "Request range",
//                         MessageBoxButtons.OK,
//                         MessageBoxIcon.Error
//                     );
//                     return;
//                 }

//                 // ====== 11. NOTIFY PUSH ======
//                 if (line.StartsWith("NOTIFY|"))
//                 {
//                     _lstLatestNotifications.Items.Add(line.Substring(7));
//                     return;
//                 }

//                 // ====== 12. UPDATE_CONTACT response ======
//                 if (line == "UPDATE_CONTACT_OK")
//                 {
//                     MessageBox.Show(
//                         "Cập nhật thông tin liên hệ thành công!",
//                         "Account",
//                         MessageBoxButtons.OK,
//                         MessageBoxIcon.Information
//                     );

//                     RefreshHeaderSubInfo();   // update UI header
//                     return;
//                 }

//                 if (line.StartsWith("UPDATE_CONTACT_ERR|"))
//                 {
//                     var msg = line.Substring("UPDATE_CONTACT_ERR|".Length);
//                     MessageBox.Show(
//                         msg,
//                         "Account",
//                         MessageBoxButtons.OK,
//                         MessageBoxIcon.Warning
//                     );
//                     return;
//                 }

//                 // ====== 13. NOW from server ======
//                 if (line.StartsWith("NOW|"))
//                 {
//                     var payload = line.Substring(4);

//                     if (DateTime.TryParseExact(
//                         payload,
//                         "yyyy-MM-dd HH:mm:ss",
//                         System.Globalization.CultureInfo.InvariantCulture,
//                         System.Globalization.DateTimeStyles.None,
//                         out var serverTime))
//                     {
//                         _serverTimeOffset = serverTime - DateTime.Now;
//                         AppendClientLog($"[TIME] Sync NOW: {serverTime:HH:mm:ss}");
//                     }
//                     else
//                     {
//                         AppendClientLog("[WARN] Invalid NOW payload: " + payload);
//                     }
//                     return;
//                 }

//                 // ====== 14. REQUEST SINGLE RESPONSE ======
//                 if (line.StartsWith("INFO|QUEUED|"))
//                 {
//                     var p = line.Split('|');
//                     _lblRequestStatus.ForeColor = Color.DarkOrange;
//                     _lblRequestStatus.Text = $"Đang chờ (vị trí {p[2]})";
//                     return;
//                 }

//                 if (line.StartsWith("INFO|ERROR|"))
//                 {
//                     var msg = line.Substring("INFO|ERROR|".Length);
//                     _lblRequestStatus.ForeColor = Color.Red;
//                     _lblRequestStatus.Text = "Lỗi: " + msg;

//                     MessageBox.Show(msg, "Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // ======15. RELEASE SUCCESS ======
//                 if (line.StartsWith("INFO|RELEASED|"))
//                 {
//                     var p = line.Split('|');
//                     // Format: INFO|RELEASED|RoomId|SlotId
//                     if (p.Length >= 4)
//                     {
//                         string room = p[2];
//                         string slot = p[3];

//                         _lblRequestStatus.ForeColor = Color.Gray;
//                         _lblRequestStatus.Text = $"Đã RELEASE: {room} – {slot}";

//                         MessageBox.Show(
//                             $"Đã RELEASE phòng {room}, ca {slot}.",
//                             "Release",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Information
//                         );
//                     }
//                     return;
//                 }

//                 // ======16. RELEASE ERROR ======
//                 if (line.StartsWith("INFO|RELEASE_ERROR|"))
//                 {
//                     string msg = line.Substring("INFO|RELEASE_ERROR|".Length);

//                     _lblRequestStatus.ForeColor = Color.Red;
//                     _lblRequestStatus.Text = "Release lỗi: " + msg;

//                     MessageBox.Show(msg, "Release", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                     return;
//                 }

//                 // =====17. RANGE RELEASE SUCCESS =====
//                 if (line.StartsWith("INFO|RANGE_RELEASED|"))
//                 {
//                     var p = line.Split('|');
//                     // Format: INFO|RANGE_RELEASED|RoomId|SlotFrom|SlotTo
//                     if (p.Length >= 5)
//                     {
//                         string room = p[2];
//                         string sFrom = p[3];
//                         string sTo = p[4];

//                         _lblRequestStatus.ForeColor = Color.Gray;
//                         _lblRequestStatus.Text = $"Đã RELEASE RANGE: {room} – {sFrom} → {sTo}";

//                         MessageBox.Show(
//                             $"Đã RELEASE RANGE phòng {room}, ca {sFrom}–{sTo}.",
//                             "Release Range",
//                             MessageBoxButtons.OK,
//                             MessageBoxIcon.Information
//                         );
//                     }
//                     return;
//                 }

//                 // ===== 18. RANGE RELEASE ERROR =====
//                 if (line.StartsWith("INFO|RANGE_RELEASE_ERROR|"))
//                 {
//                     string msg = line.Substring("INFO|RANGE_RELEASE_ERROR|".Length);

//                     _lblRequestStatus.ForeColor = Color.Red;
//                     _lblRequestStatus.Text = "Release RANGE lỗi: " + msg;

//                     MessageBox.Show(
//                         msg,
//                         "Release Range",
//                         MessageBoxButtons.OK,
//                         MessageBoxIcon.Error
//                     );
//                     return;
//                 }
//                 // ====== SLOT_UPDATE (M5 delta) ======
//                 if (line.StartsWith("SLOT_UPDATE|"))
//                 {
//                     ApplySlotDeltaUpdate(line);
//                     return;
//                 }
//                 // ====== ROOM_SLOTS_BEGIN ======
//                 if (line == "ROOM_SLOTS_BEGIN")
//                 {
//                     _isReadingRoomSlots = true;
//                     _roomSlotsBuffer.Clear();
//                     return;
//                 }

//                 // ====== Đang đọc block ROOM_SLOTS ======
//                 if (_isReadingRoomSlots)
//                 {
//                     if (line == "ROOM_SLOTS_END")
//                     {
//                         _isReadingRoomSlots = false;

//                         // ✅ END REQUEST
//                         _roomSlotsRequestInFlight = false;

//                         RenderRoomSlots();
//                         return;
//                     }

//                     if (line.StartsWith("SLOT|"))
//                         _roomSlotsBuffer.Add(line);

//                     return;
//                 }

//                 // ====== MY_BOOKINGS_BEGIN ======
//                 if (line == "MY_BOOKINGS_BEGIN")
//                 {
//                     _isReadingMyBookings = true;
//                     _myBookingsBuffer.Clear();
//                     return;
//                 }

//                 // ====== Đang đọc block MY_BOOKINGS ======
//                 if (_isReadingMyBookings)
//                 {
//                     if (line == "MY_BOOKINGS_END")
//                     {
//                         _isReadingMyBookings = false;
//                         RenderMyBookings();   // <<< bạn sẽ thêm ở bước 3
//                         return;
//                     }

//                     if (line.StartsWith("BOOKING|"))
//                     {
//                         _myBookingsBuffer.Add(line);
//                     }
//                     return;
//                 }

//                 // ====== FIXED_SESSIONS ======
//                 if (line.StartsWith("FIXED_SESSION|"))
//                 {
//                     _isReadingFixedSessions = true;
//                     if (_fixedSessionsBuffer.Count == 0)
//                         _fixedSessionsBuffer.Clear();
//                     _fixedSessionsBuffer.Add(line);
//                     return;
//                 }

//                 if (line == "FIXED_SESSIONS|END")
//                 {
//                     _isReadingFixedSessions = false;
//                     RenderFixedSessions();
//                     return;
//                 }

//                 if (line == "FIXED_SESSIONS|NONE")
//                 {
//                     _myFixedSchedules.Clear();
//                     AppendClientLog("[INFO] No fixed schedules from server");
//                     return;
//                 }

//                 if (_isReadingFixedSessions)
//                 {
//                     return;
//                 }

//                 // ====== 19. UNKNOWN ======
//                 AppendClientLog("[WARN] Unknown push: " + line);
//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] HandleServerMessage: " + ex.Message);
//             }
//         }

//         private async void BtnRequest_Click(object? sender, EventArgs e)
//         {
//             await TrySendBookingRequestAsync();
//         }

//         private readonly SemaphoreSlim _subLock = new(1, 1);

//         private async Task UpdateRoomSlotsSubscriptionAsync(string roomId, DateTime date)
//         {
//             if (_writer == null) return;

//             var dateKey = date.ToString("yyyy-MM-dd");

//             await _subLock.WaitAsync();
//             try
//             {
//                 // nếu không đổi gì thì thôi
//                 if (string.Equals(_subRoomId, roomId, StringComparison.OrdinalIgnoreCase) &&
//                     string.Equals(_subDateKey, dateKey, StringComparison.OrdinalIgnoreCase))
//                     return;

//                 // giữ cái cũ để UNSUB
//                 var oldRoom = _subRoomId;
//                 var oldDate = _subDateKey;

//                 // set cái mới TRƯỚC để nhận SLOT_UPDATE không bị lệch
//                 _subRoomId = roomId;
//                 _subDateKey = dateKey;

//                 // Unsub cái cũ (nếu có)
//                 if (!string.IsNullOrEmpty(oldRoom) && !string.IsNullOrEmpty(oldDate))
//                 {
//                     await _writer.WriteLineAsync($"UNSUB_ROOM_SLOTS|{oldRoom}|{oldDate}");
//                     AppendClientLog($"[SEND] UNSUB_ROOM_SLOTS|{oldRoom}|{oldDate}");
//                 }

//                 // Sub cái mới
//                 await _writer.WriteLineAsync($"SUB_ROOM_SLOTS|{roomId}|{dateKey}");
//                 AppendClientLog($"[SEND] SUB_ROOM_SLOTS|{roomId}|{dateKey}");
//             }
//             finally
//             {
//                 _subLock.Release();
//             }
//         }

//         private async void BtnReleaseBooking_Click(object? sender, EventArgs e)
//         {
//             if (_currentUser == null)
//             {
//                 MessageBox.Show("Chưa đăng nhập.");
//                 return;
//             }
//             if (_writer == null)
//             {
//                 MessageBox.Show("Chưa kết nối server.");
//                 return;
//             }
//             if (_gridMyBookings.CurrentRow == null)
//             {
//                 MessageBox.Show("Vui lòng chọn một booking để release.");
//                 return;
//             }

//             var row = _gridMyBookings.CurrentRow.DataBoundItem as MyBookingRow;
//             if (row == null)
//             {
//                 MessageBox.Show("Booking không hợp lệ.");
//                 return;
//             }

//             // Chỉ cho release booking của chính mình & còn hiệu lực (APPROVED/IN_USE)
//             if (row.Status != "APPROVED" && row.Status != "IN_USE")
//             {
//                 var confirmHistory = MessageBox.Show(
//                     "Booking đã ở trạng thái " + row.Status + ". Bạn vẫn muốn gửi RELEASE?",
//                     "Release lịch sử",
//                     MessageBoxButtons.YesNo,
//                     MessageBoxIcon.Question);

//                 if (confirmHistory == DialogResult.No)
//                     return;
//             }

//             string roomId = row.RoomId;
//             string slotFrom = row.SlotStartId;
//             string slotTo = row.SlotEndId;
//             string dateKey = row.Date; // yyyy-MM-dd format

//             var confirm = MessageBox.Show(
//                 $"Xác nhận RELEASE booking:\nPhòng {roomId}\nCa {slotFrom} → {slotTo}\nNgày {row.Date}",
//                 "Xác nhận RELEASE",
//                 MessageBoxButtons.YesNo,
//                 MessageBoxIcon.Question);

//             if (confirm != DialogResult.Yes)
//                 return;

//             try
//             {
//                 string cmd;
//                 if (slotFrom == slotTo)
//                 {
//                     cmd = $"RELEASE|{_currentUser.UserId}|{roomId}|{slotFrom}|{dateKey}";
//                 }
//                 else
//                 {
//                     cmd = $"RELEASE_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}|{dateKey}";
//                 }

//                 AppendClientLog("[SEND] " + cmd);
//                 await _writer.WriteLineAsync(cmd);

//                 _lblRequestStatus.ForeColor = Color.Blue;
//                 _lblRequestStatus.Text = $"Đang gửi RELEASE {roomId} {slotFrom}→{slotTo}";

//             }
//             catch (Exception ex)
//             {
//                 AppendClientLog("[ERROR] BtnReleaseBooking_Click: " + ex.Message);
//                 MessageBox.Show("Release lỗi: " + ex.Message);
//             }
//         }
//         private bool IsActiveBookingStatus(string? status)
//         {
//             if (string.IsNullOrWhiteSpace(status)) return false;
//             status = status.ToUpperInvariant();

//             return status == "APPROVED"
//                 || status == "IN_USE"
//                 || status == "WAITING"
//                 || status == "QUEUED";
//         }
//         private bool IsGrantedBookingStatus(string? status)
//         {
//             if (string.IsNullOrWhiteSpace(status)) return false;
//             status = status.ToUpperInvariant();

//             return status == "APPROVED"
//                 || status == "IN_USE";
//         }
//         private bool IsSlotGrantedToMe(int slotIdx, string dateKey)
//         {
//             if (_currentUser == null) return false;

//             foreach (var b in _myBookings)
//             {
//                 if (!IsGrantedBookingStatus(b.Status)) continue;

//                 // cùng ngày
//                 if (!string.Equals(b.Date, dateKey, StringComparison.OrdinalIgnoreCase))
//                     continue;

//                 int s = ParseSlotIndexSafe(b.SlotStartId);
//                 int e = ParseSlotIndexSafe(b.SlotEndId);
//                 if (s <= 0 || e < s) continue;

//                 if (slotIdx >= s && slotIdx <= e)
//                     return true;
//             }

//             return false;
//         }
//         private void HighlightSlotsOfMyBookingsInCurrentRoom()
//         {
//             if (_gridRooms.CurrentRow == null) return;

//             var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
//             if (string.IsNullOrEmpty(roomId)) return;

//             var dateKey = _dtBookingDate.Value.ToString("yyyy-MM-dd");

//             // Duyệt từng row slot hiện tại và set Selected = true nếu slot nằm trong
//             // bất kỳ booking (active) của user cho phòng + ngày tương ứng
//             foreach (var row in _currentRoomSlots)
//             {
//                 int idx = ParseSlotIndexSafe(row.SlotId);
//                 if (idx <= 0)
//                 {
//                     row.Selected = false;
//                     continue;
//                 }

//                 bool belongsToMyBooking = _myBookings.Any(b =>
//                 {
//                     // chỉ consider những booking đang "active" (chưa cancel)
//                     if (!IsActiveBookingStatus(b.Status)) return false;

//                     if (!string.Equals(b.RoomId, roomId, StringComparison.OrdinalIgnoreCase))
//                         return false;

//                     if (!string.Equals(b.Date, dateKey, StringComparison.OrdinalIgnoreCase))
//                         return false;

//                     int s = ParseSlotIndexSafe(b.SlotStartId);
//                     int e = ParseSlotIndexSafe(b.SlotEndId);
//                     if (s <= 0 || e < s) return false;

//                     return idx >= s && idx <= e;
//                 });

//                 row.Selected = belongsToMyBooking;
//             }

//             _gridRoomSlots.Refresh();
//         }
//         private void RenderFixedSessions()
//         {
//             var list = new List<FixedScheduleRow>();

//             foreach (var l in _fixedSessionsBuffer)
//             {
//                 // FIXED_SESSION|SessionId|SubjectCode|SubjectName|Class|LecturerUserId|RoomId|DayOfWeek|SlotStartId|SlotEndId|DateFrom|DateTo|Note
//                 var parts = l.Split('|');
//                 if (parts.Length < 13) continue;

//                 var row = new FixedScheduleRow
//                 {
//                     SessionId = Guid.TryParse(parts[1], out var sid) ? sid : Guid.Empty,
//                     SubjectCode = parts[2],
//                     SubjectName = parts[3],
//                     Class = parts[4],
//                     LecturerUserId = parts[5],
//                     RoomId = parts[6],
//                     DayOfWeek = parts[7],
//                     SlotStartId = parts[8],
//                     SlotEndId = parts[9],
//                     DateFrom = parts[10],
//                     DateTo = parts[11],
//                     Note = parts[12]
//                 };
//                 list.Add(row);
//             }

//             _myFixedSchedules = list;
//             _fixedSessionsBuffer.Clear();
            
//             AppendClientLog($"[INFO] Loaded {list.Count} fixed schedules from server");
            
//             // Reload schedule tab nếu đang hiển thị
//             if (_tabMain != null && _tabSchedule != null && _tabMain.SelectedTab == _tabSchedule)
//             {
//                 _ = Task.Run(async () =>
//                 {
//                     try { await ReloadScheduleFromServerAsync(); }
//                     catch (Exception ex) { AppendClientLog($"[ERROR] ReloadScheduleFromServerAsync: {ex.Message}"); }
//                 });
//             }
//         }

//         private void RenderMyBookings()
//         {
//             _myBookings.Clear();

//             foreach (var l in _myBookingsBuffer)
//             {
//                 // BOOKING|BookingId|Date|RoomId|SlotStartId|SlotEndId|TimeRange|Status|Purpose
//                 var parts = l.Split('|');
//                 if (parts.Length < 9) continue; // để code cũ vẫn chạy nếu chưa update server

//                 if (!Guid.TryParse(parts[1], out var bid)) continue;

//                 var row = new MyBookingRow
//                 {
//                     BookingId = bid,
//                     Date = parts[2],
//                     RoomId = parts[3],
//                     SlotStartId = parts[4],
//                     SlotEndId = parts[5],
//                     TimeRange = parts[6],
//                     Status = parts[7],
//                     Purpose = parts[8]
//                 };

//                 if (parts.Length > 9) row.CreatedAt = parts[9];
//                 if (parts.Length > 10) row.CheckinDeadline = parts[10];
//                 if (parts.Length > 11) row.CheckinTime = parts[11];
//                 if (parts.Length > 12) row.UpdatedAt = parts[12];

//                 _myBookings.Add(row);

//             }
//             // ✅ SORT theo CreatedAt tăng dần (cũ -> mới), để CreatedAt mới nhất nằm ở CUỐI
//             {
//                 var list = _myBookings.ToList();

//                 list.Sort((a, b) =>
//                 {
//                     // CreatedAt format server gửi: "yyyy-MM-dd HH:mm:ss"
//                     DateTime da, db;

//                     var oka = DateTime.TryParseExact(
//                         a.CreatedAt,
//                         "yyyy-MM-dd HH:mm:ss",
//                         System.Globalization.CultureInfo.InvariantCulture,
//                         System.Globalization.DateTimeStyles.None,
//                         out da);

//                     var okb = DateTime.TryParseExact(
//                         b.CreatedAt,
//                         "yyyy-MM-dd HH:mm:ss",
//                         System.Globalization.CultureInfo.InvariantCulture,
//                         System.Globalization.DateTimeStyles.None,
//                         out db);

//                     // cái nào parse fail cho lên đầu (MinValue)
//                     if (!oka) da = DateTime.MinValue;
//                     if (!okb) db = DateTime.MinValue;

//                     return da.CompareTo(db); // tăng dần => mới nhất nằm cuối
//                 });

//                 _myBookings.Clear();
//                 foreach (var r in list) _myBookings.Add(r);
//             }
//             // _gridMyBookings.DataSource = null;
//             // _gridMyBookings.DataSource = _myBookings;
//             _bsMyBookings.ResetBindings(false);

//             // 🔥 Sau khi load booking xong thì cập nhật lại tick ở grid slot hiện tại
//             // (không async, chỉ refresh bằng dữ liệu sẵn có)
//             HighlightSlotsOfMyBookingsInCurrentRoom();
//         }
//         private void RenderRoomSlots()
//         {
//             if (InvokeRequired) { BeginInvoke(new Action(RenderRoomSlots)); return; }

//             // ✅ tránh GET cũ về trễ đè UI mới
//             var currentKey = $"{_subRoomId}|{_subDateKey}";
//             if (!string.IsNullOrEmpty(_lastRoomSlotsKey) &&
//                 !string.Equals(_lastRoomSlotsKey, currentKey, StringComparison.OrdinalIgnoreCase))
//             {
//                 AppendClientLog($"[SKIP] RenderRoomSlots outdated. last={_lastRoomSlotsKey}, sub={currentKey}");
//                 return;
//             }

//             var oldSelected = _currentRoomSlots
//                 .Where(x => !string.IsNullOrWhiteSpace(x.SlotId) && x.Selected)
//                 .Select(x => x.SlotId)
//                 .ToHashSet(StringComparer.OrdinalIgnoreCase);

//             _currentRoomSlots.RaiseListChangedEvents = false;
//             try
//             {
//                 _currentRoomSlots.Clear();

//                 foreach (var l in _roomSlotsBuffer)
//                 {
//                     var parts = l.Split('|');
//                     if (parts.Length < 8) continue;

//                     var slotId = parts[1];
//                     string status = parts[3];
//                     string bookingStatus = parts[6];
                    
//                     // ✅ FIX: Hiển thị đúng status từ server
//                     string displayStatus = status;
//                     if (status == "LOCKED")
//                     {
//                         // Slot bị lock bởi fixed schedule - hiển thị LOCKED
//                         displayStatus = "LOCKED";
//                     }
//                     else if (status == "BUSY")
//                     {
//                         // Slot đang được sử dụng - hiển thị BUSY với booking status
//                         displayStatus = string.IsNullOrEmpty(bookingStatus) ? "BUSY" : $"BUSY ({bookingStatus})";
//                     }
//                     else if (status == "FREE")
//                     {
//                         displayStatus = "FREE";
//                     }
//                     else
//                     {
//                         // Các trạng thái khác (BUSY_EVT, etc.)
//                         displayStatus = status;
//                     }
                    
//                     _currentRoomSlots.Add(new RoomSlotRow
//                     {
//                         Selected = oldSelected.Contains(slotId),
//                         SlotId = slotId,
//                         TimeRange = parts[2],
//                         Status = displayStatus,
//                         UserId = parts[4],
//                         HolderName = parts[5],
//                         Purpose = (parts.Length == 8) ? parts[7] : string.Join("|", parts.Skip(7))
//                     });
//                 }
//             }
//             finally
//             {
//                 _currentRoomSlots.RaiseListChangedEvents = true;

//                 // ✅ refresh 1 lần (nếu dùng BindingSource)
//                 _bsRoomSlots.ResetBindings(false);
//             }

//             HighlightSlotsOfMyBookingsInCurrentRoom();
//         }



//         private HashSet<string> CaptureSelectedSlotIdsFromGrid()
//         {
//             var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//             if (_gridRoomSlots == null) return set;

//             foreach (DataGridViewRow row in _gridRoomSlots.Rows)
//             {
//                 var slotId = row.Cells["SlotId"].Value?.ToString();
//                 if (string.IsNullOrWhiteSpace(slotId)) continue;

//                 bool isChecked = false;
//                 var v = row.Cells["Selected"].Value;
//                 if (v is bool b) isChecked = b;

//                 if (isChecked) set.Add(slotId);
//             }

//             return set;
//         }


//         private void RestoreSelectedSlotIds(HashSet<string> selected)
//         {
//             if (_currentRoomSlots == null) return;

//             foreach (var r in _currentRoomSlots)
//             {
//                 if (!string.IsNullOrWhiteSpace(r.SlotId))
//                     r.Selected = selected.Contains(r.SlotId);
//             }
//             _gridRoomSlots?.Refresh();
//         }
        
//         private void ApplySlotDeltaUpdate(string line)
//         {
//             if (InvokeRequired) { BeginInvoke(new Action(() => ApplySlotDeltaUpdate(line))); return; }

//             var p = line.Split('|');
//             if (p.Length < 9) return;

//             var roomId = p[1];
//             var dateKey = p[2];
//             var slotId = p[3];
//             var status = p[4];
//             var userId = p[5];
//             var fullName = p[6];
//             var bookingStatus = p[7];
//             var purpose = (p.Length == 9) ? p[8] : string.Join("|", p.Skip(8));

//             if (!string.Equals(roomId, _subRoomId, StringComparison.OrdinalIgnoreCase)) return;
//             if (!string.Equals(dateKey, _subDateKey, StringComparison.OrdinalIgnoreCase)) return;

//             int idx = -1;
//             for (int i = 0; i < _currentRoomSlots.Count; i++)
//                 if (string.Equals(_currentRoomSlots[i].SlotId, slotId, StringComparison.OrdinalIgnoreCase))
//                 { idx = i; break; }

//             if (idx < 0) return;

//             var row = _currentRoomSlots[idx];
            
//             // ✅ FIX: Hiển thị đúng status từ SLOT_UPDATE
//             if (status == "LOCKED")
//             {
//                 row.Status = "LOCKED";
//             }
//             else if (status == "BUSY")
//             {
//                 row.Status = string.IsNullOrEmpty(bookingStatus) ? "BUSY" : $"BUSY ({bookingStatus})";
//             }
//             else if (status == "FREE")
//             {
//                 row.Status = "FREE";
//             }
//             else
//             {
//                 row.Status = status;
//             }
            
//             row.UserId = userId;
//             row.HolderName = fullName;
//             row.Purpose = purpose;

//             // ✅ refresh đúng 1 item
//             _bsRoomSlots.ResetItem(idx);

//             HighlightSlotsOfMyBookingsInCurrentRoom();
//         }

//         private void HandleGlobalHotkeys(KeyEventArgs e)
//         {
//             if (e.Control && e.Shift && e.KeyCode == Keys.L)
//             {
//                 if (!string.Equals(_currentUser.UserType, "Staff", StringComparison.OrdinalIgnoreCase))
//                     return;

//                 if (_tabMain != null && _tabLog != null)
//                     _tabMain.SelectedTab = _tabLog;

//                 e.Handled = true;
//             }
//         }

//         private Panel CreateSidebarItem(string glyph, string title, Action onClick)
//         {
//             var item = new Panel
//             {
//                 Left = 12,
//                 Width = _panelSidebar.Width - 24,
//                 Height = 44,
//                 BackColor = Color.FromArgb(20, 20, 24),
//                 Cursor = Cursors.Hand
//             };

//             var icon = new Label
//             {
//                 Left = 12,
//                 Top = 10,
//                 Width = 24,
//                 Height = 24,
//                 Text = glyph,
//                 ForeColor = Color.White,
//                 Font = new Font("Segoe MDL2 Assets", 14, FontStyle.Regular),
//                 BackColor = Color.Transparent
//             };
//             item.Controls.Add(icon);

//             var text = new Label
//             {
//                 Left = 44,
//                 Top = 12,
//                 Width = item.Width - 56,
//                 Height = 20,
//                 Text = title,
//                 ForeColor = Color.White,
//                 Font = new Font("Segoe UI", 10, FontStyle.Bold),
//                 BackColor = Color.Transparent
//             };
//             item.Controls.Add(text);

//             void click(object? s, EventArgs e)
//             {
//                 onClick();
//             }

//             item.Click += click;
//             icon.Click += click;
//             text.Click += click;

//             return item;
//         }

//         private void SetSidebarActive(Panel active)
//         {
//             foreach (Control c in _panelSidebar.Controls)
//             {
//                 if (c is Panel p)
//                 {
//                     p.BackColor = (p == active)
//                         ? Color.FromArgb(45, 45, 55)
//                         : Color.FromArgb(20, 20, 24);
//                 }
//             }
//         }

//         private static void EnableTcpKeepAlive(Socket s, uint timeMs = 30_000, uint intervalMs = 10_000)
//         {
//             try
//             {
//                 s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
//                 byte[] inOptionValues = new byte[12];
//                 BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
//                 BitConverter.GetBytes(timeMs).CopyTo(inOptionValues, 4);
//                 BitConverter.GetBytes(intervalMs).CopyTo(inOptionValues, 8);
//                 s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
//             }
//             catch { }
//         }



//     }



// }



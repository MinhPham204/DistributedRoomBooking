//bookingclient/mainclientform.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.IO;
using System.Collections.Generic;
// ĐẶT ALIAS RÕ RÀNG ĐỂ HẾT LỖI AMBIGUOUS TIMER
using WinFormsTimer = System.Windows.Forms.Timer;

namespace BookingClient
{
    public class MainClientForm : Form
    {
        private readonly LoginForm _loginForm;
        private bool _isLoggingOut = false;


        // Header
        private Panel _panelHeader = null!;
        private PictureBox _picAvatar = null!;
        private Label _lblNameWithType = null!;
        private Label _lblSubInfo = null!;
        private Label _lblToday = null!;
        private Button _btnHeaderCheckConnect = null!;
        private Panel _pnlHeaderConnectDot = null!;
        private Label _lblHeaderConnectText = null!;
        private Label _lblRunningTime = null!;
        private WinFormsTimer _timerClock = null!;
        private WinFormsTimer _timerTimeSync = null!;   // <<< thêm dòng này

        // Sub-header (cấu hình ca)
        private Panel _panelSubHeader = null!;
        private Label _lblSlotConfig = null!;
        private Button _btnSlotConfigHelp = null!;

        // Tabs
        private TabControl _tabMain = null!;
        private TabPage _tabHome = null!;
        private TabPage _tabBooking = null!;
        private TabPage _tabSchedule = null!;
        private TabPage _tabNotifications = null!;
        private TabPage _tabAccount = null!;

        // ======= Controls cho tab Trang chủ =======
        private GroupBox _grpTodaySchedule = null!;
        private DataGridView _gridTodaySchedule = null!;
        private GroupBox _grpLatestNotifications = null!;
        private ListBox _lstLatestNotifications = null!;
        private Button _btnGoBookingTab = null!;
        private Button _btnGoMyWeekSchedule = null!;

        // ======= Controls cho tab Đặt phòng (UI) =======
        // A. Filter + list phòng
        private GroupBox _grpSearchRooms = null!;
        private DateTimePicker _dtBookingDate = null!;
        private ComboBox _cbBookingFromSlot = null!;
        private ComboBox _cbBookingToSlot = null!;
        private NumericUpDown _numMinCapacity = null!;
        private CheckBox _chkNeedProjector = null!;
        private CheckBox _chkNeedPC = null!;
        private CheckBox _chkNeedAC = null!;
        private ComboBox _cbBuilding = null!;
        private Button _btnSearchRooms = null!;
        private DataGridView _gridRooms = null!;

        // B. Gửi request
        private GroupBox _grpRequest = null!;
        private ComboBox _cbReqRoom = null!;
        private DateTimePicker _dtReqDate = null!;
        private ComboBox _cbReqSlotSingle = null!;
        private ComboBox _cbReqSlotFrom = null!;
        private ComboBox _cbReqSlotTo = null!;
        private TextBox _txtPurpose = null!;
        private Label _lblSlotTimeRange = null!;
        private Button _btnReqSingle = null!;
        private Button _btnReqRange = null!;
        private Button _btnReleaseSingle = null!;
        private Button _btnReleaseRange = null!;
        private Label _lblRequestStatus = null!;
        private GroupBox _grpClientLog = null!;
        private TextBox _txtClientLog = null!;

        // ======= Tab Lịch của tôi =======
        private RadioButton _radDayView = null!;
        private RadioButton _radWeekView = null!;
        private DateTimePicker _dtScheduleDate = null!;
        private DataGridView _gridDayView = null!;
        private DataGridView _gridWeekView = null!;
        private Button _btnExportSchedule = null!;
        private Button _btnBackHomeFromSchedule = null!;

        // ======= Tab Thông báo =======
        private DataGridView _gridNotifications = null!;
        private Button _btnMarkAllRead = null!;
        private ComboBox _cbFilterType = null!;
        private DateTimePicker _dtNotiFrom = null!;
        private DateTimePicker _dtNotiTo = null!;

        // ======= Tab Tài khoản =======
        private TextBox _txtAccFullName = null!;
        private TextBox _txtAccStudentLecturerId = null!;
        private TextBox _txtAccClassFaculty = null!;
        private TextBox _txtAccDepartment = null!;
        private TextBox _txtAccEmail = null!;
        private TextBox _txtAccPhone = null!;
        private TextBox _txtOldPassword = null!;
        private TextBox _txtNewPassword = null!;
        private TextBox _txtConfirmPassword = null!;
        private Button _btnUpdateContact = null!;
        private Button _btnChangePassword = null!;
        private Button _btnLogout = null!;

        // Check connect trong tab Account
        private Button _btnAccCheckConnect = null!;
        private Panel _pnlAccConnectDot = null!;
        private Label _lblAccConnectText = null!;
        private Button _btnGoAccountTab = null!;

        // ======= Tab Đặt phòng =======
        private Button _btnBackToHome = null!;   // nút quay về tab Trang chủ
                                                 // Demo dữ liệu lịch cá nhân
        private class MyScheduleItem
        {
            public DateTime Date { get; set; }
            public int Slot { get; set; }              // 1..14
            public string TimeRange { get; set; } = "";
            public string RoomId { get; set; } = "";
            public string Subject { get; set; } = "";  // Môn / lý do
            public string Status { get; set; } = "";   // APPROVED / IN_USE / COMPLETED / NO_SHOW
            public string Note { get; set; } = "";
        }
        private class RoomSearchRow
        {
            public string RoomId { get; set; } = "";
            public string Building { get; set; } = "";
            public int Capacity { get; set; }
            public bool HasProjector { get; set; }
            public bool HasPC { get; set; }
            public bool HasAC { get; set; }
            public bool HasMic { get; set; }
            public string Status { get; set; } = "FREE";
        }
        private readonly List<RoomSearchRow> _allRoomsForSearch = new();

        private readonly Dictionary<string, string> _slotTimeLookup = new();

        private readonly DemoUserInfo _currentUser;
        private readonly string _serverIp;
        private TimeSpan _serverTimeOffset = TimeSpan.Zero;

        public MainClientForm(DemoUserInfo currentUser, string serverIp, LoginForm loginForm)
        {
            _currentUser = currentUser;
            _serverIp = serverIp;
            _loginForm = loginForm;

            InitializeComponent(); // trống cũng được, nhưng cứ giữ cho chuẩn WinForms
            SetupUi();
            this.FormClosing += MainClientForm_FormClosing;

            this.Shown += async (s, e) =>
    {
        await HeaderCheckConnectAsync();
    };
        }

        private void InitializeComponent()
        {
            // Không dùng Designer, nên để trống
        }

        private void SetupUi()
        {
            Text = "Client - Room Booking";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            // ====== HEADER ======
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.WhiteSmoke
            };
            Controls.Add(_panelHeader);

            _picAvatar = new PictureBox
            {
                Left = 10,
                Top = 10,
                Width = 60,
                Height = 60,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            // Bạn có thể load 1 icon default ở đây nếu thích
            _panelHeader.Controls.Add(_picAvatar);
            _picAvatar.SizeMode = PictureBoxSizeMode.StretchImage; // sau này nếu bạn có icon/ảnh

            _picAvatar.Paint += (s, e) =>
            {
                var gp = new GraphicsPath();
                gp.AddEllipse(0, 0, _picAvatar.Width - 1, _picAvatar.Height - 1);
                _picAvatar.Region = new Region(gp);
            };

            // Tên + loại user (giữ nguyên)
            _lblNameWithType = new Label
            {
                Left = 80,
                Top = 10,
                Width = 400,
                Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold)
            };
            _panelHeader.Controls.Add(_lblNameWithType);
            _lblNameWithType.Text = $"{_currentUser.FullName} ({_currentUser.UserType})";

            // Dòng phụ: StudentId/Class/Department hoặc LecturerId/Faculty + Email/Phone
            _lblSubInfo = new Label
            {
                Left = 80,
                Top = 35,
                Width = 700,          // tăng rộng chút cho thoải mái
                AutoSize = false
            };
            _panelHeader.Controls.Add(_lblSubInfo);

            string subText;
            if (_currentUser.UserType == "Student")
            {
                subText =
                    $"StudentId: {_currentUser.StudentId} - Class: {_currentUser.Class} - Department: {_currentUser.Department}";
            }
            else if (_currentUser.UserType == "Lecturer")
            {
                subText =
                    $"LecturerId: {_currentUser.LecturerId} - Faculty: {_currentUser.Faculty}";
            }
            else
            {
                // Staff
                subText = $"Staff - Department: {_currentUser.Department}";
            }

            // Thông tin liên hệ chung
            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_currentUser.Email))
                contactParts.Add($"Email: {_currentUser.Email}");
            if (!string.IsNullOrWhiteSpace(_currentUser.Phone))
                contactParts.Add($"Phone: {_currentUser.Phone}");

            if (contactParts.Count > 0)
            {
                subText += " | " + string.Join(" - ", contactParts);
            }

            _lblSubInfo.Text = subText;


            _lblSubInfo = new Label
            {
                Left = 80,
                Top = 35,
                Width = 400,
                Height = 20,
                Text = subText
            };
            _panelHeader.Controls.Add(_lblSubInfo);

            _lblToday = new Label
            {
                AutoSize = false,
                Width = 260,
                Height = 20,
                TextAlign = ContentAlignment.MiddleRight,
                Top = 10,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = "Date --/--/---- – --"   // placeholder, lát nữa InitHeaderTimeAsync sẽ set lại

            };
            // Left = Right - Width (sau khi form load thì Anchor sẽ tự co)
            _lblToday.Left = ClientSize.Width - _lblToday.Width - 20;
            _panelHeader.Controls.Add(_lblToday);
            // UpdateTodayLabel(DateTime.Now);

            _btnHeaderCheckConnect = new Button
            {
                Text = "Check connect",
                Width = 110,
                Height = 25,
                Top = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnHeaderCheckConnect.Left = ClientSize.Width - 180;
            _btnHeaderCheckConnect.Click += async (s, e) =>
            {
                await HeaderCheckConnectAsync();
            };
            _panelHeader.Controls.Add(_btnHeaderCheckConnect);

            // ===== Nút ĐĂNG XUẤT trên header =====
            _btnLogout = new Button
            {
                Text = "Đăng xuất",
                Width = 90,
                Height = 25,
                // Vị trí cố định
                Left = 810,
                Top = 56,
                Anchor = AnchorStyles.Top   // nếu không muốn nó chạy theo chiều ngang thì bỏ Right đi
            };

            _btnLogout.Click += (s, e) =>
            {
                _isLoggingOut = true;      // báo cho FormClosing biết đây là logout, không phải thoát app
                _loginForm.Show();         // hiện lại form login cũ
                this.Close();              // đóng MainClientForm
            };

            _panelHeader.Controls.Add(_btnLogout);


            _pnlHeaderConnectDot = new Panel
            {
                Width = 14,
                Height = 14,
                Top = 33,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Red,
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlHeaderConnectDot.Left = ClientSize.Width - 65;
            _panelHeader.Controls.Add(_pnlHeaderConnectDot);

            _lblHeaderConnectText = new Label
            {
                Width = 60,
                Height = 20,
                Top = 33,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = "Lost",
                ForeColor = Color.Red
            };
            _lblHeaderConnectText.Left = ClientSize.Width - 50;
            _panelHeader.Controls.Add(_lblHeaderConnectText);

            _lblRunningTime = new Label
            {
                Width = 200,
                Height = 20,
                Top = 58,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = "Time: --:--:--"
            };
            _lblRunningTime.Left = ClientSize.Width - 280;
            _panelHeader.Controls.Add(_lblRunningTime);

            // === FIX: dùng WinFormsTimer chứ không dùng Timer mơ hồ ===
            _timerClock = new WinFormsTimer { Interval = 1000 };
            _timerClock.Tick += (s, e) =>
            {
                var now = DateTime.Now + _serverTimeOffset;
                _lblRunningTime.Text = "Time: " + now.ToString("HH:mm:ss");
            };
            _timerClock.Start();
            // Timer định kỳ đồng bộ lại giờ với server
            _timerTimeSync = new WinFormsTimer { Interval = 1000 }; // 3 giây 1 lần, bạn muốn 1000ms cũng được
            _timerTimeSync.Tick += async (s, e) =>
            {
                var serverNow = await GetServerNowAsync();
                if (serverNow.HasValue)
                {
                    // Cập nhật lại offset
                    _serverTimeOffset = serverNow.Value - DateTime.Now;

                    // Cập nhật lại label "Hôm nay: ..."
                    UpdateTodayLabel(serverNow.Value);
                }
            };
            _timerTimeSync.Start();
            // ====== SUB HEADER: cấu hình ca ======
            _panelSubHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.Gainsboro
            };
            Controls.Add(_panelSubHeader);

            _lblSlotConfig = new Label
            {
                Left = 10,
                Top = 8,
                Width = 800,
                Text = "Cấu hình ca: Ca 1: 07:00–08:00 | Ca 2: 08:00–09:00 | Ca 3: 09:00–10:00 | ..."
            };
            _panelSubHeader.Controls.Add(_lblSlotConfig);

            _btnSlotConfigHelp = new Button
            {
                Text = "?",
                Width = 24,
                Height = 24,
                Top = 5,
                Left = 820
            };
            _btnSlotConfigHelp.Click += (s, e) =>
            {
                MessageBox.Show("Chi tiết cấu hình ca (demo).", "Cấu hình ca");
            };
            _panelSubHeader.Controls.Add(_btnSlotConfigHelp);

            // ====== TAB CONTROL ======
            _tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_tabMain);

            _tabHome = new TabPage("Trang chủ");
            _tabBooking = new TabPage("Đặt phòng");
            _tabSchedule = new TabPage("Lịch của tôi");
            _tabNotifications = new TabPage("Thông báo");
            _tabAccount = new TabPage("Tài khoản");

            _tabMain.TabPages.AddRange(new[]
            {
                _tabHome, _tabBooking, _tabSchedule, _tabNotifications, _tabAccount
            });

            BuildHomeTabUi();
            this.Shown += async (s, e) =>
            {
                await InitHeaderTimeAsync();   // anh đã có cho header
                await LoadSlotConfigFromServerAsync(); // NEW: load giờ ca từ server
                await LoadHomeFromServerAsync(); // load dữ liệu thật cho Trang chủ
                await ReloadScheduleFromServerAsync();   // phần lịch sẽ thêm ở dưới

            };
            BuildBookingTabUi();
            BuildScheduleTabUi();
            BuildNotificationsTabUi();
            BuildAccountTabUi();
            // Khởi tạo thời gian header theo server
            // _ = InitHeaderTimeAsync();
        }
        private async Task<bool> HeaderCheckConnectAsync()
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    {
                        var data = Encoding.UTF8.GetBytes("PING\n");
                        await stream.WriteAsync(data, 0, data.Length);

                        var buffer = new byte[256];
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        var resp = Encoding.UTF8.GetString(buffer, 0, read).Trim();

                        if (resp == "PONG")
                        {
                            _pnlHeaderConnectDot.BackColor = Color.LimeGreen;
                            _lblHeaderConnectText.Text = "OK";
                            _lblHeaderConnectText.ForeColor = Color.Green;
                            return true;
                        }
                    }
                }

                // Nếu không return ở trên => coi là lỗi
                _pnlHeaderConnectDot.BackColor = Color.Red;
                _lblHeaderConnectText.Text = "Lost";
                _lblHeaderConnectText.ForeColor = Color.Red;
                return false;
            }
            catch
            {
                _pnlHeaderConnectDot.BackColor = Color.Red;
                _lblHeaderConnectText.Text = "Lost";
                _lblHeaderConnectText.ForeColor = Color.Red;
                return false;
            }
        }

        private async Task<DateTime?> GetServerNowAsync()
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    {
                        var data = Encoding.UTF8.GetBytes("GET_NOW\n");
                        await stream.WriteAsync(data, 0, data.Length);

                        var buffer = new byte[256];
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (read <= 0) return null;

                        var resp = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                        // Expect: NOW|yyyy-MM-dd HH:mm:ss
                        if (!resp.StartsWith("NOW|"))
                            return null;

                        var payload = resp.Substring(4);
                        if (DateTime.TryParse(payload, out var serverNow))
                            return serverNow;

                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        private string GetVietnameseWeekday(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Monday: return "Thứ 2";
                case DayOfWeek.Tuesday: return "Thứ 3";
                case DayOfWeek.Wednesday: return "Thứ 4";
                case DayOfWeek.Thursday: return "Thứ 5";
                case DayOfWeek.Friday: return "Thứ 6";
                case DayOfWeek.Saturday: return "Thứ 7";
                case DayOfWeek.Sunday: return "Chủ nhật";
                default: return "";
            }
        }

        private void UpdateTodayLabel(DateTime now)
        {
            var thu = GetVietnameseWeekday(now.DayOfWeek);
            _lblToday.Text = $"Date {now:dd/MM/yyyy} – {thu}";
        }
        private async Task InitHeaderTimeAsync()
        {
            var serverNow = await GetServerNowAsync();
            if (serverNow.HasValue)
            {
                _serverTimeOffset = serverNow.Value - DateTime.Now;
                UpdateTodayLabel(serverNow.Value);
            }
            else
            {
                // fallback: dùng giờ máy nếu không gọi được server
                _serverTimeOffset = TimeSpan.Zero;
                UpdateTodayLabel(DateTime.Now);
            }
        }

        // ================== 2.3. TAB TRANG CHỦ ==================
        private void BuildHomeTabUi()
        {
            // ===== Trái: Lịch hôm nay =====
            _grpTodaySchedule = new GroupBox
            {
                Text = "Lịch hôm nay của bạn",
                Left = 10,
                Top = 100,
                Width = 450,
                Height = 300
            };
            _tabHome.Controls.Add(_grpTodaySchedule);

            _gridTodaySchedule = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _grpTodaySchedule.Controls.Add(_gridTodaySchedule);

            // Định nghĩa cột: Giờ, Phòng, Môn, Người dạy/người đặt, Trạng thái, Ghi chú
            _gridTodaySchedule.Columns.Clear();
            _gridTodaySchedule.Columns.Add("TimeRange", "Giờ");
            _gridTodaySchedule.Columns.Add("RoomId", "Phòng");
            _gridTodaySchedule.Columns.Add("Subject", "Môn / Mục đích");
            _gridTodaySchedule.Columns.Add("Owner", "Giảng viên / Người đặt");
            _gridTodaySchedule.Columns.Add("Status", "Trạng thái");
            _gridTodaySchedule.Columns.Add("Note", "Ghi chú");

            // ===== Phải: Thông báo mới =====
            _grpLatestNotifications = new GroupBox
            {
                Text = "Thông báo mới",
                Left = 470,
                Top = 100,
                Width = 480,
                Height = 300
            };
            _tabHome.Controls.Add(_grpLatestNotifications);

            _lstLatestNotifications = new ListBox
            {
                Dock = DockStyle.Fill
            };
            _grpLatestNotifications.Controls.Add(_lstLatestNotifications);

            // ===== Nút nhanh =====
            _btnGoBookingTab = new Button
            {
                Text = "Đặt phòng ngay",
                Left = 10,
                Top = 450,
                Width = 150,
                Height = 30
            };
            _btnGoBookingTab.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabBooking;
            };
            _tabHome.Controls.Add(_btnGoBookingTab);

            _btnGoMyWeekSchedule = new Button
            {
                Text = "Xem lịch tuần này",
                Left = 170,
                Top = 450,
                Width = 150,
                Height = 30
            };
            _btnGoMyWeekSchedule.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabSchedule;
                _radWeekView.Checked = true; // chuyển luôn sang week view
            };
            _tabHome.Controls.Add(_btnGoMyWeekSchedule);

            // Nút sang tab Tài khoản
            _btnGoAccountTab = new Button
            {
                Text = "Tài khoản của tôi",
                Left = 330,
                Top = 450,
                Width = 150,
                Height = 30
            };
            _btnGoAccountTab.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabAccount;
            };
            _tabHome.Controls.Add(_btnGoAccountTab);

            // Tạm thời load dữ liệu demo, sau này thay bằng dữ liệu thật từ server
            LoadHomeDemoData();
        }
        private void MainClientForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isLoggingOut)
            {
                // Logout chủ động: chỉ đóng MainClientForm, quay về LoginForm (đã Show ở nút Logout)
                return;
            }

            // User bấm X đóng app => thoát toàn bộ
            Application.Exit();
        }

        /// <summary>
        /// Tạm thời: dữ liệu demo cho tab Trang chủ.
        /// Sau này bạn sẽ replace bằng call server để lấy:
        /// - Lịch cố định của SV / GV
        /// - Booking của user hôm nay
        /// - Thông báo mới từ server
        /// </summary>
        private void LoadHomeDemoData()
        {
            // Xóa dữ liệu cũ
            _gridTodaySchedule.Rows.Clear();
            _lstLatestNotifications.Items.Clear();

            // Ví dụ: nếu là Student thì lịch hôm nay là các môn học
            if (_currentUser.UserType == "Student")
            {
                _gridTodaySchedule.Rows.Add("Ca 1 (07:00–08:00)", "A08", "CTDL & GT", "GV Nguyễn Văn B", "Học", "");
                _gridTodaySchedule.Rows.Add("Ca 3 (09:00–10:00)", "A16", "Lập trình .NET", "GV Trần Thị C", "Học", "");
            }
            else
            {
                // Ví dụ: nếu là Lecturer hoặc Staff -> lịch theo booking phòng
                _gridTodaySchedule.Rows.Add("Ca 2 (08:00–09:00)", "A08", "Họp nhóm đồ án", _currentUser.FullName, "APPROVED", "Book phòng thảo luận");
                _gridTodaySchedule.Rows.Add("Ca 5 (13:00–14:00)", "A16", "Seminar Khoa CNTT", _currentUser.FullName, "APPROVED", "");
            }

            // Thông báo mới (demo)
            _lstLatestNotifications.Items.Add("Phòng A08 ca 3 ngày 05/12 đã được grant. Vui lòng check-in trước 07:15.");
            _lstLatestNotifications.Items.Add("Phòng A08 ca 5 chuyển sang A16 do hội thảo Khoa CNTT.");
            _lstLatestNotifications.Items.Add("Booking ca 2 phòng B03 của bạn đã bị hủy do bảo trì phòng.");
        }

        private async Task LoadHomeFromServerAsync()
        {
            _gridTodaySchedule.Rows.Clear();
            _lstLatestNotifications.Items.Clear();

            try
            {
                using (var tcp = new TcpClient())
                {
                    // Dùng đúng cổng TCP server (5000)
                    await tcp.ConnectAsync(_serverIp, 5000);

                    using (var stream = tcp.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // Gửi lệnh
                        var cmd = $"GET_HOME_DATA|{_currentUser.UserId}\n";
                        var data = Encoding.UTF8.GetBytes(cmd);
                        await stream.WriteAsync(data, 0, data.Length);

                        // Đọc từng dòng đến HOME_DATA_END
                        bool started = false;

                        while (true)
                        {
                            var line = await reader.ReadLineAsync();
                            if (line == null) break;

                            if (line == "HOME_DATA_BEGIN")
                            {
                                started = true;
                                continue;
                            }

                            if (line == "HOME_DATA_END")
                                break;

                            if (!started)
                                continue;

                            // Xử lý dòng
                            if (line.StartsWith("SCHEDULE|"))
                            {
                                var parts = line.Split('|');
                                // [0]=SCHEDULE
                                // [1]=TimeRange
                                // [2]=RoomId
                                // [3]=Subject/Purpose
                                // [4]=Teacher/Owner
                                // [5]=Status
                                // [6]=Extra
                                if (parts.Length >= 6)
                                {
                                    var timeRange = parts[1];
                                    var roomId = parts[2];
                                    var subject = parts[3];
                                    var teacher = parts[4];
                                    var status = parts[5];
                                    var extra = parts.Length > 6 ? parts[6] : "";

                                    // Map vào cột grid hiện tại của anh
                                    // Ví dụ: Giờ, Phòng, Môn, Trạng thái, Ghi chú
                                    _gridTodaySchedule.Rows.Add(
                                        timeRange,
                                        roomId,
                                        subject,
                                        teacher,
                                        status,
                                        extra
                                    );
                                }
                            }
                            else if (line.StartsWith("NOTI|"))
                            {
                                var msg = line.Substring("NOTI|".Length);
                                _lstLatestNotifications.Items.Add(msg);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi server / mạng, fallback sang demo để UI vẫn có cái cho user xem
                _lstLatestNotifications.Items.Add("Không lấy được dữ liệu Trang chủ từ server: " + ex.Message);
                LoadHomeDemoData();
            }
        }


        // ================== 2.4. TAB ĐẶT PHÒNG ==================
        private void BuildBookingTabUi()
        {
            _tabBooking.AutoScroll = true;   // cho phép cuộn nếu cao quá

            // ======== (A) Nhóm TÌM PHÒNG TRỐNG (trên) ========
            var grpSearch = new GroupBox
            {
                Text = "Tìm phòng trống",
                Left = 10,
                Top = 100,
                Width = 950,
                Height = 200
            };
            _tabBooking.Controls.Add(grpSearch);

            // Ngày
            var lblDate = new Label
            {
                Text = "Ngày:",
                Left = 10,
                Top = 25,
                Width = 50
            };
            grpSearch.Controls.Add(lblDate);

            _dtBookingDate = new DateTimePicker
            {
                Left = 70,
                Top = 20,
                Width = 130,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            grpSearch.Controls.Add(_dtBookingDate);

            // Từ ca – Đến ca
            var lblFromSlot = new Label
            {
                Text = "Từ ca:",
                Left = 220,
                Top = 25,
                Width = 50
            };
            grpSearch.Controls.Add(lblFromSlot);

            _cbBookingFromSlot = new ComboBox
            {
                Left = 270,
                Top = 20,
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpSearch.Controls.Add(_cbBookingFromSlot);

            var lblToSlot = new Label
            {
                Text = "Đến ca:",
                Left = 360,
                Top = 25,
                Width = 60
            };
            grpSearch.Controls.Add(lblToSlot);

            _cbBookingToSlot = new ComboBox
            {
                Left = 420,
                Top = 20,
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpSearch.Controls.Add(_cbBookingToSlot);

            // Sức chứa
            var lblCapacity = new Label
            {
                Text = "Sức chứa ≥",
                Left = 10,
                Top = 60,
                Width = 80
            };
            grpSearch.Controls.Add(lblCapacity);

            _numMinCapacity = new NumericUpDown
            {
                Left = 90,
                Top = 55,
                Width = 80,
                Minimum = 0,
                Maximum = 500,
                Value = 0
            };
            grpSearch.Controls.Add(_numMinCapacity);

            // Checkboxes thiết bị
            _chkNeedProjector = new CheckBox
            {
                Text = "Có máy chiếu",
                Left = 200,
                Top = 58,
                Width = 110
            };
            grpSearch.Controls.Add(_chkNeedProjector);

            _chkNeedPC = new CheckBox
            {
                Text = "Có máy tính",
                Left = 320,
                Top = 58,
                Width = 100
            };
            grpSearch.Controls.Add(_chkNeedPC);

            _chkNeedAC = new CheckBox
            {
                Text = "Có máy lạnh",
                Left = 440,
                Top = 58,
                Width = 100
            };
            grpSearch.Controls.Add(_chkNeedAC);

            // Building
            var lblBuilding = new Label
            {
                Text = "Tòa nhà:",
                Left = 10,
                Top = 95,
                Width = 60
            };
            grpSearch.Controls.Add(lblBuilding);

            _cbBuilding = new ComboBox
            {
                Left = 80,
                Top = 90,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpSearch.Controls.Add(_cbBuilding);

            // Nút Tìm phòng trống
            _btnSearchRooms = new Button
            {
                Text = "Tìm phòng trống",
                Left = 750,
                Top = 25,
                Width = 150,
                Height = 40
            };
            grpSearch.Controls.Add(_btnSearchRooms);
            _btnSearchRooms.Click += (s, e) =>
            {
                SearchRoomsDemo();   // tạm filter local, sau này đổi sang gọi server
            };

            // ===== Grid kết quả phòng =====
            _gridRooms = new DataGridView
            {
                Left = 10,
                Top = grpSearch.Bottom + 5,
                Width = 950,
                Height = 160,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _tabBooking.Controls.Add(_gridRooms);

            _gridRooms.Columns.Clear();
            _gridRooms.Columns.Add("RoomId", "Phòng");
            _gridRooms.Columns.Add("Building", "Tòa nhà");
            _gridRooms.Columns.Add("Capacity", "Sức chứa");
            _gridRooms.Columns.Add("Projector", "Projector");
            _gridRooms.Columns.Add("PC", "PC");
            _gridRooms.Columns.Add("AC", "AC");
            _gridRooms.Columns.Add("Mic", "Mic");
            _gridRooms.Columns.Add("Status", "Trạng thái");

            _gridRooms.DoubleClick += (s, e) =>
            {
                PrefillRequestFromSelectedRoom();
            };

            // ======== (B) Nhóm YÊU CẦU MƯỢN PHÒNG (dưới) ========
            _grpRequest = new GroupBox
            {
                Text = "Yêu cầu mượn phòng",
                Left = 10,
                Top = _gridRooms.Bottom + 5,
                Width = 650,
                Height = 200
            };
            _tabBooking.Controls.Add(_grpRequest);

            // Phòng
            var lblReqRoom = new Label
            {
                Text = "Phòng:",
                Left = 10,
                Top = 25,
                Width = 50
            };
            _grpRequest.Controls.Add(lblReqRoom);

            _cbReqRoom = new ComboBox
            {
                Left = 70,
                Top = 20,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _grpRequest.Controls.Add(_cbReqRoom);

            // Ngày
            var lblReqDate = new Label
            {
                Text = "Ngày:",
                Left = 210,
                Top = 25,
                Width = 50
            };
            _grpRequest.Controls.Add(lblReqDate);

            _dtReqDate = new DateTimePicker
            {
                Left = 260,
                Top = 20,
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            _grpRequest.Controls.Add(_dtReqDate);

            // Ca single
            var lblReqSlotSingle = new Label
            {
                Text = "Ca:",
                Left = 10,
                Top = 60,
                Width = 40
            };
            _grpRequest.Controls.Add(lblReqSlotSingle);

            _cbReqSlotSingle = new ComboBox
            {
                Left = 70,
                Top = 55,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _grpRequest.Controls.Add(_cbReqSlotSingle);

            // Range ca: từ – đến
            var lblReqRange = new Label
            {
                Text = "Range ca:",
                Left = 210,
                Top = 60,
                Width = 70
            };
            _grpRequest.Controls.Add(lblReqRange);

            _cbReqSlotFrom = new ComboBox
            {
                Left = 290,
                Top = 55,
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _grpRequest.Controls.Add(_cbReqSlotFrom);

            _cbReqSlotTo = new ComboBox
            {
                Left = 380,
                Top = 55,
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _grpRequest.Controls.Add(_cbReqSlotTo);

            // Lý do
            var lblPurpose = new Label
            {
                Text = "Lý do:",
                Left = 10,
                Top = 95,
                Width = 50
            };
            _grpRequest.Controls.Add(lblPurpose);

            _txtPurpose = new TextBox
            {
                Left = 70,
                Top = 90,
                Width = 390,
                Height = 60,
                Multiline = true
            };
            _grpRequest.Controls.Add(_txtPurpose);

            // Label thời gian ca
            _lblSlotTimeRange = new Label
            {
                Text = "Thời gian ca: -",
                Left = 10,
                Top = 160,
                Width = 450,
                ForeColor = Color.Gray
            };
            _grpRequest.Controls.Add(_lblSlotTimeRange);

            // Nút REQUEST / RELEASE
            _btnReqSingle = new Button
            {
                Text = "REQUEST (single)",
                Left = 470,
                Top = 25,
                Width = 150,
                Height = 30
            };
            _grpRequest.Controls.Add(_btnReqSingle);
            _btnReqSingle.Click += (s, e) => RequestSingleSlot();

            _btnReqRange = new Button
            {
                Text = "REQUEST RANGE",
                Left = 470,
                Top = 60,
                Width = 150,
                Height = 30
            };
            _grpRequest.Controls.Add(_btnReqRange);
            _btnReqRange.Click += (s, e) => RequestRangeSlot();

            _btnReleaseSingle = new Button
            {
                Text = "RELEASE(single)",
                Left = 470,
                Top = 95,
                Width = 150,
                Height = 30
            };
            _grpRequest.Controls.Add(_btnReleaseSingle);
            _btnReleaseSingle.Click += (s, e) => ReleaseSingleSlot();

            _btnReleaseRange = new Button
            {
                Text = "RELEASE RANGE",
                Left = 470,
                Top = 130,
                Width = 150,
                Height = 30
            };
            _grpRequest.Controls.Add(_btnReleaseRange);
            _btnReleaseRange.Click += (s, e) => ReleaseRangeSlot();

            // Status label to
            _lblRequestStatus = new Label
            {
                Text = "Chưa có yêu cầu.",
                Left = 10,
                Top = _grpRequest.Bottom + 5,
                Width = 800,
                ForeColor = Color.DarkGray,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };
            _tabBooking.Controls.Add(_lblRequestStatus);

            // Client log
            _grpClientLog = new GroupBox
            {
                Text = "Client log (debug)",
                Left = 10,
                Top = _lblRequestStatus.Bottom + 5,
                Width = 950,
                Height = 100
            };
            _tabBooking.Controls.Add(_grpClientLog);

            _txtClientLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _grpClientLog.Controls.Add(_txtClientLog);

            // ===== Nút quay về Trang chủ =====
            _btnBackToHome = new Button
            {
                Text = "← Về Trang chủ",
                Width = 120,
                Height = 30,
                Left = 840,   // toạ độ X cố định (bạn có thể chỉnh lại cho đẹp)
                Top = 850,   // đảm bảo > 200, nằm phía dưới tab Booking
                Anchor = AnchorStyles.Top | AnchorStyles.Left   // hoặc bỏ hẳn Anchor cũng được
            };
            _tabBooking.Controls.Add(_btnBackToHome);

            _btnBackToHome.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabHome;
            };
            // Khởi tạo dữ liệu cho các combobox, building, room...
            InitBookingTabData();
        }
        private void InitBookingTabData()
        {
            // Slot combobox: S1..S14
            var slotIds = Enumerable.Range(1, 14)
                .Select(i => $"S{i}")
                .ToList();

            _cbBookingFromSlot.Items.Clear();
            _cbBookingToSlot.Items.Clear();
            _cbReqSlotSingle.Items.Clear();
            _cbReqSlotFrom.Items.Clear();
            _cbReqSlotTo.Items.Clear();

            foreach (var s in slotIds)
            {
                _cbBookingFromSlot.Items.Add(s);
                _cbBookingToSlot.Items.Add(s);
                _cbReqSlotSingle.Items.Add(s);
                _cbReqSlotFrom.Items.Add(s);
                _cbReqSlotTo.Items.Add(s);
            }

            if (_cbBookingFromSlot.Items.Count > 0)
                _cbBookingFromSlot.SelectedIndex = 0;
            if (_cbBookingToSlot.Items.Count > 0)
                _cbBookingToSlot.SelectedIndex = _cbBookingToSlot.Items.Count - 1;
            if (_cbReqSlotSingle.Items.Count > 0)
                _cbReqSlotSingle.SelectedIndex = 2; // ví dụ default S3
            if (_cbReqSlotFrom.Items.Count > 0)
                _cbReqSlotFrom.SelectedIndex = 2;
            if (_cbReqSlotTo.Items.Count > 0)
                _cbReqSlotTo.SelectedIndex = 4;

            // Ngày default = hôm nay theo server offset
            var now = DateTime.Now + _serverTimeOffset;
            _dtBookingDate.Value = now.Date;
            _dtReqDate.Value = now.Date;

            // Building demo
            _cbBuilding.Items.Clear();
            _cbBuilding.Items.Add("ALL");
            _cbBuilding.Items.Add("CS1 - Tòa A");
            _cbBuilding.Items.Add("CS1 - Tòa B");
            _cbBuilding.SelectedIndex = 0;

            // Demo room list – sau này bạn có thể thay bằng data lấy từ server
            if (_allRoomsForSearch.Count == 0)
            {
                _allRoomsForSearch.Add(new RoomSearchRow
                {
                    RoomId = "A08",
                    Building = "CS1 - Tòa A",
                    Capacity = 60,
                    HasProjector = true,
                    HasPC = true,
                    HasAC = true,
                    HasMic = true,
                    Status = "FREE"
                });
                _allRoomsForSearch.Add(new RoomSearchRow
                {
                    RoomId = "A16",
                    Building = "CS1 - Tòa A",
                    Capacity = 80,
                    HasProjector = true,
                    HasPC = false,
                    HasAC = true,
                    HasMic = true,
                    Status = "FREE"
                });
                _allRoomsForSearch.Add(new RoomSearchRow
                {
                    RoomId = "B03",
                    Building = "CS1 - Tòa B",
                    Capacity = 40,
                    HasProjector = false,
                    HasPC = true,
                    HasAC = false,
                    HasMic = false,
                    Status = "FREE"
                });
            }

            // Fill combobox phòng từ danh sách rooms
            _cbReqRoom.Items.Clear();
            foreach (var r in _allRoomsForSearch)
            {
                _cbReqRoom.Items.Add(r.RoomId);
            }
            if (_cbReqRoom.Items.Count > 0)
                _cbReqRoom.SelectedIndex = 0;

            // Event đổi slot → cập nhật label thời gian ca
            _cbReqSlotSingle.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();
            _cbReqSlotFrom.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();
            _cbReqSlotTo.SelectedIndexChanged += (s, e) => UpdateSlotTimeLabel();

            UpdateSlotTimeLabel();
        }

        private void UpdateSlotTimeLabel()
        {
            string GetTime(string slotId)
            {
                return _slotTimeLookup.TryGetValue(slotId, out var t) ? t : "?";
            }

            var single = _cbReqSlotSingle.SelectedItem?.ToString();
            var from = _cbReqSlotFrom.SelectedItem?.ToString();
            var to = _cbReqSlotTo.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(single))
            {
                _lblSlotTimeRange.Text = $"Thời gian ca (single): {single} – {GetTime(single)}";
            }

            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                int sIdx = ParseSlotIndexSafe(from);
                int eIdx = ParseSlotIndexSafe(to);
                if (eIdx < sIdx)
                {
                    _lblSlotTimeRange.Text += " | Range ca không hợp lệ (End < Start)";
                }
                else
                {
                    var startTime = GetTime(from);
                    var endTime = GetTime(to);
                    _lblSlotTimeRange.Text += $" | Range ca: {from}–{to} ({startTime} → {endTime})";
                }
            }
        }

        private int ParseSlotIndexSafe(string? slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId)) return -1;
            if (slotId.StartsWith("S") && int.TryParse(slotId.Substring(1), out int idx))
                return idx;
            return -1;
        }

        // ================== 2.5. TAB "LỊCH CỦA TÔI" ==================
        private void BuildScheduleTabUi()
        {
            // ==== Thanh trên: Ngày / Tuần + DatePicker + Export + Back ====
            _radDayView = new RadioButton
            {
                Left = 10,
                Top = 100,
                Width = 80,
                Text = "Ngày",
                Checked = true
            };
            _radWeekView = new RadioButton
            {
                Left = 100,
                Top = 100,
                Width = 80,
                Text = "Tuần"
            };
            _tabSchedule.Controls.Add(_radDayView);
            _tabSchedule.Controls.Add(_radWeekView);

            var lblDate = new Label
            {
                Left = 200,
                Top = 100,
                Width = 60,
                Text = "Ngày:"
            };
            _dtScheduleDate = new DateTimePicker
            {
                Left = 260,
                Top = 100,
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            _tabSchedule.Controls.Add(lblDate);
            _tabSchedule.Controls.Add(_dtScheduleDate);

            _btnExportSchedule = new Button
            {
                Text = "Xuất file (PDF/Excel)",
                Left = 430,
                Top = 100,
                Width = 150
            };
            _tabSchedule.Controls.Add(_btnExportSchedule);

            // Nút quay lại Trang chủ
            _btnBackHomeFromSchedule = new Button
            {
                Text = "Về Trang chủ",
                Left = 600,
                Top = 100,
                Width = 120
            };
            _btnBackHomeFromSchedule.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabHome;
            };
            _tabSchedule.Controls.Add(_btnBackHomeFromSchedule);

            // ==== Day View: timetable dạng list ====
            _gridDayView = new DataGridView
            {
                Left = 10,
                Top = 140,
                Width = 950,
                Height = 250,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _tabSchedule.Controls.Add(_gridDayView);

            // Cột: Ca, Giờ, Phòng, Môn/Lý do, Trạng thái
            _gridDayView.Columns.Clear();
            _gridDayView.Columns.Add("Slot", "Ca");
            _gridDayView.Columns.Add("TimeRange", "Giờ");
            _gridDayView.Columns.Add("RoomId", "Phòng");
            _gridDayView.Columns.Add("Subject", "Môn / Lý do");
            _gridDayView.Columns.Add("Status", "Trạng thái");

            // ==== Week View: 7 cột (T2–CN) x 14 dòng (ca1–14) ====
            _gridWeekView = new DataGridView
            {
                Left = 10,
                Top = 140,
                Width = 950,
                Height = 250,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                RowHeadersVisible = true,   // hiển thị "Ca 1..14" ở row header
                ShowCellToolTips = true
            };
            _tabSchedule.Controls.Add(_gridWeekView);

            // Cột T2..CN (7 cột đúng yêu cầu)
            _gridWeekView.Columns.Clear();
            _gridWeekView.Columns.Add("Mon", "T2");
            _gridWeekView.Columns.Add("Tue", "T3");
            _gridWeekView.Columns.Add("Wed", "T4");
            _gridWeekView.Columns.Add("Thu", "T5");
            _gridWeekView.Columns.Add("Fri", "T6");
            _gridWeekView.Columns.Add("Sat", "T7");
            _gridWeekView.Columns.Add("Sun", "CN");

            // Tạo 14 dòng (Ca 1..14), hiển thị ở RowHeader
            _gridWeekView.Rows.Clear();
            for (int slot = 1; slot <= 14; slot++)
            {
                int rowIndex = _gridWeekView.Rows.Add();
                _gridWeekView.Rows[rowIndex].HeaderCell.Value = "Ca " + slot;
            }

            // ==== Gắn event ====
            _radDayView.CheckedChanged += (s, e) => UpdateScheduleViewMode();
            _radWeekView.CheckedChanged += (s, e) => UpdateScheduleViewMode();

            _dtScheduleDate.ValueChanged += (s, e) =>
            {
                ReloadScheduleForSelectedDate();
            };

            _btnExportSchedule.Click += (s, e) =>
            {
                ExportCurrentSchedule();
            };

            // Set ngày mặc định: hôm nay (có offset server nếu bạn đã tính _serverTimeOffset)
            var now = DateTime.Now + _serverTimeOffset;
            _dtScheduleDate.Value = now.Date;

            // Mặc định hiển thị Day View
            UpdateScheduleViewMode();

            // TODO: sau này khi làm xong phần GET_MY_SCHEDULE thì ở đây sẽ load lịch thật
            // ReloadScheduleForSelectedDate();
        }
        private void ReloadScheduleForSelectedDate()
        {
            // fire-and-forget gọi hàm async
            _ = ReloadScheduleFromServerAsync();
        }

        // Chuyển giữa Day view / Week view
        private void UpdateScheduleViewMode()
        {
            if (_radDayView == null || _gridDayView == null || _gridWeekView == null)
                return;

            if (_radDayView.Checked)
            {
                _gridDayView.Visible = true;
                _gridWeekView.Visible = false;
            }
            else
            {
                _gridDayView.Visible = false;
                _gridWeekView.Visible = true;
            }
        }

        // Tính thứ 2 của tuần chứa ngày d
        private DateTime GetMondayOfWeek(DateTime d)
        {
            // DayOfWeek: Monday=1, Sunday=0
            int diff = (int)d.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return d.Date.AddDays(-diff);
        }

        // Tạo time range cho ca 
        private string GetTimeRangeForSlot(int slot)
        {
            var slotId = $"S{slot}";
            if (_slotTimeLookup.TryGetValue(slotId, out var range))
                return range;

            // fallback nếu vì lý do gì đó chưa có config
            var start = new TimeSpan(7 + (slot - 1), 0, 0);
            var end = start.Add(TimeSpan.FromHours(1));
            return $"{start:hh\\:mm}–{end:hh\\:mm}";
        }


        // Sinh dữ liệu DEMO cho 1 tuần xung quanh ngày được chọn
        private List<MyScheduleItem> BuildDemoScheduleForWeek(DateTime anyDayInWeek)
        {
            var list = new List<MyScheduleItem>();
            var monday = GetMondayOfWeek(anyDayInWeek);

            if (_currentUser.UserType == "Student")
            {
                // Ví dụ lịch học cố định trong tuần
                list.Add(new MyScheduleItem
                {
                    Date = monday,            // Thứ 2
                    Slot = 1,
                    TimeRange = GetTimeRangeForSlot(1),
                    RoomId = "A08",
                    Subject = "CTDL & GT",
                    Status = "APPROVED",
                    Note = "Học lý thuyết"
                });
                list.Add(new MyScheduleItem
                {
                    Date = monday.AddDays(2), // Thứ 4
                    Slot = 3,
                    TimeRange = GetTimeRangeForSlot(3),
                    RoomId = "A16",
                    Subject = ".NET Programming",
                    Status = "IN_USE",
                    Note = "Thực hành"
                });
                list.Add(new MyScheduleItem
                {
                    Date = monday.AddDays(4), // Thứ 6
                    Slot = 5,
                    TimeRange = GetTimeRangeForSlot(5),
                    RoomId = "B05",
                    Subject = "Project meeting",
                    Status = "NO_SHOW",
                    Note = "Demo trạng thái NO_SHOW"
                });
            }
            else
            {
                // Lecturer / Staff: ví dụ vài booking phòng
                list.Add(new MyScheduleItem
                {
                    Date = monday,            // T2
                    Slot = 2,
                    TimeRange = GetTimeRangeForSlot(2),
                    RoomId = "A08",
                    Subject = "Họp nhóm đồ án",
                    Status = "APPROVED",
                    Note = "Book phòng họp"
                });
                list.Add(new MyScheduleItem
                {
                    Date = monday.AddDays(1), // T3
                    Slot = 4,
                    TimeRange = GetTimeRangeForSlot(4),
                    RoomId = "A16",
                    Subject = "Seminar Khoa CNTT",
                    Status = "IN_USE",
                    Note = ""
                });
                list.Add(new MyScheduleItem
                {
                    Date = monday.AddDays(3), // T5
                    Slot = 6,
                    TimeRange = GetTimeRangeForSlot(6),
                    RoomId = "B02",
                    Subject = "Lecture OOP",
                    Status = "COMPLETED",
                    Note = ""
                });
                list.Add(new MyScheduleItem
                {
                    Date = monday.AddDays(5), // T7
                    Slot = 3,
                    TimeRange = GetTimeRangeForSlot(3),
                    RoomId = "C01",
                    Subject = "Workshop",
                    Status = "NO_SHOW",
                    Note = "Demo NO_SHOW"
                });
            }

            return list;
        }

        // Load lại dữ liệu khi chọn ngày
        private async Task ReloadScheduleFromServerAsync()
        {
            try
            {
                var selectedDate = _dtScheduleDate.Value.Date;
                var monday = GetMondayOfWeek(selectedDate);
                var sunday = monday.AddDays(6);

                // Gọi hàm đã có, tự mở TCP + parse
                var weekItems = await LoadMyScheduleFromServerAsync(monday, sunday);

                // Đổ dữ liệu
                FillDayView(selectedDate, weekItems);
                FillWeekView(selectedDate, weekItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không lấy được lịch từ server: " + ex.Message,
                    "Schedule",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                _gridDayView.Rows.Clear();
                _gridWeekView.Rows.Clear();

                for (int slot = 1; slot <= 14; slot++)
                {
                    int rowIndex = _gridWeekView.Rows.Add();
                    _gridWeekView.Rows[rowIndex].HeaderCell.Value = "Ca " + slot;
                }
            }
        }



        // Lấy thứ 2 của tuần chứa ngày d
        // private DateTime GetMondayOfWeek(DateTime d)
        // {
        //     int diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        //     return d.Date.AddDays(-diff);
        // }

        // Gọi server lấy lịch của user trong khoảng ngày
        private async Task<List<MyScheduleItem>> LoadMyScheduleFromServerAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new List<MyScheduleItem>();

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var userId = _currentUser?.UserId ?? "";
                        if (string.IsNullOrWhiteSpace(userId))
                            return result;

                        // Gửi lệnh: GET_MY_SCHEDULE|UserId|FromDate|ToDate
                        var cmd = $"GET_MY_SCHEDULE|{userId}|{fromDate:yyyy-MM-dd}|{toDate:yyyy-MM-dd}";
                        await writer.WriteLineAsync(cmd);

                        bool started = false;
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            line = line.Trim();
                            if (line.Length == 0) continue;

                            if (line == "MY_SCHEDULE_BEGIN")
                            {
                                started = true;
                                continue;
                            }
                            if (line == "MY_SCHEDULE_END")
                                break;

                            if (!started) continue;
                            if (!line.StartsWith("ITEM|")) continue;

                            var parts = line.Split('|');
                            if (parts.Length < 8) continue;

                            var dateStr = parts[1];
                            var roomId = parts[2];
                            var slotStartId = parts[3]; // ví dụ S3
                            var slotEndId = parts[4];
                            var timeRange = parts[5];
                            var status = parts[6];
                            var purpose = parts[7];

                            if (!DateTime.TryParse(dateStr, out var date))
                                continue;

                            var slotIndex = ParseSlotIndexSafe(slotStartId); // bạn đã có hàm này
                            if (slotIndex <= 0) continue;

                            result.Add(new MyScheduleItem
                            {
                                Date = date.Date,
                                Slot = slotIndex,
                                TimeRange = timeRange,
                                RoomId = roomId,
                                Subject = purpose,
                                Status = status,
                                Note = "" // nếu muốn có ghi chú riêng thì thêm field nữa trong protocol
                            });
                        }
                    }
                }

                AppendClientLog("[INFO] Loaded my schedule from server.");
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] LoadMyScheduleFromServerAsync: " + ex.Message);
                // để result rỗng, sẽ fallback sang demo
            }

            return result;
        }

        // Đổ dữ liệu cho Day View
        private void FillDayView(DateTime selectedDate, List<MyScheduleItem> weekItems)
        {
            _gridDayView.Rows.Clear();

            foreach (var item in weekItems)
            {
                if (item.Date.Date != selectedDate)
                    continue;

                _gridDayView.Rows.Add(
                    "Ca " + item.Slot,
                    item.TimeRange,
                    item.RoomId,
                    item.Subject,
                    item.Status
                );
            }
        }

        // Đổ dữ liệu cho Week View + tô màu + tooltip
        private void FillWeekView(DateTime selectedDate, List<MyScheduleItem> weekItems)
        {
            var monday = GetMondayOfWeek(selectedDate);

            // Xóa dữ liệu cũ nhưng giữ cấu trúc cột
            _gridWeekView.Rows.Clear();
            for (int slot = 1; slot <= 14; slot++)
            {
                int rowIndex = _gridWeekView.Rows.Add();
                _gridWeekView.Rows[rowIndex].HeaderCell.Value = "Ca " + slot;
            }

            foreach (var item in weekItems)
            {
                int dayOffset = (item.Date.Date - monday).Days;
                if (dayOffset < 0 || dayOffset > 6)
                    continue; // ngoài tuần

                int rowIndex = item.Slot - 1;
                if (rowIndex < 0 || rowIndex >= _gridWeekView.Rows.Count)
                    continue;

                var cell = _gridWeekView.Rows[rowIndex].Cells[dayOffset];

                // Nội dung ô: Phòng + môn
                cell.Value = $"{item.RoomId} - {item.Subject}";

                // Tooltip: phòng, ghi chú, trạng thái
                cell.ToolTipText =
                    $"Phòng: {item.RoomId}\n" +
                    $"Môn/Lý do: {item.Subject}\n" +
                    $"Trạng thái: {item.Status}\n" +
                    $"Ghi chú: {item.Note}";

                // Tô màu theo trạng thái
                if (string.Equals(item.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.BackColor = Color.Khaki; // vàng
                }
                else if (string.Equals(item.Status, "IN_USE", StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.BackColor = Color.LightGreen; // xanh
                }
                else if (string.Equals(item.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.BackColor = Color.LightGray; // xám
                }
                else if (string.Equals(item.Status, "NO_SHOW", StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.BackColor = Color.LightCoral; // đỏ nhạt
                }
            }
        }
        // Export theo mode đang chọn
        private void ExportCurrentSchedule()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Export my schedule";
                dlg.Filter = "Excel CSV (*.csv)|*.csv|PDF (text only) (*.pdf)|*.pdf";
                dlg.FileName = $"{_currentUser.UserId}_Schedule_{_dtScheduleDate.Value:yyyyMMdd}";

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                var filePath = dlg.FileName;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                try
                {
                    if (_radDayView.Checked)
                    {
                        ExportDayScheduleToFile(filePath);
                    }
                    else
                    {
                        ExportWeekScheduleToFile(filePath);
                    }

                    MessageBox.Show("Export lịch thành công.", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export lỗi: " + ex.Message, "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void ExportDayScheduleToFile(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Header
                writer.WriteLine("Ca,Giờ,Phòng,Môn/Lý do,Trạng thái");

                foreach (DataGridViewRow row in _gridDayView.Rows)
                {
                    if (row.IsNewRow) continue;

                    var ca = row.Cells["Slot"].Value?.ToString() ?? "";
                    var time = row.Cells["TimeRange"].Value?.ToString() ?? "";
                    var room = row.Cells["RoomId"].Value?.ToString() ?? "";
                    var subject = row.Cells["Subject"].Value?.ToString() ?? "";
                    var status = row.Cells["Status"].Value?.ToString() ?? "";

                    // CSV đơn giản, nếu muốn có thể escape dấu phẩy
                    writer.WriteLine($"{ca},{time},{room},{subject},{status}");
                }
            }
        }


        // Export Day View (CSV cho Excel, PDF chỉ demo)
        private void ExportDayScheduleToFile()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất lịch ngày";
                dlg.Filter = "Excel CSV (*.csv)|*.csv|PDF file (*.pdf)|*.pdf";
                dlg.FileName = "MySchedule_Day_" + _dtScheduleDate.Value.ToString("yyyyMMdd");

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (ext == ".csv")
                {
                    using (var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    {
                        // Header
                        sw.WriteLine("Ca;Giờ;Phòng;Môn/Lý do;Trạng thái");

                        foreach (DataGridViewRow row in _gridDayView.Rows)
                        {
                            if (row.IsNewRow) continue;
                            var slot = row.Cells["Slot"].Value?.ToString() ?? "";
                            var time = row.Cells["TimeRange"].Value?.ToString() ?? "";
                            var room = row.Cells["RoomId"].Value?.ToString() ?? "";
                            var subject = row.Cells["Subject"].Value?.ToString() ?? "";
                            var status = row.Cells["Status"].Value?.ToString() ?? "";

                            sw.WriteLine(
                                $"{slot};{time};{room};{subject};{status}"
                            );
                        }
                    }

                    MessageBox.Show(
                        "Đã xuất lịch ngày ra file CSV. Bạn có thể mở bằng Excel.",
                        "Export schedule",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (ext == ".pdf")
                {
                    MessageBox.Show(
                        "Hiện tại chưa implement export PDF. Tạm thời hãy dùng định dạng CSV để mở bằng Excel.",
                        "Export PDF",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        // Export Week View (chỉ CSV để Excel mở dạng bảng T2..CN x Ca)
        private void ExportWeekScheduleToFile(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Header: Ca,T2,T3,...,CN
                writer.WriteLine("Ca,T2,T3,T4,T5,T6,T7,CN");

                foreach (DataGridViewRow row in _gridWeekView.Rows)
                {
                    if (row.IsNewRow) continue;

                    var ca = row.HeaderCell.Value?.ToString() ?? "";

                    var values = new List<string> { ca };

                    for (int col = 0; col < _gridWeekView.Columns.Count; col++)
                    {
                        var cell = row.Cells[col];
                        var room = cell.Value?.ToString() ?? "";
                        values.Add(room);
                    }

                    writer.WriteLine(string.Join(",", values));
                }
            }
        }

        // ================== 2.6. TAB THÔNG BÁO ==================
        private void BuildNotificationsTabUi()
        {
            var lblType = new Label { Left = 10, Top = 15, Width = 60, Text = "Loại:" };
            _cbFilterType = new ComboBox
            {
                Left = 70,
                Top = 12,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbFilterType.Items.AddRange(new object[]
            {
                "Tất cả", "Grant", "ChangeRoom", "NoShow", "Reminder"
            });
            _cbFilterType.SelectedIndex = 0;

            var lblFrom = new Label { Left = 240, Top = 15, Width = 40, Text = "Từ:" };
            _dtNotiFrom = new DateTimePicker
            {
                Left = 280,
                Top = 12,
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            var lblTo = new Label { Left = 410, Top = 15, Width = 30, Text = "Đến:" };
            _dtNotiTo = new DateTimePicker
            {
                Left = 440,
                Top = 12,
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };

            _btnMarkAllRead = new Button
            {
                Text = "Đánh dấu đã đọc tất cả",
                Left = 580,
                Top = 10,
                Width = 180
            };

            _tabNotifications.Controls.AddRange(new Control[]
            {
                lblType, _cbFilterType,
                lblFrom, _dtNotiFrom,
                lblTo, _dtNotiTo,
                _btnMarkAllRead
            });

            _gridNotifications = new DataGridView
            {
                Left = 10,
                Top = 45,
                Width = 950,
                Height = 500,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _tabNotifications.Controls.Add(_gridNotifications);
        }

        // ================== 2.7. TAB TÀI KHOẢN ==================
        private void BuildAccountTabUi()
        {
            // ===== Thông tin tài khoản =====
            var lblName = new Label { Left = 10, Top = 105, Width = 100, Text = "Họ tên:" };
            _txtAccFullName = new TextBox { Left = 120, Top = 105, Width = 300, ReadOnly = true };

            var lblId = new Label { Left = 10, Top = 135, Width = 100, Text = "MSSV/Mã GV:" };
            _txtAccStudentLecturerId = new TextBox { Left = 120, Top = 135, Width = 300, ReadOnly = true };

            var lblClassFac = new Label { Left = 10, Top = 165, Width = 100, Text = "Lớp / Khoa:" };
            _txtAccClassFaculty = new TextBox { Left = 120, Top = 165, Width = 300, ReadOnly = true };

            var lblDept = new Label { Left = 10, Top = 195, Width = 100, Text = "Bộ môn / Khoa:" };
            _txtAccDepartment = new TextBox { Left = 120, Top = 195, Width = 300, ReadOnly = true };

            var lblEmail = new Label { Left = 10, Top = 225, Width = 100, Text = "Email:" };
            _txtAccEmail = new TextBox { Left = 120, Top = 225, Width = 300 };

            var lblPhone = new Label { Left = 10, Top = 255, Width = 100, Text = "Phone:" };
            _txtAccPhone = new TextBox { Left = 120, Top = 255, Width = 300 };

            _btnUpdateContact = new Button
            {
                Text = "Lưu liên hệ",
                Left = 120,
                Top = 285,
                Width = 120
            };

            // ===== Nhóm đổi mật khẩu =====
            var lblOldPwd = new Label { Left = 10, Top = 315, Width = 100, Text = "Mật khẩu cũ:" };
            _txtOldPassword = new TextBox
            {
                Left = 120,
                Top = 315,
                Width = 300,
                UseSystemPasswordChar = true
            };

            var lblNewPwd = new Label { Left = 10, Top = 345, Width = 100, Text = "Mật khẩu mới:" };
            _txtNewPassword = new TextBox
            {
                Left = 120,
                Top = 345,
                Width = 300,
                UseSystemPasswordChar = true
            };

            var lblConfirmPwd = new Label { Left = 10, Top = 375, Width = 100, Text = "Nhập lại:" };
            _txtConfirmPassword = new TextBox
            {
                Left = 120,
                Top = 375,
                Width = 300,
                UseSystemPasswordChar = true
            };

            _btnChangePassword = new Button
            {
                Text = "Đổi mật khẩu",
                Left = 120,
                Top = 405,
                Width = 120
            };

            // ===== Check connect trong tab Account =====
            _btnAccCheckConnect = new Button
            {
                Text = "Check connect",
                Left = 500,
                Top = 105,
                Width = 120
            };

            _pnlAccConnectDot = new Panel
            {
                Left = 630,
                Top = 110,
                Width = 14,
                Height = 14,
                BackColor = Color.Red,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblAccConnectText = new Label
            {
                Left = 650,
                Top = 108,
                Width = 80,
                Text = "Lost",
                ForeColor = Color.Red
            };

            // ===== Nút Logout =====
            _btnLogout = new Button
            {
                Text = "Đăng xuất",
                Left = 800,
                Top = 105,
                Width = 120
            };
            _btnLogout.Click += (s, e) =>
            {
                // TODO: tùy bạn, tạm thời đóng form
                Close();
            };

            // ===== Nút TRỞ VỀ TRANG CHỦ (nút Back riêng) =====
            var btnBackToHome = new Button
            {
                Text = "Trở về trang chủ",
                Left = 800,
                Top = 145,
                Width = 120
            };
            btnBackToHome.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabHome;
            };

            // Add control vào tab
            _tabAccount.Controls.AddRange(new Control[]
            {
        lblName, _txtAccFullName,
        lblId, _txtAccStudentLecturerId,
        lblClassFac, _txtAccClassFaculty,
        lblDept, _txtAccDepartment,
        lblEmail, _txtAccEmail,
        lblPhone, _txtAccPhone,
        _btnUpdateContact,
        lblOldPwd, _txtOldPassword,
        lblNewPwd, _txtNewPassword,
        lblConfirmPwd, _txtConfirmPassword,
        _btnChangePassword,
        // _btnAccCheckConnect, _pnlAccConnectDot, _lblAccConnectText,
        // _btnLogout,
        btnBackToHome          // dùng biến local, không đụng tới _btnBackToHome ở tab khác
            });

            // ===== Fill info từ _currentUser =====
            _txtAccFullName.Text = _currentUser.FullName;
            _txtAccEmail.Text = _currentUser.Email ?? "";
            _txtAccPhone.Text = _currentUser.Phone ?? "";

            if (_currentUser.UserType == "Student")
            {
                _txtAccStudentLecturerId.Text = _currentUser.StudentId;
                _txtAccClassFaculty.Text = _currentUser.Class;
                _txtAccDepartment.Text = _currentUser.Department;
            }
            else if (_currentUser.UserType == "Lecturer")
            {
                _txtAccStudentLecturerId.Text = _currentUser.LecturerId;
                _txtAccClassFaculty.Text = _currentUser.Faculty;
                _txtAccDepartment.Text = "";
            }
            else
            {
                _txtAccStudentLecturerId.Text = _currentUser.UserId;
                _txtAccClassFaculty.Text = _currentUser.Department;
                _txtAccDepartment.Text = "";
            }

            // ===== Gắn event handler =====
            _btnAccCheckConnect.Click += async (s, e) =>
            {
                await AccountCheckConnectAsync();
            };

            _btnUpdateContact.Click += async (s, e) =>
            {
                await UpdateContactAsync();
            };

            _btnChangePassword.Click += async (s, e) =>
            {
                await ChangePasswordAsync();
            };
        }

        private void RefreshHeaderSubInfo()
        {
            if (_lblSubInfo == null) return;

            string subText;
            if (_currentUser.UserType == "Student")
            {
                subText =
                    $"StudentId: {_currentUser.StudentId} - Class: {_currentUser.Class} - Department: {_currentUser.Department}";
            }
            else if (_currentUser.UserType == "Lecturer")
            {
                subText =
                    $"LecturerId: {_currentUser.LecturerId} - Faculty: {_currentUser.Faculty}";
            }
            else
            {
                subText = $"Staff - Department: {_currentUser.Department}";
            }

            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_currentUser.Email))
                contactParts.Add($"Email: {_currentUser.Email}");
            if (!string.IsNullOrWhiteSpace(_currentUser.Phone))
                contactParts.Add($"Phone: {_currentUser.Phone}");

            if (contactParts.Count > 0)
            {
                subText += " | " + string.Join(" - ", contactParts);
            }

            _lblSubInfo.Text = subText;
        }
        private async Task UpdateContactAsync()
        {
            var email = _txtAccEmail.Text.Trim();
            var phone = _txtAccPhone.Text.Trim();

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Email hoặc Phone phải có ít nhất một giá trị.", "Account",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var cmd = $"UPDATE_CONTACT|{_currentUser.UserId}|{email}|{phone}\n";
                        var data = Encoding.UTF8.GetBytes(cmd);
                        await stream.WriteAsync(data, 0, data.Length);

                        var resp = await reader.ReadLineAsync();
                        if (resp == null)
                        {
                            MessageBox.Show("Không nhận được phản hồi từ server.", "Account",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (resp.StartsWith("OK"))
                        {
                            // Cập nhật local
                            _currentUser.Email = email;
                            _currentUser.Phone = phone;
                            RefreshHeaderSubInfo();

                            MessageBox.Show("Cập nhật thông tin liên hệ thành công.", "Account",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (resp.StartsWith("ERR|"))
                        {
                            var msg = resp.Substring("ERR|".Length);
                            MessageBox.Show(msg, "Account",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Phản hồi không hợp lệ từ server: " + resp, "Account",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật thông tin liên hệ:\n" + ex.Message,
                    "Account", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task ChangePasswordAsync()
        {
            var oldPwd = _txtOldPassword.Text;
            var newPwd = _txtNewPassword.Text;
            var confirm = _txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(oldPwd) ||
                string.IsNullOrWhiteSpace(newPwd) ||
                string.IsNullOrWhiteSpace(confirm))
            {
                MessageBox.Show("Vui lòng nhập đủ mật khẩu cũ, mật khẩu mới và xác nhận.", "Đổi mật khẩu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPwd != confirm)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận không khớp.", "Đổi mật khẩu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPwd.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự.", "Đổi mật khẩu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var cmd = $"CHANGE_PASSWORD|{_currentUser.UserId}|{oldPwd}|{newPwd}\n";
                        var data = Encoding.UTF8.GetBytes(cmd);
                        await stream.WriteAsync(data, 0, data.Length);

                        var resp = await reader.ReadLineAsync();
                        if (resp == null)
                        {
                            MessageBox.Show("Không nhận được phản hồi từ server.", "Đổi mật khẩu",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (resp.StartsWith("OK"))
                        {
                            MessageBox.Show("Đổi mật khẩu thành công.", "Đổi mật khẩu",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            _txtOldPassword.Text = "";
                            _txtNewPassword.Text = "";
                            _txtConfirmPassword.Text = "";
                        }
                        else if (resp.StartsWith("ERR|"))
                        {
                            var msg = resp.Substring("ERR|".Length);
                            MessageBox.Show(msg, "Đổi mật khẩu",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Phản hồi không hợp lệ từ server: " + resp, "Đổi mật khẩu",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đổi mật khẩu:\n" + ex.Message,
                    "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchRoomsDemo()
        {
            if (_allRoomsForSearch.Count == 0)
                InitBookingTabData();

            int minCap = (int)_numMinCapacity.Value;
            bool needProj = _chkNeedProjector.Checked;
            bool needPc = _chkNeedPC.Checked;
            bool needAc = _chkNeedAC.Checked;
            string building = _cbBuilding.SelectedItem?.ToString() ?? "ALL";

            var filtered = _allRoomsForSearch.Where(r =>
                r.Capacity >= minCap &&
                (!needProj || r.HasProjector) &&
                (!needPc || r.HasPC) &&
                (!needAc || r.HasAC) &&
                (building == "ALL" || r.Building == building)
            ).ToList();

            _gridRooms.Rows.Clear();
            foreach (var r in filtered)
            {
                _gridRooms.Rows.Add(
                    r.RoomId,
                    r.Building,
                    r.Capacity,
                    r.HasProjector ? "✓" : "",
                    r.HasPC ? "✓" : "",
                    r.HasAC ? "✓" : "",
                    r.HasMic ? "✓" : "",
                    r.Status
                );
            }

            AppendClientLog($"Search rooms: found {filtered.Count} rooms.");
        }
        private async Task AccountCheckConnectAsync()
        {
            var ok = await HeaderCheckConnectAsync();

            if (ok)
            {
                _pnlAccConnectDot.BackColor = Color.LimeGreen;
                _lblAccConnectText.Text = "Connected";
                _lblAccConnectText.ForeColor = Color.Green;
            }
            else
            {
                _pnlAccConnectDot.BackColor = Color.Red;
                _lblAccConnectText.Text = "Lost";
                _lblAccConnectText.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// Double click 1 dòng → prefill group “Yêu cầu mượn phòng”
        /// </summary>
        private void PrefillRequestFromSelectedRoom()
        {
            if (_gridRooms.CurrentRow == null)
                return;

            var roomId = _gridRooms.CurrentRow.Cells["RoomId"].Value?.ToString();
            if (string.IsNullOrEmpty(roomId))
                return;

            // Chọn phòng tương ứng
            if (_cbReqRoom.Items.Contains(roomId))
                _cbReqRoom.SelectedItem = roomId;

            // Đồng bộ ngày và ca với phần filter trên
            _dtReqDate.Value = _dtBookingDate.Value;

            if (_cbBookingFromSlot.SelectedItem != null)
                _cbReqSlotFrom.SelectedItem = _cbBookingFromSlot.SelectedItem;

            if (_cbBookingToSlot.SelectedItem != null)
                _cbReqSlotTo.SelectedItem = _cbBookingToSlot.SelectedItem;

            // Single slot default là from
            if (_cbReqSlotFrom.SelectedItem != null)
                _cbReqSlotSingle.SelectedItem = _cbReqSlotFrom.SelectedItem;

            UpdateSlotTimeLabel();
            AppendClientLog($"Prefill request from room {roomId}.");
        }
        private void AppendClientLog(string message)
        {
            if (_txtClientLog == null) return;
            _txtClientLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        private async void RequestSingleSlot()
        {
            var roomId = _cbReqRoom.SelectedItem?.ToString();
            var date = _dtReqDate.Value.Date; // hiện tại server vẫn dùng _currentDateKey, date client chỉ để hiển thị
            var slot = _cbReqSlotSingle.SelectedItem?.ToString();
            var purpose = _txtPurpose.Text.Trim();

            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slot))
            {
                MessageBox.Show("Vui lòng chọn phòng và ca.", "Request", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(purpose))
            {
                MessageBox.Show("Vui lòng nhập lý do mượn phòng.", "Request", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // Server hiện tại CHƯA nhận purpose, nên tạm thời chỉ gửi 4 tham số
                        var msg = $"REQUEST|{_currentUser.UserId}|{roomId}|{slot}";
                        AppendClientLog("[SEND] " + msg);
                        await writer.WriteLineAsync(msg);

                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            MessageBox.Show("No response from server.", "Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        AppendClientLog("[RECV] " + line);

                        var parts = line.Split('|');
                        if (parts[0] == "GRANT")
                        {
                            // GRANT|RoomId|SlotId|Position? (tuỳ bạn implement)
                            _lblRequestStatus.ForeColor = Color.Green;
                            _lblRequestStatus.Text = $"ĐÃ ĐƯỢC GRANT: Phòng {roomId} – {slot}, ngày {date:dd/MM/yyyy}.";
                            MessageBox.Show("Yêu cầu của bạn đã được GRANT.", "Request", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (parts[0] == "INFO" && parts.Length >= 3 && parts[1] == "QUEUED")
                        {
                            _lblRequestStatus.ForeColor = Color.DarkOrange;
                            _lblRequestStatus.Text = $"Đang chờ: Phòng {roomId} – {slot} (vị trí {parts[2]} trong hàng đợi).";
                        }
                        else if (parts[0] == "INFO" && parts.Length >= 3 && parts[1] == "ERROR")
                        {
                            _lblRequestStatus.ForeColor = Color.Red;
                            _lblRequestStatus.Text = "ERROR: " + parts[2];
                            MessageBox.Show("Error: " + parts[2], "Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            _lblRequestStatus.ForeColor = Color.Black;
                            _lblRequestStatus.Text = "Response: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] RequestSingleSlot: " + ex.Message);
                MessageBox.Show("Request failed: " + ex.Message, "Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RequestRangeSlot()
        {
            var roomId = _cbReqRoom.SelectedItem?.ToString();
            var date = _dtReqDate.Value.Date;
            var slotFrom = _cbReqSlotFrom.SelectedItem?.ToString();
            var slotTo = _cbReqSlotTo.SelectedItem?.ToString();
            var purpose = _txtPurpose.Text.Trim();

            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotFrom) || string.IsNullOrEmpty(slotTo))
            {
                MessageBox.Show("Vui lòng chọn đầy đủ phòng và range ca.", "Request range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int sIdx = ParseSlotIndexSafe(slotFrom);
            int eIdx = ParseSlotIndexSafe(slotTo);
            if (eIdx < sIdx)
            {
                MessageBox.Show("SlotEnd phải ≥ SlotStart.", "Request range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(purpose))
            {
                MessageBox.Show("Vui lòng nhập lý do mượn phòng.", "Request range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var msg = $"REQUEST_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}";
                        AppendClientLog("[SEND] " + msg);
                        await writer.WriteLineAsync(msg);

                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            MessageBox.Show("No response from server.", "Request range", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        AppendClientLog("[RECV] " + line);

                        var parts = line.Split('|');
                        if (parts[0] == "GRANT_RANGE")
                        {
                            _lblRequestStatus.ForeColor = Color.DarkGreen;
                            _lblRequestStatus.Text = $"ĐÃ ĐƯỢC GRANT RANGE: Phòng {roomId} – {slotFrom} đến {slotTo}, ngày {date:dd/MM/yyyy}.";
                        }
                        else if (parts[0] == "INFO" && parts.Length >= 3 && parts[1] == "QUEUED")
                        {
                            _lblRequestStatus.ForeColor = Color.DarkOrange;
                            _lblRequestStatus.Text = $"Đang chờ (range): Phòng {roomId} – {slotFrom}-{slotTo} (vị trí {parts[2]}).";
                        }
                        else if (parts[0] == "INFO" && parts.Length >= 3 && parts[1] == "ERROR")
                        {
                            _lblRequestStatus.ForeColor = Color.Red;
                            _lblRequestStatus.Text = "ERROR: " + parts[2];
                            MessageBox.Show("Error: " + parts[2], "Request range", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            _lblRequestStatus.ForeColor = Color.Black;
                            _lblRequestStatus.Text = "Response: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] RequestRangeSlot: " + ex.Message);
                MessageBox.Show("Request range failed: " + ex.Message, "Request range", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void ReleaseSingleSlot()
        {
            var roomId = _cbReqRoom.SelectedItem?.ToString();
            var date = _dtReqDate.Value.Date;
            var slot = _cbReqSlotSingle.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slot))
            {
                MessageBox.Show("Vui lòng chọn phòng và ca.", "Release", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentUser == null)
            {
                MessageBox.Show("Chưa có thông tin user.", "Release", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // RELEASE|UserId|RoomId|SlotId
                        var msg = $"RELEASE|{_currentUser.UserId}|{roomId}|{slot}";
                        AppendClientLog("[SEND] " + msg);
                        await writer.WriteLineAsync(msg);

                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            MessageBox.Show("Không nhận được phản hồi từ server.", "Release", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        AppendClientLog("[RECV] " + line);

                        var parts = line.Split('|');
                        if (parts.Length >= 2 && parts[0] == "INFO")
                        {
                            if (parts[1] == "RELEASED")
                            {
                                _lblRequestStatus.ForeColor = Color.DarkGray;
                                _lblRequestStatus.Text =
                                    $"Đã RELEASE: {roomId} – {slot} ngày {date:dd/MM/yyyy}.";
                            }
                            else if (parts[1] == "ERROR" && parts.Length >= 3)
                            {
                                _lblRequestStatus.ForeColor = Color.Red;
                                _lblRequestStatus.Text = "Release lỗi: " + parts[2];
                                MessageBox.Show("Release lỗi: " + parts[2],
                                    "Release", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                _lblRequestStatus.ForeColor = Color.Black;
                                _lblRequestStatus.Text = "Response: " + line;
                            }
                        }
                        else
                        {
                            _lblRequestStatus.ForeColor = Color.Black;
                            _lblRequestStatus.Text = "Response: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] ReleaseSingleSlot: " + ex.Message);
                MessageBox.Show("Release failed: " + ex.Message,
                    "Release", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task ExportCurrentScheduleAsync(string type)
        {
            // Gom data theo mode hiện tại
            var selectedDate = _dtScheduleDate.Value.Date;
            var weekMonday = GetMondayOfWeek(selectedDate);
            var weekSunday = weekMonday.AddDays(6);

            var dataForWeek = await LoadMyScheduleFromServerAsync(weekMonday, weekSunday);
            if (dataForWeek == null || dataForWeek.Count == 0)
            {
                dataForWeek = BuildDemoScheduleForWeek(selectedDate);
            }

            if (type == "Excel")
            {
                // Ở đây bạn dùng SaveFileDialog + ghi CSV (Excel mở được)
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "Excel (CSV)|*.csv";
                    dlg.FileName = $"MySchedule_{weekMonday:yyyyMMdd}_{weekSunday:yyyyMMdd}.csv";

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        using (var writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                        {
                            writer.WriteLine("Date,Slot,TimeRange,Room,Subject,Status,Note");
                            foreach (var item in dataForWeek.OrderBy(x => x.Date).ThenBy(x => x.Slot))
                            {
                                writer.WriteLine(
                                    $"{item.Date:yyyy-MM-dd},{item.Slot},{item.TimeRange},{item.RoomId}," +
                                    $"{item.Subject},{item.Status},{item.Note}");
                            }
                        }

                        MessageBox.Show(this, "Export Excel (CSV) thành công.", "Export", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            else if (type == "PDF")
            {
                // Nếu bạn chưa dùng thư viện PDF, cứ báo tạm:
                MessageBox.Show(this,
                    "Export PDF chưa implement. Bạn có thể dùng thư viện như iTextSharp / QuestPDF để xuất PDF.",
                    "Export PDF",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }


        private async void ReleaseRangeSlot()
        {
            var roomId = _cbReqRoom.SelectedItem?.ToString();
            var date = _dtReqDate.Value.Date;
            var slotFrom = _cbReqSlotFrom.SelectedItem?.ToString();
            var slotTo = _cbReqSlotTo.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(slotFrom) || string.IsNullOrEmpty(slotTo))
            {
                MessageBox.Show("Vui lòng chọn đầy đủ phòng và range ca.", "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int sIdx = ParseSlotIndexSafe(slotFrom);
            int eIdx = ParseSlotIndexSafe(slotTo);
            if (eIdx < sIdx)
            {
                MessageBox.Show("SlotEnd phải ≥ SlotStart.", "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentUser == null)
            {
                MessageBox.Show("Chưa có thông tin user.", "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // RELEASE_RANGE|UserId|RoomId|SlotStart|SlotEnd
                        var msg = $"RELEASE_RANGE|{_currentUser.UserId}|{roomId}|{slotFrom}|{slotTo}";
                        AppendClientLog("[SEND] " + msg);
                        await writer.WriteLineAsync(msg);

                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            MessageBox.Show("Không nhận được phản hồi từ server.", "Release range", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        AppendClientLog("[RECV] " + line);

                        var parts = line.Split('|');
                        if (parts.Length >= 2 && parts[0] == "INFO")
                        {
                            if (parts[1] == "RANGE_RELEASED")
                            {
                                _lblRequestStatus.ForeColor = Color.DarkGray;
                                _lblRequestStatus.Text =
                                    $"Đã RELEASE RANGE: {roomId} – {slotFrom} đến {slotTo} ngày {date:dd/MM/yyyy}.";
                            }
                            else if (parts[1] == "ERROR" && parts.Length >= 3)
                            {
                                _lblRequestStatus.ForeColor = Color.Red;
                                _lblRequestStatus.Text = "Release RANGE lỗi: " + parts[2];
                                MessageBox.Show("Release RANGE lỗi: " + parts[2],
                                    "Release range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                _lblRequestStatus.ForeColor = Color.Black;
                                _lblRequestStatus.Text = "Response: " + line;
                            }
                        }
                        else
                        {
                            _lblRequestStatus.ForeColor = Color.Black;
                            _lblRequestStatus.Text = "Response: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] ReleaseRangeSlot: " + ex.Message);
                MessageBox.Show("Release range failed: " + ex.Message,
                    "Release range", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSlotConfigFromServerAsync()
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(_serverIp, 5000);
                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // Gửi lệnh lấy cấu hình ca
                        await writer.WriteLineAsync("GET_SLOT_CONFIG");

                        // Clear dictionary cũ
                        _slotTimeLookup.Clear();

                        string? line;
                        var listForHeader = new List<(int index, string slotId, string start, string end)>();

                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            line = line.Trim();
                            if (line.Length == 0) continue;
                            if (line == "END_SLOT_CONFIG")
                                break;

                            // SLOT|S1|07:00|08:00
                            var parts = line.Split('|');
                            if (parts.Length < 4) continue;
                            if (!string.Equals(parts[0], "SLOT", StringComparison.OrdinalIgnoreCase)) continue;

                            var slotId = parts[1];   // "S1"
                            var start = parts[2];   // "07:00"
                            var end = parts[3];   // "08:00"

                            // lưu cho UpdateSlotTimeLabel
                            _slotTimeLookup[slotId] = $"{start}–{end}";

                            // Lấy index từ "S1" -> 1
                            var idx = ParseSlotIndexSafe(slotId);
                            if (idx > 0)
                            {
                                listForHeader.Add((idx, slotId, start, end));
                            }
                        }

                        // Cập nhật label cấu hình ca ở dưới header
                        if (listForHeader.Count > 0 && _lblSlotConfig != null)
                        {
                            var ordered = listForHeader.OrderBy(x => x.index).ToList();
                            var partsText = ordered.Select(x =>
                                $"Ca {x.index}: {x.start}–{x.end}");
                            _lblSlotConfig.Text = "Slot config: " + string.Join(" | ", partsText);
                        }

                        // Cập nhật lại label "Thời gian ca: ..." trong group Yêu cầu mượn phòng
                        UpdateSlotTimeLabel();
                    }
                }

                AppendClientLog("[INFO] Loaded slot config from server.");
            }
            catch (Exception ex)
            {
                AppendClientLog("[ERROR] LoadSlotConfigFromServerAsync: " + ex.Message);
            }
        }

        private void UpdateSlotConfigLabel()
        {
            // ví dụ hiển thị: "Cấu hình ca: S1 07:00–08:00 | S2 08:00–09:00 | ..."
            if (_lblSlotConfig == null) return;

            var parts = _slotTimeLookup
                .OrderBy(k => ParseSlotIndexSafe(k.Key))
                .Select(kv => $"{kv.Key} {kv.Value}");

            _lblSlotConfig.Text = "Cấu hình ca: " + string.Join(" | ", parts);
        }

    }

}



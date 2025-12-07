//bookingclient/mainclientform.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// ĐẶT ALIAS RÕ RÀNG ĐỂ HẾT LỖI AMBIGUOUS TIMER
using WinFormsTimer = System.Windows.Forms.Timer;

namespace BookingClient
{
    public class MainClientForm : Form
    {
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
        private ComboBox _cbFromSlot = null!;
        private ComboBox _cbToSlot = null!;
        private NumericUpDown _numCapacity = null!;
        private CheckBox _chkProjector = null!;
        private CheckBox _chkPC = null!;
        private CheckBox _chkAC = null!;
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

        private readonly DemoUserInfo _currentUser;
        private readonly string _serverIp;

        public MainClientForm(DemoUserInfo currentUser, string serverIp)
        {
            _currentUser = currentUser;
            _serverIp = serverIp;

            InitializeComponent(); // trống cũng được, nhưng cứ giữ cho chuẩn WinForms
            SetupUi();
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

            _lblNameWithType = new Label
            {
                Left = 80,
                Top = 10,
                Width = 400,
                Height = 25,
                Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
                Text = $"{_currentUser.FullName} ({_currentUser.UserType})"
            };
            _panelHeader.Controls.Add(_lblNameWithType);

            string subText = "";

            // Thông tin theo loại user
            if (_currentUser.UserType == "Student")
            {
                subText = $"MSSV: {_currentUser.StudentId} - Lớp: {_currentUser.Class} - Khoa: {_currentUser.Department}";
            }
            else if (_currentUser.UserType == "Lecturer")
            {
                subText = $"Mã GV: {_currentUser.LecturerId} - Khoa: {_currentUser.Faculty}";
            }
            else if (_currentUser.UserType == "Staff")
            {
                subText = $"Nhân viên - Phòng/Ban: {_currentUser.Department}";
            }

            // Thông tin liên hệ chung (nếu có)
            List<string> contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_currentUser.Email))
            {
                contactParts.Add($"Email: {_currentUser.Email}");
            }
            if (!string.IsNullOrWhiteSpace(_currentUser.Phone))
            {
                contactParts.Add($"ĐT: {_currentUser.Phone}");
            }

            if (contactParts.Count > 0)
            {
                // thêm sau cùng, cách nhau bằng " | "
                subText += (subText.Length > 0 ? " | " : "") + string.Join(" - ", contactParts);
            }


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
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            // Left = Right - Width (sau khi form load thì Anchor sẽ tự co)
            _lblToday.Left = ClientSize.Width - _lblToday.Width - 20;
            _panelHeader.Controls.Add(_lblToday);
            UpdateTodayLabel();

            _btnHeaderCheckConnect = new Button
            {
                Text = "Check connect",
                Width = 110,
                Height = 25,
                Top = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnHeaderCheckConnect.Left = ClientSize.Width - 280;
            _btnHeaderCheckConnect.Click += async (s, e) =>
            {
                await HeaderCheckConnectAsync();
            };
            _panelHeader.Controls.Add(_btnHeaderCheckConnect);

            _pnlHeaderConnectDot = new Panel
            {
                Width = 14,
                Height = 14,
                Top = 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Red,
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlHeaderConnectDot.Left = ClientSize.Width - 165;
            _panelHeader.Controls.Add(_pnlHeaderConnectDot);

            _lblHeaderConnectText = new Label
            {
                Width = 60,
                Height = 20,
                Top = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Text = "Lost",
                ForeColor = Color.Red
            };
            _lblHeaderConnectText.Left = ClientSize.Width - 145;
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
                _lblRunningTime.Text = "Time: " + DateTime.Now.ToString("HH:mm:ss");
            };
            _timerClock.Start();

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
            BuildBookingTabUi();
            BuildScheduleTabUi();
            BuildNotificationsTabUi();
            BuildAccountTabUi();
        }
        private async Task HeaderCheckConnectAsync()
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
                            return;
                        }
                    }
                }

                // Nếu không return ở trên => coi là lỗi
                _pnlHeaderConnectDot.BackColor = Color.Red;
                _lblHeaderConnectText.Text = "Lost";
                _lblHeaderConnectText.ForeColor = Color.Red;
            }
            catch
            {
                _pnlHeaderConnectDot.BackColor = Color.Red;
                _lblHeaderConnectText.Text = "Lost";
                _lblHeaderConnectText.ForeColor = Color.Red;
            }
        }

        private void UpdateTodayLabel()
        {
            var now = DateTime.Now;
            var thu = now.ToString("dddd"); // sau này nếu muốn tiếng Việt thì map thủ công
            _lblToday.Text = $"Hôm nay: {now:dd/MM/yyyy} – {thu}";
        }

        // ================== 2.3. TAB TRANG CHỦ ==================
        private void BuildHomeTabUi()
        {
            _grpTodaySchedule = new GroupBox
            {
                Text = "Lịch hôm nay của bạn",
                Left = 10,
                Top = 10,
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _grpTodaySchedule.Controls.Add(_gridTodaySchedule);

            _grpLatestNotifications = new GroupBox
            {
                Text = "Thông báo mới",
                Left = 470,
                Top = 10,
                Width = 480,
                Height = 300
            };
            _tabHome.Controls.Add(_grpLatestNotifications);

            _lstLatestNotifications = new ListBox
            {
                Dock = DockStyle.Fill
            };
            _grpLatestNotifications.Controls.Add(_lstLatestNotifications);

            _btnGoBookingTab = new Button
            {
                Text = "Đặt phòng ngay",
                Left = 10,
                Top = 320,
                Width = 150,
                Height = 30
            };
            _btnGoBookingTab.Click += (s, e) => { _tabMain.SelectedTab = _tabBooking; };
            _tabHome.Controls.Add(_btnGoBookingTab);

            _btnGoMyWeekSchedule = new Button
            {
                Text = "Xem lịch tuần này",
                Left = 170,
                Top = 320,
                Width = 150,
                Height = 30
            };
            _btnGoMyWeekSchedule.Click += (s, e) =>
            {
                _tabMain.SelectedTab = _tabSchedule;
                _radWeekView.Checked = true;
            };
            _tabHome.Controls.Add(_btnGoMyWeekSchedule);
        }

        // ================== 2.4. TAB ĐẶT PHÒNG ==================
        private void BuildBookingTabUi()
        {
            _grpSearchRooms = new GroupBox
            {
                Text = "Tìm phòng trống",
                Left = 10,
                Top = 10,
                Width = 950,
                Height = 200
            };
            _tabBooking.Controls.Add(_grpSearchRooms);

            var lblDate = new Label { Left = 10, Top = 30, Width = 80, Text = "Ngày:" };
            _dtBookingDate = new DateTimePicker
            {
                Left = 90,
                Top = 27,
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };

            var lblFromSlot = new Label { Left = 260, Top = 30, Width = 80, Text = "Từ ca:" };
            _cbFromSlot = new ComboBox
            {
                Left = 310,
                Top = 27,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var lblToSlot = new Label { Left = 380, Top = 30, Width = 40, Text = "Đến:" };
            _cbToSlot = new ComboBox
            {
                Left = 420,
                Top = 27,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 14; i++)
            {
                _cbFromSlot.Items.Add(i.ToString());
                _cbToSlot.Items.Add(i.ToString());
            }
            _cbFromSlot.SelectedIndex = 0;
            _cbToSlot.SelectedIndex = 0;

            var lblCapacity = new Label { Left = 10, Top = 65, Width = 80, Text = "Sức chứa ≥" };
            _numCapacity = new NumericUpDown
            {
                Left = 90,
                Top = 60,
                Width = 80,
                Minimum = 0,
                Maximum = 200,
                Value = 0
            };

            _chkProjector = new CheckBox { Left = 200, Top = 62, Width = 100, Text = "Máy chiếu" };
            _chkPC = new CheckBox { Left = 300, Top = 62, Width = 100, Text = "Máy tính" };
            _chkAC = new CheckBox { Left = 400, Top = 62, Width = 100, Text = "Máy lạnh" };

            var lblBuilding = new Label { Left = 10, Top = 95, Width = 80, Text = "Tòa nhà:" };
            _cbBuilding = new ComboBox
            {
                Left = 90,
                Top = 92,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbBuilding.Items.AddRange(new object[]
            {
                "Tất cả", "CS1 - Tòa A", "CS1 - Tòa B"
            });
            _cbBuilding.SelectedIndex = 0;

            _btnSearchRooms = new Button
            {
                Text = "Tìm phòng trống",
                Left = 260,
                Top = 90,
                Width = 150
            };
            _btnSearchRooms.Click += (s, e) =>
            {
                MessageBox.Show("Demo tìm phòng trống (chưa có logic).");
            };

            _grpSearchRooms.Controls.AddRange(new Control[]
            {
                lblDate, _dtBookingDate,
                lblFromSlot, _cbFromSlot,
                lblToSlot, _cbToSlot,
                lblCapacity, _numCapacity,
                _chkProjector, _chkPC, _chkAC,
                lblBuilding, _cbBuilding,
                _btnSearchRooms
            });

            _gridRooms = new DataGridView
            {
                Left = 10,
                Top = 125,
                Width = 930,
                Height = 65,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _gridRooms.CellDoubleClick += (s, e) =>
            {
                // sau này prefill form request phía dưới
            };
            _grpSearchRooms.Controls.Add(_gridRooms);

            _grpRequest = new GroupBox
            {
                Text = "Yêu cầu mượn phòng",
                Left = 10,
                Top = 220,
                Width = 950,
                Height = 220
            };
            _tabBooking.Controls.Add(_grpRequest);

            var lblReqRoom = new Label { Left = 10, Top = 30, Width = 60, Text = "Phòng:" };
            _cbReqRoom = new ComboBox
            {
                Left = 70,
                Top = 27,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblReqDate = new Label { Left = 200, Top = 30, Width = 60, Text = "Ngày:" };
            _dtReqDate = new DateTimePicker
            {
                Left = 260,
                Top = 27,
                Width = 140,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };

            var lblReqCa = new Label { Left = 10, Top = 60, Width = 60, Text = "Ca:" };
            _cbReqSlotSingle = new ComboBox
            {
                Left = 70,
                Top = 57,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbReqSlotFrom = new ComboBox
            {
                Left = 150,
                Top = 57,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbReqSlotTo = new ComboBox
            {
                Left = 220,
                Top = 57,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 14; i++)
            {
                _cbReqSlotSingle.Items.Add(i.ToString());
                _cbReqSlotFrom.Items.Add(i.ToString());
                _cbReqSlotTo.Items.Add(i.ToString());
            }
            _cbReqSlotSingle.SelectedIndex = 0;
            _cbReqSlotFrom.SelectedIndex = 0;
            _cbReqSlotTo.SelectedIndex = 0;

            var lblPurpose = new Label { Left = 10, Top = 90, Width = 120, Text = "Lý do mượn phòng:" };
            _txtPurpose = new TextBox
            {
                Left = 10,
                Top = 110,
                Width = 390,
                Height = 80,
                Multiline = true
            };

            _lblSlotTimeRange = new Label
            {
                Left = 410,
                Top = 60,
                Width = 300,
                Text = "Thời gian ca: (ví dụ) 09:00–10:00"
            };

            _btnReqSingle = new Button
            {
                Text = "REQUEST (1 ca)",
                Left = 410,
                Top = 100,
                Width = 150
            };
            _btnReqRange = new Button
            {
                Text = "REQUEST RANGE",
                Left = 570,
                Top = 100,
                Width = 150
            };
            _btnReleaseSingle = new Button
            {
                Text = "RELEASE",
                Left = 410,
                Top = 140,
                Width = 150
            };
            _btnReleaseRange = new Button
            {
                Text = "RELEASE RANGE",
                Left = 570,
                Top = 140,
                Width = 150
            };

            _lblRequestStatus = new Label
            {
                Left = 10,
                Top = 195,
                Width = 900,
                Height = 20,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Trạng thái: Chưa gửi yêu cầu"
            };

            _grpRequest.Controls.AddRange(new Control[]
            {
                lblReqRoom, _cbReqRoom,
                lblReqDate, _dtReqDate,
                lblReqCa, _cbReqSlotSingle, _cbReqSlotFrom, _cbReqSlotTo,
                lblPurpose, _txtPurpose,
                _lblSlotTimeRange,
                _btnReqSingle, _btnReqRange,
                _btnReleaseSingle, _btnReleaseRange,
                _lblRequestStatus
            });

            _grpClientLog = new GroupBox
            {
                Text = "Client log (debug)",
                Left = 10,
                Top = 450,
                Width = 950,
                Height = 120
            };
            _tabBooking.Controls.Add(_grpClientLog);

            _txtClientLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };
            _grpClientLog.Controls.Add(_txtClientLog);
        }

        // ================== 2.5. TAB "LỊCH CỦA TÔI" ==================
        private void BuildScheduleTabUi()
        {
            _radDayView = new RadioButton
            {
                Left = 10,
                Top = 10,
                Width = 80,
                Text = "Ngày",
                Checked = true
            };
            _radWeekView = new RadioButton
            {
                Left = 100,
                Top = 10,
                Width = 80,
                Text = "Tuần"
            };
            _tabSchedule.Controls.Add(_radDayView);
            _tabSchedule.Controls.Add(_radWeekView);

            var lblDate = new Label { Left = 200, Top = 12, Width = 50, Text = "Ngày:" };
            _dtScheduleDate = new DateTimePicker
            {
                Left = 250,
                Top = 8,
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            _tabSchedule.Controls.Add(lblDate);
            _tabSchedule.Controls.Add(_dtScheduleDate);

            _btnExportSchedule = new Button
            {
                Text = "Xuất file (PDF/Excel)",
                Left = 420,
                Top = 8,
                Width = 150
            };
            _tabSchedule.Controls.Add(_btnExportSchedule);

            _gridDayView = new DataGridView
            {
                Left = 10,
                Top = 40,
                Width = 950,
                Height = 250,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _tabSchedule.Controls.Add(_gridDayView);

            _gridWeekView = new DataGridView
            {
                Left = 10,
                Top = 300,
                Width = 950,
                Height = 250,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _tabSchedule.Controls.Add(_gridWeekView);
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
            var lblName = new Label { Left = 10, Top = 15, Width = 100, Text = "Họ tên:" };
            _txtAccFullName = new TextBox { Left = 120, Top = 12, Width = 300, ReadOnly = true };

            var lblId = new Label { Left = 10, Top = 45, Width = 100, Text = "MSSV/Mã GV:" };
            _txtAccStudentLecturerId = new TextBox { Left = 120, Top = 42, Width = 300, ReadOnly = true };

            var lblClassFac = new Label { Left = 10, Top = 75, Width = 100, Text = "Lớp/Khoa:" };
            _txtAccClassFaculty = new TextBox { Left = 120, Top = 72, Width = 300, ReadOnly = true };

            var lblDept = new Label { Left = 10, Top = 105, Width = 100, Text = "Khoa/Bộ môn:" };
            _txtAccDepartment = new TextBox { Left = 120, Top = 102, Width = 300, ReadOnly = true };

            var lblEmail = new Label { Left = 10, Top = 135, Width = 100, Text = "Email:" };
            _txtAccEmail = new TextBox { Left = 120, Top = 132, Width = 300 };

            var lblPhone = new Label { Left = 10, Top = 165, Width = 100, Text = "Phone:" };
            _txtAccPhone = new TextBox { Left = 120, Top = 162, Width = 300 };

            _btnUpdateContact = new Button
            {
                Text = "Cập nhật email/phone",
                Left = 120,
                Top = 195,
                Width = 180
            };

            var lblOldPwd = new Label { Left = 10, Top = 240, Width = 100, Text = "Mật khẩu cũ:" };
            _txtOldPassword = new TextBox
            {
                Left = 120,
                Top = 237,
                Width = 300,
                UseSystemPasswordChar = true
            };

            var lblNewPwd = new Label { Left = 10, Top = 270, Width = 100, Text = "Mật khẩu mới:" };
            _txtNewPassword = new TextBox
            {
                Left = 120,
                Top = 267,
                Width = 300,
                UseSystemPasswordChar = true
            };

            var lblConfirmPwd = new Label { Left = 10, Top = 300, Width = 100, Text = "Nhập lại:" };
            _txtConfirmPassword = new TextBox
            {
                Left = 120,
                Top = 297,
                Width = 300,
                UseSystemPasswordChar = true
            };

            _btnChangePassword = new Button
            {
                Text = "Đổi mật khẩu",
                Left = 120,
                Top = 330,
                Width = 120
            };

            _btnLogout = new Button
            {
                Text = "Đăng xuất",
                Left = 800,
                Top = 12,
                Width = 120
            };
            _btnLogout.Click += (s, e) =>
            {
                // TODO: logic logout (ví dụ: Close + show LoginForm)
                Close();
            };

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
                _btnLogout
            });

            // Fill sẵn info demo
            _txtAccFullName.Text = _currentUser.FullName;
            _txtAccEmail.Text = "";  // sau này lấy từ UserInfo
            _txtAccPhone.Text = "";
            if (_currentUser.UserType == "Student")
            {
                _txtAccStudentLecturerId.Text = _currentUser.StudentId;
                _txtAccClassFaculty.Text = _currentUser.Class;
                _txtAccDepartment.Text = _currentUser.Department;
            }
            else
            {
                _txtAccStudentLecturerId.Text = _currentUser.LecturerId;
                _txtAccClassFaculty.Text = _currentUser.Faculty;
                _txtAccDepartment.Text = "";
            }
        }
    }
}

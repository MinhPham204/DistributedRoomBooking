//bookingclient/loginform.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace BookingClient
{
    public class LoginForm : Form
    {
        private PictureBox _picLogo = null!;
        private Label _lblSystemName = null!;

        private TextBox _txtServerIp = null!;
        private Button _btnCheckConnect = null!;
        private Panel _pnlConnectDot = null!;
        private Label _lblConnectStatus = null!;

        private TextBox _txtUserId = null!;
        private TextBox _txtPassword = null!;
        private Button _btnTogglePassword = null!;
        private CheckBox _chkRemember = null!;
        private Button _btnLogin = null!;
        private LinkLabel _lnkForgotPassword = null!;
        private Label _lblError = null!;

        private GlassPanel _pnlCard = null!;

        private bool _isConnectedOk = false;
        private string? _detectedServerIp = null;

        private const int SERVER_TCP_PORT = 5000;
        private const int DISCOVERY_UDP_PORT = 5001;

        private readonly string _rememberFilePath =
            Path.Combine(Application.UserAppDataPath, "remember_login.txt");

        public LoginForm()
        {
            InitializeComponent();
            SetupUi();
            LoadRememberedLogin();
        }
        private void LoadRememberedLogin()
        {
            try
            {
                if (!File.Exists(_rememberFilePath))
                    return;

                var line = File.ReadAllText(_rememberFilePath).Trim();
                if (string.IsNullOrWhiteSpace(line))
                    return;

                var parts = line.Split('|');
                if (parts.Length >= 2 && bool.TryParse(parts[1], out var remember) && remember)
                {
                    _txtUserId.Text = parts[0];
                    _chkRemember.Checked = true;
                }
            }
            catch
            {
                // lỗi đọc file thì bỏ qua, không cần báo
            }
        }

        private void SaveRememberedLogin(string userId)
        {
            try
            {
                var dir = Path.GetDirectoryName(_rememberFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                if (_chkRemember.Checked)
                {
                    File.WriteAllText(_rememberFilePath, $"{userId}|true");
                }
                else
                {
                    if (File.Exists(_rememberFilePath))
                        File.Delete(_rememberFilePath);
                }
            }
            catch
            {
                // lỗi ghi file thì cũng bỏ qua, không crash app
            }
        }

        private void InitializeComponent()
        {
            // Không dùng designer, mình build tay trong SetupUi
        }

        private void SetupUi()
        {
            Text = "Đăng nhập - Hệ thống đặt phòng học";
            Width = 1000;
            Height = 600;
            MinimumSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Font = new Font("Segoe UI", 10, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

            Controls.Clear();

            try
            {
                var bg = TryLoadLoginBackgroundImage();
                if (bg != null)
                {
                    BackgroundImage = bg;
                    BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    BackgroundImage = null;
                    BackColor = Color.FromArgb(30, 144, 255);
                }
            }
            catch
            {
                BackgroundImage = null;
                BackColor = Color.FromArgb(30, 144, 255);
            }

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 3,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 410f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            Controls.Add(root);

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(stack, 1, 1);

            _pnlCard = new GlassPanel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.FromArgb(110, 255, 255, 255),
                BorderColor = Color.FromArgb(140, 255, 255, 255),
                BorderThickness = 1f,
                CornerRadius = 16,
                ShadowColor = Color.FromArgb(60, 0, 0, 0),
                ShadowBlur = 16,
                ShadowOffset = new Point(0, 6),
                Padding = new Padding(22, 18, 22, 18),
                MinimumSize = new Size(0, 320)
            };
            stack.Controls.Add(_pnlCard, 0, 0);

            _lblError = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(239, 68, 68),
                BackColor = Color.FromArgb(90, 0, 0, 0),
                Text = "",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10, 10, 10, 10)
            };
            stack.Controls.Add(_lblError, 0, 1);

            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 9
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // status
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // user label
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // user textbox
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // pwd label
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // pwd row
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // remember
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // login
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // forgot
            _pnlCard.Controls.Add(cardLayout);

            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            cardLayout.Controls.Add(lblTitle, 0, 0);

            var statusRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            cardLayout.Controls.Add(statusRow, 0, 1);

            _pnlConnectDot = new Panel
            {
                Width = 10,
                Height = 10,
                BackColor = Color.Orange,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 6, 8, 0)
            };
            var dotPath = new GraphicsPath();
            dotPath.AddEllipse(0, 0, _pnlConnectDot.Width, _pnlConnectDot.Height);
            _pnlConnectDot.Region = new Region(dotPath);
            statusRow.Controls.Add(_pnlConnectDot);

            _lblConnectStatus = new Label
            {
                AutoSize = true,
                Text = "Checking...",
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            statusRow.Controls.Add(_lblConnectStatus);

            _btnCheckConnect = new Button
            {
                Text = "Connect",
                AutoSize = true,
                Height = 26,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(170, 255, 255, 255),
                ForeColor = Color.Black,
                Margin = new Padding(12, 0, 0, 0)
            };
            _btnCheckConnect.FlatAppearance.BorderSize = 0;
            _btnCheckConnect.Click += async (s, e) => { await CheckConnectAsync(); };
            statusRow.Controls.Add(_btnCheckConnect);

            var lblUser = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Tên đăng nhập",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            cardLayout.Controls.Add(lblUser, 0, 2);

            _txtUserId = new TextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 10)
            };
            cardLayout.Controls.Add(_txtUserId, 0, 3);

            var lblPwd = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Mật khẩu",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            cardLayout.Controls.Add(lblPwd, 0, 4);

            var pwdRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };
            pwdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pwdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64f));
            cardLayout.Controls.Add(pwdRow, 0, 5);

            _btnTogglePassword = new Button
            {
                Dock = DockStyle.Fill,
                Width = 64,
                Text = "Hiện",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(90, 255, 255, 255),
                ForeColor = Color.Black
            };
            _btnTogglePassword.FlatAppearance.BorderSize = 0;

            _txtPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            pwdRow.Controls.Add(_txtPassword, 0, 0);
            pwdRow.Controls.Add(_btnTogglePassword, 1, 0);

            _btnTogglePassword.Click += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
                _btnTogglePassword.Text = _txtPassword.UseSystemPasswordChar ? "Hiện" : "Ẩn";
            };

            _chkRemember = new CheckBox
            {
                Dock = DockStyle.Fill,
                Text = "Nhớ tài khoản",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };
            cardLayout.Controls.Add(_chkRemember, 0, 6);

            _btnLogin = new Button
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 255, 255, 255),
                ForeColor = Color.Black,
                Margin = new Padding(0, 0, 0, 8)
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += BtnLogin_Click;
            cardLayout.Controls.Add(_btnLogin, 0, 7);
            SetRoundedRegion(_btnLogin, 10);

            _lnkForgotPassword = new LinkLabel
            {
                Dock = DockStyle.Fill,
                Text = "Quên mật khẩu?",
                LinkColor = Color.White,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0)
            };
            _lnkForgotPassword.Click += (s, e) =>
            {
                var serverIp = (_detectedServerIp ?? _txtServerIp?.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(serverIp))
                {
                    MessageBox.Show("Không tìm thấy Server IP. Vui lòng đợi check kết nối hoặc thử lại.", "Quên mật khẩu");
                    return;
                }

                using (var f = new ForgotPasswordForm(serverIp))
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
            };
            cardLayout.Controls.Add(_lnkForgotPassword, 0, 8);

            // Control phụ trợ cho logic (không cho user chỉnh IP ở UI)
            _txtServerIp = new TextBox { Visible = false, Text = "127.0.0.1" };
            Controls.Add(_txtServerIp);

            // Bắt buộc phải Check connect trước khi login
            _btnLogin.Enabled = false;

            Shown += async (s, e) =>
            {
                await CheckConnectAsync();
            };
        }

        private static void SetRoundedRegion(Control control, int radius)
        {
            try
            {
                var rect = new Rectangle(0, 0, control.Width, control.Height);
                var path = CreateRoundRectPath(rect, radius);
                control.Region = new Region(path);
            }
            catch
            {
                // ignore
            }
        }

        private static GraphicsPath CreateRoundRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ====== chỗ này sau này bạn gắn network/login thật ======
        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            // BẮT BUỘC: phải check connect trước
            if (!_isConnectedOk || string.IsNullOrEmpty(_detectedServerIp))
            {
                _lblError.Text = "Vui lòng bấm Check connect trước khi đăng nhập.";
                return;
            }

            _lblError.Text = "";

            var userId = (_txtUserId.Text ?? "").Trim().ToUpperInvariant();
            var password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                _lblError.Text = "Vui lòng nhập User ID và Password.";
                return;
            }

            _btnLogin.Enabled = false;

            try
            {
                using (var tcp = new TcpClient())
                {
                    // có thể dùng port đã detect được, ở đây anh đang dùng SERVER_TCP_PORT = 5000
                    await tcp.ConnectAsync(IPAddress.Parse(_detectedServerIp!), SERVER_TCP_PORT);

                    using (var stream = tcp.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.NewLine = "\n";
                        writer.AutoFlush = true;

                        // Gửi lệnh LOGIN: LOGIN|userId|password\n
                        var request = $"LOGIN|{userId}|{password}";
                        await writer.WriteLineAsync(request);

                        // Đọc 1 dòng trả về
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            _lblError.Text = "Không nhận được phản hồi từ server.";
                            return;
                        }

                        var parts = line.Split('|');
                        if (parts.Length == 0)
                        {
                            _lblError.Text = "Phản hồi LOGIN không hợp lệ.";
                            return;
                        }

                        if (parts[0] == "LOGIN_OK")
                        {
                            // LOGIN_OK|UserId|UserType|FullName|Email|Phone|StudentId|Class|Department|LecturerId|Faculty
                            if (parts.Length < 11)
                            {
                                _lblError.Text = "Dữ liệu người dùng trả về không đầy đủ.";
                                return;
                            }

                            var info = new DemoUserInfo
                            {
                                UserId = parts[1],
                                UserType = parts[2],
                                FullName = parts[3],
                                Email = parts[4],
                                Phone = parts[5],
                                StudentId = parts[6],
                                Class = parts[7],
                                Department = parts[8],
                                LecturerId = parts[9],
                                Faculty = parts[10],
                                Password = password
                            };

                            // Lưu nhớ tài khoản nếu user chọn
                            SaveRememberedLogin(info.UserId);

                            try
                            {
                                var mainForm = new MainClientForm(info, _detectedServerIp, this);
                                mainForm.Show();
                                this.Hide();
                            }
                            catch (Exception ex2)
                            {
                                MessageBox.Show("Lỗi trong MainClientForm:\n" + ex2.ToString());
                            }
                        }
                        else if (parts[0] == "LOGIN_FAIL")
                        {
                            // LOGIN_FAIL|REASON|Message
                            var reason = parts.Length >= 2 ? parts[1] : "";
                            // var messageFromServer = parts.Length >= 3 ? parts[2] : "";

                            switch (reason)
                            {
                                case "USER_NOT_FOUND":
                                    _lblError.Text = "Sai thông tin đăng nhập.";
                                    break;
                                case "USER_INACTIVE":
                                    _lblError.Text = "Tài khoản đang bị khóa (Inactive).";
                                    break;
                                case "INVALID_PASSWORD":
                                    _lblError.Text = "Sai thông tin đăng nhập.";
                                    break;
                                default:
                                    _lblError.Text = "Đăng nhập thất bại.";
                                    break;
                            }
                        }
                        else
                        {
                            _lblError.Text = "Phản hồi không hỗ trợ: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lblError.Text = "Lỗi khi đăng nhập: " + ex.Message;
            }
            finally
            {
                _btnLogin.Enabled = true;
            }
        }

        private async Task CheckConnectAsync()
        {
            _isConnectedOk = false;
            _detectedServerIp = null;

            _btnCheckConnect.Enabled = false;
            _lblError.Text = "";
            if (_lblConnectStatus != null)
            {
                _lblConnectStatus.Text = "Checking...";
                _lblConnectStatus.ForeColor = Color.White;
            }
            if (_pnlConnectDot != null)
            {
                _pnlConnectDot.BackColor = Color.Orange;
            }

            try
            {
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;

                    var requestBytes = Encoding.UTF8.GetBytes("DISCOVER_BOOKING_SERVER");
                    var broadcastEp = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_UDP_PORT);

                    // Gửi broadcast
                    await udp.SendAsync(requestBytes, requestBytes.Length, broadcastEp);

                    // Đợi reply tối đa 2 giây
                    var receiveTask = udp.ReceiveAsync();
                    var finished = await Task.WhenAny(receiveTask, Task.Delay(2000));

                    if (finished != receiveTask)
                    {
                        // Timeout
                        if (_lblConnectStatus != null)
                        {
                            _lblConnectStatus.Text = "No server";
                            _lblConnectStatus.ForeColor = Color.White;
                        }
                        if (_pnlConnectDot != null)
                        {
                            _pnlConnectDot.BackColor = Color.Red;
                        }
                        _lblError.Text = "Không tìm thấy server trên cùng mạng WiFi.\nKiểm tra lại Start Server.";
                        return;
                    }

                    var result = receiveTask.Result;
                    var response = Encoding.UTF8.GetString(result.Buffer).Trim();
                    // SERVER_INFO|ip|port
                    var parts = response.Split('|');
                    if (parts.Length != 3 || parts[0] != "SERVER_INFO")
                    {
                        if (_lblConnectStatus != null)
                        {
                            _lblConnectStatus.Text = "Invalid";
                            _lblConnectStatus.ForeColor = Color.White;
                        }
                        if (_pnlConnectDot != null)
                        {
                            _pnlConnectDot.BackColor = Color.Red;
                        }
                        _lblError.Text = "Packet discovery nhận được không đúng định dạng.";
                        return;
                    }

                    var ip = parts[1];
                    if (!int.TryParse(parts[2], out var port))
                    {
                        port = SERVER_TCP_PORT;
                    }

                    // Thử mở TCP tới server để chắc ăn
                    using (var tcp = new TcpClient())
                    {
                        tcp.ReceiveTimeout = 2000;
                        tcp.SendTimeout = 2000;

                        try
                        {
                            await tcp.ConnectAsync(IPAddress.Parse(ip), port);
                        }
                        catch
                        {
                            if (_lblConnectStatus != null)
                            {
                                _lblConnectStatus.Text = "Lost";
                                _lblConnectStatus.ForeColor = Color.White;
                            }
                            if (_pnlConnectDot != null)
                            {
                                _pnlConnectDot.BackColor = Color.Red;
                            }
                            _lblError.Text = "Tìm thấy server nhưng không connect TCP được.\nHãy kiểm tra Firewall / Start Server.";
                            return;
                        }
                    }

                    // ✅ Thành công
                    _detectedServerIp = ip;
                    _txtServerIp.Text = ip;

                    if (_lblConnectStatus != null)
                    {
                        _lblConnectStatus.Text = "OK";
                        _lblConnectStatus.ForeColor = Color.White;
                    }
                    if (_pnlConnectDot != null)
                    {
                        _pnlConnectDot.BackColor = Color.LimeGreen;
                    }

                    _isConnectedOk = true;
                    _btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                if (_lblConnectStatus != null)
                {
                    _lblConnectStatus.Text = "Error";
                    _lblConnectStatus.ForeColor = Color.White;
                }
                if (_pnlConnectDot != null)
                {
                    _pnlConnectDot.BackColor = Color.Red;
                }
                _lblError.Text = "Lỗi khi Check connect: " + ex.Message;
            }
            finally
            {
                _btnCheckConnect.Enabled = true;
            }
        }

        private class GlassPanel : Panel
        {
            public Color FillColor { get; set; } = Color.FromArgb(160, 255, 255, 255);
            public Color BorderColor { get; set; } = Color.FromArgb(120, 255, 255, 255);
            public float BorderThickness { get; set; } = 1f;
            public int CornerRadius { get; set; } = 16;
            public Color ShadowColor { get; set; } = Color.FromArgb(60, 0, 0, 0);
            public int ShadowBlur { get; set; } = 16;
            public Point ShadowOffset { get; set; } = new Point(0, 6);

            public GlassPanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);

                // Shadow (simple layered draw to fake blur)
                if (ShadowBlur > 0)
                {
                    var shadowRect = new Rectangle(
                        rect.X + ShadowOffset.X,
                        rect.Y + ShadowOffset.Y,
                        rect.Width,
                        rect.Height);

                    for (int i = ShadowBlur; i >= 1; i -= 4)
                    {
                        var alpha = (int)(ShadowColor.A * (i / (float)ShadowBlur));
                        if (alpha <= 0) continue;

                        var inflate = (ShadowBlur - i) / 2;
                        var r = shadowRect;
                        r.Inflate(inflate, inflate);

                        using (var sp = CreateRoundRectPath(r, CornerRadius + inflate))
                        using (var sb = new SolidBrush(Color.FromArgb(alpha, ShadowColor)))
                        {
                            e.Graphics.FillPath(sb, sp);
                        }
                    }
                }

                using (var path = CreateRoundRectPath(rect, CornerRadius))
                using (var brush = new SolidBrush(FillColor))
                using (var pen = new Pen(BorderColor, BorderThickness))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }

                base.OnPaint(e);
            }
        }

        private static Image? TryLoadLoginBackgroundImage()
        {
            // Prefer the repo's data image: data\1-e1649378997106.jpg
            var candidates = new[]
            {
                Path.Combine("data", "1-e1649378997106.jpg"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_bg.jpg")
            };

            // Search upwards from bin folder to repo root
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                foreach (var rel in candidates)
                {
                    var full = Path.GetFullPath(Path.Combine(dir.FullName, rel));
                    if (!File.Exists(full)) continue;
                    return LoadImageUnlocked(full);
                }
            }

            // As a last resort, try the absolute path given by the user
            var abs = @"D:\Downloads\DistributedRoomBooking\data\1-e1649378997106.jpg";
            if (File.Exists(abs))
                return LoadImageUnlocked(abs);

            return null;
        }

        private static Image LoadImageUnlocked(string path)
        {
            // Avoid locking the file on disk
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                ms.Position = 0;
                using (var img = Image.FromStream(ms))
                {
                    return (Image)img.Clone();
                }
            }
        }

    }

    // Class demo info, sau này bạn thay bằng UserInfo từ server
    public class DemoUserInfo
    {
        public string UserId { get; set; } = "";
        public string UserType { get; set; } = "";   // Student / Lecturer / Staff
        public string FullName { get; set; } = "";

        // Sinh viên
        public string StudentId { get; set; } = "";
        public string Class { get; set; } = "";
        public string Department { get; set; } = "";

        // Giảng viên
        public string LecturerId { get; set; } = "";
        public string Faculty { get; set; } = "";

        // Thông tin liên hệ chung (thêm mới)
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // 🔐 Lưu password để LOGIN lại trên kết nối chính
        public string Password { get; set; } = "";
    }
}

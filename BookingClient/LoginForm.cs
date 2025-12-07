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
        private CheckBox _chkRemember = null!;
        private Button _btnLogin = null!;
        private LinkLabel _lnkForgotPassword = null!;
        private Label _lblError = null!;

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
            Width = 420;
            Height = 420;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // ===== Logo + tên hệ thống =====
            _picLogo = new PictureBox
            {
                Left = 20,
                Top = 20,
                Width = 64,
                Height = 64,
                BorderStyle = BorderStyle.FixedSingle, // sau này gán Image logo
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            Controls.Add(_picLogo);

            _lblSystemName = new Label
            {
                Left = 100,
                Top = 35,
                Width = 280,
                Height = 40,
                Text = "HỆ THỐNG ĐẶT PHÒNG HỌC",
                Font = new Font(Font.FontFamily, 11, FontStyle.Bold)
            };
            Controls.Add(_lblSystemName);

            // ===== Group: Server =====
            var grpServer = new GroupBox
            {
                Text = "Server",
                Left = 20,
                Top = 100,
                Width = 360,
                Height = 90
            };
            Controls.Add(grpServer);

            var lblIp = new Label
            {
                Text = "Server IP:",
                Left = 10,
                Top = 30,
                Width = 70
            };
            _txtServerIp = new TextBox
            {
                Left = 80,
                Top = 27,
                Width = 120,
                Text = "127.0.0.1"
            };

            _btnCheckConnect = new Button
            {
                Text = "Check",
                Left = 210,
                Top = 25,
                Width = 70
            };
            // sau này gắn event ping server
            _btnCheckConnect.Click += async (s, e) =>
            {
                await CheckConnectAsync();
            };

            _pnlConnectDot = new Panel
            {
                Left = 290,
                Top = 30,
                Width = 16,
                Height = 16,
                BackColor = Color.Red,         // mặc định Lost
                BorderStyle = BorderStyle.FixedSingle
            };
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, _pnlConnectDot.Width, _pnlConnectDot.Height);
            _pnlConnectDot.Region = new Region(path);

            _lblConnectStatus = new Label
            {
                Left = 310,
                Top = 30,
                Width = 50,
                Text = "Lost",
                ForeColor = Color.Red
            };

            grpServer.Controls.AddRange(new Control[]
            {
                lblIp, _txtServerIp, _btnCheckConnect, _pnlConnectDot, _lblConnectStatus
            });

            // ===== Group: Đăng nhập =====
            var grpLogin = new GroupBox
            {
                Text = "Đăng nhập",
                Left = 20,
                Top = 200,
                Width = 360,
                Height = 140
            };
            Controls.Add(grpLogin);

            var lblUser = new Label
            {
                Text = "User ID:",
                Left = 10,
                Top = 30,
                Width = 70
            };
            _txtUserId = new TextBox
            {
                Left = 80,
                Top = 27,
                Width = 260,
                Text = "sv001"
            };

            var lblPwd = new Label
            {
                Text = "Password:",
                Left = 10,
                Top = 60,
                Width = 70
            };
            _txtPassword = new TextBox
            {
                Left = 80,
                Top = 57,
                Width = 260,
                UseSystemPasswordChar = true,
                Text = "sv123"
            };

            _chkRemember = new CheckBox
            {
                Left = 80,
                Top = 85,
                Width = 200,
                Text = "Nhớ tài khoản trên máy này"
            };

            _btnLogin = new Button
            {
                Text = "Đăng nhập",
                Left = 245,
                Top = 105,
                Width = 95
            };
            _btnLogin.Click += BtnLogin_Click; // sau này viết logic

            // // Không cho người dùng tự gõ IP nữa
            // _txtServerIp.ReadOnly = true;
            // _txtServerIp.Text = "(auto)";

            // Bắt buộc phải Check connect trước khi login
            _btnLogin.Enabled = false;

            grpLogin.Controls.AddRange(new Control[]
            {
                lblUser, _txtUserId,
                lblPwd, _txtPassword,
                _chkRemember, _btnLogin
            });

            // ===== Quên mật khẩu + lỗi =====
            _lnkForgotPassword = new LinkLabel
            {
                Left = 20,
                Top = 350,
                Width = 120,
                Text = "Quên mật khẩu?"
            };
            _lnkForgotPassword.Click += (s, e) =>
            {
                var serverIp = _txtServerIp.Text.Trim();
                if (string.IsNullOrWhiteSpace(serverIp))
                {
                    MessageBox.Show("Vui lòng nhập Server IP trước.", "Quên mật khẩu");
                    return;
                }

                using (var f = new ForgotPasswordForm(serverIp))
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
            };
            Controls.Add(_lnkForgotPassword);

            _lblError = new Label
            {
                Left = 150,
                Top = 345,
                Width = 230,
                Height = 40,
                ForeColor = Color.Red,
                Text = "" // rỗng, khi sai mới set
            };
            Controls.Add(_lblError);
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

            var userId = _txtUserId.Text.Trim();
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
                                Faculty = parts[10]
                            };

                            // Lưu nhớ tài khoản nếu user chọn
                            SaveRememberedLogin(info.UserId);

                            // Mở Main Client Form
                            var mainForm = new MainClientForm(info, _detectedServerIp);
                            mainForm.Show();
                            this.Hide();
                        }
                        else if (parts[0] == "LOGIN_FAIL")
                        {
                            // LOGIN_FAIL|REASON|Message
                            var reason = parts.Length >= 2 ? parts[1] : "";
                            // var messageFromServer = parts.Length >= 3 ? parts[2] : "";

                            switch (reason)
                            {
                                case "USER_NOT_FOUND":
                                    _lblError.Text = "User không tồn tại.";
                                    break;
                                case "USER_INACTIVE":
                                    _lblError.Text = "Tài khoản đang bị khóa (Inactive).";
                                    break;
                                case "INVALID_PASSWORD":
                                    _lblError.Text = "Sai mật khẩu, vui lòng thử lại.";
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
            _lblConnectStatus.Text = "Checking...";
            _lblConnectStatus.ForeColor = Color.Orange;
            _pnlConnectDot.BackColor = Color.Orange;

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
                        _lblConnectStatus.Text = "No server";
                        _lblConnectStatus.ForeColor = Color.Red;
                        _pnlConnectDot.BackColor = Color.Red;
                        _lblError.Text = "Không tìm thấy server trên cùng mạng WiFi.\nKiểm tra lại Start Server.";
                        return;
                    }

                    var result = receiveTask.Result;
                    var response = Encoding.UTF8.GetString(result.Buffer).Trim();
                    // SERVER_INFO|ip|port
                    var parts = response.Split('|');
                    if (parts.Length != 3 || parts[0] != "SERVER_INFO")
                    {
                        _lblConnectStatus.Text = "Invalid";
                        _lblConnectStatus.ForeColor = Color.Red;
                        _pnlConnectDot.BackColor = Color.Red;
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
                            _lblConnectStatus.Text = "Lost";
                            _lblConnectStatus.ForeColor = Color.Red;
                            _pnlConnectDot.BackColor = Color.Red;
                            _lblError.Text = "Tìm thấy server nhưng không connect TCP được.\nHãy kiểm tra Firewall / Start Server.";
                            return;
                        }
                    }

                    // ✅ Thành công
                    _detectedServerIp = ip;
                    _txtServerIp.Text = ip;

                    _lblConnectStatus.Text = "OK";
                    _lblConnectStatus.ForeColor = Color.Green;
                    _pnlConnectDot.BackColor = Color.LimeGreen;

                    _isConnectedOk = true;
                    _btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                _lblConnectStatus.Text = "Error";
                _lblConnectStatus.ForeColor = Color.Red;
                _pnlConnectDot.BackColor = Color.Red;
                _lblError.Text = "Lỗi khi Check connect: " + ex.Message;
            }
            finally
            {
                _btnCheckConnect.Enabled = true;
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
    }
}

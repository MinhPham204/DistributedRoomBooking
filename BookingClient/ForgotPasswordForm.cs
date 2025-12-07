//BookingClient/ForgotPasswordForm.cs
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace BookingClient
{
    public class ForgotPasswordForm : Form
    {
        private readonly string _serverIp;
        private TextBox _txtEmail = null!;
        private Button _btnSend = null!;
        private Label _lblStatus = null!;

        private const int SERVER_TCP_PORT = 5000; // đúng với port server anh đang dùng

        public ForgotPasswordForm(string serverIp)
        {
            _serverIp = serverIp;
            InitializeUi();
        }

        private void InitializeUi()
        {
            this.Text = "Khôi phục mật khẩu";
            this.Width = 380;
            this.Height = 200;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblEmail = new Label
            {
                Text = "Nhập Email đã đăng ký:",
                Left = 20,
                Top = 20,
                Width = 320
            };
            this.Controls.Add(lblEmail);

            _txtEmail = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 320
            };
            this.Controls.Add(_txtEmail);

            _btnSend = new Button
            {
                Text = "Gửi mật khẩu mới",
                Left = 20,
                Top = 90,
                Width = 150
            };
            _btnSend.Click += BtnSend_Click;
            this.Controls.Add(_btnSend);

            _lblStatus = new Label
            {
                Left = 20,
                Top = 130,
                Width = 320,
                ForeColor = System.Drawing.Color.Red
            };
            this.Controls.Add(_lblStatus);
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            var email = _txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _lblStatus.Text = "Vui lòng nhập email.";
                return;
            }

            _btnSend.Enabled = false;
            _lblStatus.Text = "Đang gửi yêu cầu...";

            try
            {
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync(IPAddress.Parse(_serverIp), SERVER_TCP_PORT);
                    using (var stream = tcp.GetStream())
                    using (var writer = new System.IO.StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.NewLine = "\n";
                        writer.AutoFlush = true;

                        // Gửi: FORGOT_PASSWORD|email
                        var request = $"FORGOT_PASSWORD|{email}";
                        await writer.WriteLineAsync(request);

                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            _lblStatus.Text = "Không nhận được phản hồi từ server.";
                            return;
                        }

                        var parts = line.Split('|');
                        if (parts.Length == 0)
                        {
                            _lblStatus.Text = "Phản hồi không hợp lệ.";
                            return;
                        }

                        if (parts[0] == "FORGOT_OK")
                        {
                            // FORGOT_OK|message
                            var msg = parts.Length >= 2 ? parts[1] : "Đã gửi mật khẩu mới qua email.";
                            MessageBox.Show(
                                msg,
                                "Khôi phục mật khẩu",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else if (parts[0] == "FORGOT_FAIL")
                        {
                            // FORGOT_FAIL|REASON|Message
                            var reason = parts.Length >= 2 ? parts[1] : "";
                            var msg = parts.Length >= 3 ? parts[2] : "Khôi phục mật khẩu thất bại.";

                            _lblStatus.Text = msg;

                            if (reason == "EMAIL_NOT_FOUND")
                            {
                                _lblStatus.Text = "Email chưa được đăng ký trong hệ thống.";
                            }
                        }
                        else
                        {
                            _lblStatus.Text = "Phản hồi không hỗ trợ: " + line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                _btnSend.Enabled = true;
            }
        }
    }
}

// BookingServer/Form1.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BookingServer;

public partial class Form1 : Form
{
    private TextBox _txtLog = null!;
    private Button _btnStart = null!;
    private TcpListener? _listener;
    private bool _running = false;
    private readonly ServerState _state = new();

    public Form1()
    {
        InitializeComponent();
        SetupUi();
        _state.InitSlots();
    }

    private void SetupUi()
    {
        this.Text = "Server - Centralized Mutex";
        this.Width = 800;
        this.Height = 500;

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

        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Left = 10,
            Top = 50,
            Width = 760,
            Height = 400
        };
        this.Controls.Add(_txtLog);
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_running) return;

        int port = 5000;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _running = true;
        Log($"[SERVER] Listening on port {port}...");

        while (_running)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Log("[SERVER] New client connected");
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        using (tcpClient)
        using (var stream = tcpClient.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            string? line;
            string? clientId = null;

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
                        case "REQUEST":
                            if (parts.Length != 4)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid REQUEST format\n");
                                break;
                            }
                            clientId ??= parts[1];
                            _state.HandleRequest(parts[1], parts[2], parts[3], stream, new UiLogger(this));
                            break;

                        case "RELEASE":
                            if (parts.Length != 4)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid RELEASE format\n");
                                break;
                            }
                            clientId ??= parts[1];
                            _state.HandleRelease(parts[1], parts[2], parts[3], stream, new UiLogger(this));
                            break;

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
                    // Xử lý auto RELEASE / remove from queue khi client mất kết nối
                    _state.HandleDisconnect(clientId, new UiLogger(this));
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
}

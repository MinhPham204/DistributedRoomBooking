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
    private Button _btnStart = null!;
    private DataGridView _gridSlots = null!;
    private Label _lblQueueTitle = null!;
    private ListBox _lstQueue = null!;
    private TextBox _txtLog = null!;

    private TcpListener? _listener;
    private bool _running = false;
    private readonly ServerState _state = new();

    public Form1()
    {
        InitializeComponent();
        SetupUi();
        _state.InitSlots();
        RefreshSlotsSafe();
    }

    private void SetupUi()
    {
        this.Text = "Server - Centralized Mutual Exclusion";
        this.Width = 900;
        this.Height = 550;
        this.StartPosition = FormStartPosition.CenterScreen;

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

        // Bảng tổng quan slot
        _gridSlots = new DataGridView
        {
            Left = 10,
            Top = 50,
            Width = 400,
            Height = 450,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _gridSlots.SelectionChanged += GridSlots_SelectionChanged;
        this.Controls.Add(_gridSlots);

        _lblQueueTitle = new Label
        {
            Left = 420,
            Top = 50,
            Width = 450,
            Height = 20,
            Text = "Queue for: (select a room/slot)"
        };
        this.Controls.Add(_lblQueueTitle);

        _lstQueue = new ListBox
        {
            Left = 420,
            Top = 75,
            Width = 450,
            Height = 150
        };
        this.Controls.Add(_lstQueue);

        _txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Left = 420,
            Top = 240,
            Width = 450,
            Height = 260
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

        RefreshSlotsSafe();

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
        using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
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
                            RefreshSlotsSafe();
                            break;

                        case "RELEASE":
                            if (parts.Length != 4)
                            {
                                await SendAsync(stream, "INFO|ERROR|Invalid RELEASE format\n");
                                break;
                            }
                            clientId ??= parts[1];
                            _state.HandleRelease(parts[1], parts[2], parts[3], stream, new UiLogger(this));
                            RefreshSlotsSafe();
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
    }

    private void GridSlots_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateQueueViewForSelected();
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

        _lblQueueTitle.Text = $"Queue for: {roomId}-{slotId}";

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
}

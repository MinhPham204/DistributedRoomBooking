    /// <summary>
    /// Returns the Booking for a slot, or a pseudo-booking if the slot is event-locked but has no booking.
    /// </summary>
    

// BookingServer/ServerState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using BC = BCrypt.Net.BCrypt;
using BCryptNet = BCrypt.Net.BCrypt;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace BookingServer;

// Trạng thái 1 slot (phòng + ca)
class SlotState
{
    public bool IsBusy { get; set; }
    public string? CurrentHolderClientId { get; set; }   // ai đang giữ slot này
    public string? CurrentHolderUserId { get; set; }     // ✅ chuẩn nghiệp vụ    

    // NEW: Booking tương ứng với slot này (nếu đã tạo record Booking)
    public Guid? CurrentBookingId { get; set; }
    public bool IsEventLocked { get; set; } = false;
    // NEW: ghi chú ngắn (ví dụ: "Event Khoa CNTT", "Hội thảo ABC")
    public string? EventNote { get; set; }

    public Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)> WaitingQueue { get; } = new();

}

// Dùng để hiển thị lên DataGridView trên UI server
public class SlotSummary
{
    public string Date { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string SlotId { get; set; } = "";   // "S1" .. "S14"
    public string Status { get; set; } = "";   // "FREE" / "BUSY"
    public string Holder { get; set; } = "";   // Client đang giữ (nếu có)
    public int QueueLength { get; set; }       // Số client đang chờ
    public bool IsEventLocked { get; set; }

}

class ServerState{
    // ===== FIXED SCHEDULE INDEX =====
    // dateKey -> list session
    private readonly Dictionary<string, List<FixedSession>> _fixedSessionsByDate = new();
    // userId -> set sessionId
    private readonly Dictionary<string, HashSet<Guid>> _fixedSessionIdsByUser = new(StringComparer.OrdinalIgnoreCase);

    // Tạo danh sách ngày trong khoảng from..to theo thứ trong tuần
    
    private const int SlotCount = 14; // ca 1..14
                                      // KEY dạng "A08::RANGE::S3-S6"
    private readonly Dictionary<string, Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>> _rangeQueues
        = new();


    // SlotId sẽ là "S1".."S14"
    private static string GetSlotId(int index) => $"S{index}";

    // ===== STATE THEO NGÀY =====
    // dateKey = "yyyy-MM-dd" -> (room::slot) -> SlotState
    private readonly Dictionary<string, Dictionary<string, SlotState>> _slotsByDate = new();
    private readonly object _lock = new();
    public event Action? StateChanged;
    public event Action? FixedScheduleCreated; // ✅ Event riêng cho fixed schedule

    private string _currentDateKey = DateTime.Today.ToString("yyyy-MM-dd");
    // ===== DATA MÔ HÌNH THỰC TẾ (ROOMS / USERS / BOOKINGS) =====

    // Thông tin phòng (RoomInfo) key theo RoomId (A08, A16,...)
    private readonly Dictionary<string, RoomInfo> _rooms = new();

    // Thông tin người dùng (UserInfo) key theo UserId (sv001, gv001, admin,...)
    private readonly Dictionary<string, UserInfo> _users = new(StringComparer.OrdinalIgnoreCase);


    // Danh sách booking "thực tế" – sẽ dùng ở các milestone sau
    private readonly List<Booking> _bookings = new();

    // ===== FIXED SCHEDULE DATA =====
    // Danh sách các FixedSession (lịch cố định)
    private readonly List<FixedSession> _fixedSessions = new();
    // (Đã loại bỏ _fixedSessionByUser, chỉ dùng _fixedSessionIdsByUser)

    // Expose read-only cho UI/logic khác (dùng ở milestone sau)
    public IReadOnlyDictionary<string, RoomInfo> RoomsInfo => _rooms;
    public IReadOnlyDictionary<string, UserInfo> UsersInfo => _users;
    public IReadOnlyList<Booking> Bookings => _bookings;
    // ===== App settings (Tab Settings) =====
    private AppSettings _settings = AppSettings.CreateDefault();
    private const string SettingsFileName = "appsettings.json";
    private const string RoomsFileName = "rooms.json";

    // Mapping mỗi connection (clientId) -> userId thật
    private readonly Dictionary<string, string> _clientToUser = new();
    private readonly object _clientMapLock = new();
    // Subscriptions
    private readonly object _subLock = new();
    private readonly Dictionary<string, HashSet<string>> _subHome
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HashSet<string>> _subMyBookings
        = new(StringComparer.OrdinalIgnoreCase);

    // key = $"{roomId}::{dateKey}"
    private readonly Dictionary<string, HashSet<string>> _subRoomSlots
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _homeSubsByUser = new(); // userId -> set(clientId)

    private static string RoomDateKey(string roomId, string dateKey)
=> $"{roomId}::{dateKey}";
    public void SubMyBookings(string clientId, string userId)
    {
        lock (_subLock)
        {
            if (!_subMyBookings.TryGetValue(userId, out var set))
                _subMyBookings[userId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            set.Add(clientId);
        }
    }

    public void UnsubMyBookings(string clientId, string userId)
    {
        lock (_subLock)
        {
            if (_subMyBookings.TryGetValue(userId, out var set))
            {
                set.Remove(clientId);
                if (set.Count == 0) _subMyBookings.Remove(userId);
            }
        }
    }

    public void SubRoomSlots(string clientId, string roomId, string dateKey)
    {
        var k = RoomDateKey(roomId, dateKey);
        lock (_subLock)
        {
            if (!_subRoomSlots.TryGetValue(k, out var set))
                _subRoomSlots[k] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            set.Add(clientId);
        }
    }

    public void UnsubRoomSlots(string clientId, string roomId, string dateKey)
    {
        var k = RoomDateKey(roomId, dateKey);
        lock (_subLock)
        {
            if (_subRoomSlots.TryGetValue(k, out var set))
            {
                set.Remove(clientId);
                if (set.Count == 0) _subRoomSlots.Remove(k);
            }
        }
    }

    public AppSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }


    private bool _useDemoNow;
    private DateTime _demoLogicalStart; // thời gian demo mà bạn chọn
    private DateTime _demoRealStart;    // thời gian hệ thống khi bắt đầu demo

    public DateTime Now
    {
        get
        {
            lock (_lock)
            {
                if (!_useDemoNow)
                {
                    // Dùng thời gian hệ thống bình thường
                    return DateTime.Now;
                }

                // Demo time: chạy theo delta của thời gian thật
                var delta = DateTime.Now - _demoRealStart;
                return _demoLogicalStart + delta;
            }
        }
    }


    public void SetDemoNow(DateTime demoNow, TextWriter log)
    {
        lock (_lock)
        {
            _demoLogicalStart = demoNow;
            _demoRealStart = DateTime.Now;
            _useDemoNow = true;

            log.WriteLine($"[TIME] Switch to DEMO time: {_demoLogicalStart:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public void ResetDemoNow(TextWriter log)
    {
        lock (_lock)
        {
            _useDemoNow = false;
            log.WriteLine("[TIME] Switch back to SYSTEM time");
        }
    }


    private static readonly string[] FacultyList =
{
        "CNTT2", "IOT2", "MKT2", "CNDPT2",
        "KT2", "DTVT2", "QTKD2", "ATTT2"
    };

    private static readonly HashSet<string> FacultySet =
        new HashSet<string>(FacultyList, StringComparer.OrdinalIgnoreCase);

    // Constructor: seed dữ liệu demo
    public ServerState()
    {
        LoadRooms();
        InitDemoData();
        LoadSettings(); // <- load appsettings.json nếu có
        // Start background timer to sweep queued bookings and cancel overdue ones
        var timer = new System.Threading.Timer(_ => CancelOverdueQueuedBookings(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

    }
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFileName))
            {
                var json = File.ReadAllText(SettingsFileName);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null && loaded.SlotTimes != null && loaded.SlotTimes.Count == 14)
                {
                    _settings = loaded;
                    return;
                }
            }
        }
        catch
        {
            // ignore lỗi, dùng default
        }

        _settings = AppSettings.CreateDefault();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFileName, json);
        }
        catch
        {
            // có thể log nếu muốn
        }
    }

    private void LoadRooms()
    {
        try
        {
            if (!File.Exists(RoomsFileName))
                return;

            var json = File.ReadAllText(RoomsFileName);
            var loaded = JsonSerializer.Deserialize<List<RoomInfo>>(json);
            if (loaded == null)
                return;

            lock (_lock)
            {
                _rooms.Clear();
                foreach (var r in loaded)
                {
                    if (r == null) continue;
                    r.RoomId = (r.RoomId ?? "").Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(r.RoomId)) continue;
                    r.Status = NormalizeRoomStatus(r.Status);
                    _rooms[r.RoomId] = r;
                }
            }
        }
        catch
        {
            // ignore lỗi, dùng demo data
        }
    }

    private static string NormalizeRoomStatus(string? status)
    {
        var s = (status ?? "").Trim().ToUpperInvariant();
        if (string.Equals(s, "DISABLED", StringComparison.OrdinalIgnoreCase))
            return "DISABLED";
        return "ACTIVE";
    }

    private bool IsRoomDisabled(string roomId)
    {
        roomId = (roomId ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(roomId))
            return false;

        if (!_rooms.TryGetValue(roomId, out var r) || r == null)
            return false;

        return string.Equals(NormalizeRoomStatus(r.Status), "DISABLED", StringComparison.OrdinalIgnoreCase);
    }

    private void SaveRooms()
    {
        try
        {
            List<RoomInfo> snapshot;
            lock (_lock)
            {
                snapshot = _rooms.Values
                    .Select(r => new RoomInfo
                    {
                        RoomId = (r.RoomId ?? "").Trim().ToUpperInvariant(),
                        Building = r.Building ?? "",
                        Capacity = r.Capacity,
                        HasProjector = r.HasProjector,
                        HasPC = r.HasPC,
                        HasAirConditioner = r.HasAirConditioner,
                        HasMic = r.HasMic,
                        Status = NormalizeRoomStatus(r.Status)
                    })
                    .ToList();
            }

            var json = JsonSerializer.Serialize(snapshot,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RoomsFileName, json);
        }
        catch
        {
            // ignore
        }
    }

    public void UpdateSettings(AppSettings newSettings)
    {
        if (newSettings == null) return;

        lock (_lock)
        {
            _settings = newSettings;
            PushSlotConfigChanged();
            SaveSettings();
        }
    }

    // Cập nhật ngày hiện tại từ UI server
    public void SetCurrentDate(DateTime date, TextWriter log)
    {
        var key = date.ToString("yyyy-MM-dd");
        lock (_lock)
        {
            _currentDateKey = key;
            EnsureDateInitialized(key, log);
        }
    }

    /// Seed dữ liệu demo cho Rooms / Users.
    private void InitDemoData()
    {
        // Seed một vài user demo (Student / Lecturer / Staff)
        if (_users.Count == 0)
        {
            _users["admin"] = new UserInfo
            {
                UserId = "admin",
                UserType = "Staff",                  // dùng như Admin hệ thống
                FullName = "Phòng Đào Tạo",
                Department = "Phòng Đào Tạo",
                Email = "admin@example.com",
                Phone = "0900000004",
                PasswordHash = BC.HashPassword("admin123"),
                IsActive = true
            };
        }

        // _bookings: hiện tại để rỗng,
        // sẽ được thêm record khi GRANT/RELEASE ở Milestone 3.
    }

    private void EnsureDateInitialized(string dateKey, TextWriter log)
    {
        if (_slotsByDate.ContainsKey(dateKey)) return;

        var dict = new Dictionary<string, SlotState>();
        foreach (var room in _rooms.Keys)
        {
            for (int i = 1; i <= SlotCount; i++)
            {
                var slotId = GetSlotId(i);
                var key = MakeKey(room, slotId);
                dict[key] = new SlotState
                {
                    IsBusy = false,
                    CurrentHolderClientId = null,
                    CurrentHolderUserId = null
                };
            }
        }

        _slotsByDate[dateKey] = dict;
        log.WriteLine($"[INIT] Created slot state for date {dateKey}");
    }

    private string MakeKey(string roomId, string slotId) => $"{roomId}::{slotId}";

    // =========================
    // M1 - ONLINE SESSIONS
    // =========================
    public class ClientSession
    {
        public string ClientId { get; set; } = "";
        public string? UserId { get; set; }           // null trước login
        public NetworkStream Stream { get; set; } = null!;
        public DateTime LastSeen { get; set; }
    }

    private readonly object _sessionLock = new();

    // clientId -> session
    private readonly Dictionary<string, ClientSession> _sessions = new();

    // userId -> list clientId (1 user có thể login nhiều nơi)
    private readonly Dictionary<string, HashSet<string>> _userToClients = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _notifyLock = new();
    private readonly Dictionary<string, Queue<string>> _pendingNotifications =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _homeNotiLock = new();
    private readonly Dictionary<string, Queue<string>> _homeNotifications =
        new(StringComparer.OrdinalIgnoreCase);

    private string? TryConvertNotifyLineToHomeMessage(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        var trimmed = line.Trim();
        if (trimmed.EndsWith("\n", StringComparison.Ordinal))
            trimmed = trimmed.TrimEnd('\n');

        if (!trimmed.StartsWith("NOTIFY_", StringComparison.OrdinalIgnoreCase))
            return null;

        var p = trimmed.Split('|');
        if (p.Length == 0) return null;

        if (trimmed.StartsWith("NOTIFY_GRANT|", StringComparison.OrdinalIgnoreCase) && p.Length >= 5)
            return $"Phòng {p[1]} ca {p[2]} ngày {p[3]} đã được grant. Vui lòng check-in trước {p[4]}.";

        if (trimmed.StartsWith("NOTIFY_CHECKIN|", StringComparison.OrdinalIgnoreCase) && p.Length >= 5)
            return $"Bạn đã check-in phòng {p[1]} ca {p[2]} ngày {p[3]} lúc {p[4]}.";

        if (trimmed.StartsWith("NOTIFY_COMPLETE|", StringComparison.OrdinalIgnoreCase) && p.Length >= 5)
            return $"Booking phòng {p[1]} ca {p[2]} ngày {p[3]} đã hoàn thành lúc {p[4]}.";

        if (trimmed.StartsWith("NOTIFY_NO_SHOW|", StringComparison.OrdinalIgnoreCase) && p.Length >= 5)
            return $"⚠️ Bạn đã bị NO-SHOW: phòng {p[1]} ca {p[2]} ngày {p[3]} (quá deadline check-in {p[4]}).";

        if (trimmed.StartsWith("NOTIFY_FIXED|", StringComparison.OrdinalIgnoreCase) && p.Length >= 5)
            return $"Fixed Schedule: {p[4]} in {p[1]}-{p[2]} on {p[3]}";

        if (trimmed.StartsWith("NOTIFY_FIXED_CREATED|", StringComparison.OrdinalIgnoreCase) && p.Length >= 8)
        {
            var slotRange = p[4] == p[5] ? p[4] : $"{p[4]}-{p[5]}";
            return $"Bạn có lịch cố định mới: {p[0]} {p[1]} - {p[2]} | {p[3]} | {slotRange} | {p[6]} -> {p[7]}";
        }

        if (trimmed.StartsWith("NOTIFY_FIXED_DELETED|", StringComparison.OrdinalIgnoreCase) && p.Length >= 8)
        {
            var slotRange = p[4] == p[5] ? p[4] : $"{p[4]}-{p[5]}";
            return $"Lịch cố định đã bị xoá: {p[0]} {p[1]} - {p[2]} | {p[3]} | {slotRange} | {p[6]} -> {p[7]}";
        }

        return trimmed;
    }

    private void AddHomeNotification(string userId, string message)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message))
            return;

        lock (_homeNotiLock)
        {
            if (!_homeNotifications.TryGetValue(userId, out var q))
                _homeNotifications[userId] = q = new Queue<string>();

            if (q.Count >= 200)
                q.Dequeue();

            q.Enqueue(message);
        }
    }

    public List<string> GetHomeNotifications(string userId, int max = 20)
    {
        if (string.IsNullOrWhiteSpace(userId) || max <= 0)
            return new List<string>();

        lock (_homeNotiLock)
        {
            if (!_homeNotifications.TryGetValue(userId, out var q) || q.Count == 0)
                return new List<string>();

            return q.Reverse().Take(max).ToList();
        }
    }

    public void RegisterClient(string clientId, NetworkStream stream)
    {
        lock (_sessionLock)
        {
            _sessions[clientId] = new ClientSession
            {
                ClientId = clientId,
                UserId = null,
                Stream = stream,
                LastSeen = Now
            };
        }
    }

    public void BindUser(string clientId, string userId)
    {
        lock (_sessionLock)
        {
            if (!_sessions.TryGetValue(clientId, out var ss))
                return;

            ss.UserId = userId;
            ss.LastSeen = Now;

            if (!_userToClients.TryGetValue(userId, out var set))
            {
                set = new HashSet<string>();
                _userToClients[userId] = set;
            }
            set.Add(clientId);

            // (tuỳ chọn) nếu bạn vẫn đang dùng map cũ client->user ở nơi khác,
            // thì gọi luôn để đồng bộ (nếu method này tồn tại trong code bạn).
            // MapClientToUser(clientId, userId);
        }
    }

    public void UnregisterClient(string clientId)
    {
        lock (_sessionLock)
        {
            if (_sessions.TryGetValue(clientId, out var ss))
            {
                if (!string.IsNullOrWhiteSpace(ss.UserId) &&
                    _userToClients.TryGetValue(ss.UserId!, out var set))
                {
                    set.Remove(clientId);
                    if (set.Count == 0) _userToClients.Remove(ss.UserId!);
                }

                _sessions.Remove(clientId);
            }
        }
    }

    public bool SendToClient(string clientId, string msg)
    {
        ClientSession? ss;
        lock (_sessionLock)
        {
            _sessions.TryGetValue(clientId, out ss);
            if (ss != null) ss.LastSeen = Now;
        }

        if (ss == null) return false;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            ss.Stream.Write(bytes, 0, bytes.Length);
            return true;
        }
        catch
        {
            // stream chết -> cleanup
            UnregisterClient(clientId);
            return false;
        }
    }

    public int SendToUser(string userId, string msg)
    {
        List<string> clientIds;
        lock (_sessionLock)
        {
            if (!_userToClients.TryGetValue(userId, out var set) || set.Count == 0)
                return 0;
            clientIds = set.ToList();
        }

        int ok = 0;
        foreach (var cid in clientIds)
            if (SendToClient(cid, msg)) ok++;

        return ok;
    }

    public void FlushPendingNotifications(string userId, TextWriter? log = null)
    {
        Queue<string>? q;
        lock (_notifyLock)
        {
            if (!_pendingNotifications.TryGetValue(userId, out q) || q.Count == 0)
                return;
        }

        int delivered = 0;
        while (true)
        {
            string? msg;
            lock (_notifyLock)
            {
                if (!_pendingNotifications.TryGetValue(userId, out q) || q.Count == 0)
                    break;
                msg = q.Dequeue();
                if (q.Count == 0) _pendingNotifications.Remove(userId);
            }

            if (!string.IsNullOrEmpty(msg))
            {
                SendToUser(userId, msg);
                delivered++;
            }
        }

        log?.WriteLine($"[NOTIFY] Flushed pending notifications for {userId}: {delivered}");
    }

    private void NotifyUser(string userId, string msgLine)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(msgLine))
            return;

        var line = msgLine.EndsWith("\n", StringComparison.Ordinal) ? msgLine : (msgLine + "\n");

        try
        {
            var homeMsg = TryConvertNotifyLineToHomeMessage(line);
            if (!string.IsNullOrWhiteSpace(homeMsg))
                AddHomeNotification(userId, homeMsg);
        }
        catch { }

        var sent = SendToUser(userId, line);
        if (sent > 0) return;

        lock (_notifyLock)
        {
            if (!_pendingNotifications.TryGetValue(userId, out var q))
                _pendingNotifications[userId] = q = new Queue<string>();

            if (q.Count >= 200)
                q.Dequeue();
            q.Enqueue(line);
        }
    }


    // Lấy danh sách summary cho tất cả slot của ngày hiện tại -> hiển thị lên grid
    public List<SlotSummary> GetAllSlotSummaries()
    {
        var result = new List<SlotSummary>();

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(_currentDateKey, out var dict))
                return result;

            foreach (var kvp in dict)
            {
                var key = kvp.Key;
                var slot = kvp.Value;

                var parts = key.Split(new[] { "::" }, StringSplitOptions.None);
                var roomId = parts[0];
                var slotId = parts.Length > 1 ? parts[1] : "?";

                var status = "FREE";
                if (slot.IsEventLocked)
                {
                    // nếu đang lock + có người đang dùng (do admin FORCE_GRANT) -> BUSY_EVT
                    status = slot.IsBusy ? "BUSY_EVT" : "LOCKED";
                }
                else
                {
                    status = slot.IsBusy ? "BUSY" : "FREE";
                }

                var holderUserId = !string.IsNullOrEmpty(slot.CurrentHolderUserId)
                    ? slot.CurrentHolderUserId
                    : (!string.IsNullOrEmpty(slot.CurrentHolderClientId)
                        ? (GetUserIdForClient(slot.CurrentHolderClientId) ?? slot.CurrentHolderClientId)
                        : "");

                result.Add(new SlotSummary
                {
                    Date = _currentDateKey,
                    RoomId = roomId,
                    SlotId = slotId,
                    Status = status,
                    Holder = holderUserId,
                    QueueLength = slot.WaitingQueue.Count,
                    IsEventLocked = slot.IsEventLocked
                });
            }
        }

        return result;
    }

    public (bool Success, string? UserType, string Error) ValidateUserCredentials(string userId, string password)
    {
        userId = (userId ?? "").Trim().ToUpperInvariant();
        if (!_users.TryGetValue(userId, out var user))
            return (false, null, "User not found");

        if (!user.IsActive)
            return (false, null, "User inactive");

        // kiểm tra BCrypt
        if (!BCryptNet.Verify(password, user.PasswordHash))
            return (false, null, "Invalid password");

        return (true, user.UserType, "");
    }
    public UserInfo? FindUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        lock (_lock)
        {
            foreach (var u in _users.Values)
            {
                if (!string.IsNullOrEmpty(u.Email) &&
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return u;
                }
            }
        }

        return null;
    }
    public void UnsubscribeAll(string clientId)
    {
        lock (_subLock)
        {
            foreach (var kv in _subMyBookings.Values) kv.Remove(clientId);
            foreach (var kv in _subRoomSlots.Values) kv.Remove(clientId);

            // dọn key rỗng
            var emptyUsers = _subMyBookings.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList();
            foreach (var u in emptyUsers) _subMyBookings.Remove(u);

            var emptyRooms = _subRoomSlots.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList();
            foreach (var k in emptyRooms) _subRoomSlots.Remove(k);

            foreach (var kv in _subHome.Values) kv.Remove(clientId);
            var emptyHome = _subHome.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList();
            foreach (var u in emptyHome) _subHome.Remove(u);

            _subRooms.Remove(clientId);
            _subSlotConfig.Remove(clientId);
        }
    }

    public bool IsAdmin(string userId)
    {
        if (!_users.TryGetValue(userId, out var user)) return false;
        return user.UserType == "Staff" || user.UserType == "Admin";
    }

    // Lấy queue cụ thể cho 1 (room, slot) của ngày hiện tại -> hiển thị chi tiết hàng đợi
    public List<string> GetQueueClients(string dateKey, string roomId, string slotId)
    {
        var key = MakeKey(roomId, slotId);

        lock (_lock)
        {
            var result = new List<string>();

            // ===== 1) SINGLE queue của slot theo đúng ngày =====
            if (_slotsByDate.TryGetValue(dateKey, out var dict) &&
                dict.TryGetValue(key, out var slot))
            {
                result.AddRange(
                    slot.WaitingQueue.Select(q => GetUserIdForClient(q.clientId) ?? q.clientId)
                );
            }

            // ===== 2) RANGE queues: (đang global _rangeQueues nên giữ nguyên) =====
            int slotIdx = ParseSlotIndex(slotId);
            if (slotIdx > 0)
            {
                foreach (var kv in _rangeQueues)
                {
                    var rangeKey = kv.Key; // "A16::RANGE::S5-S6"
                    if (!rangeKey.StartsWith(roomId + "::RANGE::", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var tail = rangeKey.Substring((roomId + "::RANGE::").Length);
                    var dash = tail.IndexOf('-');
                    if (dash <= 0) continue;

                    var fromId = tail.Substring(0, dash);
                    var toId = tail.Substring(dash + 1);

                    int fromIdx = ParseSlotIndex(fromId);
                    int toIdx = ParseSlotIndex(toId);
                    if (fromIdx <= 0 || toIdx <= 0) continue;

                    if (slotIdx < fromIdx || slotIdx > toIdx) continue;

                    foreach (var item in kv.Value)
                    {
                        var uid = GetUserIdForClient(item.clientId) ?? item.clientId;
                        result.Add($"{uid} (RANGE {fromId}-{toId})");
                    }
                }
            }

            return result;
        }
    }


    public bool LockSlotForEvent(DateTime date, string roomId, string slotId, string? note, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            EnsureDateInitialized(dateKey, log);

            if (!_slotsByDate.TryGetValue(dateKey, out var dict))
            {
                error = "Không tìm thấy dữ liệu ngày.";
                return false;
            }

            var key = MakeKey(roomId, slotId);
            if (!dict.TryGetValue(key, out var slot))
            {
                error = "Không tìm thấy phòng/ca.";
                return false;
            }

            // Tùy bạn: có cho lock khi đang bận hay không
            if (slot.IsBusy)
            {
                error = "Slot đang có người sử dụng, hãy giải phóng trước khi lock cho event.";
                return false;
            }

            slot.IsEventLocked = true;
            slot.EventNote = note;
            PushSlotUpdate(roomId, dateKey, slotId, log);

            log.WriteLine($"[EVENT_LOCK] {dateKey} {roomId}-{slotId} note={note}");
            return true;
        }
    }

    public bool UnlockSlotFromEvent(DateTime date, string roomId, string slotId, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var dict))
            {
                error = "Không tìm thấy dữ liệu ngày.";
                return false;
            }

            var key = MakeKey(roomId, slotId);
            if (!dict.TryGetValue(key, out var slot))
            {
                error = "Không tìm thấy phòng/ca.";
                return false;
            }

            if (!slot.IsEventLocked)
            {
                error = "Slot này không ở trạng thái lock event.";
                return false;
            }

            // chỉ nên unlock khi đang rảnh (không có người giữ)
            if (slot.IsBusy)
            {
                error = "Slot đang có booking, không thể unlock event.";
                return false;
            }

            slot.IsEventLocked = false;
            slot.EventNote = null;
            PushSlotUpdate(roomId, dateKey, slotId, log);

            log.WriteLine($"[EVENT_UNLOCK] {dateKey} {roomId}-{slotId}");
            return true;
        }
    }

    // Tạo lịch cố định: chỉ tạo FixedSession, không tạo booking per-user
    public bool CreateFixedWeeklyClassSchedule(string subjectCode, string subjectName, string className, string roomId, DayOfWeek dayOfWeek, string slotStartId, string slotEndId, DateTime fromDate, DateTime toDate, TextWriter log, out string error, string lecturerId = "", List<string>? studentIds = null)
    {
        error = "";
        try
        {
            log.WriteLine($"[FIXED_SESSION][START] Creating fixed schedule:");
            log.WriteLine($"  Subject: {subjectCode} - {subjectName}");
            log.WriteLine($"  Class: {className}");
            log.WriteLine($"  Room: {roomId}");
            log.WriteLine($"  DayOfWeek: {dayOfWeek}");
            log.WriteLine($"  Slot: {slotStartId} to {slotEndId}");
            log.WriteLine($"  Date range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
            log.WriteLine($"  Lecturer: {lecturerId}");
            log.WriteLine($"  Students: {(studentIds != null ? string.Join(",", studentIds) : "(none)")}");

            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (string.IsNullOrWhiteSpace(roomId))
            {
                error = "RoomId không được để trống.";
                log.WriteLine($"[FIXED_SESSION][ERROR] {error}");
                return false;
            }
            if (fromDate.Date > toDate.Date)
            {
                error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
                log.WriteLine($"[FIXED_SESSION][ERROR] {error}");
                return false;
            }
            if (startIdx <= 0 || endIdx <= 0 || startIdx > endIdx || endIdx > SlotCount)
            {
                error = "Khoảng ca học không hợp lệ (slot start / end).";
                log.WriteLine($"[FIXED_SESSION][ERROR] {error}");
                return false;
            }
            // Ghi chú hiển thị trên Event lock
            string note = $"{subjectCode} - {subjectName} ({className})".Trim();
            if (!string.IsNullOrWhiteSpace(lecturerId))
                note += $" | GV: {lecturerId}";
            if (studentIds != null && studentIds.Count > 0)
                note += $" | SV: {string.Join(",", studentIds)}";
            if (string.IsNullOrWhiteSpace(note))
                note = "Lịch môn học cố định";

            // Tạo FixedSession
            var session = new FixedSession
            {
                SubjectCode = subjectCode,
                SubjectName = subjectName,
                Class = className,
                LecturerUserId = lecturerId,
                StudentUserIds = studentIds != null ? new List<string>(studentIds) : new List<string>(),
                RoomId = roomId,
                DayOfWeek = dayOfWeek.ToString(),
                SlotStartId = slotStartId,
                SlotEndId = slotEndId,
                DateFrom = fromDate.ToString("yyyy-MM-dd"),
                DateTo = toDate.ToString("yyyy-MM-dd"),
                Note = note,
                CreatedAt = Now,
                UpdatedAt = Now
            };
            // Add participants
            if (!string.IsNullOrWhiteSpace(lecturerId))
            {
                session.Participants.Add(new FixedParticipant { UserId = lecturerId, SessionId = session.SessionId, Role = "Lecturer" });
            }
            if (studentIds != null)
            {
                foreach (var sid in studentIds)
                {
                    session.Participants.Add(new FixedParticipant { UserId = sid, SessionId = session.SessionId, Role = "Student" });
                }
            }
            log.WriteLine($"[FIXED_SESSION][DETAILS] SessionId: {session.SessionId}");
            log.WriteLine($"[FIXED_SESSION][DETAILS] Note: {note}");
            log.WriteLine($"[FIXED_SESSION][DETAILS] Participants: {string.Join(", ", session.Participants.Select(p => p.UserId + ":" + p.Role))}");

            // Add to main list
            _fixedSessions.Add(session);
            // Cập nhật index phụ _fixedSessionIdsByUser
            if (!string.IsNullOrWhiteSpace(lecturerId))
            {
                if (!_fixedSessionIdsByUser.TryGetValue(lecturerId, out var set))
                    _fixedSessionIdsByUser[lecturerId] = set = new HashSet<Guid>();
                set.Add(session.SessionId);
            }
            if (studentIds != null)
            {
                foreach (var sid in studentIds)
                {
                    if (!_fixedSessionIdsByUser.TryGetValue(sid, out var set))
                        _fixedSessionIdsByUser[sid] = set = new HashSet<Guid>();
                    set.Add(session.SessionId);
                }
            }

            // Lock các slot event cho từng ngày phù hợp
            var current = fromDate.Date;
            var affectedSlots = new List<string>();
            while (current <= toDate.Date)
            {
                if (current.DayOfWeek == dayOfWeek)
                {
                    var dateKey = current.ToString("yyyy-MM-dd");
                    log.WriteLine($"[FIXED_SESSION] Apply {note} {roomId} {slotStartId}-{slotEndId} on {dateKey}");
                    for (int idx = startIdx; idx <= endIdx; idx++)
                    {
                        var slotId = $"S{idx}";
                        if (!LockSlotForEvent(current, roomId, slotId, note, log, out var err))
                        {
                            error = $"Không thể khóa {roomId}-{slotId} vào ngày {dateKey}: {err}";
                            log.WriteLine($"[FIXED_SESSION][ERROR] {error}");
                            return false;
                        }
                        // Đánh dấu ngày đã lock trong session
                        session.LockedDates.Add(dateKey);
                        affectedSlots.Add($"{dateKey} {roomId}-{slotId}");
                    }
                }
                current = current.AddDays(1);
            }
            log.WriteLine($"[FIXED_SESSION][AFFECTED] Total slots locked: {affectedSlots.Count}");
            foreach (var s in affectedSlots) log.WriteLine($"[FIXED_SESSION][AFFECTED] {s}");

            // Push reload cho các user liên quan
            var allUserIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(lecturerId))
            {
                allUserIds.Add(lecturerId);
                PushFixedSessionsChanged(lecturerId);
                PushHomeChanged(lecturerId);
            }
            if (studentIds != null)
            {
                foreach (var sid in studentIds)
                {
                    allUserIds.Add(sid);
                    PushFixedSessionsChanged(sid);
                    PushHomeChanged(sid);
                }
            }

            var fixedNotify =
                $"NOTIFY_FIXED_CREATED|{SafeField(subjectCode)}|{SafeField(subjectName)}|{SafeField(roomId)}|{SafeField(slotStartId)}|{SafeField(slotEndId)}|{fromDate:yyyy-MM-dd}|{toDate:yyyy-MM-dd}";
            foreach (var uid in allUserIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                NotifyUser(uid, fixedNotify);
            }

            if (_settings.SendEmailOnForceGrantRelease)
            {
                var emailSubject = "[Room booking] Bạn có lịch cố định mới";
                foreach (var uid in allUserIds.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var body =
                        "Bạn vừa được thêm vào một lịch cố định.\n\n" +
                        $"- Môn: {subjectCode} - {subjectName}\n" +
                        $"- Lớp: {className}\n" +
                        $"- Phòng: {roomId}\n" +
                        $"- Ca: {slotStartId} - {slotEndId}\n" +
                        $"- Từ ngày: {fromDate:yyyy-MM-dd}\n" +
                        $"- Đến ngày: {toDate:yyyy-MM-dd}\n" +
                        $"- Thứ: {dayOfWeek}";
                    QueueEmailToUser(uid, emailSubject, body, log);
                }
            }
            
            // Push fixed schedule ngay lập tức cho tất cả participants
            log.WriteLine($"[FIXED_SESSION][PUSH] About to push to {allUserIds.Distinct().Count()} participants");
            foreach (var uid in allUserIds.Distinct())
            {
                log.WriteLine($"[FIXED_SESSION][PUSH] Pushing to {uid}");
                PushMyFixedSchedule(uid);
            }
            
            // Raise StateChanged để server UI update ngay lập tức
            log.WriteLine($"[FIXED_SESSION][RAISE] Calling RaiseStateChanged");
            RaiseStateChanged();
            
            // ✅ Raise event riêng cho fixed schedule
            log.WriteLine($"[FIXED_SESSION][EVENT] Raising FixedScheduleCreated event");
            FixedScheduleCreated?.Invoke();
            
            log.WriteLine($"[FIXED_SESSION][SUCCESS] Fixed schedule created successfully. SessionId: {session.SessionId}");
            log.WriteLine($"[FIXED_SESSION][COMPLETE] All operations completed");
            return true;
        }
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}\n{ex.StackTrace}";
            log.WriteLine($"[FIXED_SESSION][EXCEPTION] {error}");
            return false;
        }
    }

    // Push notification cho client khi fixed session thay đổi
    private void PushFixedSessionsChanged(string userId)
    {
        // Gửi thông báo cho client: "FIXED_SESSION_CHANGED"
        SendToUser(userId, "FIXED_SESSION_CHANGED\n");
    }

    public bool CreateUser(UserInfo newUser, string passwordPlain, out string error)
    {
        error = "";

        newUser.UserId = (newUser.UserId ?? "").Trim().ToUpperInvariant();
        newUser.StudentId = (newUser.StudentId ?? "").Trim().ToUpperInvariant();
        newUser.LecturerId = (newUser.LecturerId ?? "").Trim().ToUpperInvariant();
        newUser.Class = (newUser.Class ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(newUser.UserId))
        {
            error = "UserId is required";
            return false;
        }

        if (_users.ContainsKey(newUser.UserId))
        {
            error = $"UserId {newUser.UserId} already exists";
            return false;
        }

        // 👉 Validate các trường unique: Email / Phone / MSSV / Mã GV
        if (!ValidateUserUniqueFields(newUser, ignoreUserId: null, out error))
        {
            return false;
        }

        // Validate theo loại user
        if (string.Equals(newUser.UserType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(newUser.StudentId))
            {
                error = "StudentId (MSSV) is required for Student";
                return false;
            }

            if (!IsValidFaculty(newUser.Department))
            {
                error = "Khoa (Department) không hợp lệ. Hãy chọn trong danh sách CNTT2, IOT2, ...";
                return false;
            }

            // Student không dùng LecturerId
            newUser.LecturerId = "";
            newUser.Faculty = "";
        }
        else if (string.Equals(newUser.UserType, "Lecturer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(newUser.LecturerId))
            {
                error = "LecturerId (Mã GV) is required for Lecturer";
                return false;
            }

            if (!IsValidFaculty(newUser.Faculty))
            {
                error = "Faculty không hợp lệ. Hãy chọn trong danh sách CNTT2, IOT2, ...";
                return false;
            }

            // Lecturer không dùng StudentId
            newUser.StudentId = "";
            newUser.Department = "";
        }
        else
        {
            // Staff: cho phép không có StudentId / LecturerId, khoa không bắt buộc
            newUser.StudentId = "";
            newUser.LecturerId = "";
        }

        newUser.PasswordHash = BCryptNet.HashPassword(passwordPlain);
        newUser.IsActive = true;

        _users[newUser.UserId] = newUser;
        return true;
    }
    // ====== ROOM CRUD ======
    public bool CreateRoom(RoomInfo room, out string error)
    {
        error = "";

        if (room == null)
        {
            error = "Room is null";
            return false;
        }

        room.RoomId = (room.RoomId ?? "").Trim().ToUpperInvariant();
        room.Status = NormalizeRoomStatus(room.Status);

        if (string.IsNullOrWhiteSpace(room.RoomId))
        {
            error = "RoomId is required";
            return false;
        }

        lock (_lock)
        {
            if (_rooms.ContainsKey(room.RoomId))
            {
                error = $"Room {room.RoomId} already exists.";
                return false;
            }

            _rooms[room.RoomId] = room;

            foreach (var dateEntry in _slotsByDate)
            {
                var dictSlots = dateEntry.Value;
                for (int i = 1; i <= SlotCount; i++)
                {
                    var slotId = GetSlotId(i);
                    var key = MakeKey(room.RoomId, slotId);
                    if (dictSlots.ContainsKey(key))
                        continue;

                    dictSlots[key] = new SlotState
                    {
                        IsBusy = false,
                        CurrentHolderClientId = null,
                        CurrentHolderUserId = null
                    };
                }
            }

            SaveRooms();
            PushRoomsChanged();
            return true;
        }
    }

    public bool UpdateRoom(RoomInfo room, out string error)
    {
        error = "";

        if (room == null)
        {
            error = "Room is null";
            return false;
        }

        room.RoomId = (room.RoomId ?? "").Trim().ToUpperInvariant();
        room.Status = NormalizeRoomStatus(room.Status);

        if (string.IsNullOrWhiteSpace(room.RoomId))
        {
            error = "RoomId is required";
            return false;
        }

        lock (_lock)
        {
            if (!_rooms.ContainsKey(room.RoomId))
            {
                error = $"Room {room.RoomId} not found.";
                return false;
            }

            _rooms[room.RoomId] = room;
            SaveRooms();
            PushRoomsChanged();
            return true;
        }
    }

    public bool DeleteRoom(string roomId, out string error)
    {
        error = "";

        roomId = (roomId ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            error = "RoomId is required";
            return false;
        }

        lock (_lock)
        {
            if (!_rooms.ContainsKey(roomId))
            {
                error = $"Room {roomId} not found.";
                return false;
            }

            _rooms.Remove(roomId);

            foreach (var dateEntry in _slotsByDate)
            {
                var dictSlots = dateEntry.Value;
                for (int i = 1; i <= SlotCount; i++)
                {
                    var slotId = GetSlotId(i);
                    var key = MakeKey(roomId, slotId);
                    dictSlots.Remove(key);
                }
            }

            SaveRooms();
            PushRoomsChanged();
            return true;
        }
    }

    // Chuyển "S3" -> index 3
    private int ParseSlotIndex(string slotId)
    {
        if (slotId.StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(slotId.Substring(1), out int idx))
                return idx;
        }
        return -1;
    }

    private (TimeSpan start, TimeSpan end) GetSlotTimeOfDay(int idx)
    {
        // idx 1..14
        if (idx <= 0 || idx > SlotCount ||
            _settings.SlotTimes == null || _settings.SlotTimes.Count == 0)
        {
            // fallback: 1h/ca từ 07:00
            var s = TimeSpan.FromHours(7 + (idx - 1));
            return (s, s.Add(TimeSpan.FromHours(1)));
        }

        var row = _settings.SlotTimes
            .FirstOrDefault(r => r.Index == idx)
            ?? _settings.SlotTimes.FirstOrDefault(
                r => string.Equals(r.SlotId, $"S{idx}", StringComparison.OrdinalIgnoreCase));

        if (row == null ||
            !TimeSpan.TryParse(row.Start, out var start) ||
            !TimeSpan.TryParse(row.End, out var end))
        {
            var s = TimeSpan.FromHours(7 + (idx - 1));
            return (s, s.Add(TimeSpan.FromHours(1)));
        }

        return (start, end);
    }


    private DateTime GetSlotStartTime(string dateKey, string slotId)
    {
        var date = DateTime.Parse(dateKey);
        int idx = ParseSlotIndex(slotId);
        if (idx <= 0) idx = 1;

        var (startTod, _) = GetSlotTimeOfDay(idx);
        return date.Date + startTod;
    }
    private DateTime GetSlotEndTime(string dateKey, string slotId)
    {
        var date = DateTime.Parse(dateKey);
        int idx = ParseSlotIndex(slotId);
        if (idx <= 0) idx = 1;

        var (_, endTod) = GetSlotTimeOfDay(idx);
        return date.Date + endTod;
    }
    private readonly HashSet<string> _subRooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _subSlotConfig = new(StringComparer.OrdinalIgnoreCase);
    // Kiểm tra cùng client có đang giữ slot trùng ca ở phòng khác hay không
    private bool HasCrossRoomConflict(string clientId, string dateKey, string roomIdNew, string slotIdNew, out string conflictedRoom)
    {
        conflictedRoom = "";
        if (!_slotsByDate.TryGetValue(dateKey, out var dict)) return false;

        int idxNew = ParseSlotIndex(slotIdNew);
        if (idxNew <= 0) return false;

        // User của client hiện tại
        var currentUserId = GetUserIdForClient(clientId) ?? clientId;

        foreach (var kvp in dict)
        {
            var key = kvp.Key;
            var slot = kvp.Value;

            if (string.IsNullOrEmpty(slot.CurrentHolderClientId) && string.IsNullOrEmpty(slot.CurrentHolderUserId))
                continue;

            // User của holder slot hiện tại
            var holderUserId = !string.IsNullOrEmpty(slot.CurrentHolderUserId)
                ? slot.CurrentHolderUserId
                : (!string.IsNullOrEmpty(slot.CurrentHolderClientId)
                    ? (GetUserIdForClient(slot.CurrentHolderClientId) ?? slot.CurrentHolderClientId)
                    : "");
            if (!string.Equals(holderUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = key.Split(new[] { "::" }, StringSplitOptions.None);
            var roomId = parts[0];
            var slotId = parts.Length > 1 ? parts[1] : "?";

            if (roomId == roomIdNew) continue; // cùng phòng thì cho phép

            int idx = ParseSlotIndex(slotId);
            if (idx == idxNew)
            {
                conflictedRoom = roomId;
                return true;
            }
        }

        return false;
    }


    // REQUEST: cover
    // - Case cơ bản
    // - Tranh chấp
    // - REQUEST trùng lặp (ALREADY_HOLDER / ALREADY_QUEUED)
    // - Không cho giữ 2 phòng khác nhau cùng ca (theo clientId) trong cùng ngày
    // - Không cho đặt ca đã qua (RealTime)
    public void HandleRequest(string clientId, string roomId, string slotId, string dateStr, string purpose, NetworkStream stream, TextWriter log)
    {
        lock (_lock)
        {
            roomId = (roomId ?? "").Trim().ToUpperInvariant();
            slotId = (slotId ?? "").Trim().ToUpperInvariant();

            if (IsRoomDisabled(roomId))
            {
                log.WriteLine($"[WARN] REQUEST blocked because room is DISABLED: {roomId} by {clientId}");
                Send(stream, "INFO|ERROR|ROOM_DISABLED\n");
                return;
            }

            EnsureDateInitialized(dateStr, log);

            var dict = _slotsByDate[dateStr];
            var key = MakeKey(roomId, slotId);

            if (!dict.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] REQUEST invalid slot {roomId}-{slotId} by {clientId}");
                Send(stream, "INFO|ERROR|Invalid room/slot\n");
                return;
            }
            // Map clientId -> userId để dùng cho rule
            var userId = GetUserIdForClient(clientId) ?? clientId;

            bool isAdmin = IsAdmin(userId);

            // Nếu slot đang khóa cho event -> chỉ admin mới được can thiệp (FORCE_GRANT làm riêng)
            if (slot.IsEventLocked && !isAdmin)
            {
                log.WriteLine($"[WARN] REQUEST blocked by EVENT_LOCK {clientId} on {roomId}-{slotId} {_currentDateKey}");
                Send(stream, "INFO|ERROR|SLOT_LOCKED_FOR_EVENT\n");
                return;
            }

            // 1) Chặn ca đã qua (mode RealTime đơn giản)
            var now = Now;
            var endTime = GetSlotEndTime(dateStr, slotId);
            if (endTime <= now)
            {
                log.WriteLine($"[WARN] REQUEST past slot {roomId}-{slotId} on date {dateStr} by {clientId}");
                Send(stream, "INFO|ERROR|Slot already in the past\n");
                return;
            }

            // 2) Chặn giữ 2 phòng khác nhau cùng ca trong cùng ngày
            if (HasCrossRoomConflict(clientId, dateStr, roomId, slotId, out var conflictedRoom))
            {
                log.WriteLine($"[WARN] REQUEST cross-room conflict: user {userId} already holds {conflictedRoom}-{slotId} on {dateStr}");
                Send(stream, "INFO|ERROR|User already booked another room in that time range\n");
                return;
            }

            // 3) Nếu client đã là holder -> không cấp lại, chỉ báo INFO
            if (slot.CurrentHolderClientId == clientId ||
                (!string.IsNullOrEmpty(slot.CurrentHolderUserId) &&
                 string.Equals(slot.CurrentHolderUserId, userId, StringComparison.OrdinalIgnoreCase)))
            {
                log.WriteLine($"[INFO] REQUEST from holder {clientId} on {roomId}-{slotId} -> already granted");
                Send(stream, $"INFO|ALREADY_HOLDER|{roomId}|{slotId}\n");
                return;
            }

            // 4) Nếu user đã trong queue -> không enqueue thêm, chỉ báo INFO + pos
            int pos = 1;
            bool alreadyQueued = false;
            foreach (var w in slot.WaitingQueue)
            {
                var queuedUserId = GetUserIdForClient(w.clientId) ?? w.clientId;
                if (string.Equals(queuedUserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyQueued = true;
                    break;
                }
                pos++;
            }

            if (alreadyQueued)
            {
                log.WriteLine($"[INFO] REQUEST duplicate from {clientId} on {roomId}-{slotId} -> already queued at pos {pos}");
                Send(stream, $"INFO|ALREADY_QUEUED|{roomId}|{slotId}|{pos}\n");
                return;
            }

            // 4b) Check xem user có booking QUEUED nào trong cùng room mà slot này nằm trong range không
            int slotIdx = ParseSlotIndex(slotId);
            var queuedBookingInRoom = _bookings.FirstOrDefault(b =>
                b.UserId == userId
                && b.RoomId == roomId
                && b.Date == dateStr
                && b.Status == "QUEUED"
                && b.IsRangeBooking);

            if (queuedBookingInRoom != null)
            {
                int qStartIdx = ParseSlotIndex(queuedBookingInRoom.SlotStartId);
                int qEndIdx = ParseSlotIndex(queuedBookingInRoom.SlotEndId);
                // Nếu slot đang request nằm trong range của booking QUEUED cũ
                if (slotIdx >= qStartIdx && slotIdx <= qEndIdx)
                {
                    log.WriteLine($"[INFO] REQUEST duplicate from {clientId} on {roomId}-{slotId} -> already queued in RANGE {queuedBookingInRoom.SlotStartId}-{queuedBookingInRoom.SlotEndId}");
                    Send(stream, $"INFO|ALREADY_QUEUED|{roomId}|{slotId}|1\n");
                    return;
                }
            }

            // 5) Slot đang rảnh -> cấp quyền ngay
            if (!slot.IsBusy && string.IsNullOrEmpty(slot.CurrentHolderClientId) && string.IsNullOrEmpty(slot.CurrentHolderUserId))
            {
                slot.IsBusy = true;
                slot.CurrentHolderClientId = clientId;
                slot.CurrentHolderUserId = userId;

                // Tạo booking mới cho lần GRANT này
                var booking = CreateBookingForGrant(
                    clientId,
                    roomId,
                    dateStr,
                    slotId,   // start == end với single
                    slotId,
                    false,
                    purpose,
                    log);
                slot.CurrentBookingId = booking.BookingId;
                // ✅ M5: push delta update cho UI slot của phòng đang xem
                PushMyBookingsChanged(booking.UserId);
                PushSlotUpdate(roomId, dateStr, slotId, log);
                log.WriteLine($"[GRANT] {clientId} -> {roomId}-{slotId} on date {dateStr}");
                Send(stream, $"GRANT|{roomId}|{slotId}\n");

                NotifyUser(booking.UserId, $"NOTIFY_GRANT|{roomId}|{slotId}|{dateStr}|{booking.CheckinDeadline:HH:mm}");

                log.WriteLine($"[EMAIL][GRANT] SendEmailOnGrant={_settings.SendEmailOnGrant} userId={booking.UserId} bookingId={booking.BookingId}");
                if (_settings.SendEmailOnGrant)
                {
                    var subject = "[Room booking] Booking của bạn đã được duyệt";
                    var bodyTemplate =
                        "Chào {FullName},\n\n" +
                        "Booking của bạn đã được GRANT.\n" +
                        "- Phòng: {RoomId}\n" +
                        "- Ngày: {Date}\n" +
                        "- Ca: {SlotStartId} - {SlotEndId}\n" +
                        "- Trạng thái: {Status}\n\n" +
                        "Vui lòng check-in trước deadline.";

                    SendEmailForBooking(booking, subject, bodyTemplate, log);
                }
                if (_settings.SendNotificationToClient)
                {
                    NotifyClientBookingChanged(booking.UserId,
                        $"GRANTED|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}", log);
                }
                PushHomeChanged(booking.UserId);
                RaiseStateChanged();
            }
            else
            {
                // Slot đang bận -> cho vào queue (FIFO)

                // 1) tạo booking QUEUED để show trên MyBookings
                var qb = CreateBookingQueued(
                    clientId,
                    roomId,
                    dateStr,
                    slotId,
                    slotId,
                    false,
                    purpose,
                    log);

                // 2) enqueue kèm bookingId + userId để sau này promote/cancel đúng record
                var qUserId = GetUserIdForClient(clientId) ?? clientId;
                slot.WaitingQueue.Enqueue((clientId, qUserId, stream, purpose, qb.BookingId));
                var newPos = slot.WaitingQueue.Count;

                // 3) push update UI slot (không đổi)
                PushSlotUpdate(roomId, dateStr, slotId, log);

                log.WriteLine($"[QUEUE] {clientId} -> {roomId}-{slotId} on date {dateStr} (pos {newPos})");

                // 4) trả về client (không đổi)
                Send(stream, $"QUEUED|{roomId}|{slotId}|{newPos}\n");

                // 5) push home + my bookings (giữ nguyên cách bạn đang làm)
                PushHomeChanged(userId);
                log.WriteLine("[PUSH] MY_BOOKINGS changed");
                PushMyBookingsChanged(userId); // ✅ thay vì StringWriter "booking_changed" (đỡ gây hiểu nhầm)

                RaiseStateChanged();
            }

        }
    }

    /// <summary>
    /// RELEASE được gọi khi client bấm Hủy:
    /// - Nếu đang giữ quyền -> giải phóng / cấp cho queue tiếp theo.
    /// - Nếu đang trong queue -> xóa khỏi queue.
    /// - Nếu không liên quan -> báo lỗi.
    /// </summary>
    public void HandleRelease(string clientId, string roomId, string slotId, string dateStr, NetworkStream? replyStream, TextWriter log)
    {
        lock (_lock)
        {

            var userId = GetUserIdForClient(clientId) ?? clientId;
            EnsureDateInitialized(dateStr, log);
            var dict = _slotsByDate[dateStr];
            var key = MakeKey(roomId, slotId);

            if (!dict.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] RELEASE invalid slot {roomId}-{slotId} by {clientId}");
                if (replyStream != null)
                {
                    Send(replyStream, "INFO|ERROR|Invalid room/slot\n");
                }
                return;
            }

            bool isAdmin = IsAdmin(userId);

            // ===== CASE 1: holder hoặc admin được phép RELEASE slot =====
            if (isAdmin ||
                (slot.CurrentHolderClientId == clientId) ||
                (!string.IsNullOrEmpty(slot.CurrentHolderUserId) && string.Equals(slot.CurrentHolderUserId, userId, StringComparison.OrdinalIgnoreCase)))
            {
                var oldHolder = slot.CurrentHolderClientId;
                var tag = isAdmin ? "[ADMIN RELEASE]" : "[RELEASE]";
                log.WriteLine($"{tag} {clientId} -> {roomId}-{slotId} on {dateStr} (holder = {oldHolder})");

                // tìm booking hiện tại (nếu có)
                Booking? currentBooking = null;
                if (slot.CurrentBookingId.HasValue)
                {
                    currentBooking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
                }

                // Nếu đang IN_USE mà không phải admin -> từ chối
                if (currentBooking != null && currentBooking.Status == "IN_USE" && !isAdmin)
                {
                    log.WriteLine($"[WARN] User {clientId} cannot RELEASE IN_USE booking {currentBooking.BookingId}");
                    if (replyStream != null)
                        Send(replyStream, "INFO|ERROR|CANNOT_RELEASE_IN_USE\n");
                    return;
                }

                string newStatus = "CANCELLED";

                if (currentBooking != null)
                {
                    if (currentBooking.Status == "APPROVED")
                    {
                        // hủy trước khi check-in
                        newStatus = "CANCELLED";
                    }
                    else if (currentBooking.Status == "IN_USE")
                    {
                        // chỉ admin mới vào được nhánh này phía trên
                        newStatus = "COMPLETED";
                    }

                    currentBooking.Status = newStatus;
                    currentBooking.UpdatedAt = Now;
                    log.WriteLine($"[BOOKING] {currentBooking.BookingId} -> {newStatus} by {clientId}");
                    PushMyBookingsChanged(currentBooking.UserId);
                }

                if (replyStream != null)
                    Send(replyStream, $"INFO|RELEASED|{roomId}|{slotId}\n");

                // Phần cấp queue / giải phóng slot vẫn y như cũ
                if (slot.WaitingQueue.Count == 0)
                {
                    slot.IsBusy = false;
                    slot.CurrentHolderClientId = null;
                    slot.CurrentHolderUserId = null;
                    slot.CurrentBookingId = null;
                    PushSlotUpdate(roomId, dateStr, slotId, log);
                    log.WriteLine($"[SLOT] {roomId}-{slotId} on {dateStr} -> FREE");
                }
                else
                {
                    var (nextClientId, nextUserId, nextStream, nextPurpose, queuedBookingId) = slot.WaitingQueue.Dequeue();
                    slot.IsBusy = true;
                    slot.CurrentHolderClientId = nextClientId;

                    // Promote existing queued booking -> APPROVED (don't create new booking)
                    if (PromoteQueuedToApproved(queuedBookingId, log, out var booking))
                    {
                        slot.CurrentBookingId = booking.BookingId;
                        slot.CurrentHolderUserId = booking.UserId;
                        PushHomeChanged(booking.UserId);
                        PushSlotUpdate(roomId, dateStr, slotId, log); // slot đổi holder (grant from queue)

                        log.WriteLine($"[GRANT] {nextClientId} -> {roomId}-{slotId} from queue on date {dateStr}");
                        Send(nextStream, $"GRANT|{roomId}|{slotId}\n");

                        log.WriteLine($"[EMAIL][GRANT_FROM_QUEUE] SendEmailOnGrant={_settings.SendEmailOnGrant} userId={booking.UserId} bookingId={booking.BookingId}");
                        if (_settings.SendEmailOnGrant)
                        {
                            var subject = "[Room booking] Booking của bạn đã được GRANT từ hàng chờ";
                            var bodyTemplate =
                                "Chào {FullName},\n\n" +
                                "Yêu cầu đặt phòng của bạn đã được GRANT (từ hàng chờ), do người giữ slot đã release.\n" +
                                "- Phòng: {RoomId}\n" +
                                "- Ngày: {Date}\n" +
                                "- Ca: {SlotStartId} - {SlotEndId}\n";

                            SendEmailForBooking(booking, subject, bodyTemplate, log);
                        }

                        if (_settings.SendNotificationToClient)
                        {
                            NotifyClientBookingChanged(booking.UserId,
                                $"GRANTED|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}", log);
                        }

                        PushMyBookingsChanged(booking.UserId);
                        RaiseStateChanged();
                    }
                    if (_settings.SendNotificationToClient)
                    {
                        NotifyClientBookingChanged(
                            booking.UserId,
                            $"GRANTED_FROM_QUEUE|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                            log
                        );
                    }

                }
                if (currentBooking != null) PushHomeChanged(currentBooking.UserId);
                RaiseStateChanged();
                return;
            }
            // ===== CASE 2: không phải holder, nhưng đang trong queue -> hủy yêu cầu =====
            int removed = RemoveFromQueue(slot, clientId);
            if (removed > 0)
            {
                PushSlotUpdate(roomId, dateStr, slotId, log);
                log.WriteLine($"[CANCEL] {clientId} removed from queue of {roomId}-{slotId} on {dateStr} (entries {removed})");
                if (replyStream != null)
                {
                    Send(replyStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                }
                PushHomeChanged(userId);
                RaiseStateChanged();
            }
            else
            {
                // ===== CASE 3: không phải holder, không nằm trong queue, không phải admin =====
                log.WriteLine($"[WARN] RELEASE from non-holder/non-queued {clientId} on {roomId}-{slotId} on {dateStr}");
                if (replyStream != null)
                {
                    Send(replyStream, "INFO|ERROR|Not holder or queued\n");
                }
            }
        }
    }

    /// <summary>
    /// Được gọi khi 1 client mất kết nối:
    /// - Nếu đang là holder ở slot nào -> auto RELEASE slot đó.
    /// - Nếu đang trong queue ở slot nào -> loại khỏi queue.
    /// </summary>
    public void HandleDisconnect(string clientId, TextWriter log)
    {
        lock (_lock)
        {
            foreach (var dateEntry in _slotsByDate)
            {
                var dateKey = dateEntry.Key;
                var dict = dateEntry.Value;

                foreach (var kvp in dict)
                {
                    var key = kvp.Key;
                    var slot = kvp.Value;

                    var parts = key.Split(new[] { "::" }, StringSplitOptions.None);
                    var roomId = parts[0];
                    var slotId = parts.Length > 1 ? parts[1] : "?";

                    // 1️⃣ XÓA khỏi hàng đợi
                    int removed = RemoveFromQueue(slot, clientId);
                    if (removed > 0)
                        log.WriteLine($"[DISCONNECT] Remove {clientId} from queue {roomId}-{slotId} on {dateKey}");

                    // 2️⃣ KHÔNG RELEASE HOLDER !!!
                    // Mode A: giữ nguyên booking.
                    if (slot.CurrentHolderClientId == clientId)
                    {
                        log.WriteLine($"[DISCONNECT] {clientId} is holder of {roomId}-{slotId} but Mode A => KEEP booking.");
                        // Không release, không notify, không grant người khác
                    }
                }
            }
        }
    }

    /// <summary>
    /// Xóa tất cả entry của clientId khỏi queue. Trả về số entry đã xóa.
    /// Cập nhật booking QUEUED thành CANCELLED.
    /// </summary>
    private int RemoveFromQueue(SlotState slot, string clientId)
    {
        if (slot.WaitingQueue.Count == 0) return 0;

        int removed = 0;
        var newQueue = new Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>();

        while (slot.WaitingQueue.Count > 0)
        {
            var item = slot.WaitingQueue.Dequeue();
            if (item.clientId == clientId)
            {
                removed++;
                // Cancel queued booking
                var qb = _bookings.FirstOrDefault(x => x.BookingId == item.bookingId);
                if (qb != null && string.Equals(qb.Status, "QUEUED", StringComparison.OrdinalIgnoreCase))
                {
                    qb.Status = "CANCELLED";
                    qb.UpdatedAt = Now;
                    PushMyBookingsChanged(qb.UserId);
                    PushHomeChanged(qb.UserId);
                }
            }
            else
            {
                newQueue.Enqueue(item);
            }
        }

        while (newQueue.Count > 0)
            slot.WaitingQueue.Enqueue(newQueue.Dequeue());

        return removed;
    }


    private void Send(NetworkStream stream, string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);
    }

    // Tạo booking mới khi slot được GRANT cho user
    // THÊM tham số dateKey để tránh lệ thuộc _currentDateKey
    private Booking CreateBookingForGrant(
    string clientId,
    string roomId,
    string dateKey,
    string slotStartId,
    string slotEndId,
    bool isRange,
    string purpose,
    TextWriter log)
    {
        // Map clientId (connection) -> userId (tài khoản)
        var userId = GetUserIdForClient(clientId) ?? clientId;

        var now = Now;
        var slotStartTime = GetSlotStartTime(dateKey, slotStartId);
        var slotEndTime = GetSlotEndTime(dateKey, slotEndId);
        // Rule:
        // - Single: deadline = max(SlotStart + 15', now)
        // - Range : deadline = max(SlotStart + 15', now)
        DateTime checkinDeadline = isRange
        ? slotStartTime.AddMinutes(15)
        : slotStartTime.AddMinutes(15);

        // Nếu đã trễ hơn deadline lý thuyết => set = now để tránh "quá hạn ngay lập tức"
        if (checkinDeadline < now)
            checkinDeadline = now;

        // Không vượt quá giờ kết thúc của slotEnd
        if (checkinDeadline > slotEndTime)
            checkinDeadline = slotEndTime;

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            Date = dateKey,                // yyyy-MM-dd
            SlotId = slotStartId,          // (giữ để tương thích code cũ)
            SlotStartId = slotStartId,
            SlotEndId = slotEndId,
            IsRangeBooking = isRange,
            Purpose = purpose ?? "",
            CreatedAt = now,
            UpdatedAt = now,
            Status = "APPROVED",
            CheckinDeadline = checkinDeadline,
            // CheckinTime để null khi chưa checkin
        };

        log.WriteLine($"[BOOKING] Create {booking.BookingId} {userId} {roomId} {slotStartId}-{slotEndId} APPROVED, deadline={booking.CheckinDeadline:yyyy-MM-dd HH:mm:ss}");

        _bookings.Add(booking);
        return booking;
    }
    // Cập nhật trạng thái booking đang gắn với slot.CurrentBookingId
    private void UpdateCurrentBookingStatus(SlotState slot, string roomId, string slotId, string newStatus, TextWriter log)
    {
        if (!slot.CurrentBookingId.HasValue) return;

        var bookingId = slot.CurrentBookingId.Value;
        var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId);
        if (booking == null) return;

        booking.Status = newStatus;
        var now = Now;
        booking.UpdatedAt = now;

        log.WriteLine($"[BOOKING] {booking.BookingId} -> {newStatus} for {roomId}-{slotId}");
    }

    // BookingServer/ServerState.cs

    public List<BookingView> GetBookingViews()
    {
        lock (_lock)
        {
            var list = new List<BookingView>();

            foreach (var b in _bookings)
            {
                _users.TryGetValue(b.UserId, out var u);

                // dateKey dạng yyyy-MM-dd (b.Date đã ở dạng này rồi)
                var dateKey = b.Date;

                // Tính time range: nếu có slot start/end thì dùng, nếu không thì để trống
                string timeRange = "";
                if (!string.IsNullOrEmpty(b.SlotStartId) && !string.IsNullOrEmpty(b.SlotEndId))
                {
                    var startTime = GetSlotStartTime(dateKey, b.SlotStartId);
                    var endTime = GetSlotEndTime(dateKey, b.SlotEndId);
                    timeRange = $"{startTime:HH:mm}-{endTime:HH:mm}";
                }

                list.Add(new BookingView
                {
                    BookingId = b.BookingId,

                    UserId = b.UserId,
                    FullName = u?.FullName ?? "",
                    UserType = u?.UserType ?? "",
                    Email = u?.Email ?? "",
                    Phone = u?.Phone ?? "",

                    RoomId = b.RoomId,
                    Date = b.Date,
                    SlotStartId = b.SlotStartId,
                    SlotEndId = b.SlotEndId,
                    TimeRange = timeRange,
                    IsRange = b.IsRangeBooking,
                    Purpose = b.Purpose ?? "",

                    Status = b.Status,
                    CheckinDeadline = b.CheckinDeadline,
                    CheckinTime = b.CheckinTime,

                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                });
            }

            // sort mới nhất lên trên cho dễ xem
            return list
                .OrderByDescending(v => v.CreatedAt)
                .ToList();
        }
    }

    // Admin force grant từ UI Server (không đi qua TCP)
    public bool ForceGrantFromServerUi(DateTime date, String roomId, string slotId, string targetUserId, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            // đảm bảo đã có state cho ngày này
            EnsureDateInitialized(dateKey, log);

            // 1. Check user
            if (!_users.TryGetValue(targetUserId, out var targetUser) || !targetUser.IsActive)
            {
                error = "User không tồn tại hoặc đang bị khóa.";
                return false;
            }

            if (!_slotsByDate.TryGetValue(dateKey, out var dict))
            {
                error = "Không tìm thấy dữ liệu ngày.";
                return false;
            }

            var key = MakeKey(roomId, slotId);
            if (!dict.TryGetValue(key, out var slot))
            {
                error = "Không tìm thấy phòng/ca.";
                return false;
            }

            // 2. Không cho user giữ 2 phòng khác nhau cùng ca (giống logic request)
            if (HasCrossRoomConflict(targetUserId, dateKey, roomId, slotId, out var conflictedRoom))
            {
                error = $"User đã giữ phòng {conflictedRoom} ở cùng ca.";
                return false;
            }
            // ====== NEW: nhớ user bị ảnh hưởng để push HOME/MY_BOOKINGS ======
            string? oldUserId = null;
            // 3. Nếu đang có holder → cancel booking cũ
            if (slot.IsBusy ||
                slot.CurrentBookingId.HasValue ||
                !string.IsNullOrEmpty(slot.CurrentHolderClientId) ||
                !string.IsNullOrEmpty(slot.CurrentHolderUserId))
            {
                // ✅ lấy user của booking cũ nếu có
                if (slot.CurrentBookingId.HasValue)
                {
                    var oldBooking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
                    if (oldBooking != null && !string.IsNullOrEmpty(oldBooking.UserId))
                        oldUserId = oldBooking.UserId;
                }

                log.WriteLine($"[ADMIN FORCE_GRANT-UI] override holder {slot.CurrentHolderClientId} on {roomId}-{slotId} ({dateKey})");
                UpdateCurrentBookingStatus(slot, roomId, slotId, "CANCELLED", log);
                PushSlotUpdate(roomId, dateKey, slotId, log);

                if (!string.IsNullOrEmpty(oldUserId))
                {
                    PushHomeChanged(oldUserId);
                    PushMyBookingsChanged(oldUserId);
                }
            }

            // 4) Clear queue & báo cho từng client trong queue là bị hủy do admin
            if (slot.WaitingQueue.Count > 0)
            {
                log.WriteLine($"[ADMIN FORCE_GRANT-UI] clear queue {roomId}-{slotId}, count={slot.WaitingQueue.Count}");
                while (slot.WaitingQueue.Count > 0)
                {
                    var (queuedClientId, queuedUserId, queuedStream, queuedPurpose, queuedBookingId) = slot.WaitingQueue.Dequeue();

                    // update booking queued thành CANCELLED luôn
                    var qb = _bookings.FirstOrDefault(x => x.BookingId == queuedBookingId);
                    if (qb != null && string.Equals(qb.Status, "QUEUED", StringComparison.OrdinalIgnoreCase))
                    {
                        qb.Status = "CANCELLED";
                        qb.UpdatedAt = Now;
                        PushMyBookingsChanged(qb.UserId);
                        PushHomeChanged(qb.UserId);
                    }

                    try
                    {
                        Send(queuedStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"[WARN] FORCE_GRANT cancel queued notify failed client={queuedClientId} bookingId={queuedBookingId}: {ex.Message}");
                    }
                }

                PushSlotUpdate(roomId, dateKey, slotId, log);
            }


            // 5. Gán holder mới + tạo booking mới
            slot.IsBusy = true;
            slot.CurrentHolderUserId = targetUserId;
            string? targetClientId = null;
            lock (_sessionLock)
            {
                if (_userToClients.TryGetValue(targetUserId, out var set) && set.Count > 0)
                    targetClientId = set.FirstOrDefault();
            }
            slot.CurrentHolderClientId = targetClientId;
            var booking = CreateBookingForGrant(
                targetUserId,
                roomId,
                dateKey,
                slotId,   // start == end (single slot)
                slotId,
                false,    // IsRangeBooking
                "FORCE_GRANT_BY_ADMIN", // purpose do admin
                log);

            slot.CurrentBookingId = booking.BookingId;
            PushSlotUpdate(roomId, dateKey, slotId, log); // ✅ M5: slot now points to new booking/user/purpose

            if (!string.IsNullOrEmpty(oldUserId))
            {
                PushHomeChanged(oldUserId);
                PushMyBookingsChanged(oldUserId); // nếu bạn đã có
            }

            PushHomeChanged(booking.UserId);
            PushMyBookingsChanged(booking.UserId); // nếu bạn đã có

            log.WriteLine($"[ADMIN FORCE_GRANT-UI] {targetUserId} -> {roomId}-{slotId} on {dateKey}");
            if (_settings.SendEmailOnForceGrantRelease)
            {
                var subject = "[Room booking] Booking đã được FORCE GRANT";
                var bodyTemplate =
                    "Chào {FullName},\n\n" +
                    "Admin đã FORCE GRANT một booking (1 ca) cho bạn.\n" +
                    "- Phòng: {RoomId}\n" +
                    "- Ngày: {Date}\n" +
                    "- Ca: {SlotStartId} - {SlotEndId}\n" +
                    "- Trạng thái: {Status}\n\n" +
                    "Vui lòng chú ý lịch dạy/học của mình.";
                QueueEmailForBooking(booking, subject, bodyTemplate, log);
            }

            if (_settings.SendNotificationToClient)
            {
                NotifyClientBookingChanged(
                targetUserId,
                $"FORCE_GRANT|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                log
            );
            }

            return true;
        }
    }
    // Admin force GRANT RANGE từ UI Server (không đi qua TCP)
    public bool ForceGrantRangeFromServerUi(DateTime date, string roomId, string slotStartId, string slotEndId, string targetUserId, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            // 0. Đảm bảo đã có state cho ngày này
            EnsureDateInitialized(dateKey, log);

            // 1. Check user
            if (!_users.TryGetValue(targetUserId, out var targetUser) || !targetUser.IsActive)
            {
                error = "User không tồn tại hoặc đang bị khóa.";
                return false;
            }

            if (!_slotsByDate.TryGetValue(dateKey, out var dict))
            {
                error = "Không tìm thấy dữ liệu ngày.";
                return false;
            }

            // 2. Parse range
            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
            {
                error = "Khoảng ca không hợp lệ.";
                return false;
            }

            // 3. Gom tất cả slot trong range + validate
            var slots = new List<(string slotId, SlotState state)>();
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                var sid = GetSlotId(idx);
                var key = MakeKey(roomId, sid);

                if (!dict.TryGetValue(key, out var slot))
                {
                    error = $"Không tìm thấy phòng/ca {roomId}-{sid}.";
                    return false;
                }

                // Không cho user giữ 2 phòng khác nhau cùng ca
                if (HasCrossRoomConflict(targetUserId, dateKey, roomId, sid, out var conflictedRoom))
                {
                    error = $"User đã giữ phòng {conflictedRoom} ở cùng ca {sid}.";
                    return false;
                }

                slots.Add((sid, slot));
            }
            // ===== NEW: collect users bị ảnh hưởng (booking cũ bị cancel) =====
            var impactedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 4. Hạ booking cũ & clear queue từng slot trong range
            foreach (var (sid, slot) in slots)
            {
                var wasEventLocked = slot.IsEventLocked;
                var prevEventNote = slot.EventNote;

                if (slot.IsBusy ||
                    slot.CurrentBookingId.HasValue ||
                    !string.IsNullOrEmpty(slot.CurrentHolderClientId) ||
                    !string.IsNullOrEmpty(slot.CurrentHolderUserId))
                {
                    // lấy userId booking cũ (nếu có) để push HOME/MY_BOOKINGS
                    if (slot.CurrentBookingId.HasValue)
                    {
                        var oldBooking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
                        if (oldBooking != null && !string.IsNullOrEmpty(oldBooking.UserId))
                            impactedUsers.Add(oldBooking.UserId);
                    }

                    log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI] override holder {slot.CurrentHolderClientId} on {roomId}-{sid} ({dateKey})");
                    UpdateCurrentBookingStatus(slot, roomId, sid, "CANCELLED", log);
                }

                if (slot.WaitingQueue.Count > 0)
                {
                    log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI] clear queue {roomId}-{sid}, count={slot.WaitingQueue.Count}");
                    while (slot.WaitingQueue.Count > 0)
                    {
                        var (queuedClientId, queuedUserId, queuedStream, queuedPurpose, queuedBookingId) = slot.WaitingQueue.Dequeue();
                        // update queued booking -> CANCELLED
                        var qb = _bookings.FirstOrDefault(x => x.BookingId == queuedBookingId);
                        if (qb != null && string.Equals(qb.Status, "QUEUED", StringComparison.OrdinalIgnoreCase))
                        {
                            qb.Status = "CANCELLED";
                            qb.UpdatedAt = Now;
                            PushMyBookingsChanged(qb.UserId);
                            PushHomeChanged(qb.UserId);
                        }
                        // Thông báo: yêu cầu của bạn đã bị admin hủy
                        try
                        {
                            Send(queuedStream, $"INFO|CANCELLED|{roomId}|{sid}\n");
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"[WARN] FORCE_GRANT_RANGE cancel queued notify failed client={queuedClientId} bookingId={queuedBookingId}: {ex.Message}");
                        }
                    }
                }

                // reset trước khi gán booking mới
                slot.IsBusy = false;
                slot.CurrentHolderClientId = null;
                slot.CurrentHolderUserId = null;
                slot.CurrentBookingId = null;

                slot.IsEventLocked = wasEventLocked;
                slot.EventNote = prevEventNote;
                PushSlotUpdate(roomId, dateKey, sid, log);  // ✅ slot đã đổi về FREE (hoặc bỏ holder/queue)

            }

            // 5. Tạo 1 booking RANGE mới
            var booking = CreateBookingForGrant(
                targetUserId,
                roomId,
                dateKey,
                slotStartId,
                slotEndId,
                true,       // IsRangeBooking = true
                "FORCE_GRANT_BY_ADMIN",
                log);

            // 6. Gán booking này cho toàn bộ slot trong range
            foreach (var (sid, slot) in slots)
            {
                slot.IsBusy = true;
                slot.CurrentHolderUserId = targetUserId;
                string? targetClientId = null;
                lock (_sessionLock)
                {
                    if (_userToClients.TryGetValue(targetUserId, out var set) && set.Count > 0)
                        targetClientId = set.FirstOrDefault();
                }
                slot.CurrentHolderClientId = targetClientId;
                slot.CurrentBookingId = booking.BookingId;
                PushSlotUpdate(roomId, dateKey, sid, log);  // ✅ slot đã BUSY + holder/purpose mới

                log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI_SLOT] {targetUserId} -> {roomId}-{sid} on {dateKey}");
            }
            // ===== NEW: push HOME + MY_BOOKINGS cho user bị ảnh hưởng =====
            foreach (var u in impactedUsers)
            {
                PushHomeChanged(u);
                PushMyBookingsChanged(u); // nếu bạn đã có
            }

            PushHomeChanged(booking.UserId);
            PushMyBookingsChanged(booking.UserId); // nếu bạn đã có

            log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI] {targetUserId} -> {roomId}-{slotStartId}-{slotEndId} on {dateKey}");
            if (_settings.SendEmailOnForceGrantRelease)
            {
                var subject = "[Room booking] Booking đã được FORCE GRANT";
                var bodyTemplate =
                    "Chào {FullName},\n\n" +
                    "Admin đã FORCE GRANT một booking cho bạn.\n" +
                    "- Phòng: {RoomId}\n" +
                    "- Ngày: {Date}\n" +
                    "- Ca: {SlotStartId} - {SlotEndId}\n" +
                    "- Trạng thái: {Status}\n\n" +
                    "Vui lòng chú ý lịch dạy/học của mình.";

                QueueEmailForBooking(booking, subject, bodyTemplate, log);
            }
            if (_settings.SendNotificationToClient)
            {
                NotifyClientBookingChanged(
                    targetUserId,
                    $"FORCE_GRANT|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                    log
                );
            }
            return true;
        }
    }
    // Admin Force RELEASE RANGE từ UI Server
    public bool ForceReleaseRangeFromServerUi(DateTime date, string roomId, string slotStartId, string slotEndId, TextWriter log, out string error)
    {
        // Thực ra chỉ cần 1 slot bất kỳ trong range,
        // vì CompleteAndReleaseSlot sẽ đọc booking.IsRangeBooking
        // rồi tự giải phóng toàn bộ range.
        return ForceReleaseFromServerUi(date, roomId, slotStartId, log, out error);
    }
    // Admin check-in tại UI server, không đi qua TCP client
    public void RunNoShowSweep(DateTime now, TextWriter log)
    {
        bool anyChanged = false;

        lock (_lock)
        {
            // snapshot list để tránh issue nếu _bookings bị chỉnh trong loop
            var approved = _bookings.Where(b => b.Status == "APPROVED").ToList();

            foreach (var booking in approved)
            {
                if (now <= booking.CheckinDeadline) continue;

                booking.Status = "NO_SHOW";
                booking.UpdatedAt = now;
                anyChanged = true;

                log.WriteLine($"[NO_SHOW] Booking {booking.BookingId} {booking.UserId} {booking.RoomId} {booking.SlotStartId}-{booking.SlotEndId}");

                try
                {
                    var slotRange = booking.SlotStartId == booking.SlotEndId
                        ? booking.SlotStartId
                        : $"{booking.SlotStartId}-{booking.SlotEndId}";
                    NotifyUser(booking.UserId,
                        $"NOTIFY_NO_SHOW|{booking.RoomId}|{slotRange}|{booking.Date}|{booking.CheckinDeadline:HH:mm}");
                }
                catch { /* ignore */ }

                // ✅ M6: push HOME + bookings list cho user bị NO_SHOW
                if (!string.IsNullOrEmpty(booking.UserId))
                {
                    PushHomeChanged(booking.UserId);
                    PushMyBookingsChanged(booking.UserId); // nếu bạn đã có
                }

                if (_settings.SendEmailOnNoShow)
                {
                    var subject = "[Room booking] Bạn đã bị NO-SHOW";
                    var bodyTemplate =
                        "Chào {FullName},\n\n" +
                        "Booking của bạn đã bị đánh dấu NO-SHOW.\n" +
                        "- Phòng: {RoomId}\n" +
                        "- Ngày: {Date}\n" +
                        "- Ca: {SlotStartId} - {SlotEndId}\n" +
                        "- Trạng thái: {Status}\n\n" +
                        "Vui lòng liên hệ quản trị nếu cần hỗ trợ.";

                    SendEmailForBooking(booking, subject, bodyTemplate, log);
                }

                // Giải phóng tất cả slot thuộc booking này
                if (!_slotsByDate.TryGetValue(booking.Date, out var dict))
                    continue;

                int startIdx = ParseSlotIndex(booking.SlotStartId);
                int endIdx = ParseSlotIndex(booking.SlotEndId);
                if (startIdx <= 0 || endIdx <= 0) continue;

                for (int idx = startIdx; idx <= endIdx; idx++)
                {
                    var sid = GetSlotId(idx);
                    var key = MakeKey(booking.RoomId, sid);
                    if (!dict.TryGetValue(key, out var slot)) continue;

                    if (slot.CurrentBookingId == booking.BookingId)
                    {
                        slot.IsBusy = false;
                        slot.CurrentHolderClientId = null;
                        slot.CurrentHolderUserId = null;
                        slot.CurrentBookingId = null;

                        // ✅ M5: báo client slot đổi trạng thái
                        PushSlotUpdate(booking.RoomId, booking.Date, sid, log);

                        log.WriteLine($"[SLOT] AUTO FREE by NO_SHOW {booking.RoomId}-{sid} on {booking.Date}");

                        // (tuỳ chọn M4) nếu muốn cấp cho queue tiếp theo:
                        GrantNextFromQueue(booking.Date, booking.RoomId, sid, slot, log);
                    }
                }
            }
        }

        // ✅ tránh spam: raise 1 lần sau sweep
        if (anyChanged)
            RaiseStateChanged();
    }
    public void HandleRequestRange(
        string clientId,
        string roomId,
        string slotStartId,
        string slotEndId,
        string dateStr,
        string purpose,
        NetworkStream stream,
        TextWriter log)
    {
        lock (_lock)
        {
            var userId = GetUserIdForClient(clientId) ?? clientId;

            roomId = (roomId ?? "").Trim().ToUpperInvariant();
            slotStartId = (slotStartId ?? "").Trim().ToUpperInvariant();
            slotEndId = (slotEndId ?? "").Trim().ToUpperInvariant();

            if (IsRoomDisabled(roomId))
            {
                log.WriteLine($"[WARN] REQUEST_RANGE blocked because room is DISABLED: {roomId} by {clientId}");
                _ = SendSafe(stream, "INFO|ERROR|ROOM_DISABLED\n");
                return;
            }

            EnsureDateInitialized(dateStr, log);

            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
            {
                _ = SendSafe(stream, "INFO|ERROR|Invalid slot range\n");
                return;
            }

            var dict = _slotsByDate[dateStr];
            var now = Now;

            // 1) Chặn range hoàn toàn trong quá khứ
            var rangeEndTime = GetSlotEndTime(dateStr, slotEndId);
            if (rangeEndTime <= now)
            {
                _ = SendSafe(stream, "INFO|ERROR|Slot range already in the past\n");
                return;
            }

            // 2) NEW RULE: nếu đang ở ca X thì không cho request range bắt đầu trước ca X
            // Tìm currentSlotIdx = slot đầu tiên có EndTime > now (tức slot "đang diễn ra hoặc sắp tới")
            int currentSlotIdx = -1;
            for (int i = 1; i <= 14; i++)
            {
                var sid = GetSlotId(i);
                var endTime = GetSlotEndTime(dateStr, sid);
                if (now < endTime)
                {
                    currentSlotIdx = i;
                    break;
                }
            }

            if (currentSlotIdx == -1)
            {
                _ = SendSafe(stream, "INFO|ERROR|All slots already finished today\n");
                return;
            }

            if (startIdx < currentSlotIdx)
            {
                _ = SendSafe(stream, "INFO|ERROR|Cannot request past slots in range\n");
                return;
            }

            // 3) Kiểm tra conflict chéo phòng khác (không cho chồng lịch đã GRANT)
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                var sid = GetSlotId(idx);
                if (HasCrossRoomConflict(clientId, dateStr, roomId, sid, out _))
                {
                    _ = SendSafe(stream, "INFO|ERROR|USER_SLOT_CONFLICT\n");
                    return;
                }
            }

            // 3) Gom slot state trong range
            List<(string sid, SlotState st)> slots = new();
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                string sid = GetSlotId(idx);
                string key = MakeKey(roomId, sid);

                if (!dict.TryGetValue(key, out var st))
                {
                    _ = SendSafe(stream, "INFO|ERROR|Invalid room/slot in range\n");
                    return;
                }
                slots.Add((sid, st));
            }

            bool isAdmin = IsAdmin(userId);

            // 4) Check event lock (non-admin không được queue vào slot event)
            if (!isAdmin)
            {
                foreach (var (_, st) in slots)
                {
                    if (st.IsEventLocked)
                    {
                        _ = SendSafe(stream, "INFO|ERROR|SLOT_LOCKED_FOR_EVENT\n");
                        return;
                    }
                }
            }

            // 6) Check BUSY: nếu có slot đang busy bởi người khác => queue range
            bool conflict = false;
            foreach (var (_, st) in slots)
            {
                if (st.IsBusy)
                {
                    var holderUserId = !string.IsNullOrEmpty(st.CurrentHolderUserId)
                        ? st.CurrentHolderUserId
                        : (!string.IsNullOrEmpty(st.CurrentHolderClientId)
                            ? (GetUserIdForClient(st.CurrentHolderClientId) ?? st.CurrentHolderClientId)
                            : "");
                    if (!string.Equals(holderUserId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        conflict = true;
                        break;
                    }
                }
            }

            string rangeKey = MakeRangeKey(roomId, slotStartId, slotEndId);

            if (conflict)
            {
                if (!_rangeQueues.TryGetValue(rangeKey, out var q))
                {
                    q = new Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>();
                    _rangeQueues[rangeKey] = q;
                }

                // ====== CHẶN ENQUEUE TRÙNG (theo userId) ======
                int pos = 1;
                bool alreadyQueued = false;

                // Duyệt queue hiện tại để xem user đã có trong queue chưa
                foreach (var item in q)
                {
                    var queuedUserId = GetUserIdForClient(item.clientId) ?? item.clientId;

                    if (string.Equals(queuedUserId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyQueued = true;
                        break;
                    }
                    pos++;
                }

                if (alreadyQueued)
            {
                    log.WriteLine($"[INFO] REQUEST_RANGE duplicate from {clientId} for {rangeKey} -> already queued pos={pos}");
                    _ = SendSafe(stream, $"RANGE_WAITING|{pos}\n");
                return;
            }

                // ====== Check booking QUEUED khác có overlap với range này không ======
                var queuedBookingsInRoom = _bookings.Where(b =>
                    b.UserId == userId
                    && b.RoomId == roomId
                    && b.Date == dateStr
                    && b.Status == "QUEUED").ToList();

                foreach (var qb in queuedBookingsInRoom)
                {
                    int qStartIdx = ParseSlotIndex(qb.SlotStartId);
                    int qEndIdx = ParseSlotIndex(qb.SlotEndId);
                
                    // Kiểm tra overlap: [startIdx, endIdx] và [qStartIdx, qEndIdx]
                    if (!(endIdx < qStartIdx || startIdx > qEndIdx))
                    {
                        log.WriteLine($"[INFO] REQUEST_RANGE {startIdx}-{endIdx} from {clientId} overlaps with existing QUEUED {qb.SlotStartId}-{qb.SlotEndId}");
                        _ = SendSafe(stream, $"INFO|ERROR|Already queued in overlapping slot range\n");
                        return;
                    }
                }

                var qb2 = CreateBookingQueued(clientId, roomId, dateStr, slotStartId, slotEndId, true, purpose, log);
                var qUserId = GetUserIdForClient(clientId) ?? clientId;
                q.Enqueue((clientId, qUserId, stream, purpose, qb2.BookingId));
                int position = q.Count;

                log.WriteLine($"[QUEUE_RANGE] {clientId} waiting for {rangeKey}, pos={position}");

                // ✅ Client của bạn parse "RANGE_WAITING|pos"
                _ = SendSafe(stream, $"RANGE_WAITING|{position}\n");
                PushMyBookingsChanged(qUserId);
                RaiseStateChanged();
                return;
            }

            // 7) Không conflict => grant luôn
            GrantRange(clientId, roomId, slotStartId, slotEndId, dateStr, stream, purpose, log);
        }
    }

    private Booking CreateBookingQueued(
        string clientId,
        string roomId,
        string dateKey,
        string slotStartId,
        string slotEndId,
        bool isRange,
        string purpose,
        TextWriter log)
    {
        var userId = GetUserIdForClient(clientId) ?? clientId;
        var now = Now;

        var b = new Booking
        {
            BookingId = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            Date = dateKey,
            SlotId = slotStartId,
            SlotStartId = slotStartId,
            SlotEndId = slotEndId,
            IsRangeBooking = isRange,
            Purpose = purpose ?? "",
            Status = "QUEUED",
            CreatedAt = now,
            UpdatedAt = now,
            CheckinDeadline = now,  // tạm, sẽ set lại khi APPROVED
        };

        _bookings.Add(b);
        log.WriteLine($"[BOOKING] QUEUED {b.BookingId} {b.UserId} {roomId} {slotStartId}-{slotEndId}");
        return b;
    }

    private bool PromoteQueuedToApproved(Guid bookingId, TextWriter log, out Booking? booking)
    {
        booking = _bookings.FirstOrDefault(x => x.BookingId == bookingId);
        if (booking == null) return false;

        // chỉ promote nếu còn QUEUED (tránh promote nhầm cái đã cancel)
        if (!string.Equals(booking.Status, "QUEUED", StringComparison.OrdinalIgnoreCase))
            return false;

        var now = Now;
        booking.Status = "APPROVED";
        booking.UpdatedAt = now;

        // set deadline chuẩn theo slotStart
        var start = GetSlotStartTime(booking.Date, booking.SlotStartId);
        booking.CheckinDeadline = start.AddMinutes(15);

        log.WriteLine($"[BOOKING] PROMOTE {booking.BookingId} -> APPROVED");
        
        // Gửi notification cho user
        PushHomeChanged(booking.UserId);
        
        return true;
    }

    // Remove any queue entries in the slot that reference bookingId. Return true if any removed.
    private bool RemoveBookingFromSlotQueue(SlotState slot, Guid bookingId, out List<NetworkStream> removedStreams)
    {
        removedStreams = new List<NetworkStream>();
        if (slot.WaitingQueue.Count == 0) return false;

        var newQ = new Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>();
        bool removed = false;
        while (slot.WaitingQueue.Count > 0)
        {
            var item = slot.WaitingQueue.Dequeue();
            if (item.bookingId == bookingId)
            {
                removed = true;
                if (item.stream != null) removedStreams.Add(item.stream);
            }
            else newQ.Enqueue(item);
        }

        while (newQ.Count > 0) slot.WaitingQueue.Enqueue(newQ.Dequeue());
        return removed;
    }

    // Timer job: cancel queued bookings that are now overdue (slot start passed)
    private void CancelOverdueQueuedBookings()
    {
        try
        {
            List<Booking> toCancel = new();
            lock (_lock)
            {
                var now = Now;
                foreach (var b in _bookings.Where(x => string.Equals(x.Status, "QUEUED", StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    // if slot start time already passed -> cancel queued
                    try
                    {
                        var start = GetSlotStartTime(b.Date, b.SlotStartId);
                        if (now >= start)
                        {
                            b.Status = "CANCELLED";
                            b.UpdatedAt = now;
                            toCancel.Add(b);
                        }
                    }
                    catch { }
                }

                // For each cancelled booking, remove queue entries and push updates
                foreach (var b in toCancel)
                {
                    var key = MakeKey(b.RoomId, b.SlotStartId);
                    if (_slotsByDate.TryGetValue(b.Date, out var dict) && dict.TryGetValue(key, out var slot))
                    {
                        if (RemoveBookingFromSlotQueue(slot, b.BookingId, out var streams))
                        {
                            foreach (var s in streams)
                            {
                                try { Send(s, $"INFO|CANCELLED|{b.RoomId}|{b.SlotStartId}\n"); } catch { }
                            }
                        }
                        PushSlotUpdate(b.RoomId, b.Date, b.SlotStartId, TextWriter.Null);
                    }

                    PushMyBookingsChanged(b.UserId);
                    PushHomeChanged(b.UserId);
                }
            }
        }
        catch { }
    }

    public void HandleReleaseRange(string clientId, string roomId, string slotStartId, string slotEndId, string dateStr, NetworkStream replyStream, TextWriter log)
    {
        lock (_lock)
        {
            var userId = GetUserIdForClient(clientId) ?? clientId;
            bool isAdmin = IsAdmin(userId);

            if (!_slotsByDate.TryGetValue(dateStr, out var slotsForDate))
            {
                _ = SendSafe(replyStream, "INFO|ERROR|No slots for current date\n");
                return;
            }

            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
            {
                _ = SendSafe(replyStream, "INFO|ERROR|Invalid slot range\n");
                return;
            }

            // Tìm booking range tương ứng (bao gồm cả QUEUED, APPROVED, IN_USE)
            var booking = _bookings.FirstOrDefault(b =>
               b.UserId == userId
            && b.RoomId == roomId
            && b.Date == dateStr
            && b.IsRangeBooking
            && b.SlotStartId == slotStartId
            && b.SlotEndId == slotEndId
            && (b.Status == "QUEUED" || b.Status == "APPROVED" || b.Status == "IN_USE"));

            if (booking == null)
            {
                _ = SendSafe(replyStream, "INFO|ERROR|NO_RANGE_BOOKING\n");
                return;
            }

            // Nếu đang IN_USE mà không phải admin -> từ chối
            if (!isAdmin && string.Equals(booking.Status, "IN_USE", StringComparison.OrdinalIgnoreCase))
            {
                log.WriteLine($"[WARN] User {clientId} cannot RELEASE_RANGE IN_USE booking {booking.BookingId}");
                _ = SendSafe(replyStream, "INFO|ERROR|CANNOT_RELEASE_IN_USE\n");
                return;
            }

            // Khai báo rangeKey ở đây để dùng chung cho cả 2 cases
            string rangeKey = MakeRangeKey(roomId, slotStartId, slotEndId);
            Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>? rangeQ = null;

            // CASE: Booking đang QUEUED -> loại khỏi range queue
            if (booking.Status == "QUEUED")
            {
                log.WriteLine($"[RANGE_RELEASE] {clientId} {roomId} {slotStartId}-{slotEndId} QUEUED -> CANCELLED");
                booking.Status = "CANCELLED";
                booking.UpdatedAt = Now;

                // Loại khỏi range queue
                if (_rangeQueues.TryGetValue(rangeKey, out rangeQ))
                {
                    var newQ = new Queue<(string clientId, string userId, NetworkStream stream, string purpose, Guid bookingId)>();
                    while (rangeQ.Count > 0)
                    {
                        var item = rangeQ.Dequeue();
                        if (item.bookingId != booking.BookingId)
                        {
                            newQ.Enqueue(item);
                        }
                    }
                    while (newQ.Count > 0)
                        rangeQ.Enqueue(newQ.Dequeue());
                }

                PushMyBookingsChanged(booking.UserId);
                PushHomeChanged(booking.UserId);
                _ = SendSafe(replyStream, $"INFO|RELEASED|{roomId}|{slotStartId}-{slotEndId}\n");
                RaiseStateChanged();
                return;
            }

            // CASE: Booking đang APPROVED/IN_USE -> giải phóng slot + cấp queue
            // Set trạng thái mới
            booking.Status = (booking.Status == "IN_USE") ? "COMPLETED" : "CANCELLED";
            booking.UpdatedAt = Now;

            log.WriteLine($"[RANGE_RELEASE] {clientId} {roomId} {slotStartId}-{slotEndId} -> {booking.Status}");

            // GIẢI PHÓNG TOÀN BỘ SLOT
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                var sid = GetSlotId(idx);
                var key = MakeKey(roomId, sid);

                if (!slotsForDate.TryGetValue(key, out var slot))
                    continue;

                if (slot.CurrentBookingId == booking.BookingId)
                {
                    slot.IsBusy = false;
                    slot.CurrentHolderClientId = null;
                    slot.CurrentHolderUserId = null;
                    slot.CurrentBookingId = null;

                    log.WriteLine($"[SLOT_FREE] {roomId}-{sid}");

                    // Tự động cấp cho queue SINGLE
                    GrantNextFromQueue(dateStr, roomId, sid, slot, log);
                    PushSlotUpdate(roomId, dateStr, sid, log); // ✅ M5: push trạng thái cuối cùng

                }
            }

            // ================================
            // 🔥 RANGE QUEUE: cấp user tiếp theo
            // ================================
            if (_rangeQueues.TryGetValue(rangeKey, out rangeQ) && rangeQ.Count > 0)
            {
                bool granted = false;
                // Try promoting queued range bookings until one succeeds
                while (rangeQ.Count > 0 && !granted)
                {
                    var (nextClient, nextUserId, nextStream, nextPurpose, queuedBookingId) = rangeQ.Dequeue();
                    log.WriteLine($"[RANGE_QUEUE] Attempt promote queued booking {queuedBookingId} for {rangeKey} to client {nextClient}");

                    if (PromoteQueuedToApproved(queuedBookingId, log, out var promotedBooking))
                    {
                        // 1) Assign booking to all slots in range (no network writes yet)
                        for (int idx = startIdx; idx <= endIdx; idx++)
                        {
                            var sid = GetSlotId(idx);
                            var key = MakeKey(roomId, sid);
                            if (!slotsForDate.TryGetValue(key, out var slot)) continue;

                            slot.IsBusy = true;
                            slot.CurrentHolderClientId = nextClient;
                            slot.CurrentHolderUserId = promotedBooking.UserId;
                            slot.CurrentBookingId = promotedBooking.BookingId;
                        }

                        // 2) Notify client ASAP (before SLOT_UPDATE spam)
                        Send(nextStream, $"GRANTED_RANGE|{promotedBooking.BookingId}|{roomId}|{slotStartId}|{slotEndId}\n");

                        // 3) Now push SLOT_UPDATE per slot
                        for (int idx = startIdx; idx <= endIdx; idx++)
                        {
                            var sid = GetSlotId(idx);
                            PushSlotUpdate(roomId, dateStr, sid, log);
                            log.WriteLine($"[GRANT_RANGE_SLOT] {nextClient} -> {roomId}-{sid} from queue");
                        }

                        if (_settings.SendEmailOnGrant)
                        {
                            SendEmailForBooking(promotedBooking, "[Room booking] RANGE booking granted (from queue)", "Booking RANGE của bạn đã được GRANT từ hàng chờ", log);
                        }

                        if (_settings.SendNotificationToClient)
                        {
                            NotifyClientBookingChanged(promotedBooking.UserId, $"GRANTED_FROM_QUEUE|{promotedBooking.BookingId}|{promotedBooking.RoomId}|{promotedBooking.SlotStartId}|{promotedBooking.SlotEndId}", log);
                        }

                        PushHomeChanged(promotedBooking.UserId);
                        PushMyBookingsChanged(promotedBooking.UserId);
                        PushHomeChanged(booking.UserId);
                        PushMyBookingsChanged(booking.UserId);
                        RaiseStateChanged();

                        granted = true;
                    }
                    else
                    {
                        log.WriteLine($"[WARN] queued range booking {queuedBookingId} could not be promoted");
                        // continue to next queued entry
                    }
                }

                if (!granted)
                {
                    // none could be promoted: treat as released
                    _ = SendSafe(replyStream, $"INFO|RANGE_RELEASED|{roomId}|{slotStartId}|{slotEndId}\n");
                    PushMyBookingsChanged(booking.UserId);
                    PushHomeChanged(userId);
                    RaiseStateChanged();
                }
            }
            else
            {
                // Không ai trong queue range → trả về client cũ
                _ = SendSafe(replyStream, $"INFO|RANGE_RELEASED|{roomId}|{slotStartId}|{slotEndId}\n");
                PushMyBookingsChanged(booking.UserId);
                PushHomeChanged(userId);
                RaiseStateChanged();
            }
        }
    }

    public BookingView? GetCurrentBookingForSlot(DateTime date, string roomId, string slotId)
    {
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
                return null;

            var key = MakeKey(roomId, slotId);    // 🔴 dùng MakeKey
            if (!slotsForDate.TryGetValue(key, out var slotState))
                return null;

            if (slotState.CurrentBookingId == null)
                return null;

            var booking = _bookings.FirstOrDefault(b => b.BookingId == slotState.CurrentBookingId.Value);
            if (booking == null)
                return null;

            _users.TryGetValue(booking.UserId, out var user);

            var timeRange = GetSlotTimeRange(booking.SlotStartId, booking.SlotEndId);

            return new BookingView
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                FullName = user?.FullName ?? "",
                UserType = user?.UserType ?? "",
                Email = user?.Email ?? "",
                Phone = user?.Phone ?? "",
                RoomId = booking.RoomId,
                Date = booking.Date,
                SlotStartId = booking.SlotStartId,
                SlotEndId = booking.SlotEndId,
                TimeRange = timeRange,
                IsRange = booking.IsRangeBooking,
                Purpose = booking.Purpose ?? "",
                Status = booking.Status,
                CheckinDeadline = booking.CheckinDeadline,
                CheckinTime = booking.CheckinTime,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }
    }

    public List<BookingView> GetTodayBookingsForUser(string userId)
    {
        var todayKey = Now.Date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            return GetBookingViews()
                .Where(b => string.Equals(b.UserId, userId, StringComparison.OrdinalIgnoreCase) && b.Date == todayKey)
                .OrderBy(b => b.TimeRange)
                .ToList();
        }
    }

    public bool CheckInSlot(DateTime date, string roomId, string slotId, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                error = "Không tìm thấy slot.";
                return false;
            }

            var key = MakeKey(roomId, slotId);
            if (!slotsForDate.TryGetValue(key, out var slot))
            {
                error = "Không tìm thấy slot.";
                return false;
            }

            if (slot.CurrentBookingId == null)
            {
                error = "Slot hiện không có booking.";
                return false;
            }

            var booking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
            if (booking == null)
            {
                error = "Không tìm thấy booking.";
                return false;
            }

            if (booking.Status != "APPROVED")
            {
                error = $"Booking không ở trạng thái APPROVED (hiện tại: {booking.Status}).";
                return false;
            }

            var now = Now;
            if (now > booking.CheckinDeadline)
            {
                error = "Đã quá thời gian check-in.";
                return false;
            }

            booking.Status = "IN_USE";
            booking.CheckinTime = now;
            booking.UpdatedAt = now;
            // ✅ M5: push delta cho slot này (và cả range nếu booking là range)
            if (booking.IsRangeBooking)
            {
                int startIdx = ParseSlotIndex(booking.SlotStartId);
                int endIdx = ParseSlotIndex(booking.SlotEndId);
                if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
                    startIdx = endIdx = ParseSlotIndex(slotId);

                for (int idx = startIdx; idx <= endIdx; idx++)
                {
                    var sid = GetSlotId(idx);
                    PushSlotUpdate(roomId, dateKey, sid, log);
                }
            }
            else
            {
                PushSlotUpdate(roomId, dateKey, slotId, log);
            }
            log.WriteLine($"[CHECKIN] Manual check-in booking {booking.BookingId} {booking.UserId} {roomId}-{slotId} on {dateKey} at {now:HH:mm}");
            PushMyBookingsChanged(booking.UserId);
            
            // Gửi notification cho user
            PushHomeChanged(booking.UserId);

            if (_settings.SendEmailOnForceGrantRelease)
            {
                var subject = "[Room booking] Booking đã được CHECK-IN";
                var bodyTemplate =
                    "Chào {FullName},\n\n" +
                    "Booking của bạn đã được CHECK-IN.\n" +
                    "- Phòng: {RoomId}\n" +
                    "- Ngày: {Date}\n" +
                    "- Ca: {SlotStartId} - {SlotEndId}\n" +
                    "- Trạng thái: {Status}\n";
                QueueEmailForBooking(booking, subject, bodyTemplate, log);
            }

            var slotRange = booking.SlotStartId == booking.SlotEndId
                ? booking.SlotStartId
                : $"{booking.SlotStartId}-{booking.SlotEndId}";
            NotifyUser(booking.UserId, $"NOTIFY_CHECKIN|{roomId}|{slotRange}|{dateKey}|{now:HH:mm}");
            
            return true;
        }
    }
    private void GrantNextFromQueue(string dateKey, string roomId, string slotId, SlotState slot, TextWriter log)
    {
        if (slot.WaitingQueue.Count == 0)
        {
            log.WriteLine($"[SLOT] {roomId}-{slotId} on {dateKey} -> FREE (no one in queue)");
            PushSlotUpdate(roomId, dateKey, slotId, log); // ✅ M5
            return;
        }

        while (slot.WaitingQueue.Count > 0)
        {
            var (nextClientId, nextUserId, nextStream, nextPurpose, queuedBookingId) = slot.WaitingQueue.Dequeue();

            // ✅ map ra userId thật (để check conflict + notify)
            nextUserId = nextUserId ?? (GetUserIdForClient(nextClientId) ?? nextClientId);

            string conflictedRoom;
            if (HasCrossRoomConflict(nextUserId, dateKey, roomId, slotId, out conflictedRoom))
            {
                log.WriteLine(
                    $"[GRANT_SKIP] Skip grant {roomId}-{slotId} on {dateKey} " +
                    $"cho user {nextUserId} (client {nextClientId}) vì đã có booking GRANT trùng ca ở phòng {conflictedRoom}"
                );

                try
                {
                    // mark queued booking cancelled
                    var qb = _bookings.FirstOrDefault(x => x.BookingId == queuedBookingId);
                    if (qb != null && string.Equals(qb.Status, "QUEUED", StringComparison.OrdinalIgnoreCase))
                    {
                        qb.Status = "CANCELLED";
                        qb.UpdatedAt = Now;
                        PushMyBookingsChanged(qb.UserId);
                        PushHomeChanged(qb.UserId);
                    }

                    Send(
                        nextStream,
                        "INFO|ERROR|CROSS_ROOM_GRANT_CONFLICT|" +
                        $"Bạn đã có booking được GRANT trùng ca ở phòng {conflictedRoom}. Request này bị hủy.\n"
                    );
                }
                catch (Exception ex)
                {
                    log.WriteLine($"[GRANT_SKIP] Lỗi khi gửi CROSS_ROOM_GRANT_CONFLICT cho {nextClientId}: {ex.Message}");
                }
                PushSlotUpdate(roomId, dateKey, slotId, log); // ✅ M5 (trạng thái vẫn FREE, nhưng giúp client cập nhật)
                continue;
            }

            // ✅ GRANT slot cho clientId (nhất quán)
            slot.IsBusy = true;
            slot.CurrentHolderClientId = nextClientId;

            // ✅ PROMOTE queued booking (không tạo booking mới)
            if (PromoteQueuedToApproved(queuedBookingId, log, out var booking))
            {
                slot.CurrentBookingId = booking.BookingId;
                slot.CurrentHolderUserId = booking.UserId;
                PushSlotUpdate(roomId, dateKey, slotId, log); // ✅ M5

                log.WriteLine($"[GRANT] {nextClientId} -> {roomId}-{slotId} from queue on date {dateKey}");
                Send(nextStream, $"GRANT|{roomId}|{slotId}\n");

                NotifyUser(booking.UserId, $"NOTIFY_GRANT|{roomId}|{slotId}|{dateKey}|{booking.CheckinDeadline:HH:mm}");

                if (_settings.SendEmailOnGrant)
                {
                    var subject = "[Room booking] Booking của bạn đã được GRANT từ hàng chờ";
                    var bodyTemplate =
                        "Chào {FullName},\n\n" +
                        "Yêu cầu đặt phòng của bạn đã được GRANT (từ hàng chờ).\n" +
                        "- Phòng: {RoomId}\n" +
                        "- Ngày: {Date}\n" +
                        "- Ca: {SlotStartId} - {SlotEndId}\n" +
                        "- Trạng thái: {Status}\n\n" +
                        "Vui lòng check-in trước hạn.";

                    SendEmailForBooking(booking, subject, bodyTemplate, log);
                }

                if (_settings.SendNotificationToClient)
                {
                    NotifyClientBookingChanged(
                    booking.UserId,
                    $"GRANTED_FROM_QUEUE|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                    log
                );
                }
                PushHomeChanged(booking.UserId);
                PushMyBookingsChanged(booking.UserId);
                RaiseStateChanged();
            }
            else
            {
                // Promote failed - booking was cancelled or not found
                log.WriteLine($"[WARN] queued booking {queuedBookingId} could not be promoted");
                slot.IsBusy = false;
                slot.CurrentHolderClientId = null;
                slot.CurrentHolderUserId = null;
                slot.CurrentBookingId = null;
                PushSlotUpdate(roomId, dateKey, slotId, log);
                continue;
            }
            return;
        }

        log.WriteLine($"[QUEUE] Không còn client phù hợp để GRANT cho {roomId}-{slotId} ngày {dateKey}");
        PushSlotUpdate(roomId, dateKey, slotId, log); // ✅ M5 (slot coi như FREE)

    }

    private Booking CreateBookingForQueued(
        string clientId,
        string roomId,
        string dateKey,
        string slotStartId,
        string slotEndId,
        bool isRange,
        string purpose,
        TextWriter log)
    {
        var userId = GetUserIdForClient(clientId) ?? clientId;
        var now = Now;

        // Với QUEUED: chưa có deadline check-in thực sự, nhưng để không null thì set theo slotStart + 15'
        var slotStartTime = GetSlotStartTime(dateKey, slotStartId);
        var slotEndTime = GetSlotEndTime(dateKey, slotEndId);
        var checkinDeadline = slotStartTime.AddMinutes(15);
        if (checkinDeadline < now) checkinDeadline = now;
        if (checkinDeadline > slotEndTime) checkinDeadline = slotEndTime;

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            Date = dateKey,
            SlotId = slotStartId,
            SlotStartId = slotStartId,
            SlotEndId = slotEndId,
            IsRangeBooking = isRange,
            Purpose = purpose ?? "",
            CreatedAt = now,
            UpdatedAt = now,
            Status = "QUEUED",
            CheckinDeadline = checkinDeadline,
            // CheckinTime = null
        };

        _bookings.Add(booking);
        log.WriteLine($"[BOOKING] Create {booking.BookingId} {userId} {roomId} {slotStartId}-{slotEndId} QUEUED");
        return booking;
    }
    public bool CompleteAndReleaseSlot(DateTime date, string roomId, string slotId, TextWriter log, out string error)
    {
        error = "";
        var dateKey = date.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                error = "Không tìm thấy slot.";
                return false;
            }

            var key = MakeKey(roomId, slotId);
            if (!slotsForDate.TryGetValue(key, out var slot))
            {
                error = "Không tìm thấy slot.";
                return false;
            }

            if (slot.CurrentBookingId == null)
            {
                error = "Slot hiện không có booking.";
                return false;
            }

            var booking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
            if (booking == null)
            {
                error = "Không tìm thấy booking.";
                return false;
            }

            // Chỉ admin được gọi hàm này (check IsAdmin ở ngoài)
            string newStatus;
            if (booking.Status == "IN_USE")
            {
                newStatus = "COMPLETED";
            }
            else if (booking.Status == "APPROVED")
            {
                newStatus = "CANCELLED";
            }
            else
            {
                error = $"Booking đang ở trạng thái {booking.Status}, không thể Complete.";
                return false;
            }

            booking.Status = newStatus;
            booking.UpdatedAt = Now;
            PushMyBookingsChanged(booking.UserId);
            
            // Gửi notification cho user khi COMPLETED
            if (newStatus == "COMPLETED")
            {
                PushHomeChanged(booking.UserId);

                if (_settings.SendEmailOnForceGrantRelease)
                {
                    var subject = "[Room booking] Booking đã COMPLETED";
                    var bodyTemplate =
                        "Chào {FullName},\n\n" +
                        "Booking của bạn đã được đánh dấu COMPLETED.\n" +
                        "- Phòng: {RoomId}\n" +
                        "- Ngày: {Date}\n" +
                        "- Ca: {SlotStartId} - {SlotEndId}\n" +
                        "- Trạng thái: {Status}\n";
                    QueueEmailForBooking(booking, subject, bodyTemplate, log);
                }

                var slotRange = booking.SlotStartId == booking.SlotEndId
                    ? booking.SlotStartId
                    : $"{booking.SlotStartId}-{booking.SlotEndId}";
                NotifyUser(booking.UserId, $"NOTIFY_COMPLETE|{roomId}|{slotRange}|{dateKey}|{Now:HH:mm}");
            }

            // =========================
            // 1) Nếu là booking RANGE
            // =========================
            if (booking.IsRangeBooking)
            {
                int startIdx = ParseSlotIndex(booking.SlotStartId);
                int endIdx = ParseSlotIndex(booking.SlotEndId);

                if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
                {
                    // Dữ liệu range bị lỗi, fallback: xử lý như single
                    startIdx = endIdx = ParseSlotIndex(slotId);
                }

                for (int idx = startIdx; idx <= endIdx; idx++)
                {
                    var sidRange = GetSlotId(idx);
                    var keyRange = MakeKey(roomId, sidRange);

                    if (!slotsForDate.TryGetValue(keyRange, out var slotRange))
                        continue;

                    // Chỉ đụng vào slot đang gắn đúng booking này
                    if (slotRange.CurrentBookingId == booking.BookingId)
                    {
                        slotRange.IsBusy = false;
                        slotRange.CurrentHolderClientId = null;
                        slotRange.CurrentHolderUserId = null;
                        slotRange.CurrentBookingId = null;
                        // ✅ M5: push FREE trước
                        PushSlotUpdate(roomId, dateKey, sidRange, log);

                        // Cấp quyền cho người tiếp theo (nếu có) của từng slot trong range
                        GrantNextFromQueue(dateKey, roomId, sidRange, slotRange, log);
                    }
                }
            }
            else
            {
                // =========================
                // 2) Booking single-slot (cũ)
                // =========================
                slot.IsBusy = false;
                slot.CurrentHolderClientId = null;
                slot.CurrentHolderUserId = null;
                slot.CurrentBookingId = null;
                // ✅ M5: push FREE trước
                PushSlotUpdate(roomId, dateKey, slotId, log);

                GrantNextFromQueue(dateKey, roomId, slotId, slot, log);
            }

            log.WriteLine($"[COMPLETE] {booking.UserId} {roomId}-{slotId} ({dateKey}), status={booking.Status}");

            return true;
        }
    }

    public List<UserInfo> GetUserViews()
    {
        lock (_lock)
        {
            return _users.Values
                .Select(u => new UserInfo
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    UserType = u.UserType,
                    StudentId = u.StudentId,
                    LecturerId = u.LecturerId,
                    Class = u.Class,

                    // Với Student: dùng Department; với Lecturer: dùng Faculty
                    Department = u.UserType == "Student" ? u.Department : u.Faculty,

                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive
                })
                .OrderBy(v => v.UserId)
                .ToList();
        }
    }

    // Admin Force RELEASE từ UI Server
    public bool ForceReleaseFromServerUi(DateTime date, string roomId, string slotId, TextWriter log, out string error)
    {
        error = "";

        var dateKey = date.ToString("yyyy-MM-dd");
        Guid? bookingIdToNotify = null;
        string? userIdToPush = null;

        // 1) Lấy ra BookingId đang giữ slot này (nếu có)
        lock (_lock)
        {
            if (_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                var key = MakeKey(roomId, slotId);

                if (slotsForDate.TryGetValue(key, out var slot) && slot.CurrentBookingId.HasValue)
                {
                    bookingIdToNotify = slot.CurrentBookingId.Value;

                    var oldBooking = _bookings.FirstOrDefault(b => b.BookingId == bookingIdToNotify.Value);
                    if (oldBooking != null)
                    {
                        userIdToPush = oldBooking.UserId;

                        if (!string.Equals(oldBooking.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(oldBooking.Status, "IN_USE", StringComparison.OrdinalIgnoreCase))
                        {
                            error = $"Booking đang ở trạng thái {oldBooking.Status}, không thể FORCE RELEASE.";
                            bookingIdToNotify = null;
                            userIdToPush = null;
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(error))
            return false;

        if (!bookingIdToNotify.HasValue)
        {
            error = "Slot hiện không có booking.";
            return false;
        }

        // 2) Gọi lại logic cũ: COMPLETE & RELEASE
        if (!CompleteAndReleaseSlot(date, roomId, slotId, log, out error))
        {
            return false;
        }
        // 3) ✅ M6: đảm bảo slot update được push (an toàn kể cả core đã push rồi)
        try
        {
            PushSlotUpdate(roomId, dateKey, slotId, log);
        }
        catch { /* ignore */ }

        // 4) ✅ M6: push HOME + bookings list cho user bị ảnh hưởng (nếu biết)
        if (!string.IsNullOrEmpty(userIdToPush))
        {
            PushHomeChanged(userIdToPush);
            PushMyBookingsChanged(userIdToPush); // nếu bạn đã có
        }
        // 3) Nếu có booking để notify thì lấy Booking ra và gửi email + notify
        if (bookingIdToNotify.HasValue)
        {
            Booking? booking = null;

            lock (_lock)
            {
                booking = _bookings
                    .FirstOrDefault(b => b.BookingId == bookingIdToNotify.Value);
            }

            if (booking != null)
            {
                // Email FORCE RELEASE
                if (_settings.SendEmailOnForceGrantRelease)
                {
                    var subject = "[Room booking] Booking của bạn đã bị FORCE RELEASE";
                    var bodyTemplate =
                        "Chào {FullName},\n\n" +
                        "Admin đã FORCE RELEASE booking của bạn.\n" +
                        "- Phòng: {RoomId}\n" +
                        "- Ngày: {Date}\n" +
                        "- Ca: {SlotStartId} - {SlotEndId}\n" +
                        "- Trạng thái: {Status}\n\n" +
                        "Nếu có thắc mắc, vui lòng liên hệ quản trị.";

                    QueueEmailForBooking(booking, subject, bodyTemplate, log);
                }

                // Notify client (hiện tại chỉ log, sau này có thể gửi thật)
                if (_settings.SendNotificationToClient)
                {
                    NotifyClientBookingChanged(
                        booking.UserId,
                        $"FORCE_RELEASE|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                        log
                    );
                }
                PushHomeChanged(booking.UserId);
                PushMyBookingsChanged(booking.UserId); // nếu bạn đã có
            }
        }

        return true;
    }
    public void SubHome(string clientId, string userId)
    {
        lock (_subLock)
        {
            if (!_subHome.TryGetValue(userId, out var set))
                _subHome[userId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            set.Add(clientId);
        }
    }
    private void PushHomeChanged(string userId)
    {
        SendToUser(userId, $"PUSH_HOME_DATA_CHANGED|{userId}\n");
    }


    /// Tra cứu lịch 14 ca của 1 phòng trong 1 ngày
    public List<RoomDailySlotView> GetDailySchedule(DateTime date, string roomId, TextWriter log)
    {
        var dateKey = date.ToString("yyyy-MM-dd");
        var result = new List<RoomDailySlotView>();

        lock (_lock)
        {
            EnsureDateInitialized(dateKey, log);

            if (!_slotsByDate.TryGetValue(dateKey, out var dictSlots))
                return result;

            for (int i = 1; i <= SlotCount; i++)
            {
                var slotId = GetSlotId(i);              // "S1".."S14"
                var key = MakeKey(roomId, slotId);

                dictSlots.TryGetValue(key, out var slotState);

                // Mặc định FREE
                string status = "FREE";
                string userId = "";
                string fullName = "";
                string bookingStatus = "";
                string purpose = "";

                if (slotState != null)
                {
                    // Kiểm tra IsEventLocked trước (fixed schedule)
                    if (slotState.IsEventLocked)
                    {
                        status = "LOCKED";
                        purpose = slotState.EventNote ?? "Fixed Schedule";
                    }
                    else if (slotState.IsBusy)
                    {
                        status = "BUSY";

                        // Map holderClientId -> userId (nếu có)
                        if (!string.IsNullOrEmpty(slotState.CurrentHolderClientId))
                        {
                            var holderClientId = slotState.CurrentHolderClientId;
                            var mappedUserId = GetUserIdForClient(holderClientId) ?? holderClientId;

                            userId = mappedUserId;
                            if (_users.TryGetValue(mappedUserId, out var user))
                            {
                                fullName = user.FullName;
                            }
                        }

                        if (slotState.CurrentBookingId.HasValue)
                        {
                            var booking = _bookings
                                .FirstOrDefault(b => b.BookingId == slotState.CurrentBookingId.Value);
                            if (booking != null)
                            {
                                bookingStatus = booking.Status;
                                purpose = booking.Purpose;
                            }
                        }
                    }
                }

                // Khung giờ: dùng lại logic GetSlotStartTime / GetSlotEndTime
                var start = GetSlotStartTime(dateKey, slotId);
                var end = GetSlotEndTime(dateKey, slotId);
                var timeRange = $"{start:HH:mm}-{end:HH:mm}";

                result.Add(new RoomDailySlotView
                {
                    Date = dateKey,
                    RoomId = roomId,
                    SlotId = slotId,
                    TimeRange = timeRange,
                    Status = status,
                    UserId = userId,
                    FullName = fullName,
                    BookingStatus = bookingStatus,
                    Purpose = purpose

                });
            }
        }

        return result;
    }
    /// Thống kê theo phòng: trong khoảng [fromDate, toDate]
    public List<RoomStats> GetRoomStatistics(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        static string NormStatus(string? s)
        {
            var v = (s ?? string.Empty).Trim().ToUpperInvariant();
            v = v.Replace('-', '_').Replace(' ', '_');
            while (v.Contains("__", StringComparison.Ordinal))
                v = v.Replace("__", "_", StringComparison.Ordinal);
            return v;
        }

        lock (_lock)
        {
            // Lọc booking theo CreatedAt và khoảng ngày
            var filtered = _bookings.Where(b =>
            {
                var d = DateTime.Parse(b.Date); // b.Date = "yyyy-MM-dd"
                return d >= from && d <= to;
            })
            .GroupBy(b => b.BookingId)
            .Select(g => g
                .OrderByDescending(x => x.UpdatedAt)
                .First());

            var groups = filtered.GroupBy(b => b.RoomId);

            var result = new List<RoomStats>();
            foreach (var g in groups)
            {
                var roomId = g.Key;
                var total = g.Count();
                var noShow = g.Count(x => NormStatus(x.Status) == "NO_SHOW");
                var cancelled = g.Count(x => NormStatus(x.Status) == "CANCELLED");

                result.Add(new RoomStats
                {
                    RoomId = roomId,
                    TotalBookings = total,
                    NoShowCount = noShow,
                    CancelledCount = cancelled
                });
            }

            return result.OrderBy(r => r.RoomId).ToList();
        }
    }
    //Hàm tạo key cho RANGE
    private string MakeRangeKey(string roomId, string slotFrom, string slotTo)
    {
        return $"{roomId}::RANGE::{slotFrom}-{slotTo}";
    }
    /// Thống kê theo loại user (Student / Lecturer / Staff)
    public List<UserTypeStats> GetUserTypeStatistics(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        static string NormStatus(string? s)
        {
            var v = (s ?? string.Empty).Trim().ToUpperInvariant();
            v = v.Replace('-', '_').Replace(' ', '_');
            while (v.Contains("__", StringComparison.Ordinal))
                v = v.Replace("__", "_", StringComparison.Ordinal);
            return v;
        }

        lock (_lock)
        {
            var filtered = _bookings.Where(b =>
            {
                var d = DateTime.Parse(b.Date);
                return d >= from && d <= to;
            })
            .GroupBy(b => b.BookingId)
            .Select(g => g
                .OrderByDescending(x => x.UpdatedAt)
                .First());

            // Join sang _users để lấy UserType
            var query = filtered
                .Select(b =>
                {
                    _users.TryGetValue(b.UserId, out var user);
                    var userType = user?.UserType ?? "Unknown";
                    return new { Booking = b, UserType = userType };
                });

            var groups = query.GroupBy(x => x.UserType);

            var result = new List<UserTypeStats>();
            foreach (var g in groups)
            {
                var total = g.Count();
                var noShow = g.Count(x => NormStatus(x.Booking.Status) == "NO_SHOW");

                result.Add(new UserTypeStats
                {
                    UserType = g.Key,
                    TotalBookings = total,
                    NoShowCount = noShow
                });
            }

            return result.OrderBy(r => r.UserType).ToList();
        }
    }

    private Snapshot BuildSnapshot()
    {
        var snapshot = new Snapshot();

        // SlotsByDate: convert SlotState -> SlotSnapshot
        foreach (var dateEntry in _slotsByDate)
        {
            var dateKey = dateEntry.Key;
            var dict = dateEntry.Value;

            var dictSnap = new Dictionary<string, SlotSnapshot>();
            foreach (var kvp in dict)
            {
                var key = kvp.Key;
                var slot = kvp.Value;

                dictSnap[key] = new SlotSnapshot
                {
                    IsBusy = slot.IsBusy,
                    CurrentHolderClientId = slot.CurrentHolderClientId,
                    CurrentHolderUserId = slot.CurrentHolderUserId,
                    CurrentBookingId = slot.CurrentBookingId,
                    IsEventLocked = slot.IsEventLocked,
                    EventNote = slot.EventNote
                };
            }

            snapshot.SlotsByDate[dateKey] = dictSnap;
        }

        // Bookings
        snapshot.Bookings.AddRange(_bookings);

        // Users
        foreach (var kvp in _users)
        {
            snapshot.Users[kvp.Key] = kvp.Value;
        }

        // ✅ Rooms
        foreach (var kvp in _rooms)
        {
            snapshot.Rooms[kvp.Key] = kvp.Value;
        }

        // ✅ FixedSessions
        snapshot.FixedSessions.AddRange(_fixedSessions);

        lock (_homeNotiLock)
        {
            foreach (var kvp in _homeNotifications)
            {
                snapshot.HomeNotifications[kvp.Key] = kvp.Value.ToList();
            }
        }

        lock (_notifyLock)
        {
            foreach (var kvp in _pendingNotifications)
            {
                snapshot.PendingNotifications[kvp.Key] = kvp.Value.ToList();
            }
        }

        return snapshot;
    }
    public bool SaveSnapshotToFile(string filePath, TextWriter log)
    {
        try
        {
            lock (_lock)
            {
                var snapshot = BuildSnapshot();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(snapshot, options);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }

            log.WriteLine($"[SNAPSHOT] Saved to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            log.WriteLine($"[SNAPSHOT][ERROR] Failed to save: {ex.Message}");
            return false;
        }
    }
    public bool LoadSnapshotIfExists(string filePath, TextWriter log)
    {
        if (!File.Exists(filePath))
        {
            log.WriteLine($"[SNAPSHOT] No snapshot file ({filePath}), using demo data.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            var snapshot = JsonSerializer.Deserialize<Snapshot>(json);

            if (snapshot == null)
                throw new Exception("Snapshot is null");

            lock (_lock)
            {
                // Khôi phục slotsByDate
                _slotsByDate.Clear();
                foreach (var dateEntry in snapshot.SlotsByDate)
                {
                    var dateKey = dateEntry.Key;
                    var dictSnap = dateEntry.Value;

                    var dictSlots = new Dictionary<string, SlotState>();
                    foreach (var kvp in dictSnap)
                    {
                        var key = kvp.Key;
                        var snap = kvp.Value;

                        dictSlots[key] = new SlotState
                        {
                            IsBusy = snap.IsBusy,
                            CurrentHolderClientId = snap.CurrentHolderClientId,
                            CurrentHolderUserId = snap.CurrentHolderUserId,
                            CurrentBookingId = snap.CurrentBookingId,
                            IsEventLocked = snap.IsEventLocked,
                            EventNote = snap.EventNote
                        };
                    }

                    _slotsByDate[dateKey] = dictSlots;
                }

                // Khôi phục bookings
                _bookings.Clear();
                if (snapshot.Bookings != null)
                    _bookings.AddRange(snapshot.Bookings);

                foreach (var dateEntry in _slotsByDate)
                {
                    foreach (var slot in dateEntry.Value.Values)
                    {
                        if (!string.IsNullOrEmpty(slot.CurrentHolderUserId))
                            continue;

                        if (!slot.CurrentBookingId.HasValue)
                            continue;

                        var b = _bookings.FirstOrDefault(x => x.BookingId == slot.CurrentBookingId.Value);
                        if (b != null && !string.IsNullOrEmpty(b.UserId))
                            slot.CurrentHolderUserId = b.UserId;
                    }
                }

                // Khôi phục users
                _users.Clear();
                if (snapshot.Users != null)
                {
                    foreach (var kvp in snapshot.Users)
                    {
                        _users[kvp.Key] = kvp.Value;
                    }
                }

                // ✅ Khôi phục rooms
                _rooms.Clear();
                if (snapshot.Rooms != null && snapshot.Rooms.Count > 0)
                {
                    foreach (var kvp in snapshot.Rooms)
                    {
                        if (kvp.Value != null)
                        {
                            kvp.Value.RoomId = (kvp.Value.RoomId ?? "").Trim().ToUpperInvariant();
                            kvp.Value.Status = NormalizeRoomStatus(kvp.Value.Status);
                        }
                        _rooms[kvp.Key] = kvp.Value;
                    }
                    log.WriteLine($"[SNAPSHOT] Restored {_rooms.Count} rooms");
                }
                else
                {
                    log.WriteLine("[SNAPSHOT] No rooms in snapshot (rooms list empty)");
                }

                // ✅ Khôi phục fixed sessions
                _fixedSessions.Clear();
                _fixedSessionsByDate.Clear();
                _fixedSessionIdsByUser.Clear();
                
                if (snapshot.FixedSessions != null)
                {
                    _fixedSessions.AddRange(snapshot.FixedSessions);
                    
                    // Rebuild indexes
                    foreach (var session in _fixedSessions)
                    {
                        // Index by date
                        foreach (var dateKey in session.LockedDates)
                        {
                            if (!_fixedSessionsByDate.ContainsKey(dateKey))
                                _fixedSessionsByDate[dateKey] = new List<FixedSession>();
                            _fixedSessionsByDate[dateKey].Add(session);
                        }
                        
                        // Index by user
                        var allUserIds = new List<string>();
                        if (!string.IsNullOrWhiteSpace(session.LecturerUserId))
                            allUserIds.Add(session.LecturerUserId);
                        allUserIds.AddRange(session.StudentUserIds);
                        
                        foreach (var userId in allUserIds)
                        {
                            if (!_fixedSessionIdsByUser.ContainsKey(userId))
                                _fixedSessionIdsByUser[userId] = new HashSet<Guid>();
                            _fixedSessionIdsByUser[userId].Add(session.SessionId);
                        }
                    }
                    
                    log.WriteLine($"[SNAPSHOT] Restored {_fixedSessions.Count} fixed sessions");
                }

                lock (_homeNotiLock)
                {
                    _homeNotifications.Clear();
                    if (snapshot.HomeNotifications != null)
                    {
                        foreach (var kvp in snapshot.HomeNotifications)
                        {
                            _homeNotifications[kvp.Key] = new Queue<string>(kvp.Value ?? new List<string>());
                        }
                    }
                }

                lock (_notifyLock)
                {
                    _pendingNotifications.Clear();
                    if (snapshot.PendingNotifications != null)
                    {
                        foreach (var kvp in snapshot.PendingNotifications)
                        {
                            _pendingNotifications[kvp.Key] = new Queue<string>(kvp.Value ?? new List<string>());
                        }
                    }
                }
            }

            log.WriteLine($"[SNAPSHOT] Loaded snapshot from {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            log.WriteLine($"[SNAPSHOT][ERROR] Failed to load snapshot: {ex.Message}");
            log.WriteLine("[SNAPSHOT] Fallback to InitDemoData");

            // Fallback: reset demo data
            lock (_lock)
            {
                _slotsByDate.Clear();
                _bookings.Clear();
                _users.Clear();
                InitDemoData();
            }

            return false;
        }
    }
    private void GrantRange(string clientId, string roomId, string slotStartId, string slotEndId, string dateKey, NetworkStream stream, string purpose, TextWriter log)
    {
        int startIdx = ParseSlotIndex(slotStartId);
        int endIdx = ParseSlotIndex(slotEndId);

        var booking = CreateBookingForGrant(
            clientId,
            roomId,
            dateKey,
            slotStartId,
            slotEndId,
            true,
            purpose,
            log);

        // 1) Update state for all slots first (no network writes yet)
        for (int idx = startIdx; idx <= endIdx; idx++)
        {
            string sid = GetSlotId(idx);
            string key = MakeKey(roomId, sid);
            var slot = _slotsByDate[dateKey][key];

            slot.IsBusy = true;
            slot.CurrentHolderClientId = clientId;
            slot.CurrentHolderUserId = booking.UserId;
            slot.CurrentBookingId = booking.BookingId;
        }

        // 2) Send GRANTED_RANGE to requester ASAP (before SLOT_UPDATE spam)
        log.WriteLine($"[GRANT_RANGE] {clientId} -> {roomId}-{slotStartId}-{slotEndId}");
        Send(stream, $"GRANTED_RANGE|{booking.BookingId}|{roomId}|{slotStartId}|{slotEndId}\n");

        // 3) Now push SLOT_UPDATE per slot
        for (int idx = startIdx; idx <= endIdx; idx++)
        {
            string sid = GetSlotId(idx);
            PushSlotUpdate(roomId, dateKey, sid, log);
            log.WriteLine($"[GRANT_RANGE_SLOT] {clientId} -> {roomId}-{sid}");
        }

        if (_settings.SendEmailOnGrant)
        {
            SendEmailForBooking(
                booking,
                "[Room booking] RANGE booking granted",
                "Booking RANGE của bạn đã được GRANT",
                log);
        }

        if (_settings.SendNotificationToClient)
        {
            NotifyClientBookingChanged(
                booking.UserId,
                $"GRANT_RANGE|{booking.BookingId}|{roomId}|{slotStartId}|{slotEndId}",
                log);
        }
        PushHomeChanged(booking.UserId);
        PushMyBookingsChanged(booking.UserId);
        RaiseStateChanged();
    }

    private async Task SendSafe(NetworkStream stream, string msg)
    {
        try
        {
            var buf = Encoding.UTF8.GetBytes(msg);
            await stream.WriteAsync(buf, 0, buf.Length);
        }
        catch
        {
            // client có thể mất kết nối, bỏ qua
        }
    }

    /// <summary>
    /// Kiểm tra trùng các trường unique: Email, Phone, StudentId, LecturerId.
    /// ignoreUserId: dùng cho case Update (bỏ qua chính nó).
    /// </summary>
    private bool ValidateUserUniqueFields(UserInfo newUser, string? ignoreUserId, out string error)
    {
        error = "";

        foreach (var u in _users.Values)
        {
            // Nếu đang update, bỏ qua chính user đó
            if (!string.IsNullOrEmpty(ignoreUserId) &&
                string.Equals(u.UserId, ignoreUserId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Email trùng
            if (!string.IsNullOrWhiteSpace(newUser.Email) &&
                string.Equals(u.Email, newUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Email {newUser.Email} đã được sử dụng bởi UserId {u.UserId}";
                return false;
            }

            // SĐT trùng
            if (!string.IsNullOrWhiteSpace(newUser.Phone) &&
                string.Equals(u.Phone, newUser.Phone, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Số điện thoại {newUser.Phone} đã được sử dụng bởi UserId {u.UserId}";
                return false;
            }

            // MSSV trùng (chỉ quan tâm khi là Student)
            if (string.Equals(newUser.UserType, "Student", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(newUser.StudentId) &&
                string.Equals(u.StudentId, newUser.StudentId, StringComparison.OrdinalIgnoreCase))
            {
                error = $"MSSV {newUser.StudentId} đã được sử dụng bởi UserId {u.UserId}";
                return false;
            }

            // Mã GV trùng (chỉ quan tâm khi là Lecturer)
            if (string.Equals(newUser.UserType, "Lecturer", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(newUser.LecturerId) &&
                string.Equals(u.LecturerId, newUser.LecturerId, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Mã GV {newUser.LecturerId} đã được sử dụng bởi UserId {u.UserId}";
                return false;
            }
        }

        return true;
    }
    public bool UpdateUser(UserInfo updatedUser, out string error)
    {
        error = "";

        updatedUser.UserId = (updatedUser.UserId ?? "").Trim().ToUpperInvariant();
        updatedUser.StudentId = (updatedUser.StudentId ?? "").Trim().ToUpperInvariant();
        updatedUser.LecturerId = (updatedUser.LecturerId ?? "").Trim().ToUpperInvariant();
        updatedUser.Class = (updatedUser.Class ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(updatedUser.UserId))
        {
            error = "UserId is required";
            return false;
        }

        if (!_users.TryGetValue(updatedUser.UserId, out var existing))
        {
            error = $"UserId {updatedUser.UserId} not found";
            return false;
        }

        // Check trùng Email/Phone/StudentId/LecturerId nhưng bỏ qua chính nó
        if (!ValidateUserUniqueFields(updatedUser, ignoreUserId: updatedUser.UserId, out error))
        {
            return false;
        }

        // Validate theo loại user
        if (string.Equals(updatedUser.UserType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updatedUser.StudentId))
            {
                error = "StudentId (MSSV) is required for Student";
                return false;
            }

            if (!IsValidFaculty(updatedUser.Department))
            {
                error = "Khoa (Department) không hợp lệ. Hãy chọn trong danh sách CNTT2, IOT2, ...";
                return false;
            }

            updatedUser.LecturerId = "";
            updatedUser.Faculty = "";
        }
        else if (string.Equals(updatedUser.UserType, "Lecturer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(updatedUser.LecturerId))
            {
                error = "LecturerId (Mã GV) is required for Lecturer";
                return false;
            }

            if (!IsValidFaculty(updatedUser.Faculty))
            {
                error = "Faculty không hợp lệ. Hãy chọn trong danh sách CNTT2, IOT2, ...";
                return false;
            }

            updatedUser.StudentId = "";
            updatedUser.Department = "";
        }
        else
        {
            updatedUser.StudentId = "";
            updatedUser.LecturerId = "";
            // Department/Faculty có thể để trống
        }

        // Không cho update trực tiếp PasswordHash
        updatedUser.PasswordHash = existing.PasswordHash;
        // updatedUser.IsActive = existing.IsActive;

        _users[updatedUser.UserId] = updatedUser;
        return true;
    }

    public bool UpdateUserContact(string userId, string? email, string? phone, out string error)
    {
        error = "";

        lock (_lock)
        {
            if (!_users.TryGetValue(userId, out var user))
            {
                error = $"UserId {userId} not found";
                return false;
            }

            // Tạo bản tạm để dùng lại ValidateUserUniqueFields
            var temp = new UserInfo
            {
                UserId = user.UserId,
                UserType = user.UserType,
                FullName = user.FullName,
                StudentId = user.StudentId,
                Class = user.Class,
                Department = user.Department,
                LecturerId = user.LecturerId,
                Faculty = user.Faculty,
                Email = email ?? "",
                Phone = phone ?? "",
                PasswordHash = user.PasswordHash,
                IsActive = user.IsActive
            };

            // Kiểm tra trùng Email / Phone / MSSV / Mã GV (ignore chính user này)
            if (!ValidateUserUniqueFields(temp, ignoreUserId: user.UserId, out error))
            {
                return false;
            }

            // OK, ghi lại vào user thật
            user.Email = temp.Email;
            user.Phone = temp.Phone;

            return true;
        }
    }
    public bool ChangeUserPassword(string userId, string oldPassword, string newPassword, out string error)
    {
        error = "";

        lock (_lock)
        {
            if (!_users.TryGetValue(userId, out var user))
            {
                error = $"UserId {userId} not found";
                return false;
            }

            // Kiểm tra mật khẩu cũ (BCrypt)
            if (!BCryptNet.Verify(oldPassword, user.PasswordHash))
            {
                error = "Mật khẩu cũ không đúng.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return false;
            }

            // Cập nhật hash mới
            user.PasswordHash = BCryptNet.HashPassword(newPassword);
            return true;
        }
    }

    public bool SetUserActive(string userId, bool isActive, out string error)
    {
        error = "";

        if (!_users.TryGetValue(userId, out var user))
        {
            error = $"UserId {userId} not found";
            return false;
        }

        user.IsActive = isActive;
        return true;
    }
    public bool DeleteUser(string userId, out string error)
    {
        error = "";

        if (!_users.TryGetValue(userId, out var user))
        {
            error = $"UserId {userId} not found";
            return false;
        }

        // OPTIONAL: chặn xoá nếu user đã có Booking
        bool hasBooking = _bookings.Any(b => string.Equals(b.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (hasBooking)
        {
            error = $"User {userId} đã có booking, không thể xoá (có thể dùng Lock/Inactive thay vì Delete).";
            return false;
        }

        _users.Remove(userId);
        return true;
    }

    private bool IsValidFaculty(string? faculty)
    {
        if (string.IsNullOrWhiteSpace(faculty)) return false;
        return FacultySet.Contains(faculty.Trim());
    }

    private void SendEmailSafe(string to, string subject, string body, TextWriter log)
    {
        try
        {
            var smtp = _settings.Smtp;
            if (string.IsNullOrWhiteSpace(smtp.Host) ||
                string.IsNullOrWhiteSpace(smtp.Username) ||
                string.IsNullOrWhiteSpace(smtp.Password) ||
                string.IsNullOrWhiteSpace(to))
            {
                log.WriteLine("[EMAIL] Skip send: SMTP config or target email is empty.");
                return;
            }

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.EnableSsl,
                Credentials = new NetworkCredential(smtp.Username, smtp.Password)
            };

            var from = new MailAddress(smtp.Username, smtp.FromEmail ?? "Room Booking Server");
            var mail = new MailMessage(from, new MailAddress(to))
            {
                Subject = subject,
                Body = body
            };

            client.Send(mail);
            log.WriteLine($"[EMAIL] Sent to {to} | Subject={subject}");
        }
        catch (Exception ex)
        {
            log.WriteLine($"[EMAIL][ERROR] {ex.Message}");
        }
    }

    private void SendEmailForBooking(Booking booking, string subject, string bodyTemplate, TextWriter log)
    {
        if (!_users.TryGetValue(booking.UserId, out var user) || string.IsNullOrWhiteSpace(user.Email))
        {
            log.WriteLine($"[EMAIL] Skip: user {booking.UserId} not found or email empty");
            return;
        }

        var body = bodyTemplate
            .Replace("{FullName}", user.FullName)
            .Replace("{RoomId}", booking.RoomId)
            .Replace("{Date}", booking.Date)
            .Replace("{SlotStartId}", booking.SlotStartId)
            .Replace("{SlotEndId}", booking.SlotEndId)
            .Replace("{Status}", booking.Status);

        SendEmailSafe(user.Email, subject, body, log);
    }

    private void QueueEmailToUser(string userId, string subject, string body, TextWriter log)
    {
        string? to;
        lock (_lock)
        {
            if (!_users.TryGetValue(userId, out var user) || string.IsNullOrWhiteSpace(user.Email))
            {
                log.WriteLine($"[EMAIL] Skip: user {userId} not found or email empty");
                return;
            }
            to = user.Email;
        }

        _ = Task.Run(() => SendEmailSafe(to!, subject, body, log));
    }

    private void QueueEmailForBooking(Booking booking, string subject, string bodyTemplate, TextWriter log)
    {
        string? to;
        string fullName;
        string roomId;
        string date;
        string slotStartId;
        string slotEndId;
        string status;

        lock (_lock)
        {
            if (!_users.TryGetValue(booking.UserId, out var user) || string.IsNullOrWhiteSpace(user.Email))
            {
                log.WriteLine($"[EMAIL] Skip: user {booking.UserId} not found or email empty");
                return;
            }

            to = user.Email;
            fullName = user.FullName;
            roomId = booking.RoomId;
            date = booking.Date;
            slotStartId = booking.SlotStartId;
            slotEndId = booking.SlotEndId;
            status = booking.Status;
        }

        var body = bodyTemplate
            .Replace("{FullName}", fullName)
            .Replace("{RoomId}", roomId)
            .Replace("{Date}", date)
            .Replace("{SlotStartId}", slotStartId)
            .Replace("{SlotEndId}", slotEndId)
            .Replace("{Status}", status);

        _ = Task.Run(() => SendEmailSafe(to!, subject, body, log));
    }

    private void NotifyClientBookingChanged(string userId, string payload, TextWriter log)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            var line = payload.EndsWith("\n", StringComparison.Ordinal) ? payload : (payload + "\n");
            var sentPayload = SendToUser(userId, line);
            log.WriteLine($"[PUSH] BOOKING_EVENT user={userId}, sent={sentPayload}, payload={payload}");

            try
            {
                var p = payload.Split('|');
                if (p.Length >= 5)
                {
                    var kind = p[0];
                    var roomId = p[2];
                    var slotRange = p[3] == p[4] ? p[3] : $"{p[3]}-{p[4]}";

                    if (string.Equals(kind, "FORCE_GRANT", StringComparison.OrdinalIgnoreCase))
                        AddHomeNotification(userId, $"🎉 Admin đã FORCE GRANT cho bạn: {roomId} slots {slotRange}");
                    else if (string.Equals(kind, "FORCE_RELEASE", StringComparison.OrdinalIgnoreCase))
                        AddHomeNotification(userId, $"⚠️ Admin đã FORCE RELEASE booking của bạn: {roomId} slots {slotRange}");
                    else if (string.Equals(kind, "GRANTED_FROM_QUEUE", StringComparison.OrdinalIgnoreCase))
                        AddHomeNotification(userId, $"🎉 Booking của bạn đã được GRANT từ hàng chờ: {roomId} slots {slotRange}");
                    else if (string.Equals(kind, "GRANTED_RANGE", StringComparison.OrdinalIgnoreCase))
                        AddHomeNotification(userId, $"🎉 RANGE booking của bạn đã được GRANT: {roomId} slots {slotRange}");
                    else if (string.Equals(kind, "GRANTED", StringComparison.OrdinalIgnoreCase))
                        AddHomeNotification(userId, $"🎉 Booking của bạn đã được GRANT: {roomId} slots {slotRange}");
                }
            }
            catch (Exception ex)
            {
                log.WriteLine($"[WARN] AddHomeNotification failed user={userId} payload={payload}: {ex.Message}");
            }
        }

        var minimal = $"PUSH_MY_BOOKINGS_CHANGED|{userId}\n";
        var sent = SendToUser(userId, minimal);
        log.WriteLine($"[PUSH] MY_BOOKINGS_CHANGED user={userId}, sent={sent}");
    }

    public int BroadcastNow(TextWriter? log = null)
    {
        var nowStr = Now.ToString("yyyy-MM-dd HH:mm:ss");
        var msg = $"NOW|{nowStr}\n";   // ✅ phải có \n vì client ReadLineAsync()

        List<string> clientIds;

        // ✅ copy ra list để tránh “collection was modified”
        lock (_sessionLock)
        {
            clientIds = _sessions.Keys.ToList();
        }

        int sent = 0;
        foreach (var clientId in clientIds)
        {
            try
            {
                if (SendToClient(clientId, msg)) sent++;
            }
            catch { }
        }

        log?.WriteLine($"[PUSH] NOW broadcast {nowStr}, sent={sent}, online={clientIds.Count}");
        return sent;
    }


    // ở cuối class ServerState
        // ===== PROTOCOL: PUSH_MY_FIXED_SCHEDULE =====
        // Gửi toàn bộ lịch cố định (fixed schedule) của user về client
        // Dữ liệu gửi dạng: { "type": "PUSH_MY_FIXED_SCHEDULE", "data": [ { ... } ] }
        public void PushMyFixedSchedule(string userId)
        {
            var fixedList = GetUserFixedSchedule(userId);
            var arr = fixedList.Select(f => new {
                sessionId = f.SessionId,
                subjectCode = f.SubjectCode,
                subjectName = f.SubjectName,
                @class = f.Class,
                lecturerUserId = f.LecturerUserId,
                roomId = f.RoomId,
                dayOfWeek = f.DayOfWeek,
                slotStartId = f.SlotStartId,
                slotEndId = f.SlotEndId,
                dateFrom = f.DateFrom,
                dateTo = f.DateTo,
                note = f.Note
            }).ToList();
            var msg = new {
                type = "PUSH_MY_FIXED_SCHEDULE",
                data = arr
            };
            var json = JsonSerializer.Serialize(msg) + "\n";
            Console.WriteLine($"[PUSH_MY_FIXED_SCHEDULE] Sending to {userId}: {arr.Count} items");
            SendToUser(userId, json);
        }

        // Lấy danh sách FixedSession mà user là participant
        public List<FixedSession> GetUserFixedSchedule(string userId)
        {
            lock (_lock)
            {
                if (_fixedSessionIdsByUser.TryGetValue(userId, out var sessionIds))
                {
                    return _fixedSessions.Where(s => sessionIds.Contains(s.SessionId)).ToList();
                }
                return new List<FixedSession>();
            }
        }
    // public class SlotTimeConfigRow
    // {
    //     public string SlotId { get; set; } = "";
    //     public string Start { get; set; } = ""; // "HH:mm"
    //     public string End { get; set; } = "";
    // }

    public List<SlotTimeConfigRow> GetSlotTimeConfigs()
    {
        lock (_lock)
        {
            // _settings.SlotTimes đã là List<SlotTimeConfigRow> (Models)
            return _settings.SlotTimes
                .OrderBy(r => r.Index)
                .ToList();
        }
    }

    public List<BookingView> GetUserSchedule(string userId, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<BookingView>();

        var from = fromDate.Date;
        var to = toDate.Date;

        lock (_lock)
        {
            // 1. Lấy booking thường
            var allViews = GetBookingViews();
            var normalBookings = allViews
                .Where(b => {
                    var d = DateTime.Parse(b.Date);
                    return string.Equals(b.UserId, userId, StringComparison.OrdinalIgnoreCase) && d >= from && d <= to;
                })
                .ToList();

            // 2. Lấy fixed sessions mà user là participant
            var virtualBookings = new List<BookingView>();
            if (_fixedSessionIdsByUser.TryGetValue(userId, out var sessionIds))
            {
                foreach (var sid in sessionIds)
                {
                    var fs = _fixedSessions.FirstOrDefault(x => x.SessionId == sid);
                    if (fs == null) continue;
                    if (!DateTime.TryParse(fs.DateFrom, out var dateFrom) || !DateTime.TryParse(fs.DateTo, out var dateTo))
                        continue;
                    for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
                    {
                        // Nếu DayOfWeek rỗng (fixed session cho ngày cụ thể), bỏ qua filter DayOfWeek
                        if (!string.IsNullOrWhiteSpace(fs.DayOfWeek) && 
                            !string.Equals(d.DayOfWeek.ToString(), fs.DayOfWeek, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (d < from || d > to) continue;
                        var dateStr = d.ToString("yyyy-MM-dd");
                        // Nếu user đã có booking thường trùng ngày/phòng/slot thì bỏ qua fixed session này
                        bool hasNormal = normalBookings.Any(b => b.Date == dateStr && b.RoomId == fs.RoomId &&
                            SlotRangeOverlap(b.SlotStartId, b.SlotEndId, fs.SlotStartId, fs.SlotEndId));
                        if (hasNormal) continue;
                        // Tạo BookingView ảo
                        _users.TryGetValue(userId, out var u);
                        var bv = new BookingView
                        {
                            BookingId = fs.SessionId, // dùng SessionId làm BookingId ảo
                            UserId = userId,
                            FullName = u?.FullName ?? userId,
                            UserType = u?.UserType ?? "",
                            Email = u?.Email ?? "",
                            Phone = u?.Phone ?? "",
                            RoomId = fs.RoomId,
                            Date = dateStr,
                            SlotStartId = fs.SlotStartId,
                            SlotEndId = fs.SlotEndId,
                            TimeRange = GetSlotTimeRange(fs.SlotStartId, fs.SlotEndId),
                            IsRange = fs.SlotStartId != fs.SlotEndId,
                            Purpose = fs.SubjectName,
                            Status = "FIXED",
                            CreatedAt = fs.CreatedAt,
                            UpdatedAt = fs.UpdatedAt,
                            CheckinDeadline = null,
                            CheckinTime = null
                        };
                        virtualBookings.Add(bv);
                    }
                }
            }

            // 3. Merge và sort
            var result = normalBookings.Concat(virtualBookings)
                .OrderBy(r => DateTime.Parse(r.Date))
                .ThenBy(r => ParseSlotIndexSafe(r.SlotStartId))
                .ToList();
            return result;
        }
    }

    // Helper: kiểm tra 2 range slot có overlap không
    private bool SlotRangeOverlap(string s1, string e1, string s2, string e2)
    {
        int a1 = ParseSlotIndexSafe(s1), a2 = ParseSlotIndexSafe(e1);
        int b1 = ParseSlotIndexSafe(s2), b2 = ParseSlotIndexSafe(e2);
        return Math.Max(a1, b1) <= Math.Min(a2, b2);
    }

    // Helper: lấy time range từ slot id
    private string GetSlotTimeRange(string slotStartId, string slotEndId)
    {
        if (_settings == null) return "";
        var slots = _settings.SlotTimes;
        int sIdx = ParseSlotIndexSafe(slotStartId);
        int eIdx = ParseSlotIndexSafe(slotEndId);
        var s = slots.FirstOrDefault(x => ParseSlotIndexSafe(x.SlotId) == sIdx);
        var e = slots.FirstOrDefault(x => ParseSlotIndexSafe(x.SlotId) == eIdx);
        if (s == null || e == null) return "";
        return s.Start + "-" + e.End;
    }

    private int ParseSlotIndexSafe(string? slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) return 0;
        if (slotId.StartsWith("S") && int.TryParse(slotId.Substring(1), out int idx))
            return idx;
        return 0;
    }
    public void MapClientToUser(string clientId, string userId)
    {
        lock (_clientMapLock)
        {
            _clientToUser[clientId] = userId;
        }
    }

    public string? GetUserIdForClient(string clientId)
    {
        lock (_clientMapLock)
        {
            return _clientToUser.TryGetValue(clientId, out var userId) ? userId : null;
        }
    }

    public void RemoveClient(string clientId)
    {
        lock (_clientMapLock)
        {
            _clientToUser.Remove(clientId);
        }
    }
    private void RaiseStateChanged()
    {
        Console.WriteLine($"[STATE_CHANGED] RaiseStateChanged called at {DateTime.Now:HH:mm:ss.fff}");
        var handler = StateChanged;
        if (handler == null)
            return;

        _ = Task.Run(() =>
        {
            try
            {
                handler.Invoke();
                Console.WriteLine($"[STATE_CHANGED] StateChanged event invoked at {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STATE_CHANGED][ERROR] {ex.Message}");
            }
        });
    }
    public int PushMyBookingsChanged(string userId, TextWriter? log = null)
    {
        var msg = $"PUSH_MY_BOOKINGS_CHANGED|{userId}\n"; // ✅ có \n
        var sent = SendToUser(userId, msg);
        log?.WriteLine($"[PUSH] MY_BOOKINGS_CHANGED user={userId}, sent={sent}");
        return sent;
    }
    private (string status, string userId, string fullName, string bookingStatus, string purpose)
        BuildSlotUpdateView(string roomId, string dateKey, string slotId)
    {
        // status: FREE / BUSY / LOCKED
        // bookingStatus: APPROVED / IN_USE / CANCELLED / COMPLETED ... (tuỳ booking)
        // purpose: booking purpose

        var key = MakeKey(roomId, slotId);

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var dict) || !dict.TryGetValue(key, out var slot))
                return ("UNKNOWN", "", "", "", "");

            // status
            string status;
            if (slot.IsEventLocked) status = "LOCKED";
            else status = slot.IsBusy ? "BUSY" : "FREE";

            // holder user
            var holderClientId = slot.CurrentHolderClientId;
            var holderUserId = !string.IsNullOrEmpty(holderClientId)
                ? (GetUserIdForClient(holderClientId) ?? holderClientId)
                : "";

            _users.TryGetValue(holderUserId, out var u);
            var fullName = u?.FullName ?? "";

            // booking info
            string bookingStatus = "";
            string purpose = "";

            if (slot.CurrentBookingId != null)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == slot.CurrentBookingId.Value);
                if (b != null)
                {
                    bookingStatus = b.Status ?? "";
                    purpose = b.Purpose ?? "";
                }
            }

            return (status, holderUserId, fullName, bookingStatus, purpose);
        }
    }
    public void PushSlotUpdate(string roomId, string dateKey, string slotId, TextWriter log)
    {
        var k = RoomDateKey(roomId, dateKey);

        List<string> targets;
        lock (_subLock)
        {
            if (!_subRoomSlots.TryGetValue(k, out var set) || set.Count == 0)
            {
                log.WriteLine($"[PUSH] skip SLOT_UPDATE {k} (no subscribers)");
                return;
            }
            targets = set.ToList();
        }

        (string status, string userId, string fullName, string bookingStatus, string purpose) view;
        try
        {
            view = BuildSlotUpdateView(roomId, dateKey, slotId);
        }
        catch (Exception ex)
        {
            log.WriteLine($"[PUSH] SLOT_UPDATE build view failed {roomId}-{dateKey}-{slotId}: {ex.Message}");
            return;
        }

        // SLOT_UPDATE|RoomId|Date|SlotId|Status|UserId|FullName|BookingStatus|Purpose
        var msg =
            $"SLOT_UPDATE|{roomId}|{dateKey}|{slotId}|{SafeField(view.status)}|{SafeField(view.userId)}|{SafeField(view.fullName)}|{SafeField(view.bookingStatus)}|{SafeField(view.purpose)}\n";

        int sent = 0;
        foreach (var clientId in targets)
            if (SendToClient(clientId, msg)) sent++;

        log.WriteLine($"[PUSH] SLOT_UPDATE {roomId}-{dateKey}-{slotId} subs={targets.Count} sent={sent}");
    }
    private static string SafeField(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
    }
    public void SubRooms(string clientId)
    {
        lock (_subLock) _subRooms.Add(clientId);
    }

    public void SubSlotConfig(string clientId)
    {
        lock (_subLock) _subSlotConfig.Add(clientId);
    }

    private void PushRoomsChanged()
    {
        List<string> targets;
        lock (_subLock) targets = _subRooms.ToList();
        foreach (var cid in targets) SendToClient(cid, "PUSH_ROOMS_CHANGED\n");
    }

    private void PushSlotConfigChanged()
    {
        List<string> targets;
        lock (_subLock) targets = _subSlotConfig.ToList();
        foreach (var cid in targets) SendToClient(cid, "PUSH_SLOT_CONFIG_CHANGED\n");
    }
    // Trả về tất cả FixedSession của userId trong khoảng ngày (dạng yyyy-MM-dd)
    public List<FixedSession> GetFixedSessionsForUser(string userId, string fromDate, string toDate)
    {
        var result = new List<FixedSession>();
        if (!_fixedSessionIdsByUser.TryGetValue(userId, out var sessionIds) || sessionIds.Count == 0)
            return result;
        if (!DateTime.TryParse(fromDate, out var from) || !DateTime.TryParse(toDate, out var to))
            return result;
        foreach (var sid in sessionIds)
        {
            var session = _fixedSessions.FirstOrDefault(s => s.SessionId == sid);
            if (session == null) continue;
            // Kiểm tra overlap date range
            if (DateTime.TryParse(session.DateFrom, out var sFrom) && DateTime.TryParse(session.DateTo, out var sTo))
            {
                if (sTo < from || sFrom > to) continue;
                result.Add(session);
            }
        }
        return result;
    }
        // Xử lý lệnh text: GET_FIXED_SESSIONS|userId|fromDate|toDate\n
    public void HandleGetFixedSessions(string clientId, string userId, string fromDate, string toDate, NetworkStream stream, TextWriter log)
    {
        var sessions = GetFixedSessionsForUser(userId, fromDate, toDate);
        if (sessions.Count == 0)
        {
            Send(stream, "FIXED_SESSIONS|NONE\n");
            return;
        }
        foreach (var s in sessions)
        {
            // Protocol: FIXED_SESSION|SessionId|SubjectCode|SubjectName|Class|LecturerUserId|RoomId|DayOfWeek|SlotStartId|SlotEndId|DateFrom|DateTo|Note
            var msg = $"FIXED_SESSION|{s.SessionId}|{SafeField(s.SubjectCode)}|{SafeField(s.SubjectName)}|{SafeField(s.Class)}|{SafeField(s.LecturerUserId)}|{SafeField(s.RoomId)}|{SafeField(s.DayOfWeek)}|{SafeField(s.SlotStartId)}|{SafeField(s.SlotEndId)}|{SafeField(s.DateFrom)}|{SafeField(s.DateTo)}|{SafeField(s.Note)}\n";
            Send(stream, msg);
        }
        Send(stream, "FIXED_SESSIONS|END\n");
    }
public static List<DateTime> GenerateDatesInRangeByDayOfWeek(DateTime from, DateTime to, DayOfWeek dayOfWeek)
    {
        var result = new List<DateTime>();
        var current = from.Date;
        while (current <= to.Date)
        {
            if (current.DayOfWeek == dayOfWeek)
                result.Add(current);
            current = current.AddDays(1);
        }
        return result;
    }

    // Hàm chính: Apply fixed schedule cho lớp/môn
    public (bool Success, string Message, int CreatedCount, int ConflictCount) ApplyFixedSchedule(
        string subjectCode, string subjectName, string className, string roomId,
        DateTime from, DateTime to, DayOfWeek dow,
        string slotStartId, string slotEndId,
        List<string> studentUserIds, string lecturerUserId, string note)
    {
        // Validate input
        if (from > to) return (false, "From date must be <= To date", 0, 0);
        // Parse slot indices for numeric comparison
        int slotStartNum, slotEndNum;
        try {
            slotStartNum = int.Parse(slotStartId.Substring(1));
            slotEndNum = int.Parse(slotEndId.Substring(1));
        } catch {
            return (false, "Invalid slot id format", 0, 0);
        }
        if (slotStartNum > slotEndNum) return (false, "Slot start must be <= slot end", 0, 0);
        if (!_rooms.ContainsKey(roomId)) return (false, "Invalid roomId", 0, 0);
        if (studentUserIds == null || studentUserIds.Count == 0) return (false, "Student list cannot be empty", 0, 0);
        if (string.IsNullOrWhiteSpace(lecturerUserId)) return (false, "LecturerId cannot be empty", 0, 0);

        var dates = GenerateDatesInRangeByDayOfWeek(from, to, dow);
        int created = 0, conflict = 0;
        var allUserIds = new HashSet<string>(studentUserIds) { lecturerUserId };
        lock (_lock)
        {
            foreach (var date in dates)
            {
                string dateKey = date.ToString("yyyy-MM-dd");
                // Check duplicate: đã có session cùng date+room+slot+subject?
                if (_fixedSessionsByDate.TryGetValue(dateKey, out var sessions))
                {
                    if (sessions.Any(s => s.RoomId == roomId && s.SlotStartId == slotStartId && s.SlotEndId == slotEndId && s.SubjectCode == subjectCode))
                    {
                        conflict++;
                        continue;
                    }
                }
                // Check conflict: slot đã event lock hoặc có booking APPROVED/IN_USE?
                bool hasConflict = false;
                int start = int.Parse(slotStartId.Substring(1));
                int end = int.Parse(slotEndId.Substring(1));
                if (!_slotsByDate.ContainsKey(dateKey))
                    EnsureDateInitialized(dateKey, TextWriter.Null);
                for (int i = start; i <= end; i++)
                {
                    string slotId = $"S{i}";
                    var slot = _slotsByDate[dateKey][$"{roomId}::{slotId}"];
                    if (slot.IsEventLocked)
                    {
                        hasConflict = true;
                        break;
                    }
                    if (slot.CurrentBookingId != null)
                    {
                        var b = _bookings.FirstOrDefault(x => x.BookingId == slot.CurrentBookingId);
                        if (b != null && (b.Status == "APPROVED" || b.Status == "IN_USE"))
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                }
                if (hasConflict)
                {
                    conflict++;
                    continue;
                }
                // Tạo session
                var sessionId = CreateFixedSession(
                    dateKey, roomId, slotStartId, slotEndId,
                    subjectCode, subjectName, className, lecturerUserId, studentUserIds, note);
                AddParticipants(sessionId, studentUserIds, lecturerUserId);
                LockSlotsForEvent(dateKey, roomId, slotStartId, slotEndId, $"{subjectCode}-{subjectName}");
                // Index by date
                if (!_fixedSessionsByDate.TryGetValue(dateKey, out var list))
                {
                    list = new List<FixedSession>();
                    _fixedSessionsByDate[dateKey] = list;
                }
                var session = _fixedSessions.First(x => x.SessionId == sessionId);
                list.Add(session);
                // Index by user
                foreach (var uid in allUserIds)
                {
                    if (!_fixedSessionIdsByUser.TryGetValue(uid, out var set))
                    {
                        set = new HashSet<Guid>();
                        _fixedSessionIdsByUser[uid] = set;
                    }
                    set.Add(sessionId);
                }
                created++;
            }
        }
        // Push realtime cho tất cả participants
        foreach (var userId in allUserIds)
        {
            PushHomeChanged(userId);
            PushMyScheduleChanged(userId);
        }
        if (created == 0)
            return (false, $"No sessions created. {conflict} conflicts.", 0, conflict);
        return (true, $"Created {created} sessions, {conflict} conflicts.", created, conflict);
    }

    // Tạo FixedSession cho 1 ngày
    private Guid CreateFixedSession(
        string dateKey, string roomId, string slotStartId, string slotEndId,
        string subjectCode, string subjectName, string className, string lecturerUserId, List<string> studentUserIds, string note)
    {
        var session = new FixedSession
        {
            SessionId = Guid.NewGuid(),
            DateFrom = dateKey,
            DateTo = dateKey,
            DayOfWeek = "", // not needed for single session
            RoomId = roomId,
            SlotStartId = slotStartId,
            SlotEndId = slotEndId,
            SubjectCode = subjectCode,
            SubjectName = subjectName,
            Class = className,
            LecturerUserId = lecturerUserId,
            StudentUserIds = new List<string>(studentUserIds),
            Note = note,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _fixedSessions.Add(session);
        return session.SessionId;
    }

    // Thêm participants cho session
    private void AddParticipants(Guid sessionId, List<string> studentUserIds, string lecturerUserId)
    {
        var session = _fixedSessions.First(x => x.SessionId == sessionId);
        foreach (var userId in studentUserIds)
        {
            session.Participants.Add(new FixedParticipant
            {
                SessionId = sessionId,
                UserId = userId,
                Role = "Student"
            });
        }
        session.Participants.Add(new FixedParticipant
        {
            SessionId = sessionId,
            UserId = lecturerUserId,
            Role = "Lecturer"
        });
    }

    // Lock slot cho event
    private void LockSlotsForEvent(string dateKey, string roomId, string slotStartId, string slotEndId, string note)
    {
        int start = int.Parse(slotStartId.Substring(1));
        int end = int.Parse(slotEndId.Substring(1));
        for (int i = start; i <= end; i++)
        {
            string slotId = $"S{i}";
            var slot = _slotsByDate[dateKey][$"{roomId}::{slotId}"];
            slot.IsEventLocked = true;
            slot.EventNote = note;
            // Push slot update để client reload _gridRoomSlots
            PushSlotUpdate(roomId, dateKey, slotId, TextWriter.Null);
        }
    }

        // Push schedule changed for a user (for fixed sessions)
    public int PushMyScheduleChanged(string userId, TextWriter? log = null)
    {
        var msg = $"PUSH_MY_SCHEDULE_CHANGED|{userId}\n";
        List<string> clientIds;
        lock (_subLock)
        {
            if (!_subMyBookings.TryGetValue(userId, out var set) || set.Count == 0)
                return 0;
            clientIds = set.ToList();
        }
        int sent = 0;
        foreach (var clientId in clientIds)
        {
            try
            {
                if (SendToClient(clientId, msg)) sent++;
            }
            catch { }
        }
        log?.WriteLine($"[PUSH] MY_SCHEDULE_CHANGED user={userId}, sent={sent}, sub={clientIds.Count}");
        return sent;
    }
        // Helper: Lấy trạng thái slot (dùng cho UI Form1)
    public SlotState? GetSlotState(string dateKey, string roomId, string slotId)
    {
        lock (_lock)
        {
            if (_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                var key = MakeKey(roomId, slotId);
                if (slotsForDate.TryGetValue(key, out var slotState))
                    return slotState;
            }
        }
        return null;
    }

    // Helper: Lấy FixedSession cho slot event lock (dùng cho UI Form1)
    public FixedSession? GetFixedSessionForSlot(DateTime date, string roomId, string slotId)
    {
        var dateKey = date.ToString("yyyy-MM-dd");
        lock (_lock)
        {
            foreach (var session in _fixedSessions)
            {
                if (session.RoomId == roomId &&
                    int.TryParse(session.SlotStartId.Replace("S", ""), out var sIdx) &&
                    int.TryParse(session.SlotEndId.Replace("S", ""), out var eIdx) &&
                    int.TryParse(slotId.Replace("S", ""), out var idx) &&
                    idx >= sIdx && idx <= eIdx &&
                    DateTime.TryParse(session.DateFrom, out var from) &&
                    DateTime.TryParse(session.DateTo, out var to) &&
                    date >= from && date <= to &&
                    session.LockedDates.Contains(dateKey))
                {
                    return session;
                }
            }
        }
        return null;
    }

    // Lấy tất cả Fixed Schedules (cho UI admin)
    public List<FixedSession> GetAllFixedSessions()
    {
        lock (_lock)
        {
            return _fixedSessions.ToList();
        }
    }

    // Reset ALL data: bookings, fixed schedules, locks
    public void ResetAllData(TextWriter log)
    {
        lock (_lock)
        {
            log.WriteLine("[RESET_ALL] Starting reset all data...");

            // 1. Clear all bookings
            var bookingCount = _bookings.Count;
            _bookings.Clear();
            log.WriteLine($"[RESET_ALL] Cleared {bookingCount} bookings");

            // 2. Clear all fixed sessions
            var fixedCount = _fixedSessions.Count;
            _fixedSessions.Clear();
            _fixedSessionsByDate.Clear();
            _fixedSessionIdsByUser.Clear();
            log.WriteLine($"[RESET_ALL] Cleared {fixedCount} fixed schedules");

            // 3. Unlock all slots and clear event locks
            var slotCount = 0;
            foreach (var dateKey in _slotsByDate.Keys.ToList())
            {
                var slotsForDate = _slotsByDate[dateKey];
                foreach (var slot in slotsForDate.Values)
                {
                    if (slot.CurrentBookingId != null || slot.IsEventLocked)
                    {
                        slot.IsBusy = false;
                        slot.CurrentHolderClientId = null;
                        slot.CurrentHolderUserId = null;
                        slot.CurrentBookingId = null;
                        slot.IsEventLocked = false;
                        slot.EventNote = null;
                        slot.WaitingQueue.Clear();
                        slotCount++;
                    }
                }
            }
            log.WriteLine($"[RESET_ALL] Unlocked {slotCount} slots");

            // 4. Broadcast to all clients
            log.WriteLine("[RESET_ALL] Broadcasting changes to all clients...");
            foreach (var userId in UsersInfo.Keys)
            {
                PushMyBookingsChanged(userId);
                PushFixedSessionsChanged(userId);
                PushHomeChanged(userId);
            }

            RaiseStateChanged();
            log.WriteLine("[RESET_ALL] Reset completed successfully!");
        }
    }

    // Xoá Fixed Schedule và unlock tất cả slots
    public bool DeleteFixedSchedule(Guid sessionId, TextWriter log, out string error)
    {
        error = "";
        lock (_lock)
        {
            var session = _fixedSessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                error = "Fixed schedule không tồn tại.";
                return false;
            }

            // 1. Unlock tất cả slots đã lock
            if (DateTime.TryParse(session.DateFrom, out var dateFrom) && 
                DateTime.TryParse(session.DateTo, out var dateTo))
            {
                for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
                {
                    // Kiểm tra DayOfWeek nếu có
                    if (!string.IsNullOrWhiteSpace(session.DayOfWeek) && 
                        !string.Equals(d.DayOfWeek.ToString(), session.DayOfWeek, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var dateKey = d.ToString("yyyy-MM-dd");
                    
                    // Unlock slots
                    if (_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
                    {
                        int startIdx = ParseSlotIndex(session.SlotStartId);
                        int endIdx = ParseSlotIndex(session.SlotEndId);
                        
                        for (int idx = startIdx; idx <= endIdx; idx++)
                        {
                            var slotId = GetSlotId(idx);
                            var key = MakeKey(session.RoomId, slotId);
                            
                            if (slotsForDate.TryGetValue(key, out var slot))
                            {
                                slot.IsEventLocked = false;
                                slot.EventNote = "";
                                log.WriteLine($"[FIXED_SCHEDULE][DELETE] Unlocked {session.RoomId}-{slotId} on {dateKey}");
                            }
                        }
                    }
                }
            }

            // 2. Xoá khỏi index _fixedSessionsByDate
            foreach (var dateKey in session.LockedDates)
            {
                if (_fixedSessionsByDate.TryGetValue(dateKey, out var list))
                {
                    list.RemoveAll(s => s.SessionId == sessionId);
                }
            }

            // 3. Xoá khỏi index _fixedSessionIdsByUser
            var allUserIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(session.LecturerUserId))
                allUserIds.Add(session.LecturerUserId);
            allUserIds.AddRange(session.StudentUserIds);

            foreach (var userId in allUserIds)
            {
                if (_fixedSessionIdsByUser.TryGetValue(userId, out var sessionIds))
                {
                    sessionIds.Remove(sessionId);
                }
            }

            // 4. Xoá khỏi list chính
            _fixedSessions.RemoveAll(s => s.SessionId == sessionId);

            log.WriteLine($"[FIXED_SCHEDULE][DELETE] Deleted session {sessionId} ({session.SubjectCode} - {session.SubjectName})");
            
            // 5. Push notification cho các user bị ảnh hưởng
            foreach (var userId in allUserIds)
            {
                PushFixedSessionsChanged(userId);
                PushHomeChanged(userId);

                NotifyUser(userId,
                    $"NOTIFY_FIXED_DELETED|{SafeField(session.SubjectCode)}|{SafeField(session.SubjectName)}|{SafeField(session.RoomId)}|{SafeField(session.SlotStartId)}|{SafeField(session.SlotEndId)}|{SafeField(session.DateFrom)}|{SafeField(session.DateTo)}");
            }

            if (_settings.SendEmailOnForceGrantRelease)
            {
                var emailSubject = "[Room booking] Lịch cố định đã bị xoá";
                foreach (var uid in allUserIds.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var body =
                        "Một lịch cố định mà bạn tham gia đã bị xoá.\n\n" +
                        $"- Môn: {session.SubjectCode} - {session.SubjectName}\n" +
                        $"- Lớp: {session.Class}\n" +
                        $"- Phòng: {session.RoomId}\n" +
                        $"- Ca: {session.SlotStartId} - {session.SlotEndId}\n" +
                        $"- Từ ngày: {session.DateFrom}\n" +
                        $"- Đến ngày: {session.DateTo}\n";
                    QueueEmailToUser(uid, emailSubject, body, log);
                }
            }

            RaiseStateChanged();
            return true;
        }
    }

}

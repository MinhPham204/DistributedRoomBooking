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
using System.Net;
using System.Net.Mail;

namespace BookingServer;

// Trạng thái 1 slot (phòng + ca)
class SlotState
{
    public bool IsBusy { get; set; }
    public string? CurrentHolderClientId { get; set; }   // ai đang giữ slot này

    // NEW: Booking tương ứng với slot này (nếu đã tạo record Booking)
    public Guid? CurrentBookingId { get; set; }
    public bool IsEventLocked { get; set; } = false;
    // NEW: ghi chú ngắn (ví dụ: "Event Khoa CNTT", "Hội thảo ABC")
    public string? EventNote { get; set; }

    public Queue<(string clientId, NetworkStream stream)> WaitingQueue { get; } = new();

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

class ServerState
{
    // ===== CẤU HÌNH PHÒNG + CA =====
    private static readonly string[] Rooms =
    {
        "A08","A16","A24","A32",
        "A21","A22","A23",
        "A24-A25","A31","A32-A33","A34-A35"
    };

    private const int SlotCount = 14; // ca 1..14

    // SlotId sẽ là "S1".."S14"
    private static string GetSlotId(int index) => $"S{index}";

    // ===== STATE THEO NGÀY =====
    // dateKey = "yyyy-MM-dd" -> (room::slot) -> SlotState
    private readonly Dictionary<string, Dictionary<string, SlotState>> _slotsByDate = new();
    private readonly object _lock = new();

    private string _currentDateKey = DateTime.Today.ToString("yyyy-MM-dd");
    // ===== DATA MÔ HÌNH THỰC TẾ (ROOMS / USERS / BOOKINGS) =====

    // Thông tin phòng (RoomInfo) key theo RoomId (A08, A16,...)
    private readonly Dictionary<string, RoomInfo> _rooms = new();

    // Thông tin người dùng (UserInfo) key theo UserId (sv001, gv001, admin,...)
    private readonly Dictionary<string, UserInfo> _users = new();

    // Danh sách booking "thực tế" – sẽ dùng ở các milestone sau
    private readonly List<Booking> _bookings = new();

    // Expose read-only cho UI/logic khác (dùng ở milestone sau)
    public IReadOnlyDictionary<string, RoomInfo> RoomsInfo => _rooms;
    public IReadOnlyDictionary<string, UserInfo> UsersInfo => _users;
    public IReadOnlyList<Booking> Bookings => _bookings;
    // ===== App settings (Tab Settings) =====
    private AppSettings _settings = AppSettings.CreateDefault();
    private const string SettingsFileName = "appsettings.json";

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
        InitDemoData();
        LoadSettings(); // <- load appsettings.json nếu có

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

    public void UpdateSettings(AppSettings newSettings)
    {
        if (newSettings == null) return;

        lock (_lock)
        {
            _settings = newSettings;
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
        // Seed phòng từ mảng Rooms có sẵn
        foreach (var roomId in Rooms)
        {
            if (_rooms.ContainsKey(roomId)) continue;

            _rooms[roomId] = new RoomInfo
            {
                RoomId = roomId,
                Building = "CS1 - Tòa A",           // demo, sau này có thể tách theo cơ sở
                Capacity = 60,                      // demo
                HasProjector = true,                // giả sử phòng nào cũng có máy chiếu
                HasPC = roomId.StartsWith("A2", StringComparison.OrdinalIgnoreCase),
                HasAirConditioner = true,
                HasMic = roomId.StartsWith("A3", StringComparison.OrdinalIgnoreCase),
                Status = "ACTIVE"
            };
        }

        // Seed một vài user demo (Student / Lecturer / Staff)
        if (_users.Count == 0)
        {
            _users["sv001"] = new UserInfo
            {
                UserId = "sv001",
                UserType = "Student",
                FullName = "Nguyễn Văn A",
                StudentId = "N21DCCN001",
                Class = "D21CQCN01-N",
                Department = "CNTT",
                Email = "sv001@example.com",
                Phone = "0900000001",
                PasswordHash = BC.HashPassword("sv123"),
                IsActive = true
            };

            _users["sv002"] = new UserInfo
            {
                UserId = "sv002",
                UserType = "Student",
                FullName = "Trần Thị B",
                StudentId = "N21DCCN002",
                Class = "D21CQCN02-N",
                Department = "CNTT",
                Email = "sv002@example.com",
                Phone = "0900000002",
                PasswordHash = BC.HashPassword("sv123"),
                IsActive = true
            };

            _users["gv001"] = new UserInfo
            {
                UserId = "gv001",
                UserType = "Lecturer",
                FullName = "Thầy C",
                LecturerId = "GV001",
                Faculty = "Khoa CNTT",
                Email = "gv001@example.com",
                Phone = "0900000003",
                PasswordHash = BC.HashPassword("gv123"),
                IsActive = true
            };

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
        foreach (var room in Rooms)
        {
            for (int i = 1; i <= SlotCount; i++)
            {
                var slotId = GetSlotId(i);
                var key = MakeKey(room, slotId);
                dict[key] = new SlotState
                {
                    IsBusy = false,
                    CurrentHolderClientId = null
                };
            }
        }

        _slotsByDate[dateKey] = dict;
        log.WriteLine($"[INIT] Created slot state for date {dateKey}");
    }

    private Dictionary<string, SlotState> GetCurrentDateSlots(TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(_currentDateKey, log);
            return _slotsByDate[_currentDateKey];
        }
    }

    private string MakeKey(string roomId, string slotId) => $"{roomId}::{slotId}";

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

                result.Add(new SlotSummary
                {
                    Date = _currentDateKey,
                    RoomId = roomId,
                    SlotId = slotId,
                    Status = status,
                    Holder = slot.CurrentHolderClientId ?? "",
                    QueueLength = slot.WaitingQueue.Count,
                    IsEventLocked = slot.IsEventLocked
                });
            }
        }

        return result;
    }

    public (bool Success, string? UserType, string Error) ValidateUserCredentials(string userId, string password)
    {
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

    public bool IsAdmin(string userId)
    {
        if (!_users.TryGetValue(userId, out var user)) return false;
        return user.UserType == "Staff" || user.UserType == "Admin";
    }

    // Lấy queue cụ thể cho 1 (room, slot) của ngày hiện tại -> hiển thị chi tiết hàng đợi
    public List<string> GetQueueClients(string roomId, string slotId)
    {
        var key = MakeKey(roomId, slotId);
        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(_currentDateKey, out var dict))
                return new List<string>();

            if (!dict.TryGetValue(key, out var slot))
                return new List<string>();

            return slot.WaitingQueue.Select(q => q.clientId).ToList();
        }
    }
    public bool LockSlotForEvent(
        DateTime date,
        string roomId,
        string slotId,
        string? note,
        TextWriter log,
        out string error)
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

            log.WriteLine($"[EVENT_LOCK] {dateKey} {roomId}-{slotId} note={note}");
            return true;
        }
    }

    public bool UnlockSlotFromEvent(
        DateTime date,
        string roomId,
        string slotId,
        TextWriter log,
        out string error)
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

            log.WriteLine($"[EVENT_UNLOCK] {dateKey} {roomId}-{slotId}");
            return true;
        }
    }
    public bool CreateFixedWeeklyClassSchedule(
        string subjectCode,
        string subjectName,
        string className,
        string roomId,
        DayOfWeek dayOfWeek,
        string slotStartId,
        string slotEndId,
        DateTime fromDate,
        DateTime toDate,
        TextWriter log,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(roomId))
        {
            error = "RoomId không được để trống.";
            return false;
        }

        if (fromDate.Date > toDate.Date)
        {
            error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
            return false;
        }

        int startIdx = ParseSlotIndex(slotStartId);
        int endIdx = ParseSlotIndex(slotEndId);

        if (startIdx <= 0 || endIdx <= 0 || startIdx > endIdx || endIdx > SlotCount)
        {
            error = "Khoảng ca học không hợp lệ (slot start / end).";
            return false;
        }

        // Ghi chú hiển thị trên Event lock
        string note = $"{subjectCode} - {subjectName} ({className})".Trim();
        if (string.IsNullOrWhiteSpace(note))
            note = "Lịch môn học cố định";

        var current = fromDate.Date;

        while (current <= toDate.Date)
        {
            if (current.DayOfWeek == dayOfWeek)
            {
                var dateKey = current.ToString("yyyy-MM-dd");
                log.WriteLine($"[FIXED_SCHEDULE] Apply {note} {roomId} {slotStartId}-{slotEndId} on {dateKey}");

                for (int idx = startIdx; idx <= endIdx; idx++)
                {
                    var slotId = $"S{idx}"; // dùng đúng format SlotId

                    // Dùng lại logic lock event hiện có
                    if (!LockSlotForEvent(current, roomId, slotId, note, log, out var err))
                    {
                        error = $"Không thể khóa {roomId}-{slotId} vào ngày {dateKey}: {err}";
                        return false;
                    }
                }
            }

            current = current.AddDays(1);
        }

        return true;
    }

    public bool CreateUser(UserInfo newUser, string passwordPlain, out string error)
    {
        error = "";

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
            return true;
        }
    }

    public bool DeleteRoom(string roomId, out string error)
    {
        error = "";

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

    // Kiểm tra cùng client có đang giữ slot trùng ca ở phòng khác hay không
    private bool HasCrossRoomConflict(string clientId, string dateKey, string roomIdNew, string slotIdNew,
        out string conflictedRoom)
    {
        conflictedRoom = "";
        if (!_slotsByDate.TryGetValue(dateKey, out var dict)) return false;

        int idxNew = ParseSlotIndex(slotIdNew);
        if (idxNew <= 0) return false;

        foreach (var kvp in dict)
        {
            var key = kvp.Key;
            var slot = kvp.Value;

            if (slot.CurrentHolderClientId != clientId) continue;

            var parts = key.Split(new[] { "::" }, StringSplitOptions.None);
            var roomId = parts[0];
            var slotId = parts.Length > 1 ? parts[1] : "?";

            if (roomId == roomIdNew) continue; // cùng phòng thì cho phép (đang xin ca khác trong cùng phòng)

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
    public void HandleRequest(string clientId, string roomId, string slotId, NetworkStream stream, TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(_currentDateKey, log);

            var dict = _slotsByDate[_currentDateKey];
            var key = MakeKey(roomId, slotId);

            if (!dict.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] REQUEST invalid slot {roomId}-{slotId} by {clientId}");
                Send(stream, "INFO|ERROR|Invalid room/slot\n");
                return;
            }
            bool isAdmin = IsAdmin(clientId);

            // Nếu slot đang khóa cho event -> chỉ admin mới được can thiệp (FORCE_GRANT làm riêng)
            if (slot.IsEventLocked && !isAdmin)
            {
                log.WriteLine($"[WARN] REQUEST blocked by EVENT_LOCK {clientId} on {roomId}-{slotId} {_currentDateKey}");
                Send(stream, "INFO|ERROR|SLOT_LOCKED_FOR_EVENT\n");
                return;
            }

            // 1) Chặn ca đã qua (mode RealTime đơn giản)
            var now = Now;
            var endTime = GetSlotEndTime(_currentDateKey, slotId);
            if (endTime <= now)
            {
                log.WriteLine($"[WARN] REQUEST past slot {roomId}-{slotId} on date {_currentDateKey} by {clientId}");
                Send(stream, "INFO|ERROR|Slot already in the past\n");
                return;
            }

            // 2) Chặn giữ 2 phòng khác nhau cùng ca trong cùng ngày
            if (HasCrossRoomConflict(clientId, _currentDateKey, roomId, slotId, out var conflictedRoom))
            {
                log.WriteLine($"[WARN] REQUEST cross-room conflict: {clientId} already holds {conflictedRoom}-{slotId} on {_currentDateKey}");
                Send(stream, "INFO|ERROR|User already booked another room in that time range\n");
                return;
            }

            // 3) Nếu client đã là holder -> không cấp lại, chỉ báo INFO
            if (slot.CurrentHolderClientId == clientId)
            {
                log.WriteLine($"[INFO] REQUEST from holder {clientId} on {roomId}-{slotId} -> already granted");
                Send(stream, $"INFO|ALREADY_HOLDER|{roomId}|{slotId}\n");
                return;
            }

            // 4) Nếu client đã trong queue -> không enqueue thêm, chỉ báo INFO + pos
            int pos = 1;
            bool alreadyQueued = false;
            foreach (var w in slot.WaitingQueue)
            {
                if (w.clientId == clientId)
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

            // 5) Slot đang rảnh -> cấp quyền ngay
            if (!slot.IsBusy && string.IsNullOrEmpty(slot.CurrentHolderClientId))
            {
                slot.IsBusy = true;
                slot.CurrentHolderClientId = clientId;

                // 👉 Tạo booking mới cho lần GRANT này
                var booking = CreateBookingForGrant(
                    clientId,
                    roomId,
                    _currentDateKey,
                    slotId,   // start == end với single
                    slotId,
                    false,    // IsRangeBooking
                    log);
                slot.CurrentBookingId = booking.BookingId;

                log.WriteLine($"[GRANT] {clientId} -> {roomId}-{slotId} on date {_currentDateKey}");
                Send(stream, $"GRANT|{roomId}|{slotId}\n");
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
            }
            else
            {
                // Slot đang bận -> cho vào queue (FIFO)
                slot.WaitingQueue.Enqueue((clientId, stream));
                var newPos = slot.WaitingQueue.Count;
                log.WriteLine($"[QUEUE] {clientId} -> {roomId}-{slotId} on date {_currentDateKey} (pos {newPos})");
                Send(stream, $"QUEUED|{roomId}|{slotId}|{newPos}\n");
            }
        }
    }

    /// <summary>
    /// RELEASE được gọi khi client bấm Hủy:
    /// - Nếu đang giữ quyền -> giải phóng / cấp cho queue tiếp theo.
    /// - Nếu đang trong queue -> xóa khỏi queue.
    /// - Nếu không liên quan -> báo lỗi.
    /// </summary>
    public void HandleRelease(string clientId, string roomId, string slotId, NetworkStream? replyStream, TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(_currentDateKey, log);
            var dict = _slotsByDate[_currentDateKey];
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

            bool isAdmin = IsAdmin(clientId);

            // ===== CASE 1: holder hoặc admin được phép RELEASE slot =====
            if (slot.CurrentHolderClientId == clientId || isAdmin)
            {
                var oldHolder = slot.CurrentHolderClientId;
                var tag = isAdmin ? "[ADMIN RELEASE]" : "[RELEASE]";
                log.WriteLine($"{tag} {clientId} -> {roomId}-{slotId} on {_currentDateKey} (holder = {oldHolder})");

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
                }

                if (replyStream != null)
                    Send(replyStream, $"INFO|RELEASED|{roomId}|{slotId}\n");

                // Phần cấp queue / giải phóng slot vẫn y như cũ
                if (slot.WaitingQueue.Count == 0)
                {
                    slot.IsBusy = false;
                    slot.CurrentHolderClientId = null;
                    slot.CurrentBookingId = null;
                    log.WriteLine($"[SLOT] {roomId}-{slotId} on {_currentDateKey} -> FREE");
                }
                else
                {
                    var (nextClientId, nextStream) = slot.WaitingQueue.Dequeue();
                    slot.IsBusy = true;
                    slot.CurrentHolderClientId = nextClientId;

                    // tạo booking mới APPROVED cho người tiếp theo
                    var booking = CreateBookingForGrant(
                        nextClientId,
                        roomId,
                        _currentDateKey,
                        slotId,
                        slotId,
                        false,
                        log);
                    slot.CurrentBookingId = booking.BookingId;

                    log.WriteLine($"[GRANT] {nextClientId} -> {roomId}-{slotId} from queue on date {_currentDateKey}");
                    Send(nextStream, $"GRANT|{roomId}|{slotId}\n");

                    if (_settings.SendEmailOnGrant)
                    {
                        var subject = "[Room booking] Booking của bạn đã được GRANT từ hàng chờ";
                        var bodyTemplate =
                            "Chào {FullName},\n\n" +
                            "Yêu cầu đặt phòng của bạn đã được GRANT (từ hàng chờ), do người giữ slot đã release.\n" +
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

                }

                return;
            }
            // ===== CASE 2: không phải holder, nhưng đang trong queue -> hủy yêu cầu =====
            int removed = RemoveFromQueue(slot, clientId);
            if (removed > 0)
            {
                log.WriteLine($"[CANCEL] {clientId} removed from queue of {roomId}-{slotId} on {_currentDateKey} (entries {removed})");
                if (replyStream != null)
                {
                    Send(replyStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                }
            }
            else
            {
                // ===== CASE 3: không phải holder, không nằm trong queue, không phải admin =====
                log.WriteLine($"[WARN] RELEASE from non-holder/non-queued {clientId} on {roomId}-{slotId} on {_currentDateKey}");
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

                    // Xóa khỏi queue nếu có
                    int removedFromQueue = RemoveFromQueue(slot, clientId);
                    if (removedFromQueue > 0)
                    {
                        log.WriteLine($"[DISCONNECT] Removed {clientId} from queue of {roomId}-{slotId} on {dateKey} (removed {removedFromQueue})");
                    }

                    // Nếu đang là holder -> auto release
                    if (slot.CurrentHolderClientId == clientId)
                    {
                        log.WriteLine($"[DISCONNECT] Auto release {clientId} from {roomId}-{slotId} on {dateKey}");

                        // 👉 cập nhật booking hiện tại (coi như CANCELLED vì disconnect)
                        UpdateCurrentBookingStatus(slot, roomId, slotId, "CANCELLED", log);

                        if (slot.WaitingQueue.Count == 0)
                        {
                            slot.IsBusy = false;
                            slot.CurrentHolderClientId = null;
                            slot.CurrentBookingId = null;
                            log.WriteLine($"[SLOT] {roomId}-{slotId} on {dateKey} -> FREE (disconnect)");
                        }
                        else
                        {
                            var (nextClientId, nextStream) = slot.WaitingQueue.Dequeue();
                            slot.IsBusy = true;
                            slot.CurrentHolderClientId = nextClientId;

                            var newBooking = CreateBookingForGrant(
                                nextClientId,   // ✅ user mới được GRANT
                                roomId,
                                dateKey,        // ✅ đúng ngày của booking
                                slotId,
                                slotId,
                                false,
                                log);

                            slot.CurrentBookingId = newBooking.BookingId;

                            // log.WriteLine($"[GRANT] {nextClientId} (from queue, after disconnect) -> {roomId}-{slotId} on {dateKey}");
                            // Send(nextStream, $"GRANT|{roomId}|{slotId}\n");


                            log.WriteLine($"[GRANT] {nextClientId} (from queue, after disconnect) -> {roomId}-{slotId} on {dateKey}");
                            Send(nextStream, $"GRANT|{roomId}|{slotId}\n");

                            if (_settings.SendEmailOnGrant)
                            {
                                var subject = "[Room booking] Booking của bạn đã được GRANT (sau khi người trước disconnect)";
                                var bodyTemplate =
                                    "Chào {FullName},\n\n" +
                                    "Yêu cầu đặt phòng của bạn đã được GRANT sau khi người giữ slot trước đó bị ngắt kết nối.\n" +
                                    "- Phòng: {RoomId}\n" +
                                    "- Ngày: {Date}\n" +
                                    "- Ca: {SlotStartId} - {SlotEndId}\n" +
                                    "- Trạng thái: {Status}\n\n" +
                                    "Vui lòng check-in trước hạn.";
                                SendEmailForBooking(newBooking, subject, bodyTemplate, log);
                            }

                            if (_settings.SendNotificationToClient)
                            {
                                NotifyClientBookingChanged(
                                    newBooking.UserId,
                                    $"GRANTED_FROM_QUEUE|{newBooking.BookingId}|{newBooking.RoomId}|{newBooking.SlotStartId}|{newBooking.SlotEndId}",
                                    log
                                );
                            }

                        }
                    }

                }
            }
        }
    }

    /// <summary>
    /// Xóa tất cả entry của clientId khỏi queue. Trả về số entry đã xóa.
    /// </summary>
    private int RemoveFromQueue(SlotState slot, string clientId)
    {
        if (slot.WaitingQueue.Count == 0) return 0;

        int removed = 0;
        var newQueue = new Queue<(string clientId, NetworkStream stream)>();

        while (slot.WaitingQueue.Count > 0)
        {
            var item = slot.WaitingQueue.Dequeue();
            if (item.clientId == clientId)
            {
                removed++;
                // stream sẽ bị đóng ở nơi khác (disconnect), ở đây chỉ bỏ khỏi queue
            }
            else
            {
                newQueue.Enqueue(item);
            }
        }

        // Gán lại queue mới
        while (newQueue.Count > 0)
        {
            slot.WaitingQueue.Enqueue(newQueue.Dequeue());
        }

        return removed;
    }

    private void Send(NetworkStream stream, string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);
    }

    // Tạo booking mới khi slot được GRANT cho user
    // THÊM tham số dateKey để tránh lệ thuộc _currentDateKey
    private Booking CreateBookingForGrant(string userId,
    string roomId,
    string dateKey,
    string slotStartId,
    string slotEndId,
    bool isRange,
    TextWriter log)
    {
        var now = Now;
        var endTime = GetSlotEndTime(dateKey, slotEndId);

        var deadlineMinutes = _settings.CheckinDeadlineMinutes <= 0
            ? 15
            : _settings.CheckinDeadlineMinutes;

        var rawDeadline = now.AddMinutes(deadlineMinutes);

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            Date = dateKey,   // yyyy-MM-dd
            SlotId = slotStartId,
            SlotStartId = slotStartId,
            SlotEndId = slotEndId,
            IsRangeBooking = isRange,
            Purpose = "",
            CreatedAt = now,
            UpdatedAt = now,
            Status = "APPROVED",
            CheckinDeadline = (now.AddMinutes(
                    _settings.CheckinDeadlineMinutes <= 0 ? 15 : _settings.CheckinDeadlineMinutes
                ) <= endTime)
                ? now.AddMinutes(
                    _settings.CheckinDeadlineMinutes <= 0 ? 15 : _settings.CheckinDeadlineMinutes
                )
                : endTime
        };

        _bookings.Add(booking);
        log.WriteLine($"[BOOKING] Create {booking.BookingId} {userId} {roomId} {slotStartId}-{slotEndId} APPROVED, deadline={booking.CheckinDeadline:HH:mm}");

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


    public void HandleForceGrant(
                    string adminId,
                    string targetUserId,
                    string roomId,
                    string slotId,
                    NetworkStream adminStream,
                    TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(_currentDateKey, log);

            // 1) Kiểm tra target user
            if (!_users.TryGetValue(targetUserId, out var targetUser) || !targetUser.IsActive)
            {
                Send(adminStream, "INFO|ERROR|TARGET_USER_INVALID\n");
                return;
            }

            var dict = _slotsByDate[_currentDateKey];
            var key = MakeKey(roomId, slotId);

            if (!dict.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] FORCE_GRANT invalid slot {roomId}-{slotId} by {adminId}");
                Send(adminStream, "INFO|ERROR|Invalid room/slot\n");
                return;
            }

            // 2) OPTIONAL: vẫn giữ rule không double-booking cross-room cho target
            if (HasCrossRoomConflict(targetUserId, _currentDateKey, roomId, slotId, out var conflictedRoom))
            {
                log.WriteLine($"[WARN] FORCE_GRANT conflict: {targetUserId} already holds {conflictedRoom}-{slotId} on {_currentDateKey}");
                Send(adminStream, "INFO|ERROR|TARGET_ALREADY_BOOKED_IN_THAT_SLOT\n");
                return;
            }

            // 3) Bỏ qua check "ca đã qua" -> admin có quyền
            // (KHÔNG gọi GetSlotEndTime ở đây)

            // 4) Nếu đang có holder -> CANCELLED booking hiện tại
            if (slot.CurrentHolderClientId != null)
            {
                log.WriteLine($"[ADMIN FORCE_GRANT] {adminId} overrides holder {slot.CurrentHolderClientId} on {roomId}-{slotId}");

                // Override -> coi booking cũ là CANCELLED
                UpdateCurrentBookingStatus(slot, roomId, slotId, "CANCELLED", log);
            }

            // 5) Clear queue và báo cho từng client trong queue
            if (slot.WaitingQueue.Count > 0)
            {
                log.WriteLine($"[ADMIN FORCE_GRANT] {adminId} clears queue of {roomId}-{slotId} (count={slot.WaitingQueue.Count})");

                while (slot.WaitingQueue.Count > 0)
                {
                    var (queuedClientId, queuedStream) = slot.WaitingQueue.Dequeue();
                    // Báo là yêu cầu của họ bị hủy do admin can thiệp
                    Send(queuedStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                }
            }

            // 6) Cấp quyền cho targetUserId
            slot.IsBusy = true;
            slot.CurrentHolderClientId = targetUserId;


            var booking = CreateBookingForGrant(
                            targetUserId,
                            roomId,
                            _currentDateKey,
                            slotId,   // start == end với single
                            slotId,
                            false,    // IsRangeBooking
                            log);
            slot.CurrentBookingId = booking.BookingId;

            log.WriteLine($"[ADMIN FORCE_GRANT] {adminId} granted {roomId}-{slotId} to {targetUserId} on {_currentDateKey}");

            // Thông báo cho admin (client hiện tại)
            Send(adminStream, $"INFO|FORCE_GRANTED|{targetUserId}|{roomId}|{slotId}\n");

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
                SendEmailForBooking(booking, subject, bodyTemplate, log);
            }

            if (_settings.SendNotificationToClient)
            {
                NotifyClientBookingChanged(
                    booking.UserId,
                    $"FORCE_GRANT|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                    log
                );
            }

        }
    }

    // Admin force grant từ UI Server (không đi qua TCP)
    public bool ForceGrantFromServerUi(
        DateTime date,
        string roomId,
        string slotId,
        string targetUserId,
        TextWriter log,
        out string error)
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

            // 3. Nếu đang có holder → cancel booking cũ
            if (slot.CurrentHolderClientId != null)
            {
                log.WriteLine($"[ADMIN FORCE_GRANT-UI] override holder {slot.CurrentHolderClientId} on {roomId}-{slotId} ({dateKey})");
                UpdateCurrentBookingStatus(slot, roomId, slotId, "CANCELLED", log);
            }

            // 4. Clear queue & báo cho từng client trong queue là bị hủy do admin
            if (slot.WaitingQueue.Count > 0)
            {
                log.WriteLine($"[ADMIN FORCE_GRANT-UI] clear queue {roomId}-{slotId}, count={slot.WaitingQueue.Count}");
                while (slot.WaitingQueue.Count > 0)
                {
                    var (queuedClientId, queuedStream) = slot.WaitingQueue.Dequeue();
                    // Thông báo: yêu cầu của bạn đã bị admin hủy
                    Send(queuedStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                }
            }

            // 5. Gán holder mới + tạo booking mới
            slot.IsBusy = true;
            slot.CurrentHolderClientId = targetUserId;

            var booking = CreateBookingForGrant(
                targetUserId,
                roomId,
                dateKey,
                slotId,   // start == end (single slot)
                slotId,
                false,    // IsRangeBooking
                log);

            slot.CurrentBookingId = booking.BookingId;

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
                SendEmailForBooking(booking, subject, bodyTemplate, log);
            }

            if (_settings.SendNotificationToClient)
            {
                NotifyClientBookingChanged(
                    booking.UserId,
                    $"FORCE_GRANT|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                    log
                );
            }

            return true;
        }
    }
    // Admin force GRANT RANGE từ UI Server (không đi qua TCP)
    public bool ForceGrantRangeFromServerUi(
        DateTime date,
        string roomId,
        string slotStartId,
        string slotEndId,
        string targetUserId,
        TextWriter log,
        out string error)
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

                if (slot.IsEventLocked)
                {
                    error = $"Slot {roomId}-{sid} đang bị lock cho event.";
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

            // 4. Hạ booking cũ & clear queue từng slot trong range
            foreach (var (sid, slot) in slots)
            {
                if (slot.CurrentHolderClientId != null)
                {
                    log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI] override holder {slot.CurrentHolderClientId} on {roomId}-{sid} ({dateKey})");
                    UpdateCurrentBookingStatus(slot, roomId, sid, "CANCELLED", log);
                }

                if (slot.WaitingQueue.Count > 0)
                {
                    log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI] clear queue {roomId}-{sid}, count={slot.WaitingQueue.Count}");
                    while (slot.WaitingQueue.Count > 0)
                    {
                        var (queuedClientId, queuedStream) = slot.WaitingQueue.Dequeue();
                        // Thông báo: yêu cầu của bạn đã bị admin hủy
                        Send(queuedStream, $"INFO|CANCELLED|{roomId}|{sid}\n");
                    }
                }

                // reset trước khi gán booking mới
                slot.IsBusy = false;
                slot.CurrentHolderClientId = null;
                slot.CurrentBookingId = null;
            }

            // 5. Tạo 1 booking RANGE mới
            var booking = CreateBookingForGrant(
                targetUserId,
                roomId,
                dateKey,
                slotStartId,
                slotEndId,
                true,       // IsRangeBooking = true
                log);

            // 6. Gán booking này cho toàn bộ slot trong range
            foreach (var (sid, slot) in slots)
            {
                slot.IsBusy = true;
                slot.CurrentHolderClientId = targetUserId;
                slot.CurrentBookingId = booking.BookingId;

                log.WriteLine($"[ADMIN FORCE_GRANT_RANGE-UI_SLOT] {targetUserId} -> {roomId}-{sid} on {dateKey}");
            }

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

                SendEmailForBooking(booking, subject, bodyTemplate, log);
            }
            if (_settings.SendNotificationToClient)
            {
                NotifyClientBookingChanged(booking.UserId,
                    $"FORCE_GRANT|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}", log);
            }
            return true;
        }
    }
    // Admin Force RELEASE RANGE từ UI Server
    public bool ForceReleaseRangeFromServerUi(
        DateTime date,
        string roomId,
        string slotStartId,
        string slotEndId,
        TextWriter log,
        out string error)
    {
        // Thực ra chỉ cần 1 slot bất kỳ trong range,
        // vì CompleteAndReleaseSlot sẽ đọc booking.IsRangeBooking
        // rồi tự giải phóng toàn bộ range.
        return CompleteAndReleaseSlot(date, roomId, slotStartId, log, out error);
    }
    // Admin check-in tại UI server, không đi qua TCP client
    public void AdminCheckIn(string dateKey, string roomId, string slotId, TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(dateKey, log);
            var dict = _slotsByDate[dateKey];
            var key = MakeKey(roomId, slotId);

            if (!dict.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] CHECKIN invalid slot {roomId}-{slotId} on {dateKey}");
                return;
            }

            if (slot.CurrentBookingId == null)
            {
                log.WriteLine($"[WARN] CHECKIN no current booking at {roomId}-{slotId} on {dateKey}");
                return;
            }

            var booking = _bookings.FirstOrDefault(b => b.BookingId == slot.CurrentBookingId.Value);
            if (booking == null)
            {
                log.WriteLine($"[WARN] CHECKIN booking not found for {roomId}-{slotId}");
                return;
            }

            var now = Now;

            // Chỉ cho check-in nếu đang APPROVED và còn trong deadline
            if (booking.Status != "APPROVED")
            {
                log.WriteLine($"[WARN] CHECKIN invalid status {booking.Status} for {booking.BookingId}");
                return;
            }

            if (now > booking.CheckinDeadline)
            {
                log.WriteLine($"[WARN] CHECKIN late for {booking.BookingId}, now={now:HH:mm}, deadline={booking.CheckinDeadline:HH:mm}");
                return;
            }

            booking.Status = "IN_USE";
            booking.CheckinTime = now;
            booking.UpdatedAt = now;

            log.WriteLine($"[CHECKIN] Admin check-in booking {booking.BookingId} {booking.UserId} {roomId}-{slotId} at {now:HH:mm}");
        }
    }
    public void RunNoShowSweep(DateTime now, TextWriter log)
    {
        lock (_lock)
        {
            foreach (var booking in _bookings.Where(b => b.Status == "APPROVED"))
            {
                if (now > booking.CheckinDeadline)
                {
                    booking.Status = "NO_SHOW";
                    booking.UpdatedAt = now;
                    log.WriteLine($"[NO_SHOW] Booking {booking.BookingId} {booking.UserId} {booking.RoomId} {booking.SlotStartId}-{booking.SlotEndId}");

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

                        // chỉ release nếu slot đang giữ đúng booking này
                        if (slot.CurrentBookingId == booking.BookingId)
                        {
                            // giống logic RELEASE nhưng đơn giản:
                            slot.IsBusy = false;
                            slot.CurrentHolderClientId = null;
                            slot.CurrentBookingId = null;

                            log.WriteLine($"[SLOT] AUTO FREE by NO_SHOW {booking.RoomId}-{sid} on {booking.Date}");
                            // nếu muốn cấp cho queue tiếp theo ở đây thì bạn có thể reuse logic từ HandleRelease
                            // (cho M4, có thể ghi vào báo cáo, code tùy sức)
                        }
                    }
                }
            }
        }
    }
    public void HandleRequestRange(
        string clientId,
        string roomId,
        string slotStartId,
        string slotEndId,
        NetworkStream stream,
        TextWriter log)
    {
        lock (_lock)
        {
            EnsureDateInitialized(_currentDateKey, log);

            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
            {
                Send(stream, "INFO|ERROR|Invalid slot range\n");
                return;
            }

            var dict = _slotsByDate[_currentDateKey];

            // 1. Chặn ca đã qua (nếu ca cuối đã qua thì từ chối)
            var now = Now;
            var rangeEndTime = GetSlotEndTime(_currentDateKey, slotEndId);
            if (rangeEndTime <= now)
            {
                log.WriteLine($"[WARN] REQUEST_RANGE past range {roomId}-{slotStartId}-{slotEndId} by {clientId}");
                Send(stream, "INFO|ERROR|Slot range already in the past\n");
                return;
            }

            // 2. Ràng buộc: 1 user không giữ 2 phòng khác nhau cùng ca
            // → check từng ca trong range so với các slot đang giữ
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                var sid = GetSlotId(idx);
                if (HasCrossRoomConflict(clientId, _currentDateKey, roomId, sid, out var conflictedRoom))
                {
                    log.WriteLine($"[WARN] REQUEST_RANGE conflict same time at other room {conflictedRoom} for {clientId}");
                    Send(stream, "INFO|ERROR|USER_SLOT_CONFLICT\n");
                    return;
                }
            }

            // 3. Kiểm tra toàn bộ slot trong range thuộc cùng RoomId
            var slots = new List<(string slotId, SlotState state)>();
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                var sid = GetSlotId(idx);
                var key = MakeKey(roomId, sid);

                if (!dict.TryGetValue(key, out var slotState))
                {
                    log.WriteLine($"[WARN] REQUEST_RANGE invalid slot {roomId}-{sid} by {clientId}");
                    Send(stream, "INFO|ERROR|Invalid room/slot in range\n");
                    return;
                }

                slots.Add((sid, slotState));
            }
            bool isAdmin = IsAdmin(clientId);

            // 3b. Nếu bất kỳ slot nào trong range bị lock cho event -> chặn user thường
            if (!isAdmin)
            {
                foreach (var (sid, s) in slots)
                {
                    if (s.IsEventLocked)
                    {
                        log.WriteLine($"[WARN] REQUEST_RANGE blocked by EVENT_LOCK at {roomId}-{sid} for {clientId}");
                        Send(stream, "INFO|ERROR|SLOT_LOCKED_FOR_EVENT\n");
                        return;
                    }
                }
            }
            // 4. Nếu bất kỳ slot nào đang BUSY bởi user khác -> RANGE_CONFLICT (atomic)
            foreach (var (sid, s) in slots)
            {
                if (s.IsBusy && s.CurrentHolderClientId != null && s.CurrentHolderClientId != clientId)
                {
                    log.WriteLine($"[INFO] REQUEST_RANGE conflict at {roomId}-{sid}, holder={s.CurrentHolderClientId}");
                    Send(stream, "INFO|ERROR|RANGE_CONFLICT\n");
                    return;
                }
            }

            // 5. OK → tạo 1 booking range, set busy cho toàn bộ
            var booking = CreateBookingForGrant(
                clientId,
                roomId,
                _currentDateKey,
                slotStartId,
                slotEndId,
                true,
                log);

            foreach (var (sid, s) in slots)
            {
                s.IsBusy = true;
                s.CurrentHolderClientId = clientId;
                s.CurrentBookingId = booking.BookingId;
                log.WriteLine($"[GRANT_RANGE_SLOT] {clientId} -> {roomId}-{sid} on date {_currentDateKey}");
            }

            log.WriteLine($"[GRANT_RANGE] {clientId} -> {roomId}-{slotStartId}-{slotEndId} on date {_currentDateKey}");
            Send(stream, $"GRANT_RANGE|{roomId}|{slotStartId}|{slotEndId}\n");
            if (_settings.SendEmailOnGrant)
            {
                var subject = "[Room booking] Booking RANGE của bạn đã được GRANT";
                var bodyTemplate =
                    "Chào {FullName},\n\n" +
                    "Yêu cầu đặt phòng (nhiều ca liên tiếp) của bạn đã được GRANT.\n" +
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
                    $"GRANT_RANGE|{booking.BookingId}|{booking.RoomId}|{booking.SlotStartId}|{booking.SlotEndId}",
                    log
                );
            }


        }
    }
    public void HandleReleaseRange(
        string clientId,
        string roomId,
        string slotStartId,
        string slotEndId,
        NetworkStream replyStream,
        TextWriter log)
    {
        var dateKey = _currentDateKey;

        lock (_lock)
        {
            if (!_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                Send(replyStream, "INFO|ERROR|No slots for current date\n");
                return;
            }

            int startIdx = ParseSlotIndex(slotStartId);
            int endIdx = ParseSlotIndex(slotEndId);
            if (startIdx <= 0 || endIdx <= 0 || endIdx < startIdx)
            {
                Send(replyStream, "INFO|ERROR|Invalid slot range\n");
                return;
            }

            // Tìm booking range tương ứng (cùng user, room, date, range)
            var booking = _bookings.FirstOrDefault(b =>
                   b.UserId == clientId
                && b.RoomId == roomId
                && b.Date == dateKey
                && b.IsRangeBooking
                && b.SlotStartId == slotStartId
                && b.SlotEndId == slotEndId
                && (b.Status == "APPROVED" || b.Status == "IN_USE"));

            if (booking == null)
            {
                Send(replyStream, "INFO|ERROR|NO_RANGE_BOOKING\n");
                return;
            }

            // Xác định trạng thái mới: nếu đang IN_USE -> COMPLETED, nếu APPROVED -> CANCELLED
            string newStatus = (booking.Status == "IN_USE") ? "COMPLETED" : "CANCELLED";
            booking.Status = newStatus;
            booking.UpdatedAt = Now;

            log.WriteLine($"[RANGE_RELEASE] {clientId} {roomId} {slotStartId}-{slotEndId} -> {newStatus}");

            // Giải phóng TẤT CẢ slot thuộc range này
            int sIdx = ParseSlotIndex(booking.SlotStartId);
            int eIdx = ParseSlotIndex(booking.SlotEndId);
            for (int idx = sIdx; idx <= eIdx; idx++)
            {
                var sid = GetSlotId(idx);
                var key = MakeKey(roomId, sid);

                if (!slotsForDate.TryGetValue(key, out var slot))
                    continue;

                // chỉ free nếu slot đang gắn đúng booking này
                if (slot.CurrentBookingId == booking.BookingId)
                {
                    slot.IsBusy = false;
                    slot.CurrentHolderClientId = null;
                    slot.CurrentBookingId = null;

                    log.WriteLine($"[SLOT] RANGE_RELEASE free {roomId}-{sid} on {dateKey}");

                    // Option: cấp cho queue tiếp theo từng slot
                    GrantNextFromQueue(dateKey, roomId, sid, slot, log);
                }
            }

            // Báo lại cho client
            Send(replyStream,
                $"INFO|RANGE_RELEASED|{roomId}|{slotStartId}|{slotEndId}\n");
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

            return new BookingView
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                FullName = user?.FullName ?? "",
                UserType = user?.UserType ?? "",
                RoomId = booking.RoomId,
                Date = booking.Date,
                SlotStartId = booking.SlotStartId,
                SlotEndId = booking.SlotEndId,
                Status = booking.Status,
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
                .Where(b => b.UserId == userId && b.Date == todayKey)
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

            log.WriteLine($"[CHECKIN] Manual check-in booking {booking.BookingId} {booking.UserId} {roomId}-{slotId} on {dateKey} at {now:HH:mm}");

            return true;
        }
    }
    private void GrantNextFromQueue(string dateKey, string roomId, string slotId, SlotState slot, TextWriter log)
    {
        if (slot.WaitingQueue.Count == 0)
        {
            log.WriteLine($"[SLOT] {roomId}-{slotId} on {dateKey} -> FREE");
            return;
        }

        var (nextClientId, nextStream) = slot.WaitingQueue.Dequeue();
        slot.IsBusy = true;
        slot.CurrentHolderClientId = nextClientId;

        var booking = CreateBookingForGrant(
            nextClientId,
            roomId,
            dateKey,
            slotId,
            slotId,
            false,
            log);

        slot.CurrentBookingId = booking.BookingId;

        log.WriteLine($"[GRANT] {nextClientId} -> {roomId}-{slotId} from queue on date {dateKey}");
        Send(nextStream, $"GRANT|{roomId}|{slotId}\n");

        // Sau khi gửi GRANT cho client trong queue
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
                        slotRange.CurrentBookingId = null;

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
                slot.CurrentBookingId = null;

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
    public bool ForceReleaseFromServerUi(
        DateTime date,
        string roomId,
        string slotId,
        TextWriter log,
        out string error)
    {
        error = "";

        var dateKey = date.ToString("yyyy-MM-dd");
        Guid? bookingIdToNotify = null;

        // 1) Lấy ra BookingId đang giữ slot này (nếu có)
        lock (_lock)
        {
            if (_slotsByDate.TryGetValue(dateKey, out var slotsForDate))
            {
                var key = MakeKey(roomId, slotId);

                if (slotsForDate.TryGetValue(key, out var slot) &&
                    slot.CurrentBookingId.HasValue)
                {
                    bookingIdToNotify = slot.CurrentBookingId.Value;
                }
            }
        }

        // 2) Gọi lại logic cũ: COMPLETE & RELEASE
        if (!CompleteAndReleaseSlot(date, roomId, slotId, log, out error))
        {
            return false;
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

                    SendEmailForBooking(booking, subject, bodyTemplate, log);
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
            }
        }

        return true;
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

                if (slotState != null)
                {
                    if (slotState.IsBusy)
                    {
                        status = "BUSY";
                        if (!string.IsNullOrEmpty(slotState.CurrentHolderClientId))
                        {
                            userId = slotState.CurrentHolderClientId;
                            if (_users.TryGetValue(userId, out var user))
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
                    BookingStatus = bookingStatus
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

        lock (_lock)
        {
            // Lọc booking theo CreatedAt và khoảng ngày
            var filtered = _bookings.Where(b =>
            {
                var d = DateTime.Parse(b.Date); // b.Date = "yyyy-MM-dd"
                return d >= from && d <= to;
            });

            var groups = filtered.GroupBy(b => b.RoomId);

            var result = new List<RoomStats>();
            foreach (var g in groups)
            {
                var roomId = g.Key;
                var total = g.Count();
                var noShow = g.Count(x => x.Status == "NO_SHOW");
                var cancelled = g.Count(x => x.Status == "CANCELLED");

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
    /// Thống kê theo loại user (Student / Lecturer / Staff)
    public List<UserTypeStats> GetUserTypeStatistics(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        lock (_lock)
        {
            var filtered = _bookings.Where(b =>
            {
                var d = DateTime.Parse(b.Date);
                return d >= from && d <= to;
            });

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
                var noShow = g.Count(x => x.Booking.Status == "NO_SHOW");

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

                // Khôi phục users
                _users.Clear();
                if (snapshot.Users != null)
                {
                    foreach (var kvp in snapshot.Users)
                    {
                        _users[kvp.Key] = kvp.Value;
                    }
                }

                // Lưu ý: _rooms vẫn giữ nguyên từ InitDemoData (danh sách phòng demo)
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

    private void NotifyClientBookingChanged(string userId, string message, TextWriter log)
    {
        // TODO: nếu bạn có map UserId -> clientId/NetworkStream thì lookup và Send(...)
        log.WriteLine($"[NOTIFY] {userId} <- {message}");
    }
    // ở cuối class ServerState
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

    public List<(string SlotId, string Start, string End)> GetSlotConfigForClient()
    {
        lock (_lock)
        {
            var result = new List<(string SlotId, string Start, string End)>();

            if (_settings.SlotTimes == null || _settings.SlotTimes.Count == 0)
            {
                // fallback: nếu chưa cấu hình thì tạo 1h/ca từ 07:00
                for (int i = 1; i <= SlotCount; i++)
                {
                    var s = TimeSpan.FromHours(7 + (i - 1));
                    var e = s.Add(TimeSpan.FromHours(1));
                    result.Add(($"S{i}", s.ToString(@"hh\:mm"), e.ToString(@"hh\:mm")));
                }
            }
            else
            {
                foreach (var row in _settings.SlotTimes.OrderBy(r => r.Index))
                {
                    // SlotId có thể null -> fallback "S{Index}"
                    var slotId = string.IsNullOrWhiteSpace(row.SlotId)
                        ? $"S{row.Index}"
                        : row.SlotId;

                    result.Add((slotId, row.Start, row.End));
                }
            }

            return result;
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
            var allViews = GetBookingViews(); // đã include TimeRange chuẩn

            var query = allViews
                .Where(b =>
                {
                    var d = DateTime.Parse(b.Date); // b.Date = "yyyy-MM-dd"
                    return b.UserId == userId && d >= from && d <= to;
                })
                .OrderBy(b => DateTime.Parse(b.Date))
                .ThenBy(b => b.SlotStartId);

            return query.ToList();
        }
    }

}

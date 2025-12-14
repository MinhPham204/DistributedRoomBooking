// BookingServer/Models.cs
using System;
using System.Collections.Generic;

namespace BookingServer
{
    public class RoomInfo
    {
        public string RoomId { get; set; } = "";      // A08
        public string Building { get; set; } = "";    // CS1 - Tòa A
        public int Capacity { get; set; }             // 60
        public bool HasProjector { get; set; }
        public bool HasPC { get; set; }
        public bool HasAirConditioner { get; set; }
        public bool HasMic { get; set; }
        public string Status { get; set; } = "ACTIVE"; // ACTIVE / UNDER_MAINTENANCE / DISABLED
    }

    /// Thông tin người dùng: sinh viên / giảng viên / staff.
    /// Hiện tại vẫn dùng UserId thay cho ClientId, nhưng sau này
    /// có thể map 1-1 (client C1 = sv001, C2 = gv001, ...).
    public class UserInfo
    {
        public string UserId { get; set; } = "";      // dùng thay cho ClientId thuần
        public string UserType { get; set; } = "";    // Student / Lecturer / Staff
        public string FullName { get; set; } = "";

        // Sinh viên
        public string StudentId { get; set; } = "";
        public string Class { get; set; } = "";
        public string Department { get; set; } = "";

        // Giảng viên
        public string LecturerId { get; set; } = "";
        public string Faculty { get; set; } = "";

        // Thông tin liên hệ chung
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
    public class Booking
    {
        public Guid BookingId { get; set; }
        public string UserId { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string Date { get; set; } = "";        // yyyy-MM-dd
        public string SlotId { get; set; } = "";     // SINGLE-slot legacy

        public string SlotStartId { get; set; } = ""; // S1
        public string SlotEndId { get; set; } = "";   // S1 (range sau)
        public bool IsRangeBooking { get; set; }       // true nếu là range 
        public string Purpose { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // REQUESTED / APPROVED / REJECTED / CANCELLED / COMPLETED / NO_SHOW
        public string Status { get; set; } = "APPROVED";

        // === M4: admin check-in + deadline ===
        public DateTime? CheckinTime { get; set; }
        public DateTime CheckinDeadline { get; set; }
    }

    public class BookingView
    {
        public Guid BookingId { get; set; }
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string UserType { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string RoomId { get; set; } = "";
        public string Date { get; set; } = "";
        public string SlotStartId { get; set; } = "";
        public string SlotEndId { get; set; } = "";

        public string TimeRange { get; set; } = ""; // 07:00-09:00
        public bool IsRange { get; set; }           // true nếu là đặt range

        public string Purpose { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? CheckinDeadline { get; set; }
        public DateTime? CheckinTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// View cho tra cứu lịch 1 phòng trong 1 ngày
    public class RoomDailySlotView
    {
        public string Date { get; set; } = "";         // yyyy-MM-dd
        public string RoomId { get; set; } = "";
        public string SlotId { get; set; } = "";       // S1..S14
        public string TimeRange { get; set; } = "";    // "07:00-08:00" ...

        public string Status { get; set; } = "";       // FREE / BUSY
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string BookingStatus { get; set; } = ""; // APPROVED / IN_USE / COMPLETED / ...
        public string Purpose { get; set; } = "";       // NEW: lý do mượn phòng
    }

    /// Thống kê theo phòng
    public class RoomStats
    {
        public string RoomId { get; set; } = "";
        public int TotalBookings { get; set; }
        public int NoShowCount { get; set; }
        public int CancelledCount { get; set; }
    }

    /// Thống kê theo loại user (Student / Lecturer / Staff)
    public class UserTypeStats
    {
        public string UserType { get; set; } = "";
        public int TotalBookings { get; set; }
        public int NoShowCount { get; set; }
    }
    /// Cấu hình 1 ca học (slot)
    public class SlotTimeConfigRow
    {
        public int Index { get; set; }           // 1..14
        public string SlotId { get; set; } = ""; // S1..S14
        public string Start { get; set; } = "";  // "07:00"
        public string End { get; set; } = "";    // "08:00"
    }

    /// Cấu hình SMTP/email
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
    }

    /// Cấu hình chung cho server (Tab Settings)
    public class AppSettings
    {
        public List<SlotTimeConfigRow> SlotTimes { get; set; } = new();

        // Thời gian check-in deadline (phút)
        public int CheckinDeadlineMinutes { get; set; } = 15;

        // Bật/tắt gửi email
        public bool SendEmailOnGrant { get; set; }
        public bool SendEmailOnForceGrantRelease { get; set; }
        public bool SendEmailOnNoShow { get; set; }

        // Bật/tắt gửi notification cho client
        public bool SendNotificationToClient { get; set; }

        public SmtpSettings Smtp { get; set; } = new();

        public static AppSettings CreateDefault()
        {
            var settings = new AppSettings();
            var startBase = new TimeSpan(7, 0, 0); // bắt đầu từ 07:00

            for (int i = 1; i <= 14; i++)
            {
                var s = startBase.Add(TimeSpan.FromHours(i - 1));
                var e = s.Add(TimeSpan.FromHours(1));

                settings.SlotTimes.Add(new SlotTimeConfigRow
                {
                    Index = i,
                    SlotId = $"S{i}",
                    Start = s.ToString(@"hh\:mm"),
                    End = e.ToString(@"hh\:mm")
                });
            }

            return settings;
        }
    }

    /// Snapshot dùng cho backup/restore
    public class SlotSnapshot
    {
        public bool IsBusy { get; set; }
        public string? CurrentHolderClientId { get; set; }
        public Guid? CurrentBookingId { get; set; }
        // Optional: nếu muốn lưu luôn lock event
        public bool IsEventLocked { get; set; }
        public string? EventNote { get; set; }
    }

    public class Snapshot
    {
        public Dictionary<string, Dictionary<string, SlotSnapshot>> SlotsByDate { get; set; }
            = new();

        public List<Booking> Bookings { get; set; } = new();

        public Dictionary<string, UserInfo> Users { get; set; } = new();
    }

}

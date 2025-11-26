// BookingServer/ServerState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace BookingServer;

class SlotState
{
    public bool IsBusy { get; set; }
    public string? CurrentHolder { get; set; }
    public Queue<(string clientId, NetworkStream stream)> WaitingQueue { get; } = new();
}

class ServerState
{
    private readonly Dictionary<string, SlotState> _slots = new();
    private readonly object _lock = new();

    public void InitSlots()
    {
        var rooms = new[] { "A101", "A102", "B201" };
        var slots = new[] { "S1", "S2", "S3" }; // S1 = ca 1, S2 = ca 2,...

        lock (_lock)
        {
            foreach (var r in rooms)
            foreach (var s in slots)
            {
                _slots[$"{r}::{s}"] = new SlotState
                {
                    IsBusy = false,
                    CurrentHolder = null
                };
            }
        }
    }

    private string MakeKey(string roomId, string slotId) => $"{roomId}::{slotId}";

    public void HandleRequest(string clientId, string roomId, string slotId, NetworkStream stream, TextWriter log)
    {
        var key = MakeKey(roomId, slotId);

        lock (_lock)
        {
            if (!_slots.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] REQUEST invalid slot {roomId}-{slotId} by {clientId}");
                Send(stream, "INFO|ERROR|Invalid room/slot\n");
                return;
            }

            // Nếu client đã là holder -> không cấp lại, chỉ báo INFO
            if (slot.CurrentHolder == clientId)
            {
                log.WriteLine($"[INFO] REQUEST from holder {clientId} on {roomId}-{slotId} -> already granted");
                Send(stream, $"INFO|ALREADY_HOLDER|{roomId}|{slotId}\n");
                return;
            }

            // Nếu client đã trong queue -> không enqueue thêm, chỉ báo INFO + pos
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

            // Slot đang rảnh -> cấp quyền ngay
            if (!slot.IsBusy && string.IsNullOrEmpty(slot.CurrentHolder))
            {
                slot.IsBusy = true;
                slot.CurrentHolder = clientId;

                log.WriteLine($"[GRANT] {clientId} -> {roomId}-{slotId}");
                Send(stream, $"GRANT|{roomId}|{slotId}\n");
            }
            else
            {
                // Slot đang bận -> cho vào queue (FIFO)
                slot.WaitingQueue.Enqueue((clientId, stream));
                var newPos = slot.WaitingQueue.Count;
                log.WriteLine($"[QUEUE] {clientId} -> {roomId}-{slotId} (pos {newPos})");
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
        var key = MakeKey(roomId, slotId);

        lock (_lock)
        {
            if (!_slots.TryGetValue(key, out var slot))
            {
                log.WriteLine($"[WARN] RELEASE invalid slot {roomId}-{slotId} by {clientId}");
                if (replyStream != null)
                {
                    Send(replyStream, "INFO|ERROR|Invalid room/slot\n");
                }
                return;
            }

            // Trường hợp client đang là holder -> RELEASE hợp lệ
            if (slot.CurrentHolder == clientId)
            {
                log.WriteLine($"[RELEASE] {clientId} -> {roomId}-{slotId}");

                // Thông báo cho client là đã RELEASE xong (phía UI có thể chuyển về trạng thái "FREE")
                if (replyStream != null)
                {
                    Send(replyStream, $"INFO|RELEASED|{roomId}|{slotId}\n");
                }

                if (slot.WaitingQueue.Count == 0)
                {
                    slot.IsBusy = false;
                    slot.CurrentHolder = null;
                    log.WriteLine($"[SLOT] {roomId}-{slotId} -> FREE");
                }
                else
                {
                    var (nextClientId, nextStream) = slot.WaitingQueue.Dequeue();
                    slot.IsBusy = true;
                    slot.CurrentHolder = nextClientId;
                    log.WriteLine($"[GRANT] {nextClientId} (from queue) -> {roomId}-{slotId}");
                    Send(nextStream, $"GRANT|{roomId}|{slotId}\n");
                }

                return;
            }

            // Không phải holder -> thử xóa client khỏi queue (client đang chờ nhưng bấm Hủy)
            int removed = RemoveFromQueue(slot, clientId);
            if (removed > 0)
            {
                log.WriteLine($"[CANCEL] {clientId} removed from queue of {roomId}-{slotId} (entries {removed})");
                if (replyStream != null)
                {
                    Send(replyStream, $"INFO|CANCELLED|{roomId}|{slotId}\n");
                }
            }
            else
            {
                // Không giữ quyền, không nằm trong queue -> từ chối
                log.WriteLine($"[WARN] RELEASE from non-holder/non-queued {clientId} on {roomId}-{slotId}");
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
            foreach (var kvp in _slots)
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
                    log.WriteLine($"[DISCONNECT] Removed {clientId} from queue of {roomId}-{slotId} (removed {removedFromQueue})");
                }

                // Nếu đang là holder -> auto release
                if (slot.CurrentHolder == clientId)
                {
                    log.WriteLine($"[DISCONNECT] Auto release {clientId} from {roomId}-{slotId}");

                    if (slot.WaitingQueue.Count == 0)
                    {
                        slot.IsBusy = false;
                        slot.CurrentHolder = null;
                        log.WriteLine($"[SLOT] {roomId}-{slotId} -> FREE (disconnect)");
                    }
                    else
                    {
                        var (nextClientId, nextStream) = slot.WaitingQueue.Dequeue();
                        slot.IsBusy = true;
                        slot.CurrentHolder = nextClientId;
                        log.WriteLine($"[GRANT] {nextClientId} (from queue, after disconnect) -> {roomId}-{slotId}");
                        Send(nextStream, $"GRANT|{roomId}|{slotId}\n");
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
}

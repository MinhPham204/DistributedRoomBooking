 # DistributedRoomBooking
 
 Đề tài: xây dựng ứng dụng server và client thực hiện loại trừ tương hỗ theo giải thuật tập trung, áp dụng cho bài toán đặt phòng học theo ca.
 
 ## 1. Mục tiêu
 - Mô phỏng bài toán truy cập tài nguyên dùng chung trong hệ phân tán. Mỗi phòng, mỗi ca, mỗi ngày được xem như một tài nguyên.
 - Đảm bảo tại một thời điểm chỉ một client được quyền chiếm dụng một ca phòng cụ thể.
 - Các client yêu cầu trùng tài nguyên sẽ được xếp hàng chờ và được cấp quyền theo thứ tự.
 
 ## 2. Kiến trúc tổng quan
 - BookingServer
   - Đóng vai trò điều phối trung tâm.
   - Quản lý trạng thái tài nguyên theo ngày và theo phòng.
   - Xử lý yêu cầu request, release, đồng thời gửi cập nhật realtime cho client.
 - BookingClient
   - Gửi yêu cầu đặt ca phòng.
   - Nhận kết quả được cấp quyền hoặc phải chờ.
   - Hiển thị dữ liệu và cập nhật theo push từ server.
 
 Giao tiếp dùng TCP socket. Dữ liệu trao đổi dạng từng dòng, mỗi message kết thúc bằng ký tự xuống dòng.
 
 ## 3. Loại trừ tương hỗ theo giải thuật tập trung
 Trong đồ án này, server đóng vai trò coordinator.
 
 Cách hoạt động chính:
 - Client gửi request để xin quyền sử dụng một phòng, một ca, trong một ngày.
 - Nếu tài nguyên đang rảnh, server cấp quyền ngay cho client.
 - Nếu tài nguyên đang có người giữ, server đưa yêu cầu vào hàng chờ.
 - Khi client release, server sẽ cấp quyền cho người tiếp theo trong hàng chờ nếu có.
 
 Tài nguyên được xác định bởi: roomId, slotId và date.
 
 Trạng thái cơ bản:
 - FREE: chưa ai giữ.
 - BUSY: đã có người giữ.
 - LOCKED: bị khóa theo lịch cố định hoặc sự kiện, không cho request thông thường.
 
 ## 4. Chức năng chính
 - Request và release một ca hoặc nhiều ca liên tiếp theo ngày và phòng.
 - Cơ chế hàng chờ và tự động cấp quyền khi có release.
 - Chức năng admin để demo và kiểm thử: force grant, force release, lock event, unlock event.
 - Lịch cố định: khóa các slot theo tuần trong một khoảng thời gian.
 - Cập nhật realtime cho client ở các màn hình chính.
 - Lưu và tải trạng thái server bằng file snapshot state.json.
 - Gửi email hiện dùng cho chức năng quên mật khẩu.
 
 ## 5. Luồng hoạt động tiêu biểu
 Ví dụ với một slot:
 1. Client A request slot.
 2. Server cấp quyền, slot chuyển sang BUSY.
 3. Client B request cùng slot, server đưa vào hàng chờ.
 4. Client A release, server cấp quyền cho client B.
 
 Trường hợp slot bị LOCKED:
 - Client request sẽ bị từ chối.
 - Admin có thể force để phục vụ demo trong một số tình huống.
 
 ## 6. Cách chạy trên Windows
 - Yêu cầu khi chạy bằng dotnet run: .NET SDK 8.
 - Chạy server bằng source: mở terminal tại thư mục BookingServer và chạy lệnh dotnet run.
 - Chạy client bằng source: mở terminal tại thư mục BookingClient và chạy lệnh dotnet run.
 - Có thể mở nhiều client để mô phỏng nhiều tiến trình trong hệ phân tán.
 
 Chạy bằng file exe
 - Server
   - Mở file BookingServer.exe trong thư mục publish
   - Đường dẫn thường gặp
     - BookingServer\bin\Release\net8.0-windows\win-x64\publish\BookingServer.exe
 - Client
   - Mở file BookingClient.exe trong thư mục publish
   - Đường dẫn thường gặp
     - BookingClient\bin\Release\net8.0-windows\win-x64\publish\BookingClient.exe
 - Thứ tự chạy
   - Mở server trước
   - Sau đó mở một hoặc nhiều client
 
 Ghi chú khi chạy exe
 - Nếu publish self-contained true thì có thể copy cả thư mục publish sang máy khác và chạy luôn.
 - Nếu publish self-contained false thì máy chạy cần cài .NET Desktop Runtime 8.

## 7. Kịch bản demo gợi ý
 - Mở 1 server và 2 client.
 - Cho cả 2 client request cùng một slot để quan sát một client được cấp quyền và client còn lại phải chờ.
 - Thực hiện release để kiểm tra cơ chế cấp quyền từ hàng chờ.
 - Tạo lịch cố định để kiểm tra slot LOCKED và hành vi từ chối request.
 
 ## 8. Cấu trúc thư mục
 - BookingServer: ứng dụng WinForms server.
 - BookingClient: ứng dụng WinForms client.
 - state.json: file snapshot được tạo khi chạy.

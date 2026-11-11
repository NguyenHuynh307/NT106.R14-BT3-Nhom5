# Core TCP Server — phần của Huỳnh Thanh Nguyên (Bước 1.1 → 1.3)

Tài liệu ngắn để cả nhóm biết **Server đã có gì, nối vào đâu, thử thế nào**. Phần đóng khung chính thức, Router, SQL, GUI là của người khác (xem bảng phân công); nhánh này không đụng tới.

## 1. Đã làm

| Bước | Nội dung | Vị trí |
|---|---|---|
| 1.1 | Solution 3 project: `Client` (WinForms), `Server` (Console), `Shared` (thư viện) | `NT106.R14-BT3-Nhom5.sln`, `src/*` |
| 1.2 | `TcpListener(IPAddress.Any)`, `AcceptTcpClientAsync(token)`, mỗi client một tác vụ không `await`, `ConcurrentDictionary`, log có mốc thời gian | `src/Server/Network/TcpServer.cs`, `src/Server/Logging/ServerLog.cs` |
| 1.3 | 3 kiểu client rời đi, TCP KeepAlive, dừng bằng `CancellationToken`, `Stop()` trong `finally` | `TcpServer.cs` |

## 2. Cách các thành viên khác nối vào

Server chỉ có **một điểm nối**: `IMessageProcessor.XuLyAsync(phien, thongDiep, token)` trong `src/Server/Network/IMessageProcessor.cs`.

- **Hoàng Anh (2.3):** cho `RequestRouter` implement interface này, rồi đổi đúng một dòng trong `Program.cs` (`new EchoMessageProcessor()` → `new RequestRouter(...)`). Nếu chữ ký bạn chốt trong Shared khác, viết một lớp bọc mỏng.
- **DANGXUAT:** handler đặt `phien.YeuCauDong = true` rồi trả phản hồi. Server gửi xong sẽ đóng kết nối và gỡ phiên trong `finally` (chỉ **một** nơi gỡ phiên, không cần gỡ tay trong handler).
- **Trọng Phú:** sau khi DANGNHAP thành công có thể gán `phien.TenDangNhap` (chỉ để ghi log, không lưu mật khẩu).
- **Đóng khung:** `KhungLfTam.cs` là bản **tạm** (một dòng UTF-8 kết thúc LF, ≤ 64 KiB) để Server chạy được ngay. Khi Hoàng Anh xong hàm đóng khung trong Shared, sửa 2 chỗ trong `ClientSession.cs` (`DocAsync`, `GuiAsync`) rồi xóa file tạm.
- Cổng mặc định 5000 (như đồ án). Hoàng Anh chốt số cuối cùng trong tài liệu giao thức; đổi ở `CauHinhServer.Cong` hoặc chạy `Server.exe --port <số>`.

## 3. Ba kiểu client rời đi và cách Server phát hiện

| Kiểu | Server thấy gì | Dòng log |
|---|---|---|
| 1. Đăng xuất | handler đặt `YeuCauDong` | `rời đi: client đăng xuất (DANGXUAT)` |
| 2. Tắt nút X đỏ | socket đóng đàng hoàng → đọc ra `null`; hoặc bị reset → `IOException` | `rời đi: client đóng kết nối ...` / `mất kết nối đột ngột (ConnectionReset)` |
| 3. Mất mạng (tắt Wi-Fi) | **không có tín hiệu nào** → TCP KeepAlive dò ra → `IOException` (`TimedOut`) | `rời đi: mất kết nối đột ngột (TimedOut)` |

### Vì sao phải chỉnh KeepAlive (kiểu 3)
Khi tắt Wi-Fi, client không kịp gửi FIN/RST, Server chỉ đang chờ đọc nên **không gặp ngoại lệ nào**. KeepAlive mặc định của Windows chờ hàng giờ, nên ca kiểm thử "mất mạng" sẽ không đạt đúng lúc.

`CauHinhServer` đặt: im lặng **10 giây** → dò, mỗi **5 giây** một lần, **3 lần** không đáp → rớt. Ước tính phát hiện sau **khoảng 25 giây** (10 + 5 × 3).

Lưu ý:
- Thời gian trên là **ước tính theo cấu hình**, cần đo thật bằng ca kiểm thử số 4 rồi ghi con số thực tế vào báo cáo.
- `TcpKeepAliveRetryCount` cần Windows 10 bản 1703 trở lên. Nếu không chỉnh được, Server ghi dòng `[CANH BAO]` và dùng mặc định của hệ điều hành.
- Lớp bảo vệ thứ hai `--idle <giây>` (cắt client im lặng) **mặc định tắt**, vì giao thức BT3 không có PING nên người dùng đứng yên ở Form chính là bình thường.

## 4. Cách chạy thử

```cmd
dotnet build NT106.R14-BT3-Nhom5.sln
dotnet run --project src\Server
dotnet run --project tools\TcpThu
```

`tools/TcpThu` chạy 5 kịch bản (PASS/FAIL): 2 client × 20 chuỗi liên tiếp (Bước 4), dính gói, phân mảnh giữa chữ có dấu, DANGXUAT, đóng đàng hoàng.

## 5. Ánh xạ với Tab "Kịch bản kiểm thử" (STT 1–5 của Nguyên)

| STT | Cách thử | Bằng chứng cần chụp/lưu |
|---|---|---|
| 1 | `dotnet run --project tools\TcpThu` (T1) hoặc mở 2 client | Ảnh 2 client + cửa sổ log Server |
| 2 | Máy khác trong LAN: `dotnet run --project tools\TcpThu -- <IP máy Server> 5000`. Nếu không nối được: mở cổng tường lửa (`netsh advfirewall firewall add rule name="NT106 BT3" dir=in action=allow protocol=TCP localport=5000`) | Ảnh kết quả + log Server thấy IP máy kia |
| 3 | Xem `logs\server-yyyyMMdd.log` hoặc console | Ảnh log mỗi dòng có mốc thời gian |
| 4 | Trên máy client: `dotnet run --project tools\TcpThu -- <IP> 5000 --giu 2`, thấy `đang nối: 2`, rồi **tắt Wi-Fi**; bấm đồng hồ tới khi Server ghi `rời đi: mất kết nối đột ngột` | Ảnh log + con số giây thực tế; client còn lại vẫn hoạt động |
| 5 | Nhấn Enter/Ctrl+C trong Server, rồi chạy lại ngay | Ảnh log `Đã dừng.` và lần chạy thứ hai không báo cổng bị dùng |

## 6. Chưa làm / ngoài phạm vi nhánh này
- **Bước 1.4 (báo cáo Mục VI):** làm sau khi có bằng chứng thật từ cả nhóm.
- Chưa ghép Router, handler, GUI của người khác (chờ các nhánh của họ).
- Chưa kiểm tra bằng compiler/chạy thật trong môi trường soạn nhánh này; cần `dotnet build` và chạy `TcpThu` trên máy Windows trước khi mở PR.

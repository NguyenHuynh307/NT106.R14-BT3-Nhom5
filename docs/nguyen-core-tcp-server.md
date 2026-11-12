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

| STT | Cách thử | Bằng chứng và kết quả thực tế |
|---|---|---|
| 1 | Chạy `dotnet run --no-build --project tools\TcpThu` để kiểm tra hai client, mỗi client gửi 20 thông điệp liên tiếp | PASS: Client A nhận 20/20, Client B nhận 20/20. Có log Server và kết quả tổng hợp 5/5. Ảnh 06 bổ sung bằng chứng hai kết nối đồng thời. |
| 2 | Máy khác trong LAN: `dotnet run --project tools\TcpThu -- <IP máy Server> 5000` | PASS: Ảnh 06 ghi nhận hai kết nối từ địa chỉ LAN `192.168.123.9`; ảnh 08 ghi nhận hai kết nối mới. |
| 3 | Xem console hoặc tệp log `logs\server-yyyyMMdd.log` trong thư mục chạy của Server | PASS: ServerLog ghi mốc thời gian cho từng sự kiện. Ảnh 06–12 có log thực tế. |
| 4 | Chạy `dotnet run --project tools\TcpThu -- <IP> 5000 --giu 2`, sau đó tắt Wi-Fi máy client | PASS: Server phát hiện mất kết nối bằng `TimedOut`. Ảnh 12 ghi nhận khoảng 25,032 giây và 25,036 giây cho hai kết nối. Lưu ý: cả hai kết nối đều bị mất; chưa có bằng chứng một client còn lại vẫn hoạt động. |
| 5 | Nhấn Enter hoặc Ctrl+C trong Server, sau đó chạy lại Server trên cổng 5000 | PASS: Log ngày 12/11/2026 ghi `Đã dừng.` lúc 14:45:22.720 và lần khởi động lại thành công lúc 14:49:42.787. |

### 5.1. Kiểm tra bổ sung

- RST: chạy `dotnet run --no-build --project tools\TcpThu -- --rst`.
- Kết quả: Client gửi `RST_TEST`, nhận `ECHO:RST_TEST`, sau đó đóng socket với `Linger=0`.
- Log Server xác nhận `mất kết nối đột ngột (ConnectionReset)` lúc 14:42:56.896 ngày 12/11/2026.

### 5.2. Kết quả build

| Lệnh | Kết quả |
|---|---|
| `dotnet build tools\TcpThu\TcpThu.csproj` | Thành công |
| `dotnet build NT106.R14-BT3-Nhom5.sln` | Thành công |
| `dotnet run --no-build --project tools\TcpThu` | PASS 5/5 |

Ảnh minh chứng được lưu trong bộ ZIP `BT3_Hinh-MinhChung_Buoc-1_1-1_3`. Kết quả chạy bổ sung ngày 12/11/2026 cần được lưu cùng bộ minh chứng khi hoàn thiện báo cáo.

## 6. Chưa làm / ngoài phạm vi nhánh này

- **Bước 1.4 (báo cáo Mục VI):** Chưa hoàn thành. Cần tổng hợp bằng chứng và kết quả tích hợp của cả nhóm.
- **Shared:** Chưa tích hợp đầy đủ định nghĩa thông điệp, mã lệnh, mã lỗi và bộ đóng khung chính thức.
- **Router:** Server hiện vẫn dùng `EchoMessageProcessor` để kiểm tra đường truyền; chưa thay bằng `RequestRouter` nghiệp vụ.
- **Đóng khung:** `KhungLfTam.cs` vẫn là bản tạm, chờ bộ đóng khung chính thức trong Shared.
- Chưa ghép đầy đủ phần SQL, xử lý đăng ký, đăng nhập, băm mật khẩu và giao diện Client của các thành viên khác.
- Kiểm thử Wi-Fi đã chứng minh Server phát hiện hai kết nối mất mạng bằng KeepAlive; chưa chứng minh một client còn lại tiếp tục hoạt động trong khi client kia mất mạng.

**Kết quả phần lõi TCP:** Đã build thành công, 5/5 kịch bản TcpThu đạt, kiểm thử RST đạt, dừng và khởi động lại Server thành công. Chưa merge vào `main`.

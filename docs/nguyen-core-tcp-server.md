# Core TCP Server — phần của Huỳnh Thanh Nguyên (Bước 1.1 → 1.3)

Tài liệu ngắn để cả nhóm biết **Server đã có gì, nối vào đâu, thử thế nào**. Phần đóng khung, Router, SQL, GUI là của các thành viên khác (xem bảng phân công).

**Cập nhật 16/11/2026:** các phần đã được ghép vào `main`. Đóng khung chính thức nằm trong `Shared` (`SocketStreamHelper`), `RequestRouter` đã thay `EchoMessageProcessor`, bản tạm `KhungLfTam.cs` đã được xóa. Các mục dưới đây giữ nguyên làm tài liệu kỹ thuật cho phần lõi TCP; những chỗ lỗi thời đã được sửa.

## 1. Đã làm

| Bước | Nội dung | Vị trí |
|---|---|---|
| 1.1 | Solution 3 project: `Client` (WinForms), `Server` (Console), `Shared` (thư viện) | `NT106.R14-BT3-Nhom5.sln`, `src/*` |
| 1.2 | `TcpListener(IPAddress.Any)`, `AcceptTcpClientAsync(token)`, mỗi client một tác vụ không `await`, `ConcurrentDictionary`, log có mốc thời gian | `src/Server/Network/TcpServer.cs`, `src/Server/Logging/ServerLog.cs` |
| 1.3 | 3 kiểu client rời đi, TCP KeepAlive, dừng bằng `CancellationToken`, `Stop()` trong `finally` | `TcpServer.cs` |

## 2. Cách các thành viên khác nối vào

Server chỉ có **một điểm nối**: `IMessageProcessor.XuLyAsync(phien, thongDiep, token)` trong `src/Server/Network/IMessageProcessor.cs`.

- **Hoàng Anh (2.3):** `RequestRouter` implement interface này và đã được nối trong `Program.cs` (thay cho `EchoMessageProcessor`). `EchoMessageProcessor` chỉ còn dùng khi cần thử đường truyền riêng.
- **DANGXUAT:** handler đặt `phien.YeuCauDong = true` rồi trả phản hồi. Server gửi xong sẽ đóng kết nối và gỡ phiên trong `finally` (chỉ **một** nơi gỡ phiên, không cần gỡ tay trong handler).
- **Trọng Phú:** sau khi DANGNHAP thành công, handler gán `phien.TenDangNhap` và `phien.SessionToken` (token phiên, mục III.10); không ghi mật khẩu hay token vào log.
- **Đóng khung:** `ClientSession.DocAsync` / `GuiAsync` gọi `SocketStreamHelper.ReceiveLineAsync` / `SendLineAsync` trong `Shared` (một dòng UTF-8 kết thúc LF, ≤ 64 KiB). Bản tạm `KhungLfTam.cs` đã bị xóa.
- Cổng 5000, đã chốt trong tài liệu giao thức (`docs/TaiLieuGiaoThuc_BT3.md`); có thể đổi ở `CauHinhServer.Cong` hoặc chạy `Server.exe --port <số>`.

## 3. Ba kiểu client rời đi và cách Server phát hiện

| Kiểu | Server thấy gì | Dòng log |
|---|---|---|
| 1. Đăng xuất | handler đặt `YeuCauDong` | `rời đi: client đăng xuất (DANGXUAT)` |
| 2. Tắt nút X đỏ | socket đóng đàng hoàng → đọc ra `null`; hoặc bị reset → `IOException` | `rời đi: client đóng kết nối ...` / `mất kết nối đột ngột (ConnectionReset)` |
| 3. Mất mạng (tắt Wi-Fi) | **không có tín hiệu nào** → TCP KeepAlive dò ra → `IOException` (`TimedOut`) | `rời đi: mất kết nối đột ngột (TimedOut)` |

### Vì sao phải chỉnh KeepAlive (kiểu 3)
Khi tắt Wi-Fi, client không kịp gửi FIN/RST, Server chỉ đang chờ đọc nên **không gặp ngoại lệ nào**. KeepAlive mặc định của Windows chờ hàng giờ, nên ca kiểm thử "mất mạng" sẽ không đạt đúng lúc.

`CauHinhServer` đặt: im lặng **10 giây** → dò, mỗi **5 giây** một lần, **3 lần** không đáp → rớt. Phát hiện chậm nhất sau **khoảng 25 giây** (10 + 5 × 3) tính từ lần có dữ liệu cuối; thời gian thực tế phụ thuộc thời điểm tắt Wi-Fi so với lần trao đổi cuối. **Đo thật (ca số 4, 12/11/2026): tắt Wi-Fi lúc 08:32:39.686, Server ghi `TimedOut` lúc 08:32:56.642, khoảng 17 giây.**

Lưu ý:
- Hai mốc giờ trên lấy từ hai máy khác nhau (máy Client và máy Server) nên có thể lệch đồng hồ; dùng làm số liệu sơ bộ.
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
| 4 | Chạy `dotnet run --project tools\TcpThu -- <IP> 5000 --giu 2`, sau đó tắt Wi-Fi máy client | PASS: Server phát hiện mất kết nối bằng `TimedOut`, gỡ client khỏi danh sách (`đang nối` giảm). Lần đo có ghi giờ tắt Wi-Fi (12/11/2026): tắt lúc 08:32:39.686, Server ghi `TimedOut` lúc 08:32:56.642, khoảng 17 giây. Các client khác không bị ảnh hưởng: ngày 14/11/2026 client LAN `888ee171` bị gỡ lúc 09:54:51 (`TimedOut`), client `869b982f` vẫn giữ kết nối và Server tiếp tục phục vụ hai client mới lúc 09:56:29. Lưu ý: ca này dùng client thử `TcpThu`, chưa phải `Client.exe`. |
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

Ảnh minh chứng (bộ `BT3_Hinh-MinhChung_Buoc-1_1-1_3`) **không đưa lên repo** (repo chỉ giữ `.md`/`.txt`); ảnh được nộp kèm báo cáo.

### 5.3. Kiểm thử tích hợp trên `main` (16/11/2026, commit `37e60f7`)

Chạy tự động qua TCP thật với Server thật và SQL Server thật (Server: token mặc định 30 phút):

| STT | Nội dung kiểm thử | Kết quả |
|---|---|---|
| 1, 12 | Hai client nối cùng lúc, mỗi client gửi 20 chuỗi liên tiếp (xen kẽ) | ĐẠT: nhận đủ 20/20 và 20/20, không dính liền |
| 3 | Mốc thời gian trong nhật ký Server | ĐẠT: 222/222 dòng có mốc thời gian |
| 6 | `DANGXUAT` | ĐẠT: mã 0, Server đóng kết nối sau khi trả lời |
| 7 | Họ tên tiếng Việt có dấu gửi và nhận lại | ĐẠT: giống hệt (UTF-8) |
| 8 | Hai thông điệp trong một lần ghi | ĐẠT: nhận hai phản hồi riêng |
| 13 | `DANGNHAP` đúng | ĐẠT: mã 0, 7 trường, không có cột mật khẩu |
| 14 | Sai tên / sai mật khẩu | ĐẠT: cùng mã 3 và cùng một thông báo |
| 15 | Tên control mặc định (`textBox1`, `button1`) | ĐẠT: không còn |
| 16 | Client không băm mật khẩu | ĐẠT: không có hàm băm trong mã Client |
| 18, 19 | `DANGKY` hợp lệ; trùng Username | ĐẠT: mã 0; mã 2 |
| 20 | Hai tài khoản cùng mật khẩu | ĐẠT: hai chuỗi `PasswordHash` khác nhau |
| 21 | `LAYTHONGTIN` | ĐẠT: khớp dữ liệu đã đăng ký |
| 22 | Dấu nháy đơn / chèn SQL trong Username | ĐẠT: mã 3, không lỗi SQL; đăng ký và đăng nhập tên có dấu nháy đều mã 0 |
| 23 | Client không tham chiếu SqlClient / chuỗi kết nối | ĐẠT: lần quét tự động báo 1 dòng khớp, kiểm tra tay đó là dòng chú thích trong `Client.csproj`; dự án chỉ tham chiếu `Guna.UI2.WinForms` và `Shared` |

Token phiên (III.10): thử với `NT106_TOKEN_SECONDS=5`, sau 6 giây `LAYTHONGTIN` trả mã 6; trên giao diện, form chính hiện thông báo hết hạn và quay về form Đăng nhập (16/11/2026).

## 6. Còn lại / lưu ý

- **Bước 1.4 (báo cáo Mục VI):** đang tổng hợp bằng chứng và kết quả tích hợp của cả nhóm.
- **Ca 4 (tắt Wi-Fi) với `Client.exe`:** hiện dùng client thử `TcpThu`; nếu còn thời gian thì đo lại bằng `Client.exe` trên máy thứ hai.
- **Ca thủ công chưa có kết quả trong mục 5.3:** nút X đỏ trên giao diện, dừng SQL Server rồi đăng ký/đăng nhập, nút bị khóa khi chờ phản hồi.
- `--idle <giây>` vẫn mặc định tắt (giao thức BT3 không có PING).

**Kết quả phần lõi TCP:** build thành công, 5/5 kịch bản `TcpThu` đạt, kiểm thử RST đạt, dừng và chạy lại Server thành công, và đã ghép vào `main` cùng Router, SQL, băm mật khẩu, giao diện Client và token phiên.

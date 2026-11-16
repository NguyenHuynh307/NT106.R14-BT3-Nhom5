# Lõi TCP Server — Huỳnh Thanh Nguyên (Bước 1.1 → 1.3)

## 1. Nội dung đã cài đặt

| Hạng mục | Cách làm | Vị trí |
|---|---|---|
| Cấu trúc solution | 3 dự án: `Client` (WinForms), `Server` (Console, không giao diện), `Shared` (thư viện dùng chung) | `src/*` |
| Nhận kết nối | `TcpListener(IPAddress.Any, 5000)` + `AcceptTcpClientAsync(token)` | `Network/TcpServer.cs` |
| Mỗi client một tác vụ | Gọi `PhucVuClientAsync` không `await`, quay lại nghe ngay | `TcpServer.cs` |
| Danh sách client an toàn | `ConcurrentDictionary<string, ClientSession>` | `TcpServer.cs` |
| Client rời đi | Cả 3 kiểu đều gỡ phiên trong khối `finally`; bắt riêng `IOException`, `SocketException` | `TcpServer.cs` |
| Dừng server | `CancellationToken`, bắt `OperationCanceledException`, `listener.Stop()` trong `finally` | `TcpServer.cs` |
| Nhật ký | Mỗi dòng có mốc thời gian, ghi ra console và `logs\server-yyyyMMdd.log` | `Logging/ServerLog.cs` |
| Đóng khung | Một dòng UTF-8 kết thúc `\n`, tối đa 64 KiB (`SocketStreamHelper`, dùng chung Client/Server) | `Shared` |

Cổng 5000 (khai trong `docs/TaiLieuGiaoThuc_BT3.md`), đổi bằng `Server.exe --port <số>`.

## 2. Ba kiểu client rời đi

| Kiểu | Cách Server phát hiện | Dòng log |
|---|---|---|
| Đăng xuất | `DANGXUAT` đặt `YeuCauDong`, Server gửi phản hồi rồi đóng | `client đăng xuất (DANGXUAT)` |
| Tắt cửa sổ (nút X) | Socket đóng → đọc ra `null`, hoặc bị reset → `IOException` | `client đóng kết nối` / `ConnectionReset` |
| Mất mạng (tắt Wi-Fi) | TCP KeepAlive dò ra → `IOException` (`TimedOut`) | `mất kết nối đột ngột (TimedOut)` |

**KeepAlive:** im lặng 10 giây thì dò, mỗi 5 giây một lần, 3 lần không đáp thì rớt (tối đa ~25 giây). Mặc định của Windows phải chờ hàng giờ nên bắt buộc chỉnh.

**Đo thực tế:** tắt Wi-Fi 08:32:39 → Server ghi `TimedOut` 08:32:56 (**~17 giây**, 12/11/2026). Client khác không bị ảnh hưởng: ngày 14/11 một client bị gỡ lúc 09:54:51, client còn lại vẫn kết nối và Server nhận thêm 2 client mới lúc 09:56:29.

## 3. Kết quả kiểm thử (đều đạt)

**Đường truyền và vòng đời Server** (`tools\TcpThu`, 12–14/11/2026):

| Ca | Kết quả |
|---|---|
| 2 client × 20 chuỗi liên tiếp | Nhận đủ 20/20 và 20/20, không dính liền |
| Client từ máy khác trong LAN (`192.168.123.9`) | Kết nối và trao đổi bình thường |
| Mốc thời gian trong log | 222/222 dòng đều có |
| Tắt Wi-Fi | Phát hiện `TimedOut` sau ~17 giây (client thử `TcpThu`) |
| Reset kết nối (`--rst`) | Ghi `ConnectionReset`, gỡ phiên |
| Dừng bằng Enter/Ctrl+C rồi chạy lại cổng 5000 | Log `Đã dừng.` rồi khởi động lại thành công |
| Kịch bản `TcpThu` | 5/5 đạt |

**Tích hợp trên `main`** (16/11/2026, Server thật + SQL Server thật):

| Nội dung | Kết quả |
|---|---|
| 2 client gửi xen kẽ 20 chuỗi; 2 thông điệp trong một lần ghi | Tách đúng từng phản hồi |
| Họ tên tiếng Việt có dấu | Gửi/nhận giống hệt (UTF-8) |
| `DANGKY` hợp lệ / trùng Username | Mã `0` / mã `2` |
| `DANGNHAP` đúng | Mã `0`, không có cột mật khẩu |
| `DANGNHAP` sai tên / sai mật khẩu | Cùng mã `3`, cùng một thông báo |
| `LAYTHONGTIN`, `DANGXUAT` | Đúng dữ liệu; Server đóng kết nối sau `DANGXUAT` |
| Hai tài khoản cùng mật khẩu | Hai `PasswordHash` khác nhau |
| Chèn SQL / dấu nháy đơn trong Username | Không lỗi SQL (truy vấn có tham số) |
| Client không tham chiếu SqlClient / chuỗi kết nối | Đạt |
| Token phiên 256 bit (nâng cao) | Mã `6` khi hết hạn; giao diện quay về form Đăng nhập |

Build Solution: 0 lỗi, 0 cảnh báo.

## 4. Chạy thử

```cmd
dotnet build NT106.R14-BT3-Nhom5.sln
dotnet run --project src\Server
dotnet run --project tools\TcpThu
```

# NT106.R14 – Bài tập 3 – Nhóm 5

Ứng dụng quản lý người dùng theo mô hình **Client – Server** qua **TCP Socket** (C# / .NET 10).

`Client` (Windows Forms) chỉ đảm nhiệm giao diện và kiểm tra dữ liệu đầu vào; toàn bộ truy vấn SQL Server, băm mật khẩu và quản lý phiên đều xử lý tập trung tại `Server`.

## Chức năng

* **Quản lý tài khoản:** Đăng ký (`DANGKY`), đăng nhập (`DANGNHAP`), xem hồ sơ người dùng (`LAYTHONGTIN`) và đăng xuất (`DANGXUAT`).

* **Bảo mật mật khẩu:** Băm phía Server bằng **PBKDF2-HMAC-SHA256** (`600.000` vòng lặp, salt ngẫu nhiên `16 byte`), đối chiếu bằng `CryptographicOperations.FixedTimeEquals`.

* **Quản lý phiên (Nâng cao):** Cấp phát **Token phiên 256-bit** (mã hóa `base64url`, mặc định hết hạn sau `30 phút`, cấu hình nhanh qua biến môi trường `NT106_TOKEN_SECONDS`).

* **Dò tìm Server LAN (Nâng cao):** Client phát gói quảng bá **UDP Broadcast** (cổng `5001`) để tự động nhận địa chỉ IP và cổng TCP (`5000`) của Server trong mạng nội bộ.

* **Server đa kết nối:** Phục vụ đồng thời nhiều Client (`TcpListener` + `AcceptTcpClientAsync`), quản lý phiên an toàn đa luồng bằng `ConcurrentDictionary`, xử lý đủ 3 kiểu Client rời đi (`DANGXUAT`, tắt cửa sổ `FormClosing`, mất mạng đột ngột qua TCP Keep-Alive) và dừng Server tử tế bằng `CancellationToken`.

## Cấu trúc thư mục

```text
NT106.R14-BT3-Nhom5/
├── database/
│   └── NguoiDung.sql              # Kịch bản tạo bảng dbo.NguoiDung (7 cột)
├── docs/
│   └── TaiLieuGiaoThuc_BT3.docx   # Tài liệu đặc tả giao thức ứng dụng (<= 2 trang)
├── src/
│   ├── Shared/                    # Thư viện dùng chung: GiaoThuc, SocketStreamHelper, Interface
│   ├── Server/                    # Console Server: TcpServer, RequestRouter, Handlers, Security, Data
│   └── Client/                    # WinForms Client (Guna.UI2): FormDangNhap, FormDangKi, FormTrangChinh
├── tools/
│   ├── TcpThu/                    # Công cụ kiểm thử tự động 5 kịch bản TCP (2 Client x 20 chuỗi)
│   └── FramingThu/                # Công cụ kiểm thử đóng khung thông điệp UTF-8
├── NT106.R14-BT3-Nhom5.sln        # Visual Studio Solution
├── QUY-TAC-REPOSITORY.md          # Quy tắc làm việc Git và đặt tên nhánh của nhóm
└── README.md
```

## Chạy nhanh

### 1. Chuẩn bị cơ sở dữ liệu

* Cài đặt **.NET 10 SDK** và **SQL Server** (hoặc SQL Express / LocalDB).

* Chạy script `database/NguoiDung.sql` trong SSMS để tạo CSDL `healthInforDB` và bảng `dbo.NguoiDung`.

* (Tùy chọn) Thiết lập biến môi trường chuỗi kết nối nếu dùng instance mặc định:

  ```bat
  set NT106_DB_CONNECTION=Server=localhost;Database=healthInforDB;Trusted_Connection=True;TrustServerCertificate=True;
  ```

### 2. Khởi chạy Server & Client

Mở hai cửa sổ terminal tại thư mục gốc repository:

```bat
:: Terminal 1 - Chạy Server (lắng nghe TCP 5000 và UDP 5001)
dotnet run --project src\Server

:: Terminal 2 - Chạy Client (có thể mở nhiều cửa sổ để thử đa kết nối)
dotnet run --project src\Client
```

> Hoặc mở `NT106.R14-BT3-Nhom5.sln` bằng Visual Studio 2022/2026 và thiết lập **Multiple Startup Projects** (`Server` + `Client`).

## Giao thức ứng dụng

* **Cổng mạng:** TCP `5000` (`IPAddress.Any`) và UDP Discovery `5001` (`DISCOVER_NT106_SERVER` → `NT106_SERVER|<IP>|5000`).

* **Đóng khung & Mã hóa:** Mỗi thông điệp là một dòng văn bản **UTF-8 không BOM** kết thúc bằng `\n` (tối đa `64 KiB`), các trường phân tách bởi dấu `|` (cấm `\r`, `\n`, `|` trong dữ liệu).

  * Yêu cầu: `COMMAND|arg1|arg2|...\n`

  * Phản hồi: `STATUS|MESSAGE|data1|...\n`

### Bảng mã lệnh

| Lệnh | Tham số gửi lên | Phản hồi thành công (`Status = 0`) |
| ----- | ----- | ----- |
| `DANGKY` | `Username \| Password \| Email \| FullName \| Birthday` | `0 \| Đăng ký tài khoản thành công` |
| `DANGNHAP` | `Username \| Password` | `0 \| Thành công \| Username \| Email \| FullName \| Birthday \| <Token>` |
| `LAYTHONGTIN` | `Token` | `0 \| Thành công \| Username \| Email \| FullName \| Birthday` |
| `DANGXUAT` | `Token` | `0 \| Đăng xuất thành công` (gỡ phiên, đóng kết nối) |

### Bảng mã trạng thái / mã lỗi

| Mã | Định danh | Ý nghĩa |
| ----- | ----- | ----- |
| `0` | `CodeSuccess` | Xử lý thành công |
| `1` | `CodeInvalidFormat` | Sai cú pháp khung, thông điệp rỗng, thiếu tham số hoặc lệnh lạ |
| `2` | `CodeUserExists` | Tên đăng nhập đã tồn tại trong bảng `NguoiDung` (khi `DANGKY`) |
| `3` | `CodeAuthFailed` | Sai tên đăng nhập hoặc mật khẩu (dùng chung 1 thông báo bảo mật) |
| `4` | `CodeUnauthorized` | Chưa đăng nhập hoặc Token không hợp lệ |
| `5` | `CodeSystemError` | Lỗi hệ thống / kết nối CSDL (không lộ tên ngoại lệ về Client) |
| `6` | `CodeTokenExpired` | Token phiên đã hết hạn; Client tự động quay về Form Đăng nhập |

## Kiểm thử

```bat
:: Build toàn bộ Solution ở chế độ Release
dotnet build NT106.R14-BT3-Nhom5.sln -c Release

:: Chạy bộ kiểm thử TCP tự động (yêu cầu Server đang chạy)
dotnet run --project tools\TcpThu
```

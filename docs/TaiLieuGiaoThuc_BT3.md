# Đặc tả giao thức tầng Shared (`src/Shared`)

## 1. Cấu hình kết nối & đóng khung gói tin
- **Cổng mặc định (`DefaultPort`):** `5000` (nằm trong dải 1024–49151)
- **Bảng mã (`MessageEncoding`):** UTF-8 (không BOM), dùng thống nhất ở cả Client và Server
- **Ký tự kết thúc gói tin (`Delimiter`):** `\n` (chấp nhận `\r\n` ở cuối dòng khi nhận, không được chứa CR/LF ở giữa nội dung)
- **Giới hạn kích thước (`MaxMessageBytes`):** `64 KiB` (`65536` bytes, không tính `\n`)
- **Ký tự phân tách trường (`FieldSeparator`):** `|`
- **Lý do chọn đóng khung theo dòng (kết thúc bằng `\n`):** TCP là luồng byte, không giữ ranh giới thông điệp nên hai thông điệp có thể dính liền hoặc một thông điệp bị tách mảnh. Ký tự `\n` đánh dấu hết một thông điệp; bên nhận đọc cho tới khi gặp `\n`.

## 2. Quy ước xử lý ký tự `|` và xuống dòng
- **Quy tắc:** **CẤM** chứa ký tự `|` trong mọi trường dữ liệu (`Command`, `Args`, `Message`, `Data`).
- **Cơ chế kiểm tra:** Hàm `Serialize()` của `RequestMessage` và `ResponseMessage` tự động kiểm tra và ném `ArgumentException` nếu phát hiện chuỗi chứa `|`, tránh làm lệch cột khi phía nhận dùng `Split('|')`.
- **Ký tự CR/LF:** nội dung chứa `\r` hoặc `\n` bị **từ chối** khi gửi (không tự sửa), Client phải chặn trước khi gửi.

## 3. Cấu trúc gói tin
- **Request (Client → Server):** `COMMAND|arg1|arg2|...`
- **Response (Server → Client):** `STATUS_CODE|MESSAGE|data1|data2|...`
- Thông điệp rỗng, thiếu lệnh, lệnh lạ hoặc thiếu tham số: Server trả mã `1`.

### 3.1. Bảng lệnh

| Lệnh | Tham số (Request) | Phản hồi thành công | Phản hồi lỗi |
| :--- | :--- | :--- | :--- |
| `DANGKY` | `Username\|Password\|Email\|FullName\|Birthday` (đúng 5 tham số) | `0\|MESSAGE` | `1` thiếu/sai dữ liệu; `2` tài khoản đã tồn tại; `5` lỗi hệ thống |
| `DANGNHAP` | `Username\|Password` | `0\|MESSAGE\|Username\|Email\|FullName\|Birthday` | `1` thiếu tham số; `3` sai tên hoặc sai mật khẩu (cùng một thông báo); `5` lỗi hệ thống |
| `LAYTHONGTIN` | không có | `0\|MESSAGE\|Username\|Email\|FullName\|Birthday` | `4` chưa đăng nhập; `5` lỗi hệ thống |
| `DANGXUAT` | không có | `0\|MESSAGE`, sau đó Server đóng kết nối và gỡ phiên | — |

Ghi chú dữ liệu:
- `Birthday`: định dạng `yyyy-MM-dd` (ví dụ `2000-01-01`); không có thì để chuỗi rỗng.
- `Email`: không có thì để chuỗi rỗng.
- Phản hồi không bao giờ chứa mật khẩu, `PasswordHash` hay `PasswordSalt`.
- Khi lỗi, phản hồi chỉ có `MÃ_LỖI|MESSAGE`, không có phần dữ liệu.
- `Password` do Client gửi nguyên văn; việc băm do Server thực hiện trước khi ghi CSDL.

## 4. Bảng mã trạng thái (`StatusCode`)
| Mã | Hằng số | Ý nghĩa |
| :--- | :--- | :--- |
| `0` | `CodeSuccess` | Xử lý thành công |
| `1` | `CodeInvalidFormat` | Sai cú pháp hoặc thiếu tham số |
| `2` | `CodeUserExists` | Tài khoản đã tồn tại (`DANGKY`) |
| `3` | `CodeAuthFailed` | Sai tài khoản hoặc mật khẩu (`DANGNHAP`) |
| `4` | `CodeUnauthorized` | Chưa đăng nhập hoặc phiên không hợp lệ |
| `5` | `CodeSystemError` | Lỗi hệ thống / Lỗi xử lý nội bộ Server |

### 5. Dò tìm Server qua UDP (Tính năng nâng cao)
- Server lắng nghe gói UDP tại cổng 5001.
- Client phát gói tin `DISCOVER_NT106_SERVER`.
- Server phản hồi `NT106_SERVER|<IP_Server>|<Cổng_TCP>` để Client tự động điền IP và Port.

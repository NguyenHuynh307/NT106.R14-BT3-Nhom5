# Đặc tả giao thức tầng Shared (`src/Shared`)

## 1. Cấu hình kết nối & đóng khung gói tin
- **Cổng mặc định (`DefaultPort`):** `5000`
- **Bảng mã (`MessageEncoding`):** UTF-8 (không BOM)
- **Ký tự kết thúc gói tin (`Delimiter`):** `\n` (chấp nhận `\r\n` ở cuối dòng khi nhận, không được chứa CR/LF ở giữa nội dung)[cite: 2]
- **Giới hạn kích thước (`MaxMessageBytes`):** `64 KiB` (`65536` bytes, không tính `\n`)[cite: 2, 5]
- **Ký tự phân tách trường (`FieldSeparator`):** `|`[cite: 5]

## 2. Quy ước xử lý ký tự `|`
- **Quy tắc:** **CẤM** chứa ký tự `|` trong mọi trường dữ liệu (`Command`, `Args`, `Message`, `Data`)[cite: 2].
- **Cơ chế kiểm tra:** Hàm `Serialize()` của `RequestMessage` và `ResponseMessage` tự động kiểm tra và ném `ArgumentException` nếu phát hiện chuỗi chứa `|`, tránh làm lệch cột khi phía nhận dùng `Split('|')`.

## 3. Cấu trúc gói tin
- **Request (Client $\rightarrow$ Server):** `COMMAND|arg1|arg2|...`
  - Các lệnh chuẩn: `DANGKY`, `DANGNHAP`, `LAYTHONGTIN`, `DANGXUAT`[cite: 5]
- **Response (Server $\rightarrow$ Client):** `STATUS_CODE|MESSAGE|data1|data2|...`

## 4. Bảng mã trạng thái (`StatusCode`)
| Mã | Hằng số | Ý nghĩa |
| :--- | :--- | :--- |
| `0` | `CodeSuccess` | Xử lý thành công |
| `1` | `CodeInvalidFormat` | Sai cú pháp hoặc thiếu tham số |
| `2` | `CodeUserExists` | Tài khoản đã tồn tại (`DANGKY`) |
| `3` | `CodeAuthFailed` | Sai tài khoản hoặc mật khẩu (`DANGNHAP`) |
| `4` | `CodeUnauthorized` | Chưa đăng nhập hoặc không có quyền truy cập[cite: 2] |
| `5` | `CodeSystemError` | Lỗi hệ thống / Lỗi xử lý nội bộ Server[cite: 2] |

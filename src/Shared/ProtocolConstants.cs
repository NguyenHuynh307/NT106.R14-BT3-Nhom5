using System.Text;

namespace Shared
{
    public static class ProtocolConstants
    {
        // Cấu hình kết nối & đóng khung gói tin TCP
        public const int DefaultPort = 5000;
        public const string Delimiter = "\n";
        public const char FieldSeparator = '|';
        public static readonly Encoding MessageEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        public const int MaxMessageBytes = 64 * 1024;   // giới hạn 1 thông điệp (byte), không tính '\n'

        // Cấu hình dò tìm Server tự động qua UDP Broadcast trong mạng LAN (TC10 - Nâng cao)
        public const int UdpDiscoveryPort = 5001;
        public const string UdpDiscoveryRequest = "DISCOVER_NT106_SERVER";
        public const string UdpDiscoveryResponsePrefix = "NT106_SERVER";

        // 4 lệnh chuẩn của hệ thống
        public const string CmdRegister = "DANGKY";
        public const string CmdLogin = "DANGNHAP";
        public const string CmdGetInfo = "LAYTHONGTIN";
        public const string CmdLogout = "DANGXUAT";

        // Bảng mã trạng thái / mã lỗi (0 - 5)
        public const int CodeSuccess = 0;           // Thành công
        public const int CodeInvalidFormat = 1;     // Sai cú pháp hoặc thiếu tham số
        public const int CodeUserExists = 2;        // Tài khoản đã tồn tại (DANGKY)
        public const int CodeAuthFailed = 3;        // Sai tài khoản hoặc mật khẩu (DANGNHAP)
        public const int CodeUnauthorized = 4;      // Chưa đăng nhập hoặc không có quyền
        public const int CodeSystemError = 5;       // Lỗi hệ thống / Lỗi xử lý nội bộ Server
    }
}
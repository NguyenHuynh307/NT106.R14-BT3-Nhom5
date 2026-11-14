namespace NT106.BT3.Server.Network;

/// <summary>
/// ĐIỂM NỐI NGHIỆP VỤ DUY NHẤT của lõi TCP (Bước 1.2).
/// TcpServer đọc đúng MỘT thông điệp rồi gọi hàm này; hàm trả về phản hồi để gửi lại (null = không phản hồi).
///
/// Server KHÔNG chứa SQL và không biết lệnh nào cả, nên Nguyên không phải chờ ai:
///   - Hoàng Anh (Bước 2.3): RequestRouter implement interface này, phân tích lệnh rồi gọi handler của Trọng Phú / Vĩnh Phú.
///   - Đến lúc đó chỉ đổi MỘT dòng trong Program.cs: new EchoMessageProcessor() -> new RequestRouter(...).
///   - Nếu chữ ký hàm Hoàng Anh chốt trong Shared khác (ví dụ kiểu trả về), chỉ cần viết một lớp bọc mỏng.
///
/// Lệnh DANGXUAT: handler gọi phien.YeuCauDong = true rồi trả phản hồi. Server gửi phản hồi xong sẽ đóng kết nối
/// và gỡ phiên ở khối finally (chỉ có MỘT nơi gỡ phiên nên không bị gỡ hai lần).
/// </summary>
internal interface IMessageProcessor
{
    Task<string?> XuLyAsync(ClientSession phien, string thongDiep, CancellationToken token);
}

namespace NT106.BT3.Server.Network;

/// <summary>
/// Bộ xử lý TẠM chỉ dùng để chạy thử đường truyền (Bước 4 của đề) khi Router chưa có.
/// Trả lại "ECHO:" + nguyên văn thông điệp. Riêng thông điệp bắt đầu bằng DANGXUAT thì
/// đóng kết nối sau khi phản hồi, để thử được kiểu rời đi số 1 mà chưa cần Router thật.
/// </summary>
internal sealed class EchoMessageProcessor : IMessageProcessor
{
    public Task<string?> XuLyAsync(ClientSession phien, string thongDiep, CancellationToken token)
    {
        if (thongDiep.StartsWith("DANGXUAT", StringComparison.Ordinal))
        {
            phien.YeuCauDong = true;
            return Task.FromResult<string?>("OK:DANGXUAT");
        }

        return Task.FromResult<string?>("ECHO:" + thongDiep);
    }
}

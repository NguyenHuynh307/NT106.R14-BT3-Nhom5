using System.Net.Sockets;

namespace NT106.BT3.Server.Network;

/// <summary>
/// MỘT kết nối client đang mở tới Server. TcpServer tạo một đối tượng cho mỗi client
/// và lưu vào ConcurrentDictionary để biết ai đang nối.
/// </summary>
internal sealed class ClientSession : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _luong;
    private readonly BoDocKhungLfTam _boDoc;

    // Nhiều tác vụ có thể cùng gửi cho một client; trong lúc ghi có await nên dùng SemaphoreSlim, không dùng lock.
    private readonly SemaphoreSlim _khoaGhi = new(1, 1);

    /// <summary>Mã ngắn của phiên: khóa trong danh sách và là nhãn trong log.</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    public string DiaChiClient { get; }

    public DateTime ThoiDiemNoi { get; } = DateTime.Now;

    /// <summary>Handler (ví dụ DANGXUAT) đặt true để Server đóng kết nối sau khi gửi phản hồi.</summary>
    public bool YeuCauDong { get; set; }

    /// <summary>Tên đăng nhập của phiên sau khi DANGNHAP thành công (Trọng Phú gán); chỉ dùng cho log. Không lưu mật khẩu.</summary>
    public string? TenDangNhap { get; set; }

    public ClientSession(TcpClient client)
    {
        _client = client;
        DiaChiClient = client.Client.RemoteEndPoint?.ToString() ?? "không rõ";
        _luong = client.GetStream();
        _boDoc = new BoDocKhungLfTam(_luong);
    }

    /// <summary>Đọc một thông điệp. null = client đã đóng kết nối đàng hoàng.</summary>
    public Task<string?> DocAsync(CancellationToken token) => _boDoc.DocDongAsync(token);

    /// <summary>Gửi một thông điệp (tự thêm LF). An toàn khi nhiều tác vụ cùng gọi.</summary>
    public async Task GuiAsync(string thongDiep, CancellationToken token)
    {
        await _khoaGhi.WaitAsync(token);
        try
        {
            await KhungLfTam.GhiAsync(_luong, thongDiep, token);
        }
        finally
        {
            _khoaGhi.Release();
        }
    }

    public void Dispose()
    {
        _client.Dispose();   // đóng socket, trả lại cho hệ điều hành
    }
}

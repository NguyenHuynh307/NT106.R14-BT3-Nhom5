using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using NT106.BT3.Server.Logging;

namespace NT106.BT3.Server.Network;

/// <summary>
/// LÕI TCP ĐA KẾT NỐI CỦA SERVER (Bước 1.2 + 1.3, Mục III.8 của đề).
///
///  1. TcpListener nghe ở IPAddress.Any, chấp nhận bằng AcceptTcpClientAsync(token).          [III.8 ý 1]
///  2. Mỗi client một tác vụ riêng: gọi PhucVuClientAsync KHÔNG kèm await rồi nghe tiếp ngay.  [III.8 ý 2]
///  3. Danh sách client là ConcurrentDictionary (nhiều tác vụ cùng thêm/xóa vẫn an toàn).      [III.8 ý 3]
///  4. Client rời đi theo 3 kiểu đều được gỡ khỏi danh sách trong khối finally.                [III.8 ý 4]
///       Kiểu 1 - Đăng xuất  : handler đặt YeuCauDong, Server gửi phản hồi rồi đóng.
///       Kiểu 2 - Tắt cửa sổ : socket đóng đàng hoàng (đọc ra null) hoặc bị reset (IOException).
///       Kiểu 3 - Mất mạng   : không ai báo; TCP KeepAlive dò ra sau khoảng 25 giây (IOException/SocketException).
///  5. Dừng bằng CancellationToken: bắt OperationCanceledException rồi bỏ qua, Stop() trong finally. [III.8 ý 5]
/// </summary>
internal sealed class TcpServer
{
    private readonly IMessageProcessor _xuLy;
    private readonly CauHinhServer _cauHinh;

    // Khóa = Id phiên. Mỗi client chạy trên một tác vụ khác nhau nên không dùng Dictionary thường.
    private readonly ConcurrentDictionary<string, ClientSession> _phien = new();

    // Các tác vụ phục vụ đang chạy, để lúc dừng Server chờ chúng dọn xong (tối đa vài giây).
    private readonly ConcurrentDictionary<Task, byte> _tacVuDangChay = new();

    public TcpServer(IMessageProcessor xuLy, CauHinhServer cauHinh)
    {
        _xuLy = xuLy;
        _cauHinh = cauHinh;
    }

    /// <summary>Số client đang nối.</summary>
    public int SoPhienDangNoi => _phien.Count;

    /// <summary>
    /// Bắt đầu nghe và chạy cho tới khi token bị hủy; Task trả về hoàn thành khi đã dừng hẳn.
    /// Hàm KHÔNG async để nếu cổng đang bị chiếm thì SocketException ném ra NGAY tại đây (Program thấy ngay lúc khởi động).
    /// </summary>
    public Task StartAsync(CancellationToken token)
    {
        var listener = new TcpListener(_cauHinh.DiaChiNghe, _cauHinh.Cong);
        listener.Start();
        ServerLog.Ghi("TCP", $"Đang nghe tại {listener.LocalEndpoint}");
        if (_cauHinh.DiaChiNghe.Equals(IPAddress.Any))
        {
            ServerLog.Ghi("TCP", "0.0.0.0 nghĩa là nghe trên mọi card mạng. Client cùng máy nhập 127.0.0.1; client máy khác nhập một trong các địa chỉ sau:");
            foreach (string diaChi in LayDiaChiMangNoiBo())
                ServerLog.Ghi("TCP", $"  - {diaChi}:{_cauHinh.Cong}");
        }
        return ChayVongNhanAsync(listener, token);
    }

    private async Task ChayVongNhanAsync(TcpListener listener, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    // Chờ ở đây (không tốn CPU) cho tới khi có client gõ cửa hoặc token bị hủy.
                    client = await listener.AcceptTcpClientAsync(token);
                }
                catch (SocketException ex)
                {
                    // Lỗi lúc nhận một kết nối không nên làm Server chết: ghi log, nghỉ chút rồi nghe tiếp.
                    ServerLog.Ghi("TCP", $"Nhận kết nối thất bại ({ex.SocketErrorCode}), thử lại.");
                    await Task.Delay(100, token);
                    continue;
                }

                BatDauPhucVu(client, token);   // KHÔNG await: quay lại nghe client tiếp theo ngay
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Server được yêu cầu dừng: đây là cách dừng bình thường, không phải sự cố, nên bỏ qua.
        }
        finally
        {
            listener.Stop();   // luôn trả cổng lại cho hệ điều hành để chạy lần hai không báo "cổng đang bị dùng"

            // Đóng mọi kết nối còn lại để các tác vụ đang chờ đọc được đánh thức và thoát.
            foreach (ClientSession phien in _phien.Values)
                phien.Dispose();

            // Cho các tác vụ phục vụ tối đa 3 giây để ghi log "rời đi" rồi mới báo đã dừng.
            await Task.WhenAny(Task.WhenAll(_tacVuDangChay.Keys.ToArray()), Task.Delay(TimeSpan.FromSeconds(3)));

            ServerLog.Ghi("TCP", "Đã dừng.");
        }
    }

    /// <summary>Tạo tác vụ riêng cho một client và theo dõi nó, không chờ nó (fire-and-forget có người trông).</summary>
    private void BatDauPhucVu(TcpClient client, CancellationToken token)
    {
        Task tacVu = PhucVuClientAsync(client, token);   // KHÔNG await (Mục III.8 ý 2)
        _tacVuDangChay[tacVu] = 0;

        // Tác vụ chạy nền mà ném lỗi ngoài dự kiến thì không ai hay biết (Mục V: "Server im lặng").
        // Dòng dưới ghi log những lỗi như vậy, nên không cần bắt Exception chung trong vùng xử lý mạng.
        _ = tacVu.ContinueWith(t =>
        {
            _tacVuDangChay.TryRemove(t, out _);
            if (t.IsFaulted)
                ServerLog.Ghi("LOI", $"Tác vụ phục vụ client gặp lỗi ngoài dự kiến: {t.Exception?.GetBaseException().GetType().Name}");
        }, TaskScheduler.Default);
    }

    /// <summary>Toàn bộ đời sống của MỘT client: nhận -> xử lý -> phản hồi, lặp lại cho tới khi rời đi.</summary>
    private async Task PhucVuClientAsync(TcpClient client, CancellationToken serverToken)
    {
        ClientSession? phien = null;
        string lyDo = "server dừng";

        try
        {
            client.NoDelay = true;   // gói nhỏ gửi đi ngay, không chờ gom
            BatKeepAlive(client);

            phien = new ClientSession(client);
            _phien[phien.Id] = phien;
            ServerLog.Ghi("TCP", $"+ {phien.Id} nối từ {phien.DiaChiClient} (đang nối: {_phien.Count})");

            while (!serverToken.IsCancellationRequested)
            {
                string? thongDiep;

                // Hạn chờ im lặng (nếu bật) chỉ áp cho lúc ĐỢI client gửi, không áp cho lúc xử lý yêu cầu.
                using (var hanCho = CancellationTokenSource.CreateLinkedTokenSource(serverToken))
                {
                    if (_cauHinh.ThoiGianImLang > TimeSpan.Zero)
                        hanCho.CancelAfter(_cauHinh.ThoiGianImLang);

                    thongDiep = await phien.DocAsync(hanCho.Token);
                }

                if (thongDiep is null)
                {
                    lyDo = "client đóng kết nối (tắt cửa sổ hoặc ngắt kết nối)";
                    break;
                }
                if (thongDiep.Length == 0) continue;   // dòng trống: bỏ qua

                // Chỉ ghi SỐ BYTE, không ghi nội dung (nội dung có thể chứa mật khẩu).
                ServerLog.Ghi("TCP", $"< {phien.Id} nhận {Encoding.UTF8.GetByteCount(thongDiep)} byte");

                string? phanHoi = await _xuLy.XuLyAsync(phien, thongDiep, serverToken);
                if (phanHoi is not null)
                {
                    await phien.GuiAsync(phanHoi, serverToken);
                    ServerLog.Ghi("TCP", $"> {phien.Id} gửi {Encoding.UTF8.GetByteCount(phanHoi)} byte");
                }

                if (phien.YeuCauDong)
                {
                    lyDo = "client đăng xuất (DANGXUAT)";
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            lyDo = serverToken.IsCancellationRequested
                ? "server dừng"
                : $"im lặng quá {_cauHinh.ThoiGianImLang.TotalSeconds:0} giây";
        }
        catch (ThongDiepQuaDaiException)
        {
            lyDo = "gửi thông điệp quá 64 KiB, Server đóng kết nối";
        }
        catch (EndOfStreamException)
        {
            // Phải đứng trước IOException vì là lớp con của nó.
            lyDo = "client ngắt giữa chừng khi đang gửi dở một thông điệp";
        }
        catch (IOException ex)
        {
            lyDo = $"mất kết nối đột ngột ({MoTaLoiMang(ex)})";
        }
        catch (SocketException ex)
        {
            lyDo = $"mất kết nối đột ngột ({ex.SocketErrorCode})";
        }
        catch (ObjectDisposedException)
        {
            lyDo = "kết nối đã bị đóng";
        }
        finally
        {
            // Luôn dọn dẹp dù thoát bằng cách nào: gỡ khỏi danh sách rồi đóng socket.
            if (phien is not null)
            {
                _phien.TryRemove(phien.Id, out _);
                phien.Dispose();
            }
            else
            {
                client.Dispose();
            }

            ServerLog.Ghi("TCP", $"- {phien?.Id ?? "?"} rời đi: {lyDo} (đang nối: {_phien.Count})");
        }
    }

    /// <summary>
    /// Bật TCP KeepAlive với thời gian ngắn để Server tự phát hiện client mất mạng đột ngột (tắt Wi-Fi, rút cáp).
    /// Không có bước này, Windows mặc định chờ rất lâu (hàng giờ) nên ca kiểm thử "mất mạng" không đạt đúng lúc.
    /// </summary>
    private void BatKeepAlive(TcpClient client)
    {
        Socket socket = client.Client;
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        try
        {
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, _cauHinh.KeepAliveChoGiay);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, _cauHinh.KeepAliveCachGiay);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, _cauHinh.KeepAliveSoLan);
        }
        catch (SocketException ex)
        {
            ServerLog.Ghi("CANH BAO",
                $"Không chỉnh được thông số KeepAlive ({ex.SocketErrorCode}); hệ điều hành dùng mặc định nên có thể rất lâu mới phát hiện rớt mạng.");
        }
    }

    /// <summary>Liệt kê địa chỉ IPv4 của các card mạng đang bật (trừ loopback) để người dùng biết nhập IP nào vào Client.</summary>
    private static List<string> LayDiaChiMangNoiBo()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .ToList();
        }
        catch (NetworkInformationException)
        {
            return new List<string>();   // chỉ là thông tin phụ, lấy không được thì bỏ qua
        }
    }

    /// <summary>Lấy mã lỗi socket nằm bên trong IOException (nếu có) để log dễ đọc: TimedOut, ConnectionReset...</summary>
    private static string MoTaLoiMang(IOException ex) =>
        ex.InnerException is SocketException se ? se.SocketErrorCode.ToString() : "IOException";
}

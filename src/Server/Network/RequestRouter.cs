using NT106.BT3.Server.Logging;
using Shared;

namespace NT106.BT3.Server.Network;

/// <summary>
/// Bộ phân tích và điều phối lệnh phía Server (Bước 2.3).
/// Nhận chuỗi thông điệp từ TcpServer, phân tích thành RequestMessage,
/// gọi handler tương ứng (DANGKY, DANGNHAP, LAYTHONGTIN) hoặc xử lý trực tiếp DANGXUAT.
/// </summary>
internal sealed class RequestRouter : IMessageProcessor
{
    public delegate Task<ResponseMessage> CommandHandlerAsync(
        RequestMessage request,
        ClientSession phien,
        CancellationToken token);

    private readonly CommandHandlerAsync _xuLyDangKy;
    private readonly CommandHandlerAsync _xuLyDangNhap;
    private readonly CommandHandlerAsync _xuLyLayThongTin;

    public RequestRouter(
        CommandHandlerAsync? xuLyDangKy = null,
        CommandHandlerAsync? xuLyDangNhap = null,
        CommandHandlerAsync? xuLyLayThongTin = null)
    {
        _xuLyDangKy = xuLyDangKy ?? DangKyMacDinhAsync;
        _xuLyDangNhap = xuLyDangNhap ?? DangNhapGiaLapAsync;
        _xuLyLayThongTin = xuLyLayThongTin ?? LayThongTinGiaLapAsync;
    }

    public async Task<string?> XuLyAsync(ClientSession phien, string thongDiep, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(thongDiep))
        {
            ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] gửi gói tin rỗng.");
            return new ResponseMessage(ProtocolConstants.CodeInvalidFormat, "Thông điệp rỗng.").Serialize();
        }

        RequestMessage request;
        try
        {
            request = RequestMessage.Parse(thongDiep);
        }
        catch (Exception)
        {
            ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] gửi gói tin sai định dạng.");
            return new ResponseMessage(ProtocolConstants.CodeInvalidFormat, "Định dạng thông điệp không hợp lệ.").Serialize();
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] thiếu mã lệnh.");
            return new ResponseMessage(ProtocolConstants.CodeInvalidFormat, "Thiếu mã lệnh yêu cầu.").Serialize();
        }

        ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] ({phien.TenDangNhap ?? phien.DiaChiClient}) -> Lệnh: {request.Command}");

        try
        {
            ResponseMessage response = request.Command switch
            {
                ProtocolConstants.CmdRegister => await _xuLyDangKy(request, phien, token),
                ProtocolConstants.CmdLogin => await _xuLyDangNhap(request, phien, token),
                ProtocolConstants.CmdGetInfo => await XuLyLayThongTinAsync(request, phien, token),
                ProtocolConstants.CmdLogout => XuLyDangXuat(phien),
                _ => XuLyLenhLa(phien, request.Command)
            };

            return response.Serialize();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Không ghi mật khẩu/token và không lộ tên ngoại lệ C# về Client
            ServerLog.Ghi("LOI", $"Lỗi nội bộ khi xử lý lệnh {request.Command} cho phiên [{phien.Id}].");
            return new ResponseMessage(
                ProtocolConstants.CodeSystemError,
                "Lỗi hệ thống khi xử lý yêu cầu trên Server.").Serialize();
        }
    }

    private async Task<ResponseMessage> XuLyLayThongTinAsync(
        RequestMessage request,
        ClientSession phien,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(phien.TenDangNhap))
        {
            ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] gọi {ProtocolConstants.CmdGetInfo} khi chưa đăng nhập.");
            return new ResponseMessage(
                ProtocolConstants.CodeUnauthorized,
                "Bạn chưa đăng nhập. Vui lòng đăng nhập trước.");
        }

        return await _xuLyLayThongTin(request, phien, token);
    }

    /// <summary>
    /// Xử lý lệnh DANGXUAT (Bước 6 của đề):
    /// Đánh dấu YeuCauDong = true để TcpServer gửi phản hồi xong sẽ đóng kết nối
    /// và gỡ phiên khỏi ConcurrentDictionary trong khối finally.
    /// </summary>
    private static ResponseMessage XuLyDangXuat(ClientSession phien)
    {
        string nhanHienThi = string.IsNullOrWhiteSpace(phien.TenDangNhap)
            ? $"khách ({phien.DiaChiClient})"
            : $"tài khoản '{phien.TenDangNhap}'";

        phien.TenDangNhap = null;
        phien.YeuCauDong = true;

        ServerLog.Ghi("DANGXUAT", $"Phiên [{phien.Id}] - {nhanHienThi} đã đăng xuất và yêu cầu đóng kết nối.");

        return new ResponseMessage(ProtocolConstants.CodeSuccess, "Đăng xuất thành công.");
    }

    private static ResponseMessage XuLyLenhLa(ClientSession phien, string command)
    {
        ServerLog.Ghi("ROUTER", $"Phiên [{phien.Id}] gửi lệnh không hỗ trợ: {command}");
        return new ResponseMessage(
            ProtocolConstants.CodeInvalidFormat,
            $"Lệnh '{command}' không được hệ thống hỗ trợ.");
    }

    // --- Các handler giả lập mặc định dùng để chạy/test độc lập khi chưa ghép handler thật ---

    private static Task<ResponseMessage> DangKyMacDinhAsync(
        RequestMessage request,
        ClientSession phien,
        CancellationToken token)
    {
        if (request.Args.Length < 5)
        {
            return Task.FromResult(new ResponseMessage(
                ProtocolConstants.CodeInvalidFormat,
                "Thiếu thông tin đăng ký (cần: Username|Password|Email|FullName|Birthday)."));
        }

        string username = request.Args[0].Trim();
        ServerLog.Ghi("DANGKY", $"[Mock] Phiên [{phien.Id}] đăng ký tài khoản '{username}'.");
        return Task.FromResult(new ResponseMessage(
            ProtocolConstants.CodeSuccess,
            "Đăng ký tài khoản thành công (Mock)."));
    }

    private static Task<ResponseMessage> DangNhapGiaLapAsync(
        RequestMessage request,
        ClientSession phien,
        CancellationToken token)
    {
        if (request.Args.Length < 2 ||
            string.IsNullOrWhiteSpace(request.Args[0]) ||
            string.IsNullOrWhiteSpace(request.Args[1]))
        {
            return Task.FromResult(new ResponseMessage(
                ProtocolConstants.CodeInvalidFormat,
                "Thiếu tên đăng nhập hoặc mật khẩu."));
        }

        string username = request.Args[0].Trim();
        phien.TenDangNhap = username;
        ServerLog.Ghi("DANGNHAP", $"[Mock] Phiên [{phien.Id}] đăng nhập thành công với tài khoản '{username}'.");

        return Task.FromResult(new ResponseMessage(
            ProtocolConstants.CodeSuccess,
            "Đăng nhập thành công (Mock).",
            username));
    }

    private static Task<ResponseMessage> LayThongTinGiaLapAsync(
        RequestMessage request,
        ClientSession phien,
        CancellationToken token)
    {
        string username = phien.TenDangNhap!;
        ServerLog.Ghi("LAYTHONGTIN", $"[Mock] Phiên [{phien.Id}] lấy thông tin tài khoản '{username}'.");

        return Task.FromResult(new ResponseMessage(
            ProtocolConstants.CodeSuccess,
            "Lấy thông tin người dùng thành công (Mock).",
            username,
            $"{username}@example.com",
            "Người dùng thử nghiệm",
            "2000-01-01"));
    }
}
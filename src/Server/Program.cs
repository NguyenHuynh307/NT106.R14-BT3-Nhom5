using System.Net.Sockets;
using System.Text;
using NT106.BT3.Server.Data;
using NT106.BT3.Server.Handlers;
using NT106.BT3.Server.Logging;
using NT106.BT3.Server.Network;
using NT106.BT3.Server.Security;
using NT106.BT3.Shared.Data;
using NT106.BT3.Shared.Security;
using Shared;

namespace NT106.BT3.Server;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        CauHinhServer cauHinh;
        try
        {
            cauHinh = CauHinhServer.TuThamSo(args);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            return 2;
        }

        using var dung = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            dung.Cancel();
        };

        if (!Console.IsInputRedirected)
        {
            _ = Task.Run(() =>
            {
                Console.ReadLine();
                dung.Cancel();
            });
        }

        var dbHelper = new DatabaseHelper();
        IUserRepository userRepo = new UserRepository(dbHelper);
        IPasswordHasher hasher = new MatKhau();

        var dangKyHandler = new DangKyHandler(userRepo, hasher);
        var layThongTinHandler = new LayThongTinHandler(userRepo);
        var dangNhapHandler = new DangNhapHandler(userRepo, hasher, layThongTinHandler);

        IMessageProcessor boXuLy = new RequestRouter(
            xuLyDangKy: async (req, _, token) =>
            {
                DateTime? birthday = null;
                if (!string.IsNullOrWhiteSpace(req.Args[4]))
                {
                    if (!DateTime.TryParse(req.Args[4], out DateTime parsedDate))
                    {
                        return new ResponseMessage(
                            ProtocolConstants.CodeInvalidFormat,
                            "Ngày sinh không hợp lệ (định dạng yyyy-MM-dd).");
                    }
                    birthday = parsedDate.Date;
                }

                var r = await dangKyHandler.XuLyAsync(
                    new DangKyRequest(req.Args[0], req.Args[1], req.Args[2], req.Args[3], birthday),
                    token).ConfigureAwait(false);
                return new ResponseMessage(MapCode(r.Code), r.Message);
            },
            xuLyDangNhap: async (req, phien, token) =>
            {
                var r = await dangNhapHandler.XuLyAsync(req.Args[0], req.Args[1], phien, token).ConfigureAwait(false);
                if (r.Success && r.Data is not null)
                {
                    return new ResponseMessage(
                        ProtocolConstants.CodeSuccess,
                        r.Message,
                        r.Data.Username,
                        r.Data.Email ?? string.Empty,
                        r.Data.FullName,
                        r.Data.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty);
                }
                return new ResponseMessage(MapCode(r.Code), r.Message);
            },
            xuLyLayThongTin: async (_, phien, token) =>
            {
                var r = await layThongTinHandler.XuLyAsync(phien.TenDangNhap, token).ConfigureAwait(false);
                if (r.Success && r.Data is not null)
                {
                    return new ResponseMessage(
                        ProtocolConstants.CodeSuccess,
                        r.Message,
                        r.Data.Username,
                        r.Data.Email ?? string.Empty,
                        r.Data.FullName,
                        r.Data.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty);
                }
                return new ResponseMessage(MapCode(r.Code), r.Message);
            });

        var server = new TcpServer(boXuLy, cauHinh);
        var udpDiscovery = new UdpDiscoveryServer(cauHinh.Cong);

        Task chayTcp;
        try
        {
            chayTcp = server.StartAsync(dung.Token);
        }
        catch (SocketException ex)
        {
            ServerLog.Ghi("LOI", $"Không mở được cổng {cauHinh.Cong} ({ex.SocketErrorCode}). Có thể Server cũ chưa tắt hoặc cổng đang bị chương trình khác dùng.");
            return 1;
        }

        Task chayUdp = udpDiscovery.StartAsync(dung.Token);

        ServerLog.Ghi("SERVER", "Nhấn Enter hoặc Ctrl+C để dừng Server.");
        await Task.WhenAll(chayTcp, chayUdp);
        return 0;
    }

    private static int MapCode(string code) => code switch
    {
        "SUCCESS" or "OK" => ProtocolConstants.CodeSuccess,
        "ERR_INVALID_INPUT" or "BAD_REQUEST" or "ERR_INVALID_FORMAT" => ProtocolConstants.CodeInvalidFormat,
        "ERR_USERNAME_EXISTS" => ProtocolConstants.CodeUserExists,
        "INVALID_CREDENTIALS" or "ERR_AUTH_FAILED" => ProtocolConstants.CodeAuthFailed,
        "ERR_UNAUTHORIZED" or "UNAUTHORIZED" or "ERR_USER_NOT_FOUND" => ProtocolConstants.CodeUnauthorized,
        _ => ProtocolConstants.CodeSystemError
    };
}
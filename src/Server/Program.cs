using NT106.BT3.Server.Handlers;
using NT106.BT3.Server.Logging;
using NT106.BT3.Server.Network;
using NT106.BT3.Server.Security;
using NT106.BT3.Shared.Data;
using NT106.BT3.Shared.Security;
using Shared;
using System.Net.Sockets;
using System.Text;

namespace NT106.BT3.Server;

internal static class Program
{
    static int MapCode(string code) => code switch
    {
        "SUCCESS" or "OK" => ProtocolConstants.CodeSuccess,
        "ERR_USERNAME_EXISTS" => ProtocolConstants.CodeUserExists,
        "INVALID_CREDENTIALS" => ProtocolConstants.CodeAuthFailed,
        "ERR_UNAUTHORIZED" or "UNAUTHORIZED" => ProtocolConstants.CodeUnauthorized,
        "TOKEN_EXPIRED" => ProtocolConstants.CodeTokenExpired,
        "ERR_INVALID_INPUT" or "BAD_REQUEST" or "ERR_INVALID_FORMAT" => ProtocolConstants.CodeInvalidFormat,
        _ => ProtocolConstants.CodeSystemError
    };

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        CauHinhServer cauHinh;
        try { cauHinh = CauHinhServer.TuThamSo(args); }
        catch (ArgumentException ex) { Console.WriteLine(ex.Message); return 2; }

        using var dung = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; dung.Cancel(); };
        if (!Console.IsInputRedirected) { _ = Task.Run(() => { Console.ReadLine(); dung.Cancel(); }); }

        var dbHelper = new NT106.BT3.Server.Data.DatabaseHelper();
        IUserRepository userRepo = new NT106.BT3.Server.Data.UserRepository(dbHelper);
        IPasswordHasher hasher = new NT106.BT3.Server.Security.MatKhau();
        // Thời hạn token đọc từ biến môi trường NT106_TOKEN_SECONDS (giây); mặc định 1800 giây (30 phút).
        int tokenGiay = int.TryParse(Environment.GetEnvironmentVariable("NT106_TOKEN_SECONDS"), out int g) && g > 0 ? g : 1800;
        var tokenManager = new SessionTokenManager(TimeSpan.FromSeconds(tokenGiay));
        ServerLog.Ghi("SERVER", $"Token phiên có hạn {tokenGiay} giây.");
        
        var dangKyHandler = new DangKyHandler(userRepo, hasher);
        var layThongTinHandler = new LayThongTinHandler(userRepo);
        var dangNhapHandler = new DangNhapHandler(userRepo, hasher, layThongTinHandler, tokenManager);

        var router = new RequestRouter(
            async (req, phien, token) => {
                var r = await dangKyHandler.XuLyAsync(new DangKyRequest(req.Args[0], req.Args[1], req.Args[2], req.Args[3], DateTime.TryParse(req.Args[4], out var d) ? d : null), token);
                return new ResponseMessage(MapCode(r.Code), r.Message);
            },
            async (req, phien, token) => {
                var r = await dangNhapHandler.XuLyAsync(req.Args[0], req.Args[1], phien, token);
                if (r.Success && r.Data is not null) { 
                    return new ResponseMessage(MapCode(r.Code), r.Message, r.Data[0], r.Data[1], r.Data[2], r.Data[3], r.Data[4]); 
                }
                return new ResponseMessage(MapCode(r.Code), r.Message);
            },
            async (req, phien, token) => {
                var r = await layThongTinHandler.XuLyAsync(phien.TenDangNhap, token);
                if (r.Success && r.Data is not null) { 
                    var data = r.Data; 
                    return new ResponseMessage(MapCode(r.Code), r.Message, data.Username, data.Email ?? "", data.FullName, data.Birthday?.ToString("yyyy-MM-dd") ?? ""); 
                }
                return new ResponseMessage(MapCode(r.Code), r.Message);
            },
            tokenManager
        );

        var server = new TcpServer(router, cauHinh);
        var udpDiscovery = new UdpDiscoveryServer(cauHinh.Cong);
        Task chay;
        try { chay = server.StartAsync(dung.Token); }
        catch (SocketException ex) { ServerLog.Ghi("LOI", $"Không mở được cổng {cauHinh.Cong} ({ex.SocketErrorCode})."); return 1; }

        ServerLog.Ghi("SERVER", "Nhấn Enter hoặc Ctrl+C để dừng Server.");
        Task chayUdp = udpDiscovery.StartAsync(dung.Token);
        await Task.WhenAll(chay, chayUdp);
        return 0;
    }
}
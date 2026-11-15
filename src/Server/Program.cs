using System.Net.Sockets;
using System.Text;
using NT106.BT3.Server.Logging;
using NT106.BT3.Server.Network;

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

        // Dừng Server tử tế bằng CancellationToken: Ctrl+C hoặc Enter đều chạy về cùng một đường (Mục III.8 ý 5).
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;   // không để tiến trình bị giết đột ngột; để vòng lặp tự dừng và gọi Stop()
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

        // \u0110I\u1EC2M N\u1ED0I NGHI\u1EC6P V\u1EE4: nh\u1EADn RequestRouter l\u00E0m b\u1ED9 x\u1EED l\u00FD l\u1EC7nh m\u1EB7c \u0111\u1ECBnh.
        // C\u00E1c handler nghi\u1EC7p v\u1EE5 th\u1EADt (DANGKY, DANGNHAP, LAYTHONGTIN) s\u1EBD \u0111\u01B0\u1EE3c truy\u1EC1n v\u00E0o qua delegate khi gh\u00E9p.
        IMessageProcessor boXuLy = new RequestRouter();

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
}
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

        // ĐIỂM NỐI NGHIỆP VỤ: hiện là hàm echo để chạy thử đường truyền (Bước 4 của đề).
        // Khi Hoàng Anh xong RequestRouter (Bước 2.3), chỉ đổi đúng dòng này.
        IMessageProcessor boXuLy = new EchoMessageProcessor();

        var server = new TcpServer(boXuLy, cauHinh);

        Task chay;
        try
        {
            chay = server.StartAsync(dung.Token);
        }
        catch (SocketException ex)
        {
            ServerLog.Ghi("LOI", $"Không mở được cổng {cauHinh.Cong} ({ex.SocketErrorCode}). Có thể Server cũ chưa tắt hoặc cổng đang bị chương trình khác dùng.");
            return 1;
        }

        ServerLog.Ghi("SERVER", "Nhấn Enter hoặc Ctrl+C để dừng Server.");
        await chay;
        return 0;
    }
}

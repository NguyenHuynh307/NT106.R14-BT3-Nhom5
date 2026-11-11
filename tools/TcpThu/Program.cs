using System.Net.Sockets;
using System.Text;

// =====================================================================
// TCP THỬ - chạy thử đường truyền của Server (Bước 4 của đề) khi Router/GUI chưa có.
// Server phải đang chạy với bộ xử lý echo (bản mặc định của Bước 1.2).
//
//   dotnet run --project tools/TcpThu                      chạy toàn bộ kịch bản, in PASS/FAIL
//   dotnet run --project tools/TcpThu -- 192.168.1.10 5000  thử Server ở máy khác (ca LAN, Test 2)
//   dotnet run --project tools/TcpThu -- --giu 2            mở 2 kết nối và GIỮ MỞ (chụp log "đang nối: 2",
//                                                           rồi tắt Wi-Fi máy này để thử Test 4)
//   dotnet run --project tools/TcpThu -- --rst              đóng đột ngột kiểu RST (giống kill tiến trình)
//
// Tự đóng khung bằng tay (một dòng UTF-8 kết thúc LF), cố ý gửi "sai cách" để kiểm tra Server.
// =====================================================================

Console.OutputEncoding = Encoding.UTF8;

string host = args.FirstOrDefault(a => !a.StartsWith("--") && !int.TryParse(a, out _)) ?? "127.0.0.1";
int port = args.Select(a => int.TryParse(a, out int p) ? p : 0).FirstOrDefault(p => p >= 1024);
if (port == 0) port = 5000;

if (args.Contains("--giu"))
{
    int soKetNoi = int.TryParse(args.SkipWhile(a => a != "--giu").Skip(1).FirstOrDefault(), out int n) ? n : 2;
    await GiuKetNoiAsync(soKetNoi);
    return 0;
}

if (args.Contains("--rst"))
{
    using var c = await NoiAsync();
    c.Client.LingerState = new LingerOption(true, 0);   // đóng bằng RST thay vì FIN
    Console.WriteLine("Đã nối. Đóng đột ngột (RST). Xem log Server: dòng '- ... rời đi: mất kết nối đột ngột'.");
    return 0;
}

Console.WriteLine($"=== TCP THỬ -> {host}:{port} ===\n");
var ketQua = new List<(string Ten, bool Dat, string GhiChu)>();

await Chay("T1  Bước 4: 2 client đồng thời, mỗi client 20 chuỗi liên tiếp -> đủ 20, đúng thứ tự, không dính", T1_HaiClient);
await Chay("T2  Dính gói: 3 thông điệp trong MỘT lần ghi -> đủ 3 phản hồi đúng thứ tự", T2_DinhGoi);
await Chay("T3  Phân mảnh: 1 thông điệp tiếng Việt gửi từng 3 byte -> ghép đúng", T3_PhanManh);
await Chay("T4  Kiểu rời đi 1: gửi DANGXUAT -> nhận OK rồi Server đóng kết nối", T4_DangXuat);
await Chay("T5  Kiểu rời đi 2: client đóng đàng hoàng -> Server vẫn nhận client mới", T5_DongDangHoang);

Console.WriteLine("\n--------------------------------------------------");
foreach (var (ten, dat, ghiChu) in ketQua)
    Console.WriteLine($"{(dat ? "PASS" : "FAIL")}  {ten}{(ghiChu.Length > 0 ? "  -> " + ghiChu : "")}");
int loi = ketQua.Count(k => !k.Dat);
Console.WriteLine($"\nTổng: {ketQua.Count - loi}/{ketQua.Count} đạt.");
Console.WriteLine("Còn lại cần xem log Server: chạy thêm --rst (kiểu rời đi RST) và --giu 2 rồi tắt Wi-Fi (kiểu rời đi 3).");
return loi == 0 ? 0 : 1;

// ---------------------------------------------------------------------
async Task Chay(string ten, Func<Task<string>> kichBan)
{
    try
    {
        ketQua.Add((ten, true, await kichBan()));
    }
    catch (IOException ex)
    {
        ketQua.Add((ten, false, "IOException: " + ex.Message));
    }
    catch (SocketException ex)
    {
        ketQua.Add((ten, false, "Không nối được Server: " + ex.SocketErrorCode));
    }
    catch (InvalidOperationException ex)
    {
        ketQua.Add((ten, false, ex.Message));
    }
    catch (OperationCanceledException)
    {
        ketQua.Add((ten, false, "Quá 5 giây không nhận được phản hồi"));
    }
}

async Task<TcpClient> NoiAsync()
{
    var c = new TcpClient { NoDelay = true };
    await c.ConnectAsync(host, port);
    return c;
}

static async Task<string?> DocDongAsync(StreamReader reader)
{
    using var han = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    return await reader.ReadLineAsync(han.Token);
}

static void Kiem(bool dung, string loiNeuSai)
{
    if (!dung) throw new InvalidOperationException(loiNeuSai);
}

async Task GiuKetNoiAsync(int soKetNoi)
{
    var danhSach = new List<TcpClient>();
    for (int i = 0; i < soKetNoi; i++) danhSach.Add(await NoiAsync());
    Console.WriteLine($"Đã mở {soKetNoi} kết nối tới {host}:{port} và đang giữ mở. Xem log Server: 'đang nối: {soKetNoi}'.");
    Console.WriteLine("Chụp ảnh log, rồi (Test 4) tắt Wi-Fi/rút cáp máy này. Nhấn Enter để đóng.");
    Console.ReadLine();
    foreach (var c in danhSach) c.Dispose();
}

// ---- Kịch bản ----
async Task<string> MotClientAsync(string ten)
{
    using var c = await NoiAsync();
    using var luong = c.GetStream();
    using var doc = new StreamReader(luong, new UTF8Encoding(false));

    var daGui = new List<string>();
    for (int i = 1; i <= 20; i++)
    {
        string noiDung = $"Client {ten} - chuỗi {i:00} - Nguyễn Văn Ạ, đau họng ✓";
        daGui.Add(noiDung);
        // Gửi liên tiếp, KHÔNG chờ phản hồi: cố ý để các thông điệp dễ dính gói.
        await luong.WriteAsync(Encoding.UTF8.GetBytes(noiDung + "\n"));
    }

    for (int i = 0; i < 20; i++)
    {
        string? dong = await DocDongAsync(doc);
        Kiem(dong is not null, $"Client {ten}: Server đóng kết nối sau {i} phản hồi");
        Kiem(dong == "ECHO:" + daGui[i], $"Client {ten}: phản hồi {i + 1} sai: '{dong}'");
    }
    return $"{ten}: 20/20";
}

async Task<string> T1_HaiClient()
{
    string[] kq = await Task.WhenAll(MotClientAsync("A"), MotClientAsync("B"));
    return string.Join(", ", kq);
}

async Task<string> T2_DinhGoi()
{
    using var c = await NoiAsync();
    using var luong = c.GetStream();
    using var doc = new StreamReader(luong, new UTF8Encoding(false));

    byte[] goi = Encoding.UTF8.GetBytes("MOT\nHAI\nBA\n");   // ba thông điệp, MỘT lần ghi
    await luong.WriteAsync(goi);

    foreach (string mong in new[] { "ECHO:MOT", "ECHO:HAI", "ECHO:BA" })
    {
        string? dong = await DocDongAsync(doc);
        Kiem(dong == mong, $"Mong '{mong}', nhận '{dong}'");
    }
    return "MOT, HAI, BA đúng thứ tự";
}

async Task<string> T3_PhanManh()
{
    const string noiDung = "Đặng Thị Hạnh bị ho, sốt 38,5°C ✓";
    using var c = await NoiAsync();
    using var luong = c.GetStream();
    using var doc = new StreamReader(luong, new UTF8Encoding(false));

    byte[] byteGui = Encoding.UTF8.GetBytes(noiDung + "\n");
    for (int i = 0; i < byteGui.Length; i += 3)   // cắt từng 3 byte, kể cả giữa một chữ có dấu
    {
        await luong.WriteAsync(byteGui.AsMemory(i, Math.Min(3, byteGui.Length - i)));
        await luong.FlushAsync();
        await Task.Delay(8);   // để các mảnh tới Server thành nhiều lần đọc riêng
    }

    string? dong = await DocDongAsync(doc);
    Kiem(dong == "ECHO:" + noiDung, $"Sai nội dung: '{dong}'");
    return $"{byteGui.Length} byte chia {(byteGui.Length + 2) / 3} mảnh";
}

async Task<string> T4_DangXuat()
{
    using var c = await NoiAsync();
    using var luong = c.GetStream();
    using var doc = new StreamReader(luong, new UTF8Encoding(false));

    await luong.WriteAsync(Encoding.UTF8.GetBytes("DANGXUAT\n"));
    string? dong = await DocDongAsync(doc);
    Kiem(dong == "OK:DANGXUAT", $"Mong 'OK:DANGXUAT', nhận '{dong}'");

    string? tiepTheo = await DocDongAsync(doc);   // Server đóng -> đọc ra null
    Kiem(tiepTheo is null, "Server chưa đóng kết nối sau DANGXUAT");
    return "đã đóng; xem log: 'rời đi: client đăng xuất'";
}

async Task<string> T5_DongDangHoang()
{
    using (var c1 = await NoiAsync())
    {
        await c1.GetStream().WriteAsync(Encoding.UTF8.GetBytes("xin chao\n"));
    }   // using đóng đàng hoàng (FIN)

    using var c2 = await NoiAsync();
    using var luong = c2.GetStream();
    using var doc = new StreamReader(luong, new UTF8Encoding(false));
    await luong.WriteAsync(Encoding.UTF8.GetBytes("vẫn sống\n"));
    string? dong = await DocDongAsync(doc);
    Kiem(dong == "ECHO:vẫn sống", $"Sai phản hồi: '{dong}'");
    return "client mới vẫn được phục vụ";
}

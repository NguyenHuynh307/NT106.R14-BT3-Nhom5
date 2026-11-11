using System.Net;

namespace NT106.BT3.Server.Network;

/// <summary>
/// Các thông số của Server TCP. Mặc định dùng được ngay; đổi bằng tham số dòng lệnh:
///   Server.exe --port 5000 --idle 0
/// </summary>
internal sealed class CauHinhServer
{
    /// <summary>Cổng TCP. Phải nằm trong dải 1024-49151 (Mục III.2a). Hoàng Anh chốt số cuối cùng trong tài liệu giao thức.</summary>
    public int Cong { get; init; } = 5000;

    /// <summary>IPAddress.Any để máy khác trong mạng LAN nối vào được (Mục III.8 ý 1).</summary>
    public IPAddress DiaChiNghe { get; init; } = IPAddress.Any;

    // ---- TCP KeepAlive: cách Server tự phát hiện client mất mạng đột ngột (tắt Wi-Fi, rút cáp) ----
    // Khi đó client KHÔNG gửi gì để báo rời đi. Nếu để mặc định của hệ điều hành thì phải chờ hàng giờ.
    // Với bộ số dưới đây: im lặng 10 giây -> bắt đầu dò, cứ 5 giây dò một lần, 3 lần không ai đáp -> rớt.
    // Thời gian phát hiện ước tính = 10 + 5 x 3 = khoảng 25 giây.

    /// <summary>Số giây kết nối im lặng trước khi gửi gói dò đầu tiên.</summary>
    public int KeepAliveChoGiay { get; init; } = 10;

    /// <summary>Số giây giữa hai gói dò liên tiếp.</summary>
    public int KeepAliveCachGiay { get; init; } = 5;

    /// <summary>Số gói dò không được đáp lại thì coi là rớt mạng.</summary>
    public int KeepAliveSoLan { get; init; } = 3;

    /// <summary>
    /// Lớp bảo vệ thứ hai: cắt client im lặng quá thời gian này (tính từ lần gửi cuối).
    /// Mặc định 0 = TẮT, vì giao thức BT3 chỉ có 4 lệnh, không có PING nên người dùng đứng yên ở Form chính
    /// là bình thường. Bật bằng --idle 600 nếu muốn tự dọn client ngủ quên sau 10 phút.
    /// </summary>
    public TimeSpan ThoiGianImLang { get; init; } = TimeSpan.Zero;

    /// <summary>Đọc tham số dòng lệnh. Ném ArgumentException (thông báo tiếng Việt) nếu giá trị không hợp lệ.</summary>
    public static CauHinhServer TuThamSo(string[] args)
    {
        int cong = 5000;
        int imLangGiay = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                    cong = DocSo(args, ++i, "--port");
                    if (cong < 1024 || cong > 49151)
                        throw new ArgumentException("Cổng phải nằm trong dải 1024 đến 49151.");
                    break;

                case "--idle":
                    imLangGiay = DocSo(args, ++i, "--idle");
                    if (imLangGiay < 0)
                        throw new ArgumentException("--idle phải là số giây không âm (0 = tắt).");
                    break;

                default:
                    throw new ArgumentException($"Tham số không hợp lệ: {args[i]}. Dùng: --port <số> --idle <giây>");
            }
        }

        return new CauHinhServer
        {
            Cong = cong,
            ThoiGianImLang = TimeSpan.FromSeconds(imLangGiay)
        };
    }

    private static int DocSo(string[] args, int viTri, string ten)
    {
        if (viTri >= args.Length || !int.TryParse(args[viTri], out int so))
            throw new ArgumentException($"Thiếu hoặc sai giá trị số cho {ten}.");
        return so;
    }
}

using System.Text;
using Shared;

Console.OutputEncoding = Encoding.UTF8;

int dat = 0, khongDat = 0;
Encoding enc = ProtocolConstants.MessageEncoding;
int max = ProtocolConstants.MaxMessageBytes;

void Kiem(string ten, bool ok, string chiTiet = "")
{
    if (ok) { dat++; Console.WriteLine($"[ĐẠT]       {ten}"); }
    else { khongDat++; Console.WriteLine($"[KHÔNG ĐẠT] {ten} {chiTiet}"); }
}

async Task<Exception?> BatLoi(Func<Task> viec)
{
    try { await viec(); return null; }
    catch (Exception ex) { return ex; }
}

// T1: hai thông điệp dính trong một lần ghi
{
    var ms = new MemoryStream(enc.GetBytes("DANGNHAP|a|b\nLAYTHONGTIN\n"));
    var m1 = await SocketStreamHelper.ReceiveLineAsync(ms);
    var m2 = await SocketStreamHelper.ReceiveLineAsync(ms);
    var m3 = await SocketStreamHelper.ReceiveLineAsync(ms);
    Kiem("T1 hai thông điệp dính nhau được tách đúng, hết luồng trả null",
        m1 == "DANGNHAP|a|b" && m2 == "LAYTHONGTIN" && m3 == null,
        $"(nhận: '{m1}', '{m2}', '{m3}')");
}

// T2: tiếng Việt có dấu, UTF-8 không BOM, kết thúc bằng LF
{
    string goc = "DANGKY|nguyenvana|Nguyễn Quốc Nhật Minh|mật khẩu";
    var ms = new MemoryStream();
    await SocketStreamHelper.SendLineAsync(ms, goc);
    byte[] bytes = ms.ToArray();
    ms.Position = 0;
    var nhan = await SocketStreamHelper.ReceiveLineAsync(ms);
    Kiem("T2 tiếng Việt có dấu đi và về nguyên vẹn, không BOM, kết thúc LF",
        nhan == goc && bytes[0] != 0xEF && bytes[^1] == (byte)'\n',
        $"(nhận: '{nhan}')");
}

// T3: kết nối đóng giữa chừng một thông điệp
{
    var ex = await BatLoi(() => SocketStreamHelper.ReceiveLineAsync(new MemoryStream(enc.GetBytes("DANGKY|abc"))));
    Kiem("T3 EOF giữa chừng ném EndOfStreamException (là IOException), không trả thông điệp giả",
        ex is EndOfStreamException && ex is IOException,
        $"(nhận: {ex?.GetType().Name ?? "không có lỗi"})");
}

// T4: kết nối đóng sạch
{
    var r = await SocketStreamHelper.ReceiveLineAsync(new MemoryStream(Array.Empty<byte>()));
    Kiem("T4 luồng rỗng (đóng sạch) trả null", r == null);
}

// T5: dòng rỗng khác với EOF
{
    var r = await SocketStreamHelper.ReceiveLineAsync(new MemoryStream(enc.GetBytes("\n")));
    Kiem("T5 dòng rỗng trả chuỗi rỗng, không phải null", r == "", $"(nhận: {(r == null ? "null" : $"'{r}'")})");
}

// T6: kết thúc CRLF
{
    var r = await SocketStreamHelper.ReceiveLineAsync(new MemoryStream(enc.GetBytes("HELLO\r\n")));
    Kiem("T6 kết thúc CRLF được chấp nhận, bỏ 1 CR cuối", r == "HELLO", $"(nhận: '{r}')");
}

// T7: đúng giới hạn thì nhận được
{
    byte[] data = new byte[max + 1];
    Array.Fill(data, (byte)'a');
    data[max] = (byte)'\n';
    var r = await SocketStreamHelper.ReceiveLineAsync(new MemoryStream(data));
    Kiem($"T7 thông điệp đúng {max} byte vẫn được nhận", r != null && r.Length == max,
        $"(độ dài nhận: {r?.Length.ToString() ?? "null"})");
}

// T8: vượt giới hạn 1 byte thì bị từ chối
{
    byte[] data = new byte[max + 2];
    Array.Fill(data, (byte)'a');
    data[max + 1] = (byte)'\n';
    var ex = await BatLoi(() => SocketStreamHelper.ReceiveLineAsync(new MemoryStream(data)));
    Kiem($"T8 thông điệp {max + 1} byte bị từ chối bằng InvalidDataException", ex is InvalidDataException,
        $"(nhận: {ex?.GetType().Name ?? "không có lỗi"})");
}

// T9: gửi nội dung có LF bị từ chối
{
    var ex = await BatLoi(() => SocketStreamHelper.SendLineAsync(new MemoryStream(), "a\nb"));
    Kiem("T9 gửi nội dung chứa LF bị từ chối bằng ArgumentException", ex is ArgumentException,
        $"(nhận: {ex?.GetType().Name ?? "không có lỗi"})");
}

// T10: gửi nội dung có CR bị từ chối
{
    var ex = await BatLoi(() => SocketStreamHelper.SendLineAsync(new MemoryStream(), "a\rb"));
    Kiem("T10 gửi nội dung chứa CR bị từ chối bằng ArgumentException", ex is ArgumentException,
        $"(nhận: {ex?.GetType().Name ?? "không có lỗi"})");
}

// T11: gửi quá giới hạn bị từ chối
{
    var ex = await BatLoi(() => SocketStreamHelper.SendLineAsync(new MemoryStream(), new string('a', max + 1)));
    Kiem($"T11 gửi {max + 1} byte bị từ chối bằng ArgumentException", ex is ArgumentException,
        $"(nhận: {ex?.GetType().Name ?? "không có lỗi"})");
}

// T12: gửi đúng giới hạn rồi nhận lại
{
    string goc = new string('a', max);
    var ms = new MemoryStream();
    await SocketStreamHelper.SendLineAsync(ms, goc);
    ms.Position = 0;
    var r = await SocketStreamHelper.ReceiveLineAsync(ms);
    Kiem($"T12 gửi đúng {max} byte rồi nhận lại nguyên vẹn", r == goc);
}

Console.WriteLine();
Console.WriteLine($"Tổng: {dat} đạt, {khongDat} không đạt.");
return khongDat == 0 ? 0 : 1;
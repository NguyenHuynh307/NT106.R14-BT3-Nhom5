using System.Text;

namespace NT106.BT3.Server.Network;

// =====================================================================
// ĐÓNG KHUNG TẠM THỜI - CHỈ ĐỂ SERVER CHẠY ĐƯỢC TRƯỚC KHI CÓ BẢN CỦA HOÀNG ANH (Bước 2.2).
//
// Quy ước: mỗi thông điệp = một dòng UTF-8 kết thúc bằng LF (0x0A), tối đa 64 KiB.
// Logic lấy từ LfFraming/LfFrameReader của đồ án nhóm (nhánh feature/core-tcp).
//
// KHI HOÀNG ANH XONG: hàm đóng khung nằm trong dự án Shared. Chỉ cần sửa 2 chỗ trong
// ClientSession.cs (DocAsync và GuiAsync) rồi xóa file này. Không file nào khác phải đổi.
// =====================================================================

/// <summary>Thông điệp dài hơn giới hạn cho phép.</summary>
internal sealed class ThongDiepQuaDaiException : Exception
{
    public ThongDiepQuaDaiException()
        : base($"Thông điệp vượt quá {KhungLfTam.ToiDaByte} byte.") { }
}

internal static class KhungLfTam
{
    public const int ToiDaByte = 64 * 1024;

    /// <summary>Ghi một thông điệp: nội dung + một ký tự LF, gom thành MỘT lần ghi.</summary>
    public static async Task GhiAsync(Stream luong, string noiDung, CancellationToken token)
    {
        // Nội dung chứa xuống dòng sẽ bị tách thành hai thông điệp ở phía nhận (lỗi "dính gói" trong Mục V) nên cấm.
        if (noiDung.Contains('\n') || noiDung.Contains('\r'))
            throw new ArgumentException("Thông điệp không được chứa ký tự xuống dòng.", nameof(noiDung));

        int soByte = Encoding.UTF8.GetByteCount(noiDung);
        if (soByte > ToiDaByte)
            throw new ThongDiepQuaDaiException();

        byte[] goi = new byte[soByte + 1];
        Encoding.UTF8.GetBytes(noiDung, 0, noiDung.Length, goi, 0);
        goi[soByte] = (byte)'\n';

        await luong.WriteAsync(goi.AsMemory(), token).ConfigureAwait(false);
        await luong.FlushAsync(token).ConfigureAwait(false);
    }
}

/// <summary>
/// Đọc từng thông điệp (một dòng kết thúc bằng LF). Tự ghép mảnh khi thông điệp tới làm nhiều lần
/// và tự tách khi nhiều thông điệp dính liền trong một lần đọc. Mỗi kết nối dùng MỘT đối tượng riêng.
/// </summary>
internal sealed class BoDocKhungLfTam
{
    private readonly Stream _luong;
    private byte[] _dem = new byte[8192];
    private int _dau;   // byte đầu tiên chưa xử lý
    private int _cuoi;  // vị trí sau byte cuối đã nhận

    public BoDocKhungLfTam(Stream luong) => _luong = luong;

    /// <summary>
    /// Trả về nội dung một thông điệp (đã bỏ LF và CR đứng trước). Trả về null khi bên kia đóng kết nối
    /// đàng hoàng. Ném EndOfStreamException nếu kết nối đóng khi đang nhận dở, ThongDiepQuaDaiException nếu quá 64 KiB.
    /// </summary>
    public async Task<string?> DocDongAsync(CancellationToken token)
    {
        int quetTu = _dau;

        while (true)
        {
            int viTriLf = Array.IndexOf(_dem, (byte)'\n', quetTu, _cuoi - quetTu);
            if (viTriLf >= 0)
            {
                int doDai = viTriLf - _dau;
                if (doDai > 0 && _dem[viTriLf - 1] == (byte)'\r') doDai--;

                if (doDai > KhungLfTam.ToiDaByte)
                    throw new ThongDiepQuaDaiException();

                string dong = Encoding.UTF8.GetString(_dem, _dau, doDai);
                _dau = viTriLf + 1;
                if (_dau == _cuoi) _dau = _cuoi = 0;
                return dong;
            }

            if (_cuoi - _dau > KhungLfTam.ToiDaByte + 1)
                throw new ThongDiepQuaDaiException();

            quetTu = _cuoi;

            if (_cuoi == _dem.Length)
            {
                if (_dau > 0)
                {
                    int daDoi = _dau;
                    Buffer.BlockCopy(_dem, _dau, _dem, 0, _cuoi - _dau);
                    _cuoi -= daDoi;
                    _dau = 0;
                    quetTu -= daDoi;
                }
                else
                {
                    Array.Resize(ref _dem, Math.Min(_dem.Length * 2, KhungLfTam.ToiDaByte + 8192));
                }
            }

            int n = await _luong.ReadAsync(_dem.AsMemory(_cuoi), token).ConfigureAwait(false);
            if (n == 0)
            {
                if (_cuoi > _dau)
                    throw new EndOfStreamException("Kết nối đóng khi đang nhận dở một thông điệp.");
                return null;
            }
            _cuoi += n;
        }
    }
}

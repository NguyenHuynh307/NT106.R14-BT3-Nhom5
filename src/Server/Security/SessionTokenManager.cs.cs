using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NT106.BT3.Server.Security;

internal sealed class SessionTokenManager
{
    private sealed record TokenEntry(string Username, DateTime ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new(StringComparer.Ordinal);
    private readonly TimeSpan _thoiHanMacDinh;

    public SessionTokenManager(TimeSpan? thoiHanMacDinh = null)
    {
        // Mặc định 30 phút 
        _thoiHanMacDinh = thoiHanMacDinh ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Sinh token ngẫu nhiên 256-bit, lưu lại và trả về cho Client.
    /// </summary>
    public string TaoToken(string username)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var entry = new TokenEntry(username, DateTime.UtcNow.Add(_thoiHanMacDinh));
        _tokens[token] = entry;
        return token;
    }

    /// <summary>
    /// Kiểm tra token: trả về trạng thái hợp lệ, hết hạn, hoặc không tồn tại.
    /// </summary>
    public KetQuaKiemTraToken KiemTraVaLayUser(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokens.TryGetValue(token, out var entry))
        {
            return new KetQuaKiemTraToken(TrangThaiToken.KhongHopLe, null);
        }

        if (DateTime.UtcNow >= entry.ExpiresAtUtc)
        {
            _tokens.TryRemove(token, out _); // Dọn dẹp token đã hết hạn
            return new KetQuaKiemTraToken(TrangThaiToken.HetHan, null);
        }

        return new KetQuaKiemTraToken(TrangThaiToken.HopLe, entry.Username);
    }

    public void ThuHoiToken(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _tokens.TryRemove(token, out _);
        }
    }
}

internal enum TrangThaiToken { HopLe, HetHan, KhongHopLe }
internal sealed record KetQuaKiemTraToken(TrangThaiToken TrangThai, string? Username);
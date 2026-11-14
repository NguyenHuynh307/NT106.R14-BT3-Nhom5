using System;
using System.Security.Cryptography;

namespace NT106.BT3.Shared.Security
{

    public interface IPasswordHasher
    {
        (string PasswordHash, string PasswordSalt) Tao(string matKhau);
        bool KiemTra(string matKhau, string saltLuu, string bamLuu);
    }

    /// <summary>
    /// Bản giả (Fake) dùng tạm trong lúc chờ lớp MatKhau của Lê Trọng Phú (Bước 3.1) hoàn thiện.
    /// Vẫn sinh salt ngẫu nhiên 16 byte riêng cho mỗi lần gọi để đảm bảo 2 tài khoản cùng mật khẩu ra 2 hash khác nhau.
    /// </summary>
    public sealed class FakePasswordHasher : IPasswordHasher
    {
        public (string PasswordHash, string PasswordSalt) Tao(string matKhau)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            string saltBase64 = Convert.ToBase64String(saltBytes);
            byte[] fakeHashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{saltBase64}:{matKhau}"));
            return (Convert.ToBase64String(fakeHashBytes), saltBase64);
        }

        public bool KiemTra(string matKhau, string saltLuu, string bamLuu)
        {
            if (string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(saltLuu) || string.IsNullOrEmpty(bamLuu))
            {
                return false;
            }

            byte[] expected = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{saltLuu}:{matKhau}"));
            return Convert.ToBase64String(expected) == bamLuu;
        }
    }
}
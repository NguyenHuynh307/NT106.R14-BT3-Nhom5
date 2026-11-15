using System;
using System.Security.Cryptography;

namespace NT106.BT3.Shared.Security
{

    public interface IPasswordHasher
    {
        (string PasswordHash, string PasswordSalt) Tao(string matKhau);
        bool KiemTra(string matKhau, string saltLuu, string bamLuu);
    }

    }

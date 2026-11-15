using System;
using System.Threading;
using System.Threading.Tasks;
using NT106.BT3.Server.Logging;
using NT106.BT3.Shared.Data;
using NT106.BT3.Shared.Security;

namespace NT106.BT3.Server.Handlers
{
    public sealed record DangKyRequest(
        string Username,
        string Password,
        string? Email,
        string FullName,
        DateTime? Birthday);

    public sealed record HandlerResponse<T>(
        bool Success,
        string Code,
        string Message,
        T? Data = default);

    /// <summary>
    /// Handler xử lý lệnh DANGKY phía Server (Bước 4.2).
    /// - Kiểm tra trùng Username bằng SQL tham số qua IUserRepository.
    /// - Băm mật khẩu + salt ngẫu nhiên bằng IPasswordHasher (MatKhau của Trọng Phú hoặc FakePasswordHasher).
    /// - Ghi xuống bảng dbo.NguoiDung và trả mã lỗi chuẩn theo tài liệu giao thức (không lộ tên ngoại lệ).
    /// </summary>
    public sealed class DangKyHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public DangKyHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<HandlerResponse<int>> XuLyAsync(
            DangKyRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return new HandlerResponse<int>(
                    false,
                    "ERR_INVALID_INPUT",
                    "Vui lòng nhập đầy đủ Tên đăng nhập, Mật khẩu và Họ tên.");
            }

            string username = request.Username.Trim();
            string fullName = request.FullName.Trim();
            string? email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

            // 1. Kiểm tra trùng Username ở tầng ứng dụng bằng SQL tham số
            var existsResult = await _userRepository
                .ExistsByUsernameAsync(username, cancellationToken)
                .ConfigureAwait(false);

            if (!existsResult.IsSuccess)
            {
                return new HandlerResponse<int>(
                    false,
                    existsResult.ErrorCode,
                    existsResult.UserFriendlyMessage);
            }

            if (existsResult.Data)
            {
                ServerLog.Ghi("DANGKY", $"Từ chối đăng ký vì Username '{username}' đã tồn tại.");
                return new HandlerResponse<int>(
                    false,
                    "ERR_USERNAME_EXISTS",
                    "Tên đăng nhập này đã có người sử dụng. Vui lòng chọn tên khác.");
            }

            // 2. Băm mật khẩu phía Server ngay trước khi ghi CSDL (không bao giờ ghi mật khẩu vào log)
            var (passwordHash, passwordSalt) = _passwordHasher.Tao(request.Password);

            // 3. Lưu người dùng mới vào bảng NguoiDung (bắt luôn lỗi UNIQUE 2627/2601 nếu 2 request đến cùng lúc)
            var createResult = await _userRepository
                .CreateUserAsync(
                    username,
                    passwordHash,
                    passwordSalt,
                    email,
                    fullName,
                    request.Birthday,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!createResult.IsSuccess)
            {
                return new HandlerResponse<int>(
                    false,
                    createResult.ErrorCode,
                    createResult.UserFriendlyMessage);
            }

            ServerLog.Ghi("DANGKY", $"Đăng ký thành công tài khoản '{username}' (UserId={createResult.Data}).");
            return new HandlerResponse<int>(
                true,
                "OK",
                "Đăng ký tài khoản thành công!",
                createResult.Data);
        }
    }
}
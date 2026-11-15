using System;
using System.Threading;
using System.Threading.Tasks;

namespace NT106.BT3.Shared.Data
{
    /// <summary>
    /// DTO chứa thông tin băm mật khẩu lấy từ bảng NguoiDung phục vụ xác thực lệnh DANGNHAP (Bước 3.2).
    /// </summary>
    public sealed class UserCredentialsDto
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string PasswordSalt { get; init; } = string.Empty;
    }

    /// <summary>
    /// DTO chứa thông tin hồ sơ người dùng trả về cho Client ở lệnh DANGNHAP và LAYTHONGTIN (Bước 4.4).
    /// Tuyệt đối KHÔNG chứa cột PasswordHash và PasswordSalt theo đúng yêu cầu bảo mật của đề.
    /// </summary>
    public sealed class UserProfileDto
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string FullName { get; init; } = string.Empty;
        public DateTime? Birthday { get; init; }
    }

    /// <summary>
    /// Kết quả thao tác với CSDL được chuẩn hóa để không làm rò rỉ ngoại lệ SqlException ra tầng giao diện/mạng.
    /// </summary>
    public sealed class RepositoryResult<T>
    {
        public bool IsSuccess { get; init; }
        public string ErrorCode { get; init; } = string.Empty;
        public string UserFriendlyMessage { get; init; } = string.Empty;
        public T? Data { get; init; }

        public static RepositoryResult<T> Success(T data, string message = "Thành công.") =>
            new()
            {
                IsSuccess = true,
                ErrorCode = "OK",
                UserFriendlyMessage = message,
                Data = data
            };

        public static RepositoryResult<T> Failure(string errorCode, string userFriendlyMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                UserFriendlyMessage = userFriendlyMessage,
                Data = default
            };
    }

    /// <summary>
    /// Hợp đồng giao diện truy cập bảng NguoiDung đã chốt ở Bước 2.1 (đặt trong src/Shared/Data/IUserRepository.cs).
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 1. Kiểm tra tên đăng nhập (Username) đã tồn tại trong bảng NguoiDung hay chưa.
        /// </summary>
        Task<RepositoryResult<bool>> ExistsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 2. Thêm người dùng mới vào bảng NguoiDung (nhận PasswordHash và PasswordSalt do Server băm bằng PBKDF2).
        /// </summary>
        Task<RepositoryResult<int>> CreateUserAsync(
            string username,
            string passwordHash,
            string passwordSalt,
            string? email,
            string fullName,
            DateTime? birthday,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 3. Lấy PasswordHash và PasswordSalt theo Username để phục vụ kiểm tra mật khẩu khi xử lý lệnh DANGNHAP.
        /// </summary>
        Task<RepositoryResult<UserCredentialsDto?>> GetCredentialsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 4. Lấy thông tin người dùng theo Username (không bao giờ trả về cột mật khẩu PasswordHash/PasswordSalt)
        /// phục vụ lệnh LAYTHONGTIN và phản hồi sau khi DANGNHAP thành công.
        /// </summary>
        Task<RepositoryResult<UserProfileDto?>> GetUserProfileByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);
    }
}
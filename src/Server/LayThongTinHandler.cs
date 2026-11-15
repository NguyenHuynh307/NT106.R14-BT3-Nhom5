using System.Threading;
using System.Threading.Tasks;
using NT106.BT3.Server.Logging;
using NT106.BT3.Shared.Data;

namespace NT106.BT3.Server.Handlers
{

    public sealed class LayThongTinHandler
    {
        private readonly IUserRepository _userRepository;

        public LayThongTinHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<HandlerResponse<UserProfileDto>> XuLyAsync(
            string? currentSessionUsername,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currentSessionUsername))
            {
                return new HandlerResponse<UserProfileDto>(
                    false,
                    "ERR_UNAUTHORIZED",
                    "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var profileResult = await _userRepository
                .GetUserProfileByUsernameAsync(currentSessionUsername.Trim(), cancellationToken)
                .ConfigureAwait(false);

            if (!profileResult.IsSuccess || profileResult.Data is null)
            {
                return new HandlerResponse<UserProfileDto>(
                    false,
                    profileResult.ErrorCode,
                    profileResult.UserFriendlyMessage);
            }

            ServerLog.Ghi("LAYTHONGTIN", $"Đã trả thông tin hồ sơ cho phiên '{currentSessionUsername.Trim()}'.");
            return new HandlerResponse<UserProfileDto>(
                true,
                "OK",
                "Lấy thông tin người dùng thành công.",
                profileResult.Data);
        }
    }
}
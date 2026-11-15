using NT106.BT3.Server.Logging;
using NT106.BT3.Server.Network;
using NT106.BT3.Server.Security;
using NT106.BT3.Shared.Data;
using NT106.BT3.Shared.Security;

namespace NT106.BT3.Server.Handlers;

internal sealed class DangNhapHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly LayThongTinHandler _layThongTinHandler;
    private readonly SessionTokenManager _tokenManager;

    public DangNhapHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        LayThongTinHandler layThongTinHandler,
        SessionTokenManager tokenManager)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _layThongTinHandler = layThongTinHandler;
        _tokenManager = tokenManager;
    }

    public async Task<HandlerResponse<string[]>> XuLyAsync(
        string username,
        string password,
        ClientSession phien,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return new HandlerResponse<string[]>(false, "BAD_REQUEST", "Yêu cầu không hợp lệ");

        string cleanUsername = username.Trim();
        var credResult = await _userRepository.GetCredentialsByUsernameAsync(cleanUsername, token).ConfigureAwait(false);
        
        if (!credResult.IsSuccess)
            return new HandlerResponse<string[]>(false, "SERVER_ERROR", "Lỗi kết nối Server");

        const string thongBaoSaiChung = "Tên đăng nhập hoặc mật khẩu không chính xác";
        
        if (credResult.Data is null)
        {
            // Vá lỗi Timing Attack
            _passwordHasher.KiemTra(password, "AAAAAAAAAAAAAAAAAAAAAA==", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            return new HandlerResponse<string[]>(false, "INVALID_CREDENTIALS", thongBaoSaiChung);
        }

        if (!_passwordHasher.KiemTra(password, credResult.Data.PasswordSalt, credResult.Data.PasswordHash))
            return new HandlerResponse<string[]>(false, "INVALID_CREDENTIALS", thongBaoSaiChung);

        var profileResult = await _layThongTinHandler.XuLyAsync(credResult.Data.Username, token).ConfigureAwait(false);
        if (!profileResult.Success || profileResult.Data is null)
            return new HandlerResponse<string[]>(false, profileResult.Code, profileResult.Message);

        string sessionToken = _tokenManager.TaoToken(credResult.Data.Username);
        phien.TenDangNhap = credResult.Data.Username;
        phien.SessionToken = sessionToken;

        var loginPayload = new string[]
        {
            profileResult.Data.Username ?? "",
            profileResult.Data.Email ?? "",
            profileResult.Data.FullName ?? "",
            profileResult.Data.Birthday?.ToString("yyyy-MM-dd") ?? "",
            sessionToken
        };

        ServerLog.Ghi("DANGNHAP", $"Phiên [{phien.Id}] đăng nhập thành công cho '{phien.TenDangNhap}' (đã cấp token).");
        return new HandlerResponse<string[]>(true, "SUCCESS", "Đăng nhập thành công", loginPayload);
    }
}
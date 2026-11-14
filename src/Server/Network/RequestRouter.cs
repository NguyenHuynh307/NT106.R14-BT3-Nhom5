using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NT106.BT3.Server.Handlers;
using NT106.BT3.Server.Logging;
using NT106.BT3.Shared.Data;
using NT106.BT3.Shared.Security;

namespace NT106.BT3.Server.Network
{
    /// <summary>
    /// Bộ định tuyến thông điệp phía Server cài đặt IMessageProcessor (kết nối TcpServer với UserRepository).
    /// Nhận JSON UTF-8 từ Client -> phân tích Command (DANGKY, DANGNHAP, LAYTHONGTIN, DANGXUAT) -> trả JSON phản hồi.
    /// </summary>
    internal sealed class RequestRouter : IMessageProcessor
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly DangKyHandler _dangKyHandler;
        private readonly LayThongTinHandler _layThongTinHandler;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public RequestRouter(
            DangKyHandler dangKyHandler,
            LayThongTinHandler layThongTinHandler,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher)
        {
            _dangKyHandler = dangKyHandler ?? throw new ArgumentNullException(nameof(dangKyHandler));
            _layThongTinHandler = layThongTinHandler ?? throw new ArgumentNullException(nameof(layThongTinHandler));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<string?> XuLyAsync(ClientSession phien, string thongDiep, CancellationToken token)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(thongDiep);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Command", out JsonElement cmdEl) &&
                    !root.TryGetProperty("command", out cmdEl))
                {
                    return Serialize(new HandlerResponse<object>(false, "ERR_INVALID_FORMAT", "Thiếu trường Command."));
                }

                string command = (cmdEl.GetString() ?? string.Empty).Trim().ToUpperInvariant();

                return command switch
                {
                    "DANGKY" => await HandleDangKyAsync(root, token).ConfigureAwait(false),
                    "DANGNHAP" => await HandleDangNhapAsync(phien, root, token).ConfigureAwait(false),
                    "LAYTHONGTIN" => await HandleLayThongTinAsync(phien, token).ConfigureAwait(false),
                    "DANGXUAT" => HandleDangXuat(phien),
                    _ => Serialize(new HandlerResponse<object>(false, "ERR_UNKNOWN_COMMAND", $"Lệnh '{command}' không được hỗ trợ."))
                };
            }
            catch (JsonException)
            {
                return Serialize(new HandlerResponse<object>(false, "ERR_INVALID_JSON", "Gói tin JSON không hợp lệ."));
            }
            catch (Exception ex)
            {
                ServerLog.Ghi("LOI-ROUTER", $"Lỗi xử lý phiên {phien.Id}: {ex.GetType().Name}");
                return Serialize(new HandlerResponse<object>(false, "ERR_SERVER", "Máy chủ gặp sự cố khi xử lý yêu cầu."));
            }
        }

        private async Task<string> HandleDangKyAsync(JsonElement root, CancellationToken token)
        {
            string username = GetString(root, "Username");
            string password = GetString(root, "Password");
            string fullName = GetString(root, "FullName");
            string? email = GetOptionalString(root, "Email");
            DateTime? birthday = GetOptionalDate(root, "Birthday");

            var req = new DangKyRequest(username, password, email, fullName, birthday);
            var res = await _dangKyHandler.XuLyAsync(req, token).ConfigureAwait(false);
            return Serialize(res);
        }

        private async Task<string> HandleDangNhapAsync(ClientSession phien, JsonElement root, CancellationToken token)
        {
            string username = GetString(root, "Username");
            string password = GetString(root, "Password");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return Serialize(new HandlerResponse<UserProfileDto>(false, "ERR_INVALID_INPUT", "Vui lòng nhập tên đăng nhập và mật khẩu."));
            }

            var credResult = await _userRepository.GetCredentialsByUsernameAsync(username.Trim(), token).ConfigureAwait(false);
            if (!credResult.IsSuccess)
            {
                return Serialize(new HandlerResponse<UserProfileDto>(false, credResult.ErrorCode, credResult.UserFriendlyMessage));
            }

            // Thông báo chung duy nhất cho cả sai Username và sai Password để chống dò tài khoản
            const string genericAuthError = "Tên đăng nhập hoặc mật khẩu không đúng.";
            if (credResult.Data is null ||
                !_passwordHasher.KiemTra(password, credResult.Data.PasswordSalt, credResult.Data.PasswordHash))
            {
                return Serialize(new HandlerResponse<UserProfileDto>(false, "ERR_AUTH_FAILED", genericAuthError));
            }

            phien.TenDangNhap = credResult.Data.Username;
            var profileResult = await _layThongTinHandler.XuLyAsync(phien.TenDangNhap, token).ConfigureAwait(false);
            return Serialize(profileResult);
        }

        private async Task<string> HandleLayThongTinAsync(ClientSession phien, CancellationToken token)
        {
            var res = await _layThongTinHandler.XuLyAsync(phien.TenDangNhap, token).ConfigureAwait(false);
            return Serialize(res);
        }

        private static string HandleDangXuat(ClientSession phien)
        {
            ServerLog.Ghi("DANGXUAT", $"Phiên {phien.Id} ({phien.TenDangNhap ?? "chưa đăng nhập"}) yêu cầu đăng xuất.");
            phien.TenDangNhap = null;
            phien.YeuCauDong = true;
            return Serialize(new HandlerResponse<object>(true, "OK", "Đăng xuất thành công."));
        }

        private static string GetString(JsonElement root, string name)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.ValueKind == JsonValueKind.String)
                {
                    return prop.Value.GetString() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private static string? GetOptionalString(JsonElement root, string name)
        {
            string val = GetString(root, name);
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }

        private static DateTime? GetOptionalDate(JsonElement root, string name)
        {
            string val = GetString(root, name);
            return DateTime.TryParse(val, out DateTime dt) ? dt.Date : null;
        }

        private static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonOptions);
    }
}
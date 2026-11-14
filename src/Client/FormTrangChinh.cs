using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NT106.BT3.Client
{
    public partial class FormTrangChinh : Form
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly StreamReader? _reader;
        private readonly StreamWriter? _writer;
        private readonly Action? _dongKetNoi;
        private bool _isLoggingOut;

        public FormTrangChinh()
        {
            InitializeComponent();
        }

        public FormTrangChinh(StreamReader? reader, StreamWriter? writer, Action? dongKetNoi) : this()
        {
            _reader = reader;
            _writer = writer;
            _dongKetNoi = dongKetNoi;
            this.Shown += FormTrangChinh_Shown;
        }

        private async void FormTrangChinh_Shown(object? sender, EventArgs e)
        {
            await TaiThongTinNguoiDungAsync();
        }

        private async Task TaiThongTinNguoiDungAsync()
        {
            if (_writer == null || _reader == null) return;

            try
            {
                string request = JsonSerializer.Serialize(new { Command = "LAYTHONGTIN" }, JsonOptions);
                await _writer.WriteLineAsync(request);

                string? jsonResponse = await _reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    MessageBox.Show("Mất kết nối tới máy chủ.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                bool success = root.TryGetProperty("Success", out var sEl) && sEl.GetBoolean();

                if (!success)
                {
                    string message = root.TryGetProperty("Message", out var mEl) ? (mEl.GetString() ?? "Không thể tải thông tin.") : "Không thể tải thông tin.";
                    MessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (root.TryGetProperty("Data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
                {
                    string fullName = GetString(data, "FullName");
                    string username = GetString(data, "Username");
                    string email = GetString(data, "Email");

                    lblXinChao.Text = $"Xin chào, {(string.IsNullOrWhiteSpace(fullName) ? username : fullName)}";
                    lblUsername.Text = $"Tên đăng nhập:  {username}";
                    lblEmail.Text = $"Email:  {(string.IsNullOrWhiteSpace(email) ? "(Chưa cập nhật)" : email)}";
                    lblLastLoginTime.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
                }
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                MessageBox.Show("Mất kết nối tới máy chủ khi lấy thông tin người dùng.", "Lỗi mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException)
            {
                MessageBox.Show("Dữ liệu thông tin người dùng từ máy chủ không hợp lệ.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GuiDangXuatVaDongKetNoiAsync()
        {
            try
            {
                if (_writer != null)
                {
                    string req = JsonSerializer.Serialize(new { Command = "DANGXUAT" }, JsonOptions);
                    await _writer.WriteLineAsync(req);
                    if (_reader != null)
                    {
                        _ = await _reader.ReadLineAsync();
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi mạng khi đang đóng phiên
            }
            finally
            {
                _dongKetNoi?.Invoke();
            }
        }

        private async void btnDangXuat_Click(object sender, EventArgs e)
        {
            btnDangXuat.Enabled = false;
            _isLoggingOut = true;

            await GuiDangXuatVaDongKetNoiAsync();

            FormDangNhap? loginForm = Application.OpenForms.OfType<FormDangNhap>().FirstOrDefault();
            if (loginForm != null)
            {
                loginForm.Show();
            }
            else
            {
                FormDangNhap newLogin = new FormDangNhap();
                newLogin.Show();
            }

            this.Close();
        }

        private async void FormTrangChinh_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isLoggingOut)
            {
                return; // Đã xử lý đăng xuất và quay lại FormDangNhap
            }

            await GuiDangXuatVaDongKetNoiAsync();
            Application.Exit();
        }

        private static string GetString(JsonElement obj, string propName)
        {
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.ValueKind == JsonValueKind.String)
                {
                    return prop.Value.GetString() ?? string.Empty;
                }
            }
            return string.Empty;
        }
    }
}
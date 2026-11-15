using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Forms;

namespace NT106.BT3.Client
{
    public partial class FormDangKi : Form
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string _serverIp;
        private readonly int _serverPort;

        public FormDangKi(string serverIp = "127.0.0.1", int serverPort = 5000)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            InitializeComponent();
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnkLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
        }

        private bool inputValidator()
        {
            bool isValid = true;
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lblCBTenDangNhap.Text = "Vui lòng nhập tên đăng nhập.";
                lblCBTenDangNhap.Visible = true;
                isValid = false;
            }
            else if (txtUsername.Text.Length < 3)
            {
                lblCBTenDangNhap.Text = "Tên đăng nhập phải có ít nhất 3 ký tự.";
                lblCBTenDangNhap.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBTenDangNhap.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblCBMatKhau.Text = "Vui lòng nhập mật khẩu.";
                lblCBMatKhau.Visible = true;
                isValid = false;
            }
            else if (txtPassword.Text.Length < 8)
            {
                lblCBMatKhau.Text = "Mật khẩu phải có ít nhất 8 ký tự.";
                lblCBMatKhau.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBMatKhau.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(txtConfirmPassword.Text))
            {
                lblCBXacNhanMatKhau.Text = "Vui lòng xác nhận lại mật khẩu.";
                lblCBXacNhanMatKhau.Visible = true;
                isValid = false;
            }
            else if (txtConfirmPassword.Text != txtPassword.Text)
            {
                lblCBXacNhanMatKhau.Text = "Mật khẩu nhập lại không khớp.";
                lblCBXacNhanMatKhau.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBXacNhanMatKhau.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                lblCBHoVaTen.Text = "Vui lòng nhập họ và tên.";
                lblCBHoVaTen.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBHoVaTen.Visible = false;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
                {
                    lblEmailError.Text = "Email không đúng định dạng (VD: ten@mien.com).";
                    lblEmailError.Visible = true;
                    isValid = false;
                }
                else
                {
                    lblEmailError.Visible = false;
                }
            }
            else
            {
                lblEmailError.Visible = false;
            }

            return isValid;
        }

        private async void btnDangKy_Click(object sender, EventArgs e)
        {
            if (!inputValidator()) return;

            btnDangKy.Enabled = false;
            string oldText = btnDangKy.Text;
            btnDangKy.Text = "Đang xử lý...";

            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(_serverIp, _serverPort);

                await using NetworkStream stream = tcpClient.GetStream();
                var utf8NoBom = new UTF8Encoding(false);
                using var reader = new StreamReader(stream, utf8NoBom, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                using var writer = new StreamWriter(stream, utf8NoBom, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

                var payload = new
                {
                    Command = "DANGKY",
                    Username = txtUsername.Text.Trim(),
                    Password = txtPassword.Text,
                    Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    FullName = txtFullName.Text.Trim(),
                    Birthday = (string?)null
                };

                string jsonRequest = JsonSerializer.Serialize(payload, JsonOptions);
                await writer.WriteLineAsync(jsonRequest);

                string? jsonResponse = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    MessageBox.Show("Không nhận được phản hồi từ máy chủ.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                bool success = root.TryGetProperty("Success", out var sEl) && sEl.GetBoolean();
                string code = root.TryGetProperty("Code", out var cEl) ? (cEl.GetString() ?? "") : "";
                string message = root.TryGetProperty("Message", out var mEl) ? (mEl.GetString() ?? "Lỗi không xác định.") : "Lỗi không xác định.";

                if (!success)
                {
                    if (code == "ERR_USERNAME_EXISTS")
                    {
                        lblCBTenDangNhap.Text = message;
                        lblCBTenDangNhap.Visible = true;
                        txtUsername.Focus();
                    }
                    else
                    {
                        MessageBox.Show(message, "Đăng ký không thành công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return;
                }

                MessageBox.Show("Đăng ký tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                MessageBox.Show("Không thể kết nối đến máy chủ. Vui lòng kiểm tra Server đang chạy tại " + $"{_serverIp}:{_serverPort}.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException)
            {
                MessageBox.Show("Phản hồi từ máy chủ không hợp lệ.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDangKy.Text = oldText;
                btnDangKy.Enabled = true;
            }
        }
    }
}
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NT106.BT3.Client
{
    public partial class FormDangNhap : Form
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private TcpClient? client;
        private NetworkStream? stream;
        private StreamReader? reader;
        private StreamWriter? writer;

        public FormDangNhap()
        {
            InitializeComponent();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            DongKetNoi();
            this.Close();
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string ip = string.IsNullOrWhiteSpace(txtServerIP.Text) ? "127.0.0.1" : txtServerIP.Text.Trim();
            int port = int.TryParse(txtPort.Text.Trim(), out int p) ? p : 5000;

            using FormDangKi formDK = new FormDangKi(ip, port);
            this.Hide();
            formDK.ShowDialog();
            this.Show();
        }

        private bool inputValidatorForKN()
        {
            bool isValid = true;
            if (string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                lblCBIPServer.Text = "Vui lòng nhập IP Server.";
                lblCBIPServer.Visible = true;
                isValid = false;
            }
            else if (!IPAddress.TryParse(txtServerIP.Text.Trim(), out _))
            {
                lblCBIPServer.Text = "Địa chỉ IP không đúng định dạng (VD: 127.0.0.1).";
                lblCBIPServer.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBIPServer.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(txtPort.Text))
            {
                lblCBPort.Text = "Vui lòng nhập Port.";
                lblCBPort.Visible = true;
                isValid = false;
            }
            else if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                lblCBPort.Text = "Port phải là số từ 1 đến 65535.";
                lblCBPort.Visible = true;
                isValid = false;
            }
            else
            {
                lblCBPort.Visible = false;
            }

            return isValid;
        }

        private bool inputValidatorForDN()
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

            return isValid;
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (!inputValidatorForKN()) return;

            btnConnect.Enabled = false;
            try
            {
                string ip = txtServerIP.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());

                await KetNoiServerAsync(ip, port);

                lblConnectionStatus.Text = "Trạng thái: Đã kết nối";
                lblConnectionStatus.ForeColor = Color.FromArgb(40, 167, 69);

                txtServerIP.Enabled = false;
                txtPort.Enabled = false;
                btnConnect.Enabled = false;

                MessageBox.Show("Kết nối đến server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                DongKetNoi();
                lblConnectionStatus.Text = "Trạng thái: Chưa kết nối";
                lblConnectionStatus.ForeColor = Color.FromArgb(220, 53, 69);
                btnConnect.Enabled = true;

                MessageBox.Show("Không thể kết nối đến máy chủ. Vui lòng kiểm tra IP, Port hoặc chắc chắn Server đang chạy.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnDangNhap_Click(object sender, EventArgs e)
        {
            if (!inputValidatorForDN()) return;

            if (client == null || !client.Connected || writer == null || reader == null)
            {
                MessageBox.Show("Vui lòng bấm Kết nối đến Server trước khi đăng nhập.", "Chưa kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnDangNhap.Enabled = false;
            string oldText = btnDangNhap.Text;
            btnDangNhap.Text = "Đang xử lý...";

            try
            {
                var payload = new
                {
                    Command = "DANGNHAP",
                    Username = txtUsername.Text.Trim(),
                    Password = txtPassword.Text
                };

                string jsonRequest = JsonSerializer.Serialize(payload, JsonOptions);
                await writer.WriteLineAsync(jsonRequest);

                string? jsonResponse = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    MessageBox.Show("Mất kết nối tới máy chủ.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DongKetNoi();
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                bool success = root.TryGetProperty("Success", out var sEl) && sEl.GetBoolean();
                string message = root.TryGetProperty("Message", out var mEl) ? (mEl.GetString() ?? "Lỗi không xác định.") : "Lỗi không xác định.";

                if (!success)
                {
                    lblCBMatKhau.Text = message;
                    lblCBMatKhau.Visible = true;
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                lblCBMatKhau.Visible = false;
                txtPassword.Clear();

                FormTrangChinh mainForm = new FormTrangChinh(reader, writer, ResetTrangThaiKetNoi);
                mainForm.Show();
                this.Hide();
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                ResetTrangThaiKetNoi();
                MessageBox.Show("Mất kết nối tới máy chủ. Vui lòng kết nối lại.", "Lỗi mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException)
            {
                MessageBox.Show("Phản hồi từ máy chủ không hợp lệ.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDangNhap.Text = oldText;
                btnDangNhap.Enabled = true;
            }
        }

        private void ResetTrangThaiKetNoi()
        {
            DongKetNoi();
            lblConnectionStatus.Text = "Trạng thái: Chưa kết nối";
            lblConnectionStatus.ForeColor = Color.FromArgb(220, 53, 69);
            txtServerIP.Enabled = true;
            txtPort.Enabled = true;
            btnConnect.Enabled = true;
        }

        private async Task KetNoiServerAsync(string ip, int port)
        {
            DongKetNoi();
            client = new TcpClient();
            await client.ConnectAsync(ip, port);
            stream = client.GetStream();
            var utf8NoBom = new UTF8Encoding(false);
            reader = new StreamReader(stream, utf8NoBom, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            writer = new StreamWriter(stream, utf8NoBom, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        }

        private void DongKetNoi()
        {
            try { writer?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { stream?.Dispose(); } catch { }
            try { client?.Dispose(); } catch { }
            writer = null;
            reader = null;
            stream = null;
            client = null;
        }
    }
}
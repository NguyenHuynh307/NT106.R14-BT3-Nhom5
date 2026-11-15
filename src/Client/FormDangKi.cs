using System.Net.Sockets;
using Shared;

namespace NT106.BT3.Client;

public partial class FormDangKi : Form
{
    private bool _dangXuLy;

    public FormDangKi()
    {
        InitializeComponent();
    }

    private void btnHuy_Click(object sender, EventArgs e)
    {
        if (!_dangXuLy)
        {
            Close();
        }
    }

    private void lnkLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (!_dangXuLy)
        {
            Close();
        }
    }

    private bool KiemTraHopLe()
    {
        bool isValid = true;
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string confirm = txtConfirmPassword.Text;
        string fullName = txtFullName.Text.Trim();
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            lblCBTenDangNhap.Text = "Vui lòng nhập tên đăng nhập.";
            lblCBTenDangNhap.Visible = true;
            isValid = false;
        }
        else if (username.Length < 3 || InputValidator.ChuaKyTuCam(username))
        {
            lblCBTenDangNhap.Text = "Tên đăng nhập phải từ 3 ký tự trở lên và không chứa '|' hoặc xuống dòng.";
            lblCBTenDangNhap.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBTenDangNhap.Visible = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            lblCBMatKhau.Text = "Vui lòng nhập mật khẩu.";
            lblCBMatKhau.Visible = true;
            isValid = false;
        }
        else if (password.Length < 8 || InputValidator.ChuaKyTuCam(password))
        {
            lblCBMatKhau.Text = "Mật khẩu phải từ 8 ký tự trở lên và không chứa '|' hoặc xuống dòng.";
            lblCBMatKhau.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBMatKhau.Visible = false;
        }

        if (string.IsNullOrEmpty(confirm))
        {
            lblCBXacNhanMatKhau.Text = "Vui lòng xác nhận lại mật khẩu.";
            lblCBXacNhanMatKhau.Visible = true;
            isValid = false;
        }
        else if (confirm != password)
        {
            lblCBXacNhanMatKhau.Text = "Mật khẩu nhập lại không khớp.";
            lblCBXacNhanMatKhau.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBXacNhanMatKhau.Visible = false;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            lblCBHoVaTen.Text = "Vui lòng nhập họ và tên.";
            lblCBHoVaTen.Visible = true;
            isValid = false;
        }
        else if (InputValidator.ChuaKyTuCam(fullName))
        {
            lblCBHoVaTen.Text = "Họ và tên không được chứa '|' hoặc xuống dòng.";
            lblCBHoVaTen.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBHoVaTen.Visible = false;
        }

        if (!InputValidator.KiemTraEmailHopLe(email))
        {
            lblEmailError.Text = "Email không đúng định dạng! (VD: ten@mien.com)";
            lblEmailError.Visible = true;
            isValid = false;
        }
        else
        {
            lblEmailError.Visible = false;
        }

        return isValid;
    }

    private void DatTrangThaiCho(bool dangCho)
    {
        _dangXuLy = dangCho;
        btnDangKy.Enabled = !dangCho;
        btnHuy.Enabled = !dangCho;
        lnkLogin.Enabled = !dangCho;
        Cursor = dangCho ? Cursors.WaitCursor : Cursors.Default;
    }

    private async void btnDangKy_Click(object sender, EventArgs e)
    {
        if (_dangXuLy || !KiemTraHopLe())
        {
            return;
        }

        if (!TcpClientHelper.Instance.IsConnected)
        {
            MessageBox.Show("Chưa kết nối tới Server. Vui lòng quay lại màn hình Đăng nhập để kết nối trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string email = txtEmail.Text.Trim();
        string fullName = txtFullName.Text.Trim();

        DatTrangThaiCho(true);
        try
        {
            string payload = new RequestMessage(
                ProtocolConstants.CmdRegister,
                username,
                password,
                email,
                fullName,
                string.Empty).Serialize();

            ResponseMessage response = await InputValidator.GuiVaChoPhanHoiAsync(payload);

            if (response.StatusCode == ProtocolConstants.CodeSuccess)
            {
                MessageBox.Show(response.Message, "Đăng ký thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show(response.Message, "Đăng ký không thành công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (TimeoutException)
        {
            MessageBox.Show("Server không phản hồi. Vui lòng thử lại.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException)
        {
            MessageBox.Show("Mất kết nối tới Server. Vui lòng kết nối lại.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (ArgumentException)
        {
            MessageBox.Show("Dữ liệu nhập chứa ký tự không hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            DatTrangThaiCho(false);
        }
    }
}
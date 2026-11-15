using System.Net.Sockets;
using Shared;

namespace NT106.BT3.Client;

public partial class FormTrangChinh : Form
{
    private readonly string _username;
    private readonly string _email;
    private readonly string _fullName;
    private readonly string _birthday;
    private bool _dangDangXuat;

    public FormTrangChinh() : this(string.Empty, string.Empty, string.Empty, string.Empty)
    {
    }

    public FormTrangChinh(string username, string email, string fullName, string birthday)
    {
        InitializeComponent();
        _username = username;
        _email = email;
        _fullName = fullName;
        _birthday = birthday;

        Load += FormTrangChinh_Load;
    }

    private async void FormTrangChinh_Load(object? sender, EventArgs e)
    {
        TcpClientHelper.Instance.OnDisconnected += OnServerDisconnected;
        HienThiThongTin(_username, _email, _fullName, _birthday);

        if (!TcpClientHelper.Instance.IsConnected)
        {
            return;
        }

        try
        {
            string payload = new RequestMessage(ProtocolConstants.CmdGetInfo).Serialize();
            ResponseMessage response = await InputValidator.GuiVaChoPhanHoiAsync(payload);

            if (IsDisposed)
            {
                return;
            }

            if (response.StatusCode == ProtocolConstants.CodeSuccess)
            {
                string u = response.Data.Length > 0 ? response.Data[0] : _username;
                string em = response.Data.Length > 1 ? response.Data[1] : _email;
                string fn = response.Data.Length > 2 ? response.Data[2] : _fullName;
                string bd = response.Data.Length > 3 ? response.Data[3] : _birthday;
                HienThiThongTin(u, em, fn, bd);
            }
            else if (response.StatusCode == ProtocolConstants.CodeUnauthorized)
            {
                QuayVeDangNhap("Phiên đăng nhập chưa hợp lệ. Vui lòng đăng nhập lại.");
            }
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException or TimeoutException)
        {
            // Giữ thông tin đã nhận từ bước đăng nhập nếu tải lại gặp lỗi tạm thời
        }
    }

    private void HienThiThongTin(string username, string email, string fullName, string birthday)
    {
        lblXinChao.Text = $"Xin chào, {(string.IsNullOrWhiteSpace(fullName) ? username : fullName)}";
        lblUsername.Text = $"Tên đăng nhập:  {username}";
        lblEmail.Text = $"Email:  {(string.IsNullOrWhiteSpace(email) ? "(Chưa cập nhật)" : email)}";
        lblLastLoginTitle.Text = string.IsNullOrWhiteSpace(birthday)
            ? "Đăng nhập lúc:"
            : $"Ngày sinh: {birthday}  |  Đăng nhập lúc:";
        lblLastLoginTime.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
    }

    private void OnServerDisconnected()
    {
        if (_dangDangXuat || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(() => QuayVeDangNhap("Mất kết nối tới Server. Vui lòng đăng nhập lại."));
    }

    private void QuayVeDangNhap(string? thongBao = null)
    {
        if (_dangDangXuat)
        {
            return;
        }

        _dangDangXuat = true;
        TcpClientHelper.Instance.OnDisconnected -= OnServerDisconnected;
        TcpClientHelper.Instance.Disconnect();

        if (!string.IsNullOrWhiteSpace(thongBao))
        {
            MessageBox.Show(thongBao, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        FormDangNhap? loginForm = Application.OpenForms.OfType<FormDangNhap>().FirstOrDefault();
        if (loginForm is not null)
        {
            loginForm.CapNhatTrangThaiKetNoi(false);
            loginForm.Show();
        }
        else
        {
            new FormDangNhap().Show();
        }

        Close();
    }

    private async void btnDangXuat_Click(object sender, EventArgs e)
    {
        btnDangXuat.Enabled = false;
        _dangDangXuat = true;
        TcpClientHelper.Instance.OnDisconnected -= OnServerDisconnected;

        try
        {
            if (TcpClientHelper.Instance.IsConnected)
            {
                string payload = new RequestMessage(ProtocolConstants.CmdLogout).Serialize();
                await InputValidator.GuiVaChoPhanHoiAsync(payload, 3000);
            }
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException or TimeoutException)
        {
            // Bỏ qua lỗi mạng khi đang đăng xuất để đóng kết nối ngay
        }
        finally
        {
            TcpClientHelper.Instance.Disconnect();
        }

        FormDangNhap? loginForm = Application.OpenForms.OfType<FormDangNhap>().FirstOrDefault();
        if (loginForm is not null)
        {
            loginForm.CapNhatTrangThaiKetNoi(false);
            loginForm.Show();
        }
        else
        {
            new FormDangNhap().Show();
        }

        Close();
    }

    private void FormTrangChinh_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_dangDangXuat)
        {
            return;
        }

        TcpClientHelper.Instance.OnDisconnected -= OnServerDisconnected;
        try
        {
            if (TcpClientHelper.Instance.IsConnected)
            {
                string payload = new RequestMessage(ProtocolConstants.CmdLogout).Serialize();
                _ = TcpClientHelper.Instance.SendAsync(payload);
            }
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException)
        {
        }
        finally
        {
            TcpClientHelper.Instance.Disconnect();
            Application.Exit();
        }
    }
}
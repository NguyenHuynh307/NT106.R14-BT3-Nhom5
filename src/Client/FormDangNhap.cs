using System.Net.Sockets;
using NT106.BT3.Client.Network;
using Shared;

namespace NT106.BT3.Client;

public partial class FormDangNhap : Form
{
    private bool _dangXuLy;

    public FormDangNhap()
    {
        InitializeComponent();
        Load += FormDangNhap_Load;
        FormClosed += FormDangNhap_FormClosed;
    }

    private async void FormDangNhap_Load(object? sender, EventArgs e)
    {
        TcpClientHelper.Instance.OnDisconnected += OnServerDisconnected;
        CapNhatTrangThaiKetNoi(TcpClientHelper.Instance.IsConnected);

        if (TcpClientHelper.Instance.IsConnected)
        {
            return;
        }

        lblConnectionStatus.Text = "Trạng thái: Đang dò Server LAN...";
        lblConnectionStatus.ForeColor = Color.FromArgb(107, 114, 128);
        lblConnectionStatus.Visible = true;

        DiscoveredServerInfo? discovered = await UdpDiscoveryClient.DiscoverServerAsync();
        if (IsDisposed || TcpClientHelper.Instance.IsConnected)
        {
            return;
        }

        if (discovered is not null)
        {
            if (string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                txtServerIP.Text = discovered.IpAddress;
            }
            if (string.IsNullOrWhiteSpace(txtPort.Text))
            {
                txtPort.Text = discovered.TcpPort.ToString();
            }

            lblConnectionStatus.Text = "Trạng thái: Đã tìm thấy Server LAN (bấm Kết nối)";
            lblConnectionStatus.ForeColor = Color.FromArgb(27, 111, 108);
        }
        else
        {
            CapNhatTrangThaiKetNoi(false);
        }
    }

    private void FormDangNhap_FormClosed(object? sender, FormClosedEventArgs e)
    {
        TcpClientHelper.Instance.OnDisconnected -= OnServerDisconnected;
        TcpClientHelper.Instance.Disconnect();
    }

    private void OnServerDisconnected()
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(() => CapNhatTrangThaiKetNoi(false));
    }

    public void CapNhatTrangThaiKetNoi(bool daKetNoi)
    {
        lblConnectionStatus.Visible = true;
        if (daKetNoi)
        {
            lblConnectionStatus.Text = "Trạng thái: Đã kết nối";
            lblConnectionStatus.ForeColor = Color.FromArgb(40, 167, 69);
            txtServerIP.Enabled = false;
            txtPort.Enabled = false;
            btnConnect.Enabled = false;
        }
        else
        {
            lblConnectionStatus.Text = "Trạng thái: Chưa kết nối";
            lblConnectionStatus.ForeColor = Color.FromArgb(220, 53, 69);
            txtServerIP.Enabled = true;
            txtPort.Enabled = true;
            btnConnect.Enabled = true;
        }
    }

    private void btnThoat_Click(object sender, EventArgs e)
    {
        TcpClientHelper.Instance.Disconnect();
        Close();
    }

    private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (_dangXuLy)
        {
            return;
        }

        using var formDK = new FormDangKi();
        Hide();
        formDK.ShowDialog();
        Show();
        CapNhatTrangThaiKetNoi(TcpClientHelper.Instance.IsConnected);
    }

    private bool InputValidatorForKetNoi(out string ip, out int port)
    {
        bool isValid = true;
        if (!InputValidator.KiemTraIp(txtServerIP.Text, out ip, out string ipError))
        {
            lblCBIPServer.Text = ipError;
            lblCBIPServer.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBIPServer.Visible = false;
        }

        if (!InputValidator.KiemTraPort(txtPort.Text, out port, out string portError))
        {
            lblCBPort.Text = portError;
            lblCBPort.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBPort.Visible = false;
        }

        return isValid;
    }

    private bool InputValidatorForDangNhap()
    {
        bool isValid = true;
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            lblCBTenDangNhap.Text = "Vui lòng nhập tên đăng nhập.";
            lblCBTenDangNhap.Visible = true;
            isValid = false;
        }
        else if (InputValidator.ChuaKyTuCam(username))
        {
            lblCBTenDangNhap.Text = "Tên đăng nhập không được chứa ký tự '|' hoặc xuống dòng.";
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
        else if (InputValidator.ChuaKyTuCam(password))
        {
            lblCBMatKhau.Text = "Mật khẩu không được chứa ký tự '|' hoặc xuống dòng.";
            lblCBMatKhau.Visible = true;
            isValid = false;
        }
        else
        {
            lblCBMatKhau.Visible = false;
        }

        return isValid;
    }

    private void DatTrangThaiCho(bool dangCho)
    {
        _dangXuLy = dangCho;
        btnDangNhap.Enabled = !dangCho;
        btnThoat.Enabled = !dangCho;
        lnkRegister.Enabled = !dangCho;
        if (!TcpClientHelper.Instance.IsConnected)
        {
            btnConnect.Enabled = !dangCho;
        }
        Cursor = dangCho ? Cursors.WaitCursor : Cursors.Default;
    }

    private async void btnConnect_Click(object sender, EventArgs e)
    {
        if (_dangXuLy || !InputValidatorForKetNoi(out string ip, out int port))
        {
            return;
        }

        DatTrangThaiCho(true);
        try
        {
            await TcpClientHelper.Instance.ConnectAsync(ip, port);
            CapNhatTrangThaiKetNoi(true);
            MessageBox.Show("Kết nối đến Server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SocketException)
        {
            CapNhatTrangThaiKetNoi(false);
            MessageBox.Show("Không thể kết nối đến Server. Vui lòng kiểm tra IP, Port hoặc đảm bảo Server đang bật.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (IOException)
        {
            CapNhatTrangThaiKetNoi(false);
            MessageBox.Show("Mất kết nối trong quá trình thiết lập phiên với Server.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            DatTrangThaiCho(false);
            CapNhatTrangThaiKetNoi(TcpClientHelper.Instance.IsConnected);
        }
    }

    private async void btnDangNhap_Click(object sender, EventArgs e)
    {
        if (_dangXuLy || !InputValidatorForDangNhap())
        {
            return;
        }

        if (!TcpClientHelper.Instance.IsConnected)
        {
            MessageBox.Show("Vui lòng kết nối tới Server trước khi đăng nhập.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        DatTrangThaiCho(true);
        try
        {
            string payload = new RequestMessage(ProtocolConstants.CmdLogin, username, password).Serialize();
            ResponseMessage response = await InputValidator.GuiVaChoPhanHoiAsync(payload);

            if (response.StatusCode == ProtocolConstants.CodeSuccess)
            {
                string resUser = response.Data.Length > 0 && !string.IsNullOrWhiteSpace(response.Data[0])
                    ? response.Data[0]
                    : username;
                string resEmail = response.Data.Length > 1 ? response.Data[1] : string.Empty;
                string resFullName = response.Data.Length > 2 ? response.Data[2] : string.Empty;
                string resBirthday = response.Data.Length > 3 ? response.Data[3] : string.Empty;

                TcpClientHelper.Instance.SessionToken = response.Data.Length > 4 ? response.Data[4] : null;
                txtPassword.Clear();
                var mainForm = new FormTrangChinh(resUser, resEmail, resFullName, resBirthday);
                Hide();
                mainForm.Show();
            }
            else
            {
                txtPassword.Clear();
                txtPassword.Focus();
                MessageBox.Show(response.Message, "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (TimeoutException)
        {
            MessageBox.Show("Server không phản hồi. Vui lòng thử lại.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException)
        {
            CapNhatTrangThaiKetNoi(false);
            MessageBox.Show("Không thể trao đổi với Server. Vui lòng kết nối lại.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
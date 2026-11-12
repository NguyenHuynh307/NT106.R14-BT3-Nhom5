using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;

namespace NT106.BT3.Client
{
    public partial class FormDangNhap : Form
    {
        private TcpClient client; 
        private NetworkStream stream;
        public FormDangNhap()
        {
            InitializeComponent();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDangKi formDK = new FormDangKi();
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
            else if (!System.Net.IPAddress.TryParse(txtServerIP.Text.Trim(), out _))
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

        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            if (!inputValidatorForDN()) return;
            //Phần này kiểm tra Client đã kết nối Server, rồi mới xác thực dữ liệu có sẵn rồi mới cho chuyển form
            //Giả sử đã xác thực rồi chuyển form
            FormTrangChinh mainForm = new FormTrangChinh();
            mainForm.Show(); 
            this.Hide();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!inputValidatorForKN()) return;
            try
            {
                string ip = txtServerIP.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());

                client = new TcpClient();
                client.Connect(ip, port);

                stream = client.GetStream();

                lblConnectionStatus.Text = "Trạng thái: Đã kết nối";
                lblConnectionStatus.ForeColor = Color.FromArgb(40, 167, 69); 

                txtServerIP.Enabled = false;
                txtPort.Enabled = false;
                btnConnect.Enabled = false;

                MessageBox.Show("Kết nối đến server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblConnectionStatus.Text = "Trạng thái: Chưa kết nối";
                lblConnectionStatus.ForeColor = Color.FromArgb(220, 53, 69);

                MessageBox.Show("Không thể kết nối đến server.\nChi tiết lỗi: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

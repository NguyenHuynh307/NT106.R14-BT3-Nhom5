using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NT106.BT3.Client
{
    public partial class FormDangKi : Form
    {
        public FormDangKi()
        {
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

        private void btnDangKy_Click(object sender, EventArgs e)
        {
            if (!inputValidator()) return;
            //Lưu ý: Còn phải kiểm tra thêm các trường hợp sau trước khi ghi vào CSDL
            //1. Email người dùng nhập vào đã được sử dụng chưa
            //2. Thông tin tên đăng nhập và mật khẩu đã tồn tại chưa, nếu đã tồn tại thì không cho phép đăng nhập
            //Rồi mới ghi nhận thông tin đăng ký của người dùng vào CSDL
        }
    }
}

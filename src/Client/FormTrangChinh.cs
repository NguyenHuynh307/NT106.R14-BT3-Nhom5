using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NT106.BT3.Client
{
    public partial class FormTrangChinh : Form
    {
        public FormTrangChinh()
        {
            InitializeComponent();
        }

        private void btnDangXuat_Click(object sender, EventArgs e)
        {
            //Ngắt kết nối với Server trước
            //Rồi cho quay về lại trang Đăng nhập
            FormDangNhap loginForm = Application.OpenForms.OfType<FormDangNhap>().FirstOrDefault();
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

        private void FormTrangChinh_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Ngắt kết nối với Server trước
            //Rồi đóng toàn bộ chương trình
            Application.Exit();
        }
    }
}

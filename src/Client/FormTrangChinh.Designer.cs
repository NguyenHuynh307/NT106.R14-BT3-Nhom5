using Guna.UI2.WinForms;

namespace NT106.BT3.Client
{
    partial class FormTrangChinh
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            panelTitle = new Guna2Panel();
            lblTitle = new Label();
            guna2ControlBoxClose = new Guna2ControlBox();
            guna2ControlBoxMaximize = new Guna2ControlBox();
            guna2ControlBoxMinimize = new Guna2ControlBox();
            lblXinChao = new Label();
            lblUsername = new Label();
            lblEmail = new Label();
            lblLastLoginTitle = new Label();
            lblLastLoginTime = new Label();
            btnDangXuat = new Guna2Button();
            guna2BorderlessForm = new Guna2BorderlessForm(components);
            panelTitle.SuspendLayout();
            SuspendLayout();
            // 
            // panelTitle
            // 
            panelTitle.BackColor = Color.FromArgb(27, 111, 108);
            panelTitle.Controls.Add(lblTitle);
            panelTitle.Controls.Add(guna2ControlBoxClose);
            panelTitle.Controls.Add(guna2ControlBoxMaximize);
            panelTitle.Controls.Add(guna2ControlBoxMinimize);
            panelTitle.CustomizableEdges = customizableEdges7;
            panelTitle.Dock = DockStyle.Top;
            panelTitle.Location = new Point(0, 0);
            panelTitle.Name = "panelTitle";
            panelTitle.ShadowDecoration.CustomizableEdges = customizableEdges8;
            panelTitle.Size = new Size(500, 45);
            panelTitle.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(16, 11);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(113, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Trang chính";
            // 
            // guna2ControlBoxClose
            // 
            guna2ControlBoxClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2ControlBoxClose.CustomizableEdges = customizableEdges1;
            guna2ControlBoxClose.FillColor = Color.FromArgb(27, 111, 108);
            guna2ControlBoxClose.HoverState.FillColor = Color.FromArgb(232, 17, 35);
            guna2ControlBoxClose.IconColor = Color.White;
            guna2ControlBoxClose.Location = new Point(442, 0);
            guna2ControlBoxClose.Name = "guna2ControlBoxClose";
            guna2ControlBoxClose.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2ControlBoxClose.Size = new Size(58, 45);
            guna2ControlBoxClose.TabIndex = 1;
            // 
            // guna2ControlBoxMaximize
            // 
            guna2ControlBoxMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2ControlBoxMaximize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox;
            guna2ControlBoxMaximize.CustomizableEdges = customizableEdges3;
            guna2ControlBoxMaximize.FillColor = Color.FromArgb(27, 111, 108);
            guna2ControlBoxMaximize.IconColor = Color.White;
            guna2ControlBoxMaximize.Location = new Point(384, 0);
            guna2ControlBoxMaximize.Name = "guna2ControlBoxMaximize";
            guna2ControlBoxMaximize.ShadowDecoration.CustomizableEdges = customizableEdges4;
            guna2ControlBoxMaximize.Size = new Size(58, 45);
            guna2ControlBoxMaximize.TabIndex = 2;
            // 
            // guna2ControlBoxMinimize
            // 
            guna2ControlBoxMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guna2ControlBoxMinimize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            guna2ControlBoxMinimize.CustomizableEdges = customizableEdges5;
            guna2ControlBoxMinimize.FillColor = Color.FromArgb(27, 111, 108);
            guna2ControlBoxMinimize.IconColor = Color.White;
            guna2ControlBoxMinimize.Location = new Point(326, 0);
            guna2ControlBoxMinimize.Name = "guna2ControlBoxMinimize";
            guna2ControlBoxMinimize.ShadowDecoration.CustomizableEdges = customizableEdges6;
            guna2ControlBoxMinimize.Size = new Size(58, 45);
            guna2ControlBoxMinimize.TabIndex = 3;
            // 
            // lblXinChao
            // 
            lblXinChao.AutoSize = true;
            lblXinChao.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblXinChao.ForeColor = Color.FromArgb(17, 24, 39);
            lblXinChao.Location = new Point(30, 75);
            lblXinChao.Name = "lblXinChao";
            lblXinChao.Size = new Size(240, 28);
            lblXinChao.TabIndex = 1;
            lblXinChao.Text = "Xin chào, Nguyễn Văn A";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 10F);
            lblUsername.ForeColor = Color.FromArgb(55, 65, 81);
            lblUsername.Location = new Point(30, 130);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(231, 23);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Tên đăng nhập:  nguyenvana";
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 10F);
            lblEmail.ForeColor = Color.FromArgb(55, 65, 81);
            lblEmail.Location = new Point(30, 178);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(191, 23);
            lblEmail.TabIndex = 3;
            lblEmail.Text = "Email:  a@example.com";
            // 
            // lblLastLoginTitle
            // 
            lblLastLoginTitle.AutoSize = true;
            lblLastLoginTitle.Font = new Font("Segoe UI", 10F);
            lblLastLoginTitle.ForeColor = Color.FromArgb(55, 65, 81);
            lblLastLoginTitle.Location = new Point(30, 226);
            lblLastLoginTitle.Name = "lblLastLoginTitle";
            lblLastLoginTitle.Size = new Size(203, 23);
            lblLastLoginTitle.TabIndex = 4;
            lblLastLoginTitle.Text = "Lần đăng nhập gần nhất:";
            // 
            // lblLastLoginTime
            // 
            lblLastLoginTime.AutoSize = true;
            lblLastLoginTime.Font = new Font("Segoe UI", 10F);
            lblLastLoginTime.ForeColor = Color.FromArgb(55, 65, 81);
            lblLastLoginTime.Location = new Point(30, 274);
            lblLastLoginTime.Name = "lblLastLoginTime";
            lblLastLoginTime.Size = new Size(146, 23);
            lblLastLoginTime.TabIndex = 5;
            lblLastLoginTime.Text = "21/09/2026  09:15";
            // 
            // btnDangXuat
            // 
            btnDangXuat.BorderColor = Color.FromArgb(209, 213, 219);
            btnDangXuat.BorderRadius = 6;
            btnDangXuat.BorderThickness = 1;
            btnDangXuat.CustomizableEdges = customizableEdges9;
            btnDangXuat.FillColor = Color.FromArgb(27, 111, 108);
            btnDangXuat.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDangXuat.ForeColor = Color.White;
            btnDangXuat.HoverState.FillColor = Color.FromArgb(210, 230, 228);
            btnDangXuat.Location = new Point(310, 340);
            btnDangXuat.Name = "btnDangXuat";
            btnDangXuat.ShadowDecoration.CustomizableEdges = customizableEdges10;
            btnDangXuat.Size = new Size(160, 45);
            btnDangXuat.TabIndex = 6;
            btnDangXuat.Text = "Đăng xuất";
            btnDangXuat.Click += btnDangXuat_Click;
            // 
            // guna2BorderlessForm
            // 
            guna2BorderlessForm.BorderRadius = 10;
            guna2BorderlessForm.ContainerControl = this;
            guna2BorderlessForm.DockIndicatorTransparencyValue = 0.6D;
            guna2BorderlessForm.TransparentWhileDrag = true;
            // 
            // FormTrangChinh
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(500, 415);
            Controls.Add(btnDangXuat);
            Controls.Add(lblLastLoginTime);
            Controls.Add(lblLastLoginTitle);
            Controls.Add(lblEmail);
            Controls.Add(lblUsername);
            Controls.Add(lblXinChao);
            Controls.Add(panelTitle);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FormTrangChinh";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Trang chính";
            panelTitle.ResumeLayout(false);
            panelTitle.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel panelTitle;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBoxClose;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBoxMaximize;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBoxMinimize;
        private Label lblTitle;
        private Label lblXinChao;
        private Label lblUsername;
        private Label lblEmail;
        private Label lblLastLoginTitle;
        private Label lblLastLoginTime;
        private Guna.UI2.WinForms.Guna2Button btnDangXuat;
        private Guna.UI2.WinForms.Guna2BorderlessForm guna2BorderlessForm;
    }
}
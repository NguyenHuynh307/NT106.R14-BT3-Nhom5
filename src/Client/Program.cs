namespace NT106.BT3.Client;

internal static class Program
{
    /// <summary>
    /// KHUNG TẠM của Bước 1.1 để Solution build được.
    /// Nhật Minh (Bước 5.1) thay phần Application.Run bằng FrmDangNhap kế thừa từ BT2.2.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormDangNhap());
    }
}

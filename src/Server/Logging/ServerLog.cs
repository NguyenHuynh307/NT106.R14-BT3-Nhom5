using System.Text;

namespace NT106.BT3.Server.Logging;

/// <summary>
/// Nhật ký của Server: ghi ra màn hình console VÀ ra tệp logs/server-yyyyMMdd.log (Mục III.1).
/// Mỗi dòng đều kèm mốc thời gian. KHÔNG ghi mật khẩu, token hay nội dung thông điệp vào đây (Bước 1.2, 2.3).
/// </summary>
internal static class ServerLog
{
    private static readonly object _khoa = new();
    private static readonly string _thuMucLog = Path.Combine(AppContext.BaseDirectory, "logs");

    /// <summary>Mốc thời gian dùng chung cho mọi dòng log.</summary>
    public static string Gio() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>Ghi một dòng: "mốc thời gian [LOẠI] nội dung". Nhiều tác vụ gọi cùng lúc vẫn an toàn.</summary>
    public static void Ghi(string loai, string noiDung)
    {
        string dong = $"{Gio()} [{loai}] {noiDung}";

        lock (_khoa)
        {
            Console.WriteLine(dong);

            try
            {
                Directory.CreateDirectory(_thuMucLog);
                string tep = Path.Combine(_thuMucLog, $"server-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(tep, dong + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException)
            {
                // Không ghi được tệp (ví dụ đang bị khóa) thì vẫn còn dòng trên console; không làm sập Server.
            }
            catch (UnauthorizedAccessException)
            {
                // Không có quyền ghi vào thư mục logs: bỏ qua phần ghi tệp.
            }
        }
    }
}

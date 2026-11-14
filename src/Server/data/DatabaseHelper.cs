using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NT106.BT3.Server.Logging;

namespace NT106.BT3.Server.Data
{
    /// <summary>
    /// Lớp quản lý kết nối SQL Server đặt tại src/Server/Data/DatabaseHelper.cs (Bước 5 của đề / Bước 4.1).
    /// Khớp 100% với CSDL [healthInforDB] và bảng [dbo].[NguoiDung].
    /// </summary>
    public sealed class DatabaseHelper
    {
        private const string DefaultConnectionString =
            "Server=localhost\\SQLEXPRESS;Database=healthInforDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;";

        private readonly string _connectionString;

        public DatabaseHelper(string? connectionString = null)
        {
            _connectionString = !string.IsNullOrWhiteSpace(connectionString)
                ? connectionString
                : LoadConnectionStringFromConfig();
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<(bool IsConnected, string Message)> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT 1;";
            try
            {
                await using var conn = CreateConnection();
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var cmd = new SqlCommand(sql, conn);
                _ = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return (true, "Kết nối SQL Server (CSDL healthInforDB) sẵn sàng.");
            }
            catch (SqlException ex)
            {
                ServerLog.Ghi("LOI-DB", $"CheckHealth thất bại (SqlError={ex.Number}).");
                return (false, MapSqlExceptionToVietnamese(ex, out _));
            }
            catch (Exception)
            {
                ServerLog.Ghi("LOI-DB", "Không thể khởi tạo kết nối cơ sở dữ liệu.");
                return (false, "Không thể khởi tạo kết nối cơ sở dữ liệu. Vui lòng kiểm tra cấu hình Server.");
            }
        }

        public static string MapSqlExceptionToVietnamese(SqlException ex, out string errorCode)
        {
            foreach (SqlError err in ex.Errors)
            {
                switch (err.Number)
                {
                    case 2627:
                    case 2601:
                        errorCode = "ERR_USERNAME_EXISTS";
                        return "Tên đăng nhập này đã có người sử dụng. Vui lòng chọn tên đăng nhập khác.";

                    case -2:
                    case 2:
                    case 53:
                    case 4060:
                    case 18456:
                    case 10054:
                    case 10060:
                    case 10061:
                        errorCode = "ERR_DB_UNAVAILABLE";
                        return "Không kết nối được cơ sở dữ liệu. Hãy thử lại sau.";

                    case 547:
                    case 8152:
                    case 2628:
                        errorCode = "ERR_INVALID_DATA";
                        return "Dữ liệu người dùng không hợp lệ hoặc vượt quá độ dài cho phép.";
                }
            }

            errorCode = "ERR_DB_ERROR";
            return "Hệ thống cơ sở dữ liệu đang bận hoặc gặp sự cố. Vui lòng thử lại sau.";
        }

        private static string LoadConnectionStringFromConfig()
        {
            string? envConn = Environment.GetEnvironmentVariable("NT106_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConn))
            {
                return envConn;
            }

            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                }

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connSection))
                    {
                        if (connSection.TryGetProperty("healthInforDB", out JsonElement connValue) ||
                            connSection.TryGetProperty("DefaultConnection", out connValue))
                        {
                            string? configured = connValue.GetString();
                            if (!string.IsNullOrWhiteSpace(configured))
                            {
                                return configured;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Dùng chuỗi kết nối mặc định nếu đọc file cấu hình gặp lỗi
            }

            return DefaultConnectionString;
        }
    }
}
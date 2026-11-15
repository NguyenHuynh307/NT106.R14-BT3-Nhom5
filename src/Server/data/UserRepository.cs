using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NT106.BT3.Server.Logging;
using NT106.BT3.Shared.Data;

namespace NT106.BT3.Server.Data
{
    /// <summary>
    /// Lớp truy cập dữ liệu bảng [dbo].[NguoiDung] đặt tại src/Server/Data/UserRepository.cs (Bước 5 của đề / Bước 4.1).
    /// - Cài đặt đúng interface IUserRepository trong NT106.BT3.Shared.Data.
    /// - 100% câu lệnh SQL dùng SqlParameter chống SQL Injection.
    /// - Bắt SqlException an toàn, ghi log qua ServerLog.Ghi và trả về thông báo tiếng Việt thân thiện.
    /// </summary>
    public sealed class UserRepository : IUserRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public UserRepository(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public async Task<RepositoryResult<bool>> ExistsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return RepositoryResult<bool>.Failure("ERR_INVALID_USERNAME", "Tên đăng nhập không được để trống.");
            }

            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM [dbo].[NguoiDung]
                    WHERE [Username] = @Username
                ) THEN 1 ELSE 0 END;";

            try
            {
                await using var conn = _dbHelper.CreateConnection();
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50)
                {
                    Value = username.Trim()
                });

                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                object? scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                bool exists = scalar is not null && Convert.ToInt32(scalar) == 1;

                return RepositoryResult<bool>.Success(exists);
            }
            catch (SqlException ex)
            {
                ServerLog.Ghi("LOI-DB", $"{nameof(ExistsByUsernameAsync)}: SqlErrorNumber={ex.Number}");
                string friendlyMsg = DatabaseHelper.MapSqlExceptionToVietnamese(ex, out string errorCode);
                return RepositoryResult<bool>.Failure(errorCode, friendlyMsg);
            }
        }

        public async Task<RepositoryResult<int>> CreateUserAsync(
            string username,
            string passwordHash,
            string passwordSalt,
            string? email,
            string fullName,
            DateTime? birthday,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(passwordHash) ||
                string.IsNullOrWhiteSpace(passwordSalt) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                return RepositoryResult<int>.Failure(
                    "ERR_INVALID_INPUT",
                    "Thông tin đăng ký bắt buộc không được để trống.");
            }

            const string sql = @"
                INSERT INTO [dbo].[NguoiDung] (
                    [Username],
                    [PasswordHash],
                    [PasswordSalt],
                    [Email],
                    [FullName],
                    [Birthday]
                )
                OUTPUT INSERTED.[UserId]
                VALUES (
                    @Username,
                    @PasswordHash,
                    @PasswordSalt,
                    @Email,
                    @FullName,
                    @Birthday
                );";

            try
            {
                await using var conn = _dbHelper.CreateConnection();
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50)
                {
                    Value = username.Trim()
                });

                cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 64)
                {
                    Value = passwordHash
                });

                cmd.Parameters.Add(new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 32)
                {
                    Value = passwordSalt
                });

                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100)
                {
                    Value = string.IsNullOrWhiteSpace(email) ? DBNull.Value : email.Trim()
                });

                cmd.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100)
                {
                    Value = fullName.Trim()
                });

                cmd.Parameters.Add(new SqlParameter("@Birthday", SqlDbType.Date)
                {
                    Value = birthday.HasValue ? birthday.Value.Date : DBNull.Value
                });

                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                object? insertedIdObj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                int newUserId = insertedIdObj is not null ? Convert.ToInt32(insertedIdObj) : 0;

                return RepositoryResult<int>.Success(newUserId, "Đăng ký tài khoản thành công!");
            }
            catch (SqlException ex)
            {
                ServerLog.Ghi("LOI-DB", $"{nameof(CreateUserAsync)}: SqlErrorNumber={ex.Number}");
                string friendlyMsg = DatabaseHelper.MapSqlExceptionToVietnamese(ex, out string errorCode);
                return RepositoryResult<int>.Failure(errorCode, friendlyMsg);
            }
        }

        public async Task<RepositoryResult<UserCredentialsDto?>> GetCredentialsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return RepositoryResult<UserCredentialsDto?>.Success(null);
            }

            const string sql = @"
                SELECT
                    [UserId],
                    [Username],
                    [PasswordHash],
                    [PasswordSalt]
                FROM [dbo].[NguoiDung]
                WHERE [Username] = @Username;";

            try
            {
                await using var conn = _dbHelper.CreateConnection();
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50)
                {
                    Value = username.Trim()
                });

                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);

                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return RepositoryResult<UserCredentialsDto?>.Success(null);
                }

                var credentials = new UserCredentialsDto
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    PasswordSalt = reader.GetString(3)
                };

                return RepositoryResult<UserCredentialsDto?>.Success(credentials);
            }
            catch (SqlException ex)
            {
                ServerLog.Ghi("LOI-DB", $"{nameof(GetCredentialsByUsernameAsync)}: SqlErrorNumber={ex.Number}");
                string friendlyMsg = DatabaseHelper.MapSqlExceptionToVietnamese(ex, out string errorCode);
                return RepositoryResult<UserCredentialsDto?>.Failure(errorCode, friendlyMsg);
            }
        }

        public async Task<RepositoryResult<UserProfileDto?>> GetUserProfileByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return RepositoryResult<UserProfileDto?>.Failure("ERR_INVALID_USERNAME", "Tên đăng nhập không hợp lệ.");
            }

            const string sql = @"
                SELECT
                    [UserId],
                    [Username],
                    [Email],
                    [FullName],
                    [Birthday]
                FROM [dbo].[NguoiDung]
                WHERE [Username] = @Username;";

            try
            {
                await using var conn = _dbHelper.CreateConnection();
                await using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50)
                {
                    Value = username.Trim()
                });

                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);

                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return RepositoryResult<UserProfileDto?>.Failure(
                        "ERR_USER_NOT_FOUND",
                        "Không tìm thấy thông tin người dùng.");
                }

                var profile = new UserProfileDto
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FullName = reader.GetString(3),
                    Birthday = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                };

                return RepositoryResult<UserProfileDto?>.Success(profile);
            }
            catch (SqlException ex)
            {
                ServerLog.Ghi("LOI-DB", $"{nameof(GetUserProfileByUsernameAsync)}: SqlErrorNumber={ex.Number}");
                string friendlyMsg = DatabaseHelper.MapSqlExceptionToVietnamese(ex, out string errorCode);
                return RepositoryResult<UserProfileDto?>.Failure(errorCode, friendlyMsg);
            }
        }
    }
}
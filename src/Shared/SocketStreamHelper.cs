namespace Shared
{
    public static class SocketStreamHelper
    {
        // Gửi 1 thông điệp UTF-8 kết thúc bằng '\n'.
        // Từ chối (không tự sửa) nội dung có CR/LF hoặc dài hơn giới hạn.
        public static async Task SendLineAsync(Stream stream, string payload, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(payload);

            if (payload.Contains('\r') || payload.Contains('\n'))
            {
                throw new ArgumentException("Nội dung thông điệp không được chứa ký tự xuống dòng (CR/LF).", nameof(payload));
            }

            byte[] body = ProtocolConstants.MessageEncoding.GetBytes(payload);
            if (body.Length > ProtocolConstants.MaxMessageBytes)
            {
                throw new ArgumentException($"Thông điệp vượt quá {ProtocolConstants.MaxMessageBytes} byte.", nameof(payload));
            }

            byte[] data = new byte[body.Length + 1];
            Buffer.BlockCopy(body, 0, data, 0, body.Length);
            data[body.Length] = (byte)'\n';

            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        // Đọc đúng 1 thông điệp kết thúc bằng '\n'.
        // - Trả null khi kết nối đóng sạch (chưa có byte nào của thông điệp mới).
        // - Ném EndOfStreamException (là IOException) khi kết nối đóng giữa chừng một thông điệp.
        // - Ném InvalidDataException khi thông điệp vượt giới hạn; phía gọi nên đóng kết nối vì luồng đã lệch khung.
        public static async Task<string?> ReceiveLineAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var buffer = new List<byte>();
            byte[] singleByte = new byte[1];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(singleByte, cancellationToken);
                if (bytesRead == 0)
                {
                    if (buffer.Count == 0)
                    {
                        return null;
                    }
                    throw new EndOfStreamException("Kết nối đóng trước khi nhận đủ thông điệp.");
                }

                byte b = singleByte[0];
                if (b == (byte)'\n')
                {
                    break;
                }

                if (buffer.Count >= ProtocolConstants.MaxMessageBytes)
                {
                    throw new InvalidDataException($"Thông điệp vượt quá {ProtocolConstants.MaxMessageBytes} byte.");
                }
                buffer.Add(b);
            }

            // Chấp nhận kết thúc CRLF (ví dụ telnet): bỏ đúng 1 CR ở cuối, không đụng CR ở giữa.
            if (buffer.Count > 0 && buffer[^1] == (byte)'\r')
            {
                buffer.RemoveAt(buffer.Count - 1);
            }

            return ProtocolConstants.MessageEncoding.GetString(buffer.ToArray());
        }
    }
}
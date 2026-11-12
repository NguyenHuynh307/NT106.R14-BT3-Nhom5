using System.Net.Sockets;

namespace Shared
{
    public static class SocketStreamHelper
    {
        // Gửi 1 dòng thông điệp UTF-8 kết thúc bằng '\n'
        public static async Task SendLineAsync(NetworkStream stream, string payload, CancellationToken cancellationToken = default)
        {
            string cleanPayload = payload.Replace("\r", "").Replace("\n", " ");
            byte[] data = ProtocolConstants.MessageEncoding.GetBytes(cleanPayload + ProtocolConstants.Delimiter);
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        // Đọc đúng 1 thông điệp kết thúc bằng '\n' (tránh dính gói TCP)
        public static async Task<string?> ReceiveLineAsync(NetworkStream stream, CancellationToken cancellationToken = default)
        {
            var buffer = new List<byte>();
            byte[] singleByte = new byte[1];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(singleByte, cancellationToken);
                if (bytesRead == 0)
                {
                    // Kết nối đã đóng
                    return buffer.Count > 0 ? ProtocolConstants.MessageEncoding.GetString(buffer.ToArray()) : null;
                }

                char c = (char)singleByte[0];
                if (c == '\n')
                {
                    break;
                }
                if (c != '\r')
                {
                    buffer.Add(singleByte[0]);
                }
            }

            return ProtocolConstants.MessageEncoding.GetString(buffer.ToArray());
        }
    }
}   
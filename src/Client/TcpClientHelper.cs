using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace NT106.BT3.Client
{
    public class TcpClientHelper
    {
        // Singleton: giúp FormDangNhap, FormDangKi và FormTrangChinh dùng chung 1 kết nối duy nhất
        private static TcpClientHelper? _instance;
        public static TcpClientHelper Instance => _instance ??= new TcpClientHelper();

        // Thời hạn chờ nối tới Server. Quá hạn thì ném SocketException(TimedOut) như mọi lần nối thất bại khác.
        private const int ThoiHanNoiMs = 5000;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private int _disconnectedFlag = 1; // 1 = đang ngắt, 0 = đang kết nối

        // Sự kiện để giao diện WinForms đăng ký nhận dữ liệu và trạng thái mất kết nối.
        // LƯU Ý: cả hai sự kiện có thể được gọi từ luồng nền, Form phải tự chuyển về luồng giao diện (Invoke/BeginInvoke).
        public event Action<string>? OnMessageReceived;
        public event Action? OnDisconnected;

        public bool IsConnected => _client?.Connected == true && Volatile.Read(ref _disconnectedFlag) == 0;

        private TcpClientHelper() { }

        /// <summary>
        /// Kết nối bất đồng bộ tới Server và tự động bật luồng lắng nghe ngầm.
        /// Nối thất bại thì ném SocketException (kể cả hết thời hạn chờ); Form bắt và hiện thông báo thân thiện.
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            if (IsConnected) return;

            var client = new TcpClient();
            bool noiXong = false;
            try
            {
                using var hetHan = new CancellationTokenSource(ThoiHanNoiMs);
                try
                {
                    await client.ConnectAsync(ip, port, hetHan.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new SocketException((int)SocketError.TimedOut);
                }
                noiXong = true;
            }
            finally
            {
                if (!noiXong) client.Dispose();   // nối lỗi thì không để lại socket dở dang
            }

            var stream = client.GetStream();
            var cts = new CancellationTokenSource();

            _client = client;
            _stream = stream;
            _cts = cts;
            Interlocked.Exchange(ref _disconnectedFlag, 0);

            // Chạy vòng lặp nhận tin nhắn ngầm (không làm đơ giao diện)
            _ = StartReceivingAsync(stream, cts.Token);
        }

        /// <summary>
        /// Vòng lặp đọc từng gói tin kết thúc bằng '\n' từ Server.
        /// Nhận stream làm tham số để không đọc lại trường _stream (Disconnect có thể gán null giữa chừng).
        /// </summary>
        private async Task StartReceivingAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string? message = await SocketStreamHelper.ReceiveLineAsync(stream, token);
                    if (message == null)
                    {
                        // Server đã chủ động đóng kết nối
                        break;
                    }

                    OnMessageReceived?.Invoke(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Chủ động ngắt kết nối từ phía Client
            }
            catch (IOException)
            {
                // Mất mạng đột ngột, Server sập, hoặc Server đóng giữa chừng một thông điệp (EndOfStreamException)
            }
            catch (SocketException)
            {
                // Lỗi socket ở tầng thấp
            }
            catch (ObjectDisposedException)
            {
                // Luồng đã bị Disconnect đóng
            }
            catch (InvalidDataException)
            {
                // Server gửi thông điệp vượt giới hạn: luồng đã lệch khung nên phải ngắt
            }
            finally
            {
                // Chỉ ngắt khi vòng lặp này vẫn thuộc kết nối hiện tại; tránh kéo sập kết nối mới sau khi nối lại
                if (ReferenceEquals(_stream, stream))
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Gửi 1 thông điệp chuẩn UTF-8 kèm '\n' tới Server (an toàn đa luồng).
        /// Chưa nối hoặc mất kết nối thì ném InvalidOperationException với thông báo thân thiện.
        /// Nội dung chứa CR/LF hoặc dài quá giới hạn thì SocketStreamHelper ném ArgumentException:
        /// Form phải chặn trước bằng InputValidator (không cho nhập '|', CR, LF).
        /// </summary>
        public async Task SendAsync(string payload)
        {
            NetworkStream? stream = _stream;
            if (!IsConnected || stream == null)
                throw new InvalidOperationException("Chưa kết nối đến Server.");

            await _sendLock.WaitAsync();
            try
            {
                await SocketStreamHelper.SendLineAsync(stream, payload);
            }
            catch (IOException)
            {
                Disconnect();
                throw new InvalidOperationException("Mất kết nối tới Server.");
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
                throw new InvalidOperationException("Mất kết nối tới Server.");
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Đóng kết nối và giải phóng tài nguyên an toàn.
        /// </summary>
        public void Disconnect()
        {
            // Đảm bảo sự kiện OnDisconnected chỉ bắn ra đúng 1 lần cho mỗi phiên kết nối
            if (Interlocked.Exchange(ref _disconnectedFlag, 1) == 1)
                return;

            var cts = _cts;
            var stream = _stream;
            var client = _client;

            DongAnToan(() => cts?.Cancel());
            DongAnToan(() => stream?.Close());
            DongAnToan(() => client?.Close());
            cts?.Dispose();

            _cts = null;
            _stream = null;
            _client = null;

            OnDisconnected?.Invoke();
        }

        // Dọn tài nguyên: lỗi khi đóng (đã đóng rồi, socket chết) không ảnh hưởng và không được văng ra ngoài.
        private static void DongAnToan(Action dong)
        {
            try
            {
                dong();
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
            }
        }
    }
}

using System;
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

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private int _disconnectedFlag = 1; // 1 = đang ngắt, 0 = đang kết nối

        // Sự kiện để giao diện WinForms đăng ký nhận dữ liệu và trạng thái mất kết nối
        public event Action<string>? OnMessageReceived;
        public event Action? OnDisconnected;

        public bool IsConnected => _client?.Connected == true && Volatile.Read(ref _disconnectedFlag) == 0;

        private TcpClientHelper() { }

        /// <summary>
        /// Kết nối bất đồng bộ tới Server và tự động bật luồng lắng nghe ngầm.
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            if (IsConnected) return;

            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            _cts = new CancellationTokenSource();
            Interlocked.Exchange(ref _disconnectedFlag, 0);

            // Chạy vòng lặp nhận tin nhắn ngầm (không làm đơ giao diện)
            _ = StartReceivingAsync(_cts.Token);
        }

        /// <summary>
        /// Vòng lặp đọc từng gói tin kết thúc bằng '\n' từ Server.
        /// </summary>
        private async Task StartReceivingAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _stream != null)
                {
                    string? message = await SocketStreamHelper.ReceiveLineAsync(_stream, token);
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
            catch (Exception)
            {
                // Mất mạng đột ngột hoặc Server sập
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Gửi 1 thông điệp chuẩn UTF-8 kèm '\n' tới Server (an toàn đa luồng).
        /// </summary>
        public async Task SendAsync(string payload)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Chưa kết nối đến Server.");

            await _sendLock.WaitAsync();
            try
            {
                await SocketStreamHelper.SendLineAsync(_stream, payload);
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

            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            _cts?.Dispose();
            _cts = null;
            _stream = null;
            _client = null;

            OnDisconnected?.Invoke();
        }
    }
}
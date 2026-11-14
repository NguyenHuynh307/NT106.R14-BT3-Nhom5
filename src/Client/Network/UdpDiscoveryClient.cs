using System.Net;
using System.Net.Sockets;
using NT106.BT3.Server.Logging;
using Shared;

namespace NT106.BT3.Server.Network;

/// <summary>
/// Dịch vụ lắng nghe gói UDP Broadcast từ Client trong mạng LAN (TC10)
/// và phản hồi địa chỉ IP cùng cổng TCP của Server.
/// </summary>
internal sealed class UdpDiscoveryServer
{
    private readonly int _tcpPort;
    private readonly int _udpPort;

    public UdpDiscoveryServer(int tcpPort, int udpPort = ProtocolConstants.UdpDiscoveryPort)
    {
        _tcpPort = tcpPort;
        _udpPort = udpPort;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var udpListener = new UdpClient();
        try
        {
            udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
            TatLoiConnReset(udpListener.Client);
        }
        catch (SocketException ex)
        {
            ServerLog.Ghi("LOI", $"Không mở được cổng UDP Discovery {_udpPort} ({ex.SocketErrorCode}). Server TCP vẫn hoạt động bình thường.");
            return;
        }

        ServerLog.Ghi("UDP", $"Đang lắng nghe dò tìm LAN trên cổng UDP {_udpPort} (cổng TCP: {_tcpPort}).");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await udpListener.ReceiveAsync(cancellationToken);
                string requestText = ProtocolConstants.MessageEncoding.GetString(result.Buffer).Trim();

                if (!string.Equals(requestText, ProtocolConstants.UdpDiscoveryRequest, StringComparison.Ordinal))
                {
                    continue;
                }

                string? diaChiServer = LayDiaChiCucBo(result.RemoteEndPoint);
                if (diaChiServer == null)
                {
                    ServerLog.Ghi("LOI", $"Không xác định được địa chỉ để trả lời {result.RemoteEndPoint}.");
                    continue;
                }

                string responseText = $"{ProtocolConstants.UdpDiscoveryResponsePrefix}{ProtocolConstants.FieldSeparator}{diaChiServer}{ProtocolConstants.FieldSeparator}{_tcpPort}";
                byte[] responseBytes = ProtocolConstants.MessageEncoding.GetBytes(responseText);

                try
                {
                    await udpListener.SendAsync(responseBytes, result.RemoteEndPoint, cancellationToken);
                    ServerLog.Ghi("UDP", $"Đã phản hồi dò tìm tới {result.RemoteEndPoint} -> {responseText}");
                }
                catch (SocketException ex)
                {
                    // Một Client lỗi không được làm dừng dịch vụ dò tìm
                    ServerLog.Ghi("LOI", $"Không gửi được phản hồi UDP tới {result.RemoteEndPoint} ({ex.SocketErrorCode}).");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Dừng tử tế khi Server nhận lệnh tắt
        }
        catch (SocketException ex)
        {
            ServerLog.Ghi("LOI", $"Lỗi mạng UDP Discovery ({ex.SocketErrorCode}): {ex.Message}");
        }
        finally
        {
            ServerLog.Ghi("UDP", "Đã dừng dịch vụ dò tìm LAN.");
        }
    }

    /// <summary>
    /// Địa chỉ IP của Server mà Client có thể nối tới (card mạng dùng để đi tới Client).
    /// Kết nối UDP chỉ chọn card mạng, không gửi gói nào.
    /// </summary>
    private static string? LayDiaChiCucBo(IPEndPoint khach)
    {
        try
        {
            using var socket = new Socket(khach.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(khach);
            return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch (SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// Windows: gói ICMP "port unreachable" làm lần ReceiveAsync kế tiếp ném ConnectionReset.
    /// Tắt hành vi này để dịch vụ UDP không tự dừng.
    /// </summary>
    private static void TatLoiConnReset(Socket socket)
    {
        if (OperatingSystem.IsWindows())
        {
            const int SioUdpConnReset = unchecked((int)0x9800000C);
            socket.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
        }
    }
}

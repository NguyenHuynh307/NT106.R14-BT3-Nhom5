using System.Net;
using System.Net.Sockets;
using NT106.BT3.Server.Logging;
using Shared;

namespace NT106.BT3.Server.Network;

/// <summary>
/// Dịch vụ lắng nghe gói UDP Broadcast từ Client trong mạng LAN (TC10)
/// và phản hồi lại cổng TCP mà Server đang phục vụ.
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
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
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

                string responseText = $"{ProtocolConstants.UdpDiscoveryResponsePrefix}{ProtocolConstants.FieldSeparator}{_tcpPort}";
                byte[] responseBytes = ProtocolConstants.MessageEncoding.GetBytes(responseText);

                await udpListener.SendAsync(responseBytes, result.RemoteEndPoint, cancellationToken);
                ServerLog.Ghi("UDP", $"Đã phản hồi dò tìm tới {result.RemoteEndPoint} -> {responseText}");
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
}
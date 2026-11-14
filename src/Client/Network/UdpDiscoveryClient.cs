using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Shared;

namespace NT106.BT3.Client.Network;

/// <summary>
/// Thông tin địa chỉ IP và cổng TCP của Server được tìm thấy trong mạng LAN.
/// </summary>
public sealed record DiscoveredServerInfo(string IpAddress, int TcpPort);

/// <summary>
/// Bộ dò tìm Server tự động qua gói tin UDP Broadcast trong mạng LAN (TC10).
/// </summary>
public static class UdpDiscoveryClient
{
    /// <summary>
    /// Phát gói UDP quảng bá để tìm Server đang chạy trong mạng nội bộ.
    /// Trả về thông tin (IpAddress, TcpPort) nếu tìm thấy, hoặc null nếu hết thời gian chờ.
    /// </summary>
    public static async Task<DiscoveredServerInfo?> DiscoverServerAsync(
        int udpPort = ProtocolConstants.UdpDiscoveryPort,
        int timeoutMs = 2000,
        CancellationToken cancellationToken = default)
    {
        byte[] requestBytes = ProtocolConstants.MessageEncoding.GetBytes(ProtocolConstants.UdpDiscoveryRequest);
        List<IPEndPoint> broadcastTargets = GetLanBroadcastEndpoints(udpPort);

        using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        udpClient.EnableBroadcast = true;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        // Gửi gói dò tìm 2 nhịp (lúc bắt đầu và sau 700ms nếu chưa có phản hồi) để tránh rớt gói UDP
        _ = SendProbesAsync(udpClient, requestBytes, broadcastTargets, timeoutCts.Token);

        try
        {
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(timeoutCts.Token);
                string responseText = ProtocolConstants.MessageEncoding.GetString(result.Buffer).Trim();

                string[] parts = responseText.Split(ProtocolConstants.FieldSeparator);
                if (parts.Length == 2
                    && string.Equals(parts[0], ProtocolConstants.UdpDiscoveryResponsePrefix, StringComparison.Ordinal)
                    && int.TryParse(parts[1], out int tcpPort)
                    && tcpPort >= 1024 && tcpPort <= 49151)
                {
                    IPAddress senderIp = result.RemoteEndPoint.Address.IsIPv4MappedToIPv6
                        ? result.RemoteEndPoint.Address.MapToIPv4()
                        : result.RemoteEndPoint.Address;

                    return new DiscoveredServerInfo(senderIp.ToString(), tcpPort);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Hết thời gian chờ (timeout) hoặc người dùng hủy thao tác -> trả về null để nhập tay
        }
        catch (SocketException)
        {
            // Lỗi mạng cục bộ (ví dụ chưa bật Wi-Fi/LAN) -> trả về null an toàn, không lộ ngoại lệ
        }

        return null;
    }

    private static async Task SendProbesAsync(
        UdpClient udpClient,
        byte[] requestBytes,
        List<IPEndPoint> targets,
        CancellationToken token)
    {
        for (int attempt = 0; attempt < 2 && !token.IsCancellationRequested; attempt++)
        {
            foreach (IPEndPoint target in targets)
            {
                try
                {
                    await udpClient.SendAsync(requestBytes, target, token);
                }
                catch (SocketException)
                {
                    // Bỏ qua card mạng không gửi được broadcast, tiếp tục thử card mạng khác
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            try
            {
                await Task.Delay(700, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Lấy danh sách địa chỉ Broadcast (255.255.255.255 + Broadcast của các card mạng LAN đang hoạt động)
    /// để tránh gói tin đi nhầm sang card mạng ảo (VMware/VirtualBox/WSL).
    /// </summary>
    private static List<IPEndPoint> GetLanBroadcastEndpoints(int udpPort)
    {
        var endpoints = new List<IPEndPoint>
        {
            new(IPAddress.Broadcast, udpPort)
        };

        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation unicast in ni.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork || unicast.IPv4Mask == null)
                    {
                        continue;
                    }

                    byte[] ipBytes = unicast.Address.GetAddressBytes();
                    byte[] maskBytes = unicast.IPv4Mask.GetAddressBytes();
                    byte[] broadcastBytes = new byte[4];

                    for (int i = 0; i < 4; i++)
                    {
                        broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }

                    var subnetBroadcast = new IPAddress(broadcastBytes);
                    if (!endpoints.Any(ep => ep.Address.Equals(subnetBroadcast)))
                    {
                        endpoints.Add(new IPEndPoint(subnetBroadcast, udpPort));
                    }
                }
            }
        }
        catch (NetworkInformationException)
        {
            // Nếu không đọc được danh sách card mạng thì vẫn dùng 255.255.255.255 mặc định
        }

        return endpoints;
    }
}
public static async Task<DiscoveredServerInfo?> DiscoverServerAsync(
    int udpPort = ProtocolConstants.UdpDiscoveryPort,
    int timeoutMs = 2000,
    CancellationToken cancellationToken = default)
{
    byte[] requestBytes = ProtocolConstants.MessageEncoding.GetBytes(ProtocolConstants.UdpDiscoveryRequest);
    List<IPEndPoint> broadcastTargets = GetLanBroadcastEndpoints(udpPort);

    using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
    udpClient.EnableBroadcast = true;
    TatLoiConnReset(udpClient.Client);

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(timeoutMs);

    // Gửi gói dò tìm 2 nhịp; giữ lại Task để chờ nó kết thúc trước khi đóng socket
    Task probeTask = SendProbesAsync(udpClient, requestBytes, broadcastTargets, timeoutCts.Token);

    try
    {
        while (!timeoutCts.Token.IsCancellationRequested)
        {
            UdpReceiveResult result = await udpClient.ReceiveAsync(timeoutCts.Token);
            string responseText = ProtocolConstants.MessageEncoding.GetString(result.Buffer).Trim();

            // Định dạng: NT106_SERVER|<địa chỉ IP>|<cổng TCP>
            string[] parts = responseText.Split(ProtocolConstants.FieldSeparator);
            if (parts.Length == 3
                && string.Equals(parts[0], ProtocolConstants.UdpDiscoveryResponsePrefix, StringComparison.Ordinal)
                && IPAddress.TryParse(parts[1], out IPAddress? serverIp)
                && serverIp.AddressFamily == AddressFamily.InterNetwork
                && int.TryParse(parts[2], out int tcpPort)
                && tcpPort >= 1024 && tcpPort <= 49151)
            {
                return new DiscoveredServerInfo(serverIp.ToString(), tcpPort);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Hết thời gian chờ hoặc người dùng hủy -> trả về null để nhập tay
    }
    catch (SocketException)
    {
        // Lỗi mạng cục bộ -> trả về null an toàn, không lộ ngoại lệ
    }
    finally
    {
        timeoutCts.Cancel();
        await probeTask;
    }

    return null;
}

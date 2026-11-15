using System.Net;
using System.Net.Mail;
using Shared;

namespace NT106.BT3.Client;

internal static class InputValidator
{
    public static bool ChuaKyTuCam(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.Contains(ProtocolConstants.FieldSeparator)
            || value.Contains('\r')
            || value.Contains('\n');
    }

    public static bool KiemTraIp(string? text, out string ip, out string error)
    {
        ip = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            error = "Vui lòng nhập IP Server.";
            return false;
        }

        if (!IPAddress.TryParse(ip, out _))
        {
            error = "Địa chỉ IP không đúng định dạng (VD: 127.0.0.1).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool KiemTraPort(string? text, out int port, out string error)
    {
        string raw = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            port = 0;
            error = "Vui lòng nhập Port.";
            return false;
        }

        if (!int.TryParse(raw, out port) || port < 1024 || port > 49151)
        {
            error = "Port phải là số từ 1024 đến 49151.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool KiemTraEmailHopLe(string? email)
    {
        string trimmed = (email ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return true;
        }

        if (ChuaKyTuCam(trimmed))
        {
            return false;
        }

        return MailAddress.TryCreate(trimmed, out MailAddress? addr)
            && string.Equals(addr.Address, trimmed, StringComparison.OrdinalIgnoreCase)
            && addr.Host.Contains('.');
    }

    public static async Task<ResponseMessage> GuiVaChoPhanHoiAsync(string payload, int timeoutMs = 10000)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnMessage(string msg) => tcs.TrySetResult(msg);
        void OnDisconnected() => tcs.TrySetException(new IOException("Mất kết nối tới Server."));

        TcpClientHelper.Instance.OnMessageReceived += OnMessage;
        TcpClientHelper.Instance.OnDisconnected += OnDisconnected;

        try
        {
            await TcpClientHelper.Instance.SendAsync(payload);

            using var cts = new CancellationTokenSource(timeoutMs);
            using CancellationTokenRegistration reg = cts.Token.Register(
                () => tcs.TrySetException(new TimeoutException("Server không phản hồi.")));

            string rawResponse = await tcs.Task;
            return ResponseMessage.Parse(rawResponse);
        }
        finally
        {
            TcpClientHelper.Instance.OnMessageReceived -= OnMessage;
            TcpClientHelper.Instance.OnDisconnected -= OnDisconnected;
        }
    }
}
namespace Shared
{
    public class RequestMessage
    {
        public string Command { get; set; } = string.Empty;
        public string[] Args { get; set; } = Array.Empty<string>();

        public RequestMessage() { }

        public RequestMessage(string command, params string[] args)
        {
            Command = command.Trim().ToUpperInvariant();
            Args = args ?? Array.Empty<string>();
        }

        // Chuyển thành chuỗi gửi đi: COMMAND|arg1|arg2...
        public string Serialize()
        {
            if (Args.Length == 0)
                return Command;

            return $"{Command}{ProtocolConstants.FieldSeparator}{string.Join(ProtocolConstants.FieldSeparator, Args)}";
        }

        // Phân tích chuỗi nhận được từ Client
        public static RequestMessage Parse(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                return new RequestMessage();

            string cleaned = rawLine.TrimEnd('\r', '\n');
            string[] parts = cleaned.Split(ProtocolConstants.FieldSeparator);

            return new RequestMessage
            {
                Command = parts[0].Trim().ToUpperInvariant(),
                Args = parts.Length > 1 ? parts[1..] : Array.Empty<string>()
            };
        }
    }

    public class ResponseMessage
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] Data { get; set; } = Array.Empty<string>();

        public bool IsSuccess => StatusCode == ProtocolConstants.CodeSuccess;

        public ResponseMessage() { }

        public ResponseMessage(int statusCode, string message, params string[] data)
        {
            StatusCode = statusCode;
            Message = message ?? string.Empty;
            Data = data ?? Array.Empty<string>();
        }

        // Chuyển thành chuỗi phản hồi: STATUS_CODE|MESSAGE|data1|data2...
        public string Serialize()
        {
            string header = $"{StatusCode}{ProtocolConstants.FieldSeparator}{Message}";
            if (Data.Length == 0)
                return header;

            return $"{header}{ProtocolConstants.FieldSeparator}{string.Join(ProtocolConstants.FieldSeparator, Data)}";
        }

        // Phân tích chuỗi phản hồi từ Server
        public static ResponseMessage Parse(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                return new ResponseMessage(ProtocolConstants.CodeInvalidFormat, "Gói tin rỗng");

            string cleaned = rawLine.TrimEnd('\r', '\n');
            string[] parts = cleaned.Split(ProtocolConstants.FieldSeparator);

            if (!int.TryParse(parts[0], out int code))
            {
                return new ResponseMessage(ProtocolConstants.CodeInvalidFormat, "Mã trạng thái không hợp lệ");
            }

            string msg = parts.Length > 1 ? parts[1] : string.Empty;
            string[] extraData = parts.Length > 2 ? parts[2..] : Array.Empty<string>();

            return new ResponseMessage(code, msg, extraData);
        }
    }
}
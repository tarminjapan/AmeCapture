namespace AmeCapture.Application.Messages
{
    public class ExitRequestedMessage
    {
    }

    public class CaptureRequestedMessage(string captureType)
    {
        public string CaptureType { get; } = captureType;
    }
}

namespace AmeCapture.App.Messages;

public class CaptureRequestedMessage(string captureType)
{
    public string CaptureType { get; } = captureType;
}

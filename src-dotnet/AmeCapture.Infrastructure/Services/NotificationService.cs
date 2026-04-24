using System.Security;
using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

public class NotificationService : INotificationService
{
    public Task ShowNotificationAsync(string title, string message, Action? onClick = null)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var escapedTitle = SecurityElement.Escape(title) ?? title;
                var escapedMessage = SecurityElement.Escape(message) ?? message;
                
                var toastXml = System.Xml.Linq.XElement.Parse($@"
<toast>
    <visual>
        <binding template='ToastText02'>
            <text id='1'>{escapedTitle}</text>
            <text id='2'>{escapedMessage}</text>
        </binding>
    </visual>
</toast>");

                var xml = new Windows.Data.Xml.Dom.XmlDocument();
                xml.LoadXml(toastXml.ToString());
                
                var toastNotification = new Windows.UI.Notifications.ToastNotification(xml);
                
                if (onClick != null)
                {
                    toastNotification.Activated += (s, e) => onClick();
                }
                
                Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to show notification");
            }
        }

        return Task.CompletedTask;
    }
}

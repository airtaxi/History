using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Firebase.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public class NotificationMessagingService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        var notification = message.GetNotification();
        var notificationId = Guid.NewGuid().GetHashCode();
        var pendingIntent = BuildIntent(notificationId, message.Data);

        var builtNotification = BuildNotification(pendingIntent, notification);
        CreateNotificationChannel();
        SendNotification(notificationId, builtNotification);

        UpdatePostIfExists(message.Data);
    }

    private PendingIntent BuildIntent(int notificationId, IDictionary<string, string> data)
    {
        using var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop);

        foreach (var entry in data)
        {
            intent.PutExtra(entry.Key, entry.Value);
        }

        return PendingIntent.GetActivity(
            this,
            notificationId,
            intent,
            PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
    }

    private Notification BuildNotification(PendingIntent intent, RemoteMessage.Notification notification)
    {
        var builder = new NotificationCompat.Builder(this, $"{PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(notification.Title)
            .SetContentText(notification.Body)
            .SetContentIntent(intent)
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true);

        if (notification.ImageUrl != null)
        {
            var url = new Java.Net.URL(notification.ImageUrl.ToString());
            var image = BitmapFactory.DecodeStream(url.OpenConnection().InputStream);

            builder = builder
            .SetLargeIcon(image)
            .SetStyle(new NotificationCompat.BigPictureStyle()
                .BigPicture(image)
                .SetSummaryText(notification.Body));
        }

        return builder.Build();
    }

    private void CreateNotificationChannel()
    {
        var channel = new NotificationChannel($"{PackageName}.push", "푸시 알림", NotificationImportance.Default);
        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        notificationManager?.CreateNotificationChannel(channel);
    }

    private void SendNotification(int notificationId, Notification notification)
    {
        var notificationManager = NotificationManagerCompat.From(this);
        notificationManager.Notify(notificationId, notification);
    }

    private async void UpdatePostIfExists(IDictionary<string, string> data)
    {
        if (!data.TryGetValue("PostId", out var postId)) return;
        else if (Shared.ApiHandler == null) return;

        try
        {
            var post = await Shared.ApiHandler.ExecuteRequestAsync(new GetPost(postId));
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));
        }
        catch { }
    }
}

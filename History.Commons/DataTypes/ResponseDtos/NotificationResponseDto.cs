using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class NotificationResponseDto()
{
    public string Id { get; set; }

    public NotificationType Type { get; set; }

    public UserResponseDto User { get; set; }

    public string Title { get; set; }
    public string Body { get; set; }

    public string ImageUrl { get; set; }

    public Dictionary<string, string> Data { get; set; }

    public DateTime CreatedAt { get; set; }

    public NotificationResponseDto(Notification notification) : this()
    {
        Id = notification.Id;

        Type = notification.Type;

        Title = notification.Title;
        Body = notification.Body;
        ImageUrl = notification.ImageUrl;

        Data = notification.Data;

        CreatedAt = notification.CreatedAt;
    }
}

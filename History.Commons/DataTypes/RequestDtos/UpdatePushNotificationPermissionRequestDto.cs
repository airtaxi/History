using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos
{
    public class UpdatePushNotificationPermissionRequestDto
    {
        public PushNotificationType Type { get; set; }
        public AccessPermission AccessPermission { get; set; }
    }
}
